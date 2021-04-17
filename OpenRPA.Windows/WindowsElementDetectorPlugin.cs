using FlaUI.Core.AutomationElements;
using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core.EventHandlers;
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
    public class WindowsElementDetectorPlugin : ObservableObject, IDetectorPlugin
    {
        public IDetector Entity { get; set; }
        public string Name
        {
            get
            {
                if (Entity != null && !string.IsNullOrEmpty(Entity.name)) return Entity.name;
                return "WindowsElement";
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
        private Views.WindowsClickDetectorView view;
        public UserControl editor
        {
            get
            {
                if (view == null)
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
        public void Initialize(IOpenRPAClient client, IDetector InEntity)
        {
            Entity = InEntity;
            Start();
        }
        private AutomationElement desktop;
        public void Start()
        {
            Task.Run(() =>
            {
                var automation = AutomationUtil.getAutomation();
                desktop = automation.GetDesktop();
                StructureChangedEventHandler = desktop.RegisterStructureChangedEvent(FlaUI.Core.Definitions.TreeScope.Descendants, DetectorCheck);
            });
        }
        public void Stop()
        {
            Task.Run(() =>
            {
                try
                {
                    if(desktop!= null) desktop.FrameworkAutomationElement.UnregisterStructureChangedEventHandler(StructureChangedEventHandler);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });
            //desktop.RemoveStructureChangedEventHandler(StructureChangedEventHandler);
        }
        //private FlaUI.Core.EventHandlers.IAutomationStructureChangedEventHandler StructureChangedEventHandler;
        private StructureChangedEventHandlerBase StructureChangedEventHandler;
        private DateTime lastTriggered = DateTime.Now;
        public void DetectorCheck(AutomationElement element, FlaUI.Core.Definitions.StructureChangeType e, int[] arg3)
        {
            try
            {
                if (Entity == null || string.IsNullOrEmpty(Selector)) return;
                TimeSpan timepassed = DateTime.Now - lastTriggered;
                if (timepassed.Milliseconds < 100) return;
                lastTriggered = DateTime.Now;
                // if (e != FlaUI.Core.Definitions.StructureChangeType.ChildAdded) return;
                var pathToRoot = new List<AutomationElement>();
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
                WindowsSelector selector = new WindowsSelector(Selector);
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

                if(element!=null)
                {
                    var _e = new DetectorEvent(new UIElement(element));
                    OnDetector?.Invoke(this, _e, EventArgs.Empty);
                }
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
}
