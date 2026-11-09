using Newtonsoft.Json;
using OpenRPA.Input;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OpenRPA.FileWatcher
{
    public class FileWatcherDetectorPlugin : ObservableObject, IDetectorPlugin
    {
        public IDetector Entity { get; set; }
        public string Name
        {
            get
            {
                if (Entity != null && !string.IsNullOrEmpty(Entity.name)) return Entity.name;
                return "FileWatcher";
            }
        }
        private Views.FileWatcherView view;
        public UserControl editor
        {
            get
            {
                if (view == null)
                {
                    view = new Views.FileWatcherView(this);
                    view.PropertyChanged += (s, e) =>
                    {
                        NotifyPropertyChanged("Entity");
                        NotifyPropertyChanged("Name");
                    };
                }
                return view;
            }
        }
        public event DetectorDelegate OnDetector;

        // Network shares cap the change buffer at 64 KB; the 8 KB default overflows on bursts of files.
        // The intervals below are internal and writable only so tests can shorten them.
        private const int WatcherBufferSize = 64 * 1024;
        // How often the health check runs: verifies the folder is reachable and rebuilds a dead watcher.
        internal static TimeSpan HealthCheckInterval = TimeSpan.FromSeconds(15);
        // How soon to try rebuilding after the watcher reports an error.
        internal static TimeSpan RetryDelay = TimeSpan.FromSeconds(5);
        // Watchers on network folders can stop delivering events without raising Error
        // (for example when the SMB session is reset server side), so they are rebuilt on this interval.
        internal static TimeSpan NetworkRefreshInterval = TimeSpan.FromMinutes(10);
        // Events for the same file closer together than this are collapsed into one trigger.
        private static readonly TimeSpan DuplicateWindow = TimeSpan.FromMilliseconds(100);

        private readonly object sync = new object();
        private FileSystemWatcher watcher = null;
        private System.Threading.Timer healthTimer = null;
        private BlockingCollection<FileSystemEventArgs> queue = null;
        private bool running = false;
        private bool watcherHealthy = false;
        private bool pathReported = false;
        private DateTime watcherCreated = DateTime.MinValue;
        private int healthCheckBusy = 0;
        private readonly Dictionary<string, DateTime> lastTriggered = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        public void Initialize(IOpenRPAClient client, IDetector InEntity)
        {
            Entity = InEntity;
            Start();
        }
        public string Watchpath
        {
            get
            {
                if (Entity == null) return null;
                if (!Entity.Properties.ContainsKey("Watchpath")) return null;
                var _val = Entity.Properties["Watchpath"];
                if (_val == null) return null;
                return _val.ToString();
            }
            set
            {
                Entity.Properties["Watchpath"] = value;
            }
        }
        public string WatchFilter
        {
            get
            {
                if (Entity == null) return null;
                if (!Entity.Properties.ContainsKey("WatchFilter")) return null;
                var _val = Entity.Properties["WatchFilter"];
                if (_val == null) return null;
                return _val.ToString();
            }
            set
            {
                if (Entity == null) return;
                Entity.Properties["WatchFilter"] = value;
            }
        }
        public bool IncludeSubdirectories
        {
            get
            {
                if (Entity == null) return false;
                if (!Entity.Properties.ContainsKey("IncludeSubdirectories")) return false;
                var _val = Entity.Properties["IncludeSubdirectories"];
                if (_val == null) return false;
                return bool.Parse(_val.ToString());
            }
            set
            {
                if (Entity == null) return;
                Entity.Properties["IncludeSubdirectories"] = value;
            }
        }
        public bool raiseOnChanged
        {
            get
            {
                if (Entity == null) return true;
                if (!Entity.Properties.ContainsKey("raiseOnChanged")) return true;
                var _val = Entity.Properties["raiseOnChanged"];
                if (_val == null) return true;
                return bool.Parse(_val.ToString());
            }
            set
            {
                if (Entity == null) return;
                Entity.Properties["raiseOnChanged"] = value;
            }
        }
        public bool raiseOnCreated
        {
            get
            {
                if (Entity == null) return false;
                if (!Entity.Properties.ContainsKey("raiseOnCreated")) return false;
                var _val = Entity.Properties["raiseOnCreated"];
                if (_val == null) return false;
                return bool.Parse(_val.ToString());
            }
            set
            {
                if (Entity == null) return;
                Entity.Properties["raiseOnCreated"] = value;
            }
        }
        public bool raiseOnDeleted
        {
            get
            {
                if (Entity == null) return false;
                if (!Entity.Properties.ContainsKey("raiseOnDeleted")) return false;
                var _val = Entity.Properties["raiseOnDeleted"];
                if (_val == null) return false;
                return bool.Parse(_val.ToString());
            }
            set
            {
                if (Entity == null) return;
                Entity.Properties["raiseOnDeleted"] = value;
            }
        }
        public bool raiseOnRenamed
        {
            get
            {
                if (Entity == null) return false;
                if (!Entity.Properties.ContainsKey("raiseOnRenamed")) return false;
                var _val = Entity.Properties["raiseOnRenamed"];
                if (_val == null) return false;
                return bool.Parse(_val.ToString());
            }
            set
            {
                if (Entity == null) return;
                Entity.Properties["raiseOnRenamed"] = value;
            }
        }

        public void Start()
        {
            lock (sync)
            {
                // Detector.Start calls this again right after Initialize, so it must be idempotent.
                if (running) return;
                running = true;
                lastTriggered.Clear();

                queue = new BlockingCollection<FileSystemEventArgs>();
                var dispatcher = new System.Threading.Thread(DispatchLoop)
                {
                    IsBackground = true,
                    Name = "FileWatcher dispatch " + Name
                };
                dispatcher.Start(queue);

                TryCreateWatcher();
                healthTimer = new System.Threading.Timer(HealthCheck, null, HealthCheckInterval, HealthCheckInterval);
            }
        }

        public void Stop()
        {
            lock (sync)
            {
                if (!running) return;
                running = false;
                healthTimer?.Dispose();
                healthTimer = null;
                DisposeWatcher(watcher);
                watcher = null;
                watcherHealthy = false;
                pathReported = false;
                queue?.CompleteAdding();
                queue = null;
            }
        }

        /// <summary>
        /// Builds a new FileSystemWatcher for the configured folder. The new watcher is started before the
        /// old one is disposed, so a refresh leaves no gap; any duplicate events are collapsed in Dispatch.
        /// Must be called while holding <see cref="sync"/>.
        /// </summary>
        private bool TryCreateWatcher()
        {
            var path = Watchpath;
            if (string.IsNullOrEmpty(path))
            {
                ReportPathProblem("FileWatcher " + Name + ": no folder configured");
                return false;
            }
            if (!Directory.Exists(path))
            {
                ReportPathProblem("FileWatcher " + Name + ": folder " + path + " is not reachable, will keep retrying every " + HealthCheckInterval.TotalSeconds + " seconds");
                DisposeWatcher(watcher);
                watcher = null;
                watcherHealthy = false;
                return false;
            }

            FileSystemWatcher created = null;
            try
            {
                created = new FileSystemWatcher();
                created.Path = path;
                created.InternalBufferSize = WatcherBufferSize;
                created.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.LastAccess | NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.Attributes;
                created.Filter = HasMultipleFilters ? "*" : (WatchFilter ?? "");
                created.IncludeSubdirectories = IncludeSubdirectories;
                // Every change type is subscribed; Dispatch drops the ones this detector is not set to raise.
                created.Changed += OnFileSystemEvent;
                created.Created += OnFileSystemEvent;
                created.Deleted += OnFileSystemEvent;
                created.Renamed += OnFileSystemEvent;
                created.Error += Watcher_Error;
                created.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                Log.Error("FileWatcher " + Name + ": could not watch " + path + ": " + ex.ToString());
                DisposeWatcher(created);
                return false;
            }

            var previous = watcher;
            watcher = created;
            watcherHealthy = true;
            watcherCreated = DateTime.UtcNow;
            DisposeWatcher(previous);
            if (pathReported || previous == null) Log.Information("FileWatcher " + Name + ": watching " + path);
            pathReported = false;
            return true;
        }

        private void ReportPathProblem(string message)
        {
            // Log once per outage instead of on every health check.
            if (pathReported) return;
            pathReported = true;
            Log.Error(message);
        }

        private void DisposeWatcher(FileSystemWatcher w)
        {
            if (w == null) return;
            try
            {
                w.EnableRaisingEvents = false;
                w.Changed -= OnFileSystemEvent;
                w.Created -= OnFileSystemEvent;
                w.Deleted -= OnFileSystemEvent;
                w.Renamed -= OnFileSystemEvent;
                w.Error -= Watcher_Error;
                w.Dispose();
            }
            catch (Exception ex)
            {
                Log.Debug("FileWatcher " + Name + ": error disposing watcher: " + ex.Message);
            }
        }

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            var ex = e.GetException();
            if (ex is InternalBufferOverflowException)
                Log.Error("FileWatcher " + Name + ": too many changes at once, some files may have been missed. Rebuilding watcher. " + ex.Message);
            else
                Log.Error("FileWatcher " + Name + ": watcher failed, rebuilding in " + RetryDelay.TotalSeconds + " seconds. " + ex?.ToString());

            lock (sync)
            {
                if (!running || !ReferenceEquals(sender, watcher)) return;
                // After an error the watcher no longer raises events; the health check replaces it.
                watcherHealthy = false;
                healthTimer?.Change(RetryDelay, HealthCheckInterval);
            }
        }

        private void HealthCheck(object state)
        {
            // Directory.Exists on an unreachable share can block for a while; never run two checks at once.
            if (System.Threading.Interlocked.Exchange(ref healthCheckBusy, 1) == 1) return;
            try
            {
                string path;
                lock (sync)
                {
                    if (!running) return;
                    path = Watchpath;
                }
                bool reachable = !string.IsNullOrEmpty(path) && Directory.Exists(path);
                lock (sync)
                {
                    if (!running) return;
                    if (!reachable)
                    {
                        if (watcher != null)
                        {
                            ReportPathProblem("FileWatcher " + Name + ": folder " + path + " is no longer reachable, will keep retrying every " + HealthCheckInterval.TotalSeconds + " seconds");
                            DisposeWatcher(watcher);
                            watcher = null;
                            watcherHealthy = false;
                        }
                        return;
                    }
                    if (!watcherHealthy)
                    {
                        TryCreateWatcher();
                    }
                    else if (IsNetworkPath(path) && DateTime.UtcNow - watcherCreated > NetworkRefreshInterval)
                    {
                        Log.Debug("FileWatcher " + Name + ": periodic refresh of network folder watcher for " + path);
                        TryCreateWatcher();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("FileWatcher " + Name + ": health check failed: " + ex.ToString());
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref healthCheckBusy, 0);
            }
        }

        internal static bool IsNetworkPath(string path)
        {
            try
            {
                if (path.StartsWith(@"\\")) return true;
                var root = Path.GetPathRoot(path);
                if (string.IsNullOrEmpty(root)) return false;
                return new DriveInfo(root).DriveType == DriveType.Network;
            }
            catch
            {
                return false;
            }
        }

        private bool HasMultipleFilters
        {
            get
            {
                var f = WatchFilter;
                return !string.IsNullOrEmpty(f) && (f.Contains(",") || f.Contains("|"));
            }
        }

        /// <summary>
        /// Runs on the FileSystemWatcher's own thread. The watcher does not listen for the next batch of
        /// changes until this returns, so it only queues the event for the dispatch thread.
        /// </summary>
        private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            try
            {
                var q = queue;
                if (q != null && !q.IsAddingCompleted) q.TryAdd(e);
            }
            catch (InvalidOperationException)
            {
                // Stop() completed the queue between the check and the add.
            }
        }

        private void DispatchLoop(object state)
        {
            var q = (BlockingCollection<FileSystemEventArgs>)state;
            foreach (var e in q.GetConsumingEnumerable())
            {
                try
                {
                    Dispatch(e);
                }
                catch (Exception ex)
                {
                    Log.Error("FileWatcher " + Name + ": " + ex.ToString());
                }
            }
        }

        private void Dispatch(FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed && !raiseOnChanged) return;
            if (e.ChangeType == WatcherChangeTypes.Created && !raiseOnCreated) return;
            if (e.ChangeType == WatcherChangeTypes.Renamed && !raiseOnRenamed) return;
            if (e.ChangeType == WatcherChangeTypes.Deleted && !raiseOnDeleted) return;
            if (HasMultipleFilters)
            {
                bool matches = false;
                foreach (var ext in WatchFilter.Split(new Char[] { ',', '|' }))
                {
                    if (PatternMatcher.FitsMask(e.FullPath, ext.Trim())) { matches = true; break; }
                }
                if (!matches) return;
            }

            // Copying one file raises several Changed events in quick succession; trigger once per file.
            // (Previously a single timestamp was shared by all files and compared with TimeSpan.Milliseconds,
            // which dropped roughly one trigger in ten and any second file arriving within 100 ms.)
            var now = DateTime.UtcNow;
            DateTime last;
            if (lastTriggered.TryGetValue(e.FullPath, out last) && now - last < DuplicateWindow) return;
            lastTriggered[e.FullPath] = now;
            if (lastTriggered.Count > 1000)
            {
                foreach (var key in lastTriggered.Where(kv => now - kv.Value > TimeSpan.FromMinutes(1)).Select(kv => kv.Key).ToList())
                    lastTriggered.Remove(key);
            }

            var _e = new DetectorEvent(e.FullPath); _e.ChangeType = e.ChangeType;
            OnDetector?.Invoke(this, _e, EventArgs.Empty);
        }

        public void Initialize(IOpenRPAClient client)
        {
        }
    }
    public class DetectorEvent : IDetectorEvent
    {
        private static readonly string MachineName = Environment.MachineName.ToLower();
        // Resolved once: a DNS lookup per event could stall dispatch whenever DNS is slow or unreachable.
        private static readonly Lazy<string> Fqdn = new Lazy<string>(() =>
        {
            try { return System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower(); }
            catch { return MachineName; }
        });

        public IElement element { get; set; }
        public string host { get; set; }
        public WatcherChangeTypes ChangeType { get; set; }
        public string fqdn { get; set; }
        public string filepath { get; set; }
        public string result { get; set; }
        public DetectorEvent(string FullPath)
        {
            host = MachineName;
            fqdn = Fqdn.Value;
            filepath = FullPath;
            result = FullPath;
        }

    }

}
