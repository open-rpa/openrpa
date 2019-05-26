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
    public class FileWatcherDetector : Detector
    {
        public string Watchpath { get; set; }
        public string WatchFilter { get; set; }
    }
    public class FileWatcherDetectorPlugin : ObservableObject, IDetectorPlugin
    {
        Detector IDetectorPlugin.Entity { get => Entity; }
        public FileWatcherDetector Entity { get; set; }
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
            Entity = InEntity as FileWatcherDetector;
            if (Entity == null)
            {
                if (System.IO.File.Exists(InEntity.Filepath))
                    Entity = JsonConvert.DeserializeObject<FileWatcherDetector>(System.IO.File.ReadAllText(InEntity.Filepath));
            }
            if (Entity == null)
            { 
                Entity = new FileWatcherDetector();
                if (InEntity != null)
                {
                    Entity.Filename = InEntity.Filename;
                    Entity.name = InEntity.name;
                    Entity.Path = InEntity.Path;
                    Entity.Plugin = InEntity.Plugin;
                    Entity.Selector = InEntity.Selector;
                    Entity._acl = InEntity._acl;
                    Entity._id = InEntity._id;
                    Entity._type = InEntity._type;
                }
                if (string.IsNullOrEmpty(Entity.name)) Entity.name = Name;
            }
            Entity.Path = InEntity.Path;
            watcher = new FileSystemWatcher();
            Start();
        }
        public void Start()
        {
            try
            {
                watcher.Path = Entity.Watchpath;
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.Filter = Entity.WatchFilter;
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
