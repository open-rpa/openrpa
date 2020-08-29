using FlaUI.Core.AutomationElements.Infrastructure;
using OpenRPA.Input;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OpenRPA.Java
{
    public class JavaClickDetectorPlugin : ObservableObject, IDetectorPlugin
    {
        public Detector Entity { get; set; }
        public string Name
        {
            get
            {
                if (Entity != null && !string.IsNullOrEmpty(Entity.name)) return Entity.name;
                return "JavaClick";
            }
        }
        public string Selector
        {
            get
            {
                if (Entity == null) return null;
                if (!Entity.Properties.ContainsKey("Selector")) return null;
                var _val = Entity.Properties["Selector"];
                if (_val == null) return null;
                return _val.ToString();
            }
            set
            {
                if (Entity == null) return;
                Entity.Properties["Selector"] = value;
            }
        }

        private Views.JavaClickDetectorView view;
        public UserControl editor
        {
            get
            {
                if(view==null)
                {
                    view = new Views.JavaClickDetectorView(this);
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
        public Javahook hook { get; set; } = new Javahook();
        public void Initialize(IOpenRPAClient client, Detector InEntity)
        {
            Entity = InEntity;
            hook.init();
            Start();
        }
        public void Start()
        {
            hook.OnMouseClicked += Hook_OnMouseClicked;
        }
        public void Stop()
        {
            hook.OnMouseClicked -= Hook_OnMouseClicked;
        }
        private void Hook_OnMouseClicked(int vmID, WindowsAccessBridgeInterop.AccessibleContextNode ac)
        {
            if (string.IsNullOrEmpty(Selector)) return;
            var element = new JavaElement(ac);
            element.SetPath();
            Log.Debug("OnMouseClicked: " + element.id + " " + element.role + " " + element.Name);

            var pathToRoot = new List<JavaElement>();
            while (element != null)
            {
                if (pathToRoot.Contains(element)) { break; }
                try
                {
                    if (element.Parent != null) pathToRoot.Add(element);
                }
                catch (Exception)
                {
                }
                try
                {
                    element = element.Parent;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    return;
                }
            }
            JavaSelector selector = new JavaSelector(Selector);
            if (pathToRoot.Count < (selector.Count - 1)) return;
            if (pathToRoot.Count > (selector.Count - 1)) return;
            pathToRoot.Reverse();
            for (var i = 0; i < pathToRoot.Count; i++)
            {
                element = pathToRoot[i];
                if (selector.Count > (i + 1))
                {
                    JavaSelectorItem s = new JavaSelectorItem(selector[(i + 1)]);
                    if (!s.Match(element)) { return; }
                }
                else { return; }
            }
            var _e = new DetectorEvent(element);
            OnDetector?.Invoke(this, _e, EventArgs.Empty);

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
        public string result { get; set; }
        public DetectorEvent(JavaElement element)
        {
            this.element = element;
            host = Environment.MachineName.ToLower();
            fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
        }
    }
}
