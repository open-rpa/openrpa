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

namespace OpenRPA.NM
{
    public class DownloadDetectorPlugin : ObservableObject, IDetectorPlugin
    {
        public Detector Entity { get; set; }
        public string Name
        {
            get
            {
                if (Entity != null && !string.IsNullOrEmpty(Entity.name)) return Entity.name;
                return "DownloadDetector";
            }
        }
        private Views.DownloadDetectorView view;
        public UserControl editor
        {
            get
            {
                if (view == null)
                {
                    view = new Views.DownloadDetectorView(this);
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
        public void RaiseDetector(Download download)
        {
            if (!Running) return;
            var e = new DetectorEvent(download);
            OnDetector?.Invoke(this, e, EventArgs.Empty);

            foreach (var wi in client.WorkflowInstances.ToList())
            {
                if (wi.isCompleted) continue;
                if (wi.Bookmarks != null)
                {
                    foreach (var b in wi.Bookmarks)
                    {
                        if(b.Key == "DownloadDetectorPlugin")
                        {
                            wi.ResumeBookmark(b.Key, e);
                        }
                    }
                }
            }
        }
        FileSystemWatcher watcher = null;
        private IOpenRPAClient client = null;
        public void Initialize(IOpenRPAClient client, Detector InEntity)
        {
            this.client = client;
            Entity = InEntity;
            watcher = new FileSystemWatcher();
            Start();
        }
        public bool Running { get; set; } = false;
        public void Start()
        {
            try
            {
                Running = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public void Stop()
        {
            Running = false;
        }
        public void Initialize(IOpenRPAClient client)
        {
        }
    }
    public class DetectorEvent : IDetectorEvent
    {
        public IElement element { get; set; }
        public string host { get; set; }
        public string fqdn { get; set; }
        public string filepath { get; set; }
        public string filename { get; set; }
        public Download download { get; set; }
        public DetectorEvent(Download download)
        {
            host = Environment.MachineName.ToLower();
            fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
            this.download = download;
            filepath = download.filename;
            filename = Path.GetFileName(download.filename); 
        }

    }

}
