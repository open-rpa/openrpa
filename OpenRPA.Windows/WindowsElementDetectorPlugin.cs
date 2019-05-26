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
    public class WindowsElementDetectorPlugin : ObservableObject, IDetectorPlugin
    {
        Detector IDetectorPlugin.Entity { get => Entity; }
        public Detector Entity { get; set; }
        public string Name
        {
            get
            {
                if (Entity != null && !string.IsNullOrEmpty(Entity.name)) return Entity.name;
                return "WindowsElement";
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
        public void Initialize(Detector InEntity)
        {
            Entity = InEntity;
            Start();
        }
        private AutomationElement desktop;
        public void Start()
        {
            var automation = AutomationUtil.getAutomation();
            desktop = automation.GetDesktop();
            StructureChangedEventHandler = desktop.RegisterStructureChangedEvent(FlaUI.Core.Definitions.TreeScope.Descendants, detectorCheck);

        }
        public void Stop()
        {
            desktop.RemoveStructureChangedEventHandler(StructureChangedEventHandler);

        }
        private FlaUI.Core.EventHandlers.IAutomationStructureChangedEventHandler StructureChangedEventHandler;
        private DateTime lastTriggered = DateTime.Now;
        public void detectorCheck(AutomationElement element, FlaUI.Core.Definitions.StructureChangeType e, int[] arg3)
        {
            try
            {
                if (Entity == null || string.IsNullOrEmpty(Entity.Selector)) return;
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
                var _e = new DetectorEvent(new UIElement(element));
                OnDetector?.Invoke(this, _e, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}
