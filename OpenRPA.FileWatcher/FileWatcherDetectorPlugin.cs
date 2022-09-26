using Newtonsoft.Json;
using OpenRPA.Input;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using System;
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
        FileSystemWatcher watcher = null;
        public void Initialize(IOpenRPAClient client, IDetector InEntity)
        {
            Entity = InEntity;
            watcher = new FileSystemWatcher();
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
            try
            {
                watcher.Path = Watchpath;
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.LastAccess | NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.Attributes;

                if (!string.IsNullOrEmpty(WatchFilter) && (WatchFilter.Contains(",") || WatchFilter.Contains("|")))
                {
                    watcher.Filter = "*";
                } 
                else
                {
                    watcher.Filter = WatchFilter;
                }

                if (raiseOnChanged) watcher.Changed += new FileSystemEventHandler(OnChanged);
                if (raiseOnCreated) watcher.Created += new FileSystemEventHandler(OnChanged);
                if (raiseOnDeleted) watcher.Deleted += new FileSystemEventHandler(OnChanged);
                if (raiseOnRenamed) watcher.Renamed += new RenamedEventHandler(OnChanged);
                //watcher.EnableRaisingEvents = true;
                watcher.EnableRaisingEvents = true;
                watcher.Error += Watcher_Error;
                watcher.IncludeSubdirectories = IncludeSubdirectories;
                
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            Log.Error(e.GetException()?.ToString());
        }

        public void Stop()
        {
            //watcher.EnableRaisingEvents = false;
            watcher.Changed -= new FileSystemEventHandler(OnChanged);
            watcher.Created -= new FileSystemEventHandler(OnChanged);
            watcher.Deleted -= new FileSystemEventHandler(OnChanged);
            watcher.Renamed -= new RenamedEventHandler(OnChanged);
        }
        private DateTime lastTriggered = DateTime.Now;
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (e.ChangeType == WatcherChangeTypes.Changed && !raiseOnChanged) return;
                if (e.ChangeType == WatcherChangeTypes.Created && !raiseOnCreated) return;
                if (e.ChangeType == WatcherChangeTypes.Renamed && !raiseOnRenamed) return;
                if (e.ChangeType == WatcherChangeTypes.Deleted && !raiseOnDeleted) return;
                if (!string.IsNullOrEmpty(WatchFilter) && (WatchFilter.Contains(",") || WatchFilter.Contains("|")))
                {
                    bool cont = false;
                    var array = WatchFilter.Split(new Char[] { ',', '|' });
                    foreach(var ext in array)
                    {
                        if (PatternMatcher.FitsMask(e.FullPath, ext)) cont = true;
                    }
                    if (!cont) return;
                }
                TimeSpan timepassed = DateTime.Now - lastTriggered;
                if (timepassed.Milliseconds < 100) return;
                lastTriggered = DateTime.Now;
                var _e = new DetectorEvent(e.FullPath); _e.ChangeType = e.ChangeType;
                OnDetector?.Invoke(this, _e, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public void Initialize(IOpenRPAClient client)
        {
        }
    }
    public class DetectorEvent : IDetectorEvent
    {
        public IElement element { get; set; }
        public string host { get; set; }
        public WatcherChangeTypes ChangeType { get; set; }
        public string fqdn { get; set; }
        public string filepath { get; set; }
        public string result { get; set; }
        public DetectorEvent(string FullPath)
        {
            host = Environment.MachineName.ToLower();
            fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
            filepath = FullPath;
            result = FullPath;
        }

    }

}
