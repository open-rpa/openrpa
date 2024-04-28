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
    public class URLDetectorPlugin : ObservableObject, IDetectorPlugin
    {
        public IDetector Entity { get; set; }
        public string Name
        {
            get
            {
                if (Entity != null && !string.IsNullOrEmpty(Entity.name)) return Entity.name;
                return "URLDetector";
            }
        }
        public string URL
        {
            get
            {
                if (Entity == null) return null;
                if (!Entity.Properties.ContainsKey("URL")) return null;
                var _val = Entity.Properties["URL"];
                if (_val == null) return null;
                return _val.ToString();
            }
        }
        public bool IgnoreCase
        {
            get
            {
                if (Entity == null) return false;
                if (!Entity.Properties.ContainsKey("IgnoreCase")) return false;
                var _val = Entity.Properties["URL"];
                if (_val == null) return IgnoreCase;
                return _val.ToString().ToLower() == "true";
            }
            set
            {
                Entity.Properties["IgnoreCase"] = value.ToString();
                NotifyPropertyChanged("IgnoreCase");
            }
        }
        private Views.URLDetectorView view;
        public UserControl editor
        {
            get
            {
                if (view == null)
                {
                    view = new Views.URLDetectorView(this);
                    view.PropertyChanged += (s, e) =>
                    {
                        NotifyPropertyChanged("Entity");
                        NotifyPropertyChanged("Name");
                        NotifyPropertyChanged("URL");
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
        }
        public void RaiseDetector(URLDetectorEvent e)
        {
            if (!Running) return;
            OnDetector?.Invoke(this, e, EventArgs.Empty);
        }
        FileSystemWatcher watcher = null;
        private IOpenRPAClient client = null;
        public void Initialize(IOpenRPAClient client, IDetector InEntity)
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
    public class URLDetectorEvent : IDetectorEvent
    {
        public IElement element { get; set; }
        public string host { get; set; }
        public string fqdn { get; set; }
        public string url { get; set; }
        public string result { get; set; }
        public URLDetectorEvent(string url)
        {
            host = Environment.MachineName.ToLower();
            fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
            this.url = url;
            result = url;
        }

    }

}
