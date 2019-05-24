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

namespace OpenRPA.Windows
{
    public class WindowsClickDetectorPlugin : ObservableObject, IDetectorPlugin
    {
        
        public Detector Entity { get; set; }
        public string Name
        {
            get
            {
                if (Entity != null && !string.IsNullOrEmpty(Entity.name)) return Entity.name;
                return "WindowsClick";
            }
        }
        private Views.WindowsClickDetectorView view;
        public UserControl editor
        {
            get
            {
                if(view==null)
                {
                    view = new Views.WindowsClickDetectorView(this);
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
        public void Initialize()
        {
            Start();
        }
        public void Start()
        {
            InputDriver.Instance.OnMouseUp += OnMouseUp;
        }
        public void Stop()
        {
            InputDriver.Instance.OnMouseUp -= OnMouseUp;
        }
        private void OnMouseUp(InputEventArgs e)
        {
            try
            {
                if (e.Element == null) return;
                if (Entity == null || string.IsNullOrEmpty(Entity.Selector)) return;
                var pathToRoot = new List<AutomationElement>();
                AutomationElement element = e.Element.RawElement;
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
                WindowsSelector selector = new WindowsSelector(Entity.Selector);
                if (pathToRoot.Count < (selector.Count - 1)) return;
                if (pathToRoot.Count > (selector.Count - 1)) return;
                pathToRoot.Reverse();
                for (var i = 0; i < pathToRoot.Count; i++)
                {
                    element = pathToRoot[i];
                    if (selector.Count > (i + 1))
                    {
                        WindowsSelectorItem s = new WindowsSelectorItem(selector[(i + 1)]);
                        if (!s.Match(element)) { return; }
                    }
                    else { return; }
                }
                var _e = new DetectorEvent(e.Element);
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
        public TokenUser user { get; set; }
        public DetectorEvent(UIElement element)
        {
            this.element = element;
            host = Environment.MachineName.ToLower();
            fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
        }

    }
}
