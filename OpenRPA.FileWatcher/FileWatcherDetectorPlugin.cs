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
        public Detector Entity { get; set; }
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
        public void Initialize(Detector InEntity)
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
        public void Start()
        {
            try
            {
                watcher.Path = Watchpath;
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.Filter = WatchFilter;
                watcher.Changed += new FileSystemEventHandler(OnChanged);
                watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public void Stop()
        {
            watcher.EnableRaisingEvents = false;
        }
        private DateTime lastTriggered = DateTime.Now;
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                TimeSpan timepassed = DateTime.Now - lastTriggered;
                if (timepassed.Milliseconds < 100) return;
                lastTriggered = DateTime.Now;
                var _e = new DetectorEvent(e.FullPath);
                OnDetector?.Invoke(this, _e, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
    public class DetectorEvent : IDetectorEvent
    {
        public IElement element { get; set; }
        public string host { get; set; }
        public string fqdn { get; set; }
        public string filepath { get; set; }
        public TokenUser user { get; set; }
        public DetectorEvent(string FullPath)
        {
            host = Environment.MachineName.ToLower();
            fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
            filepath = FullPath;
        }

    }

}
