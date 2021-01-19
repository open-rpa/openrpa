using FlaUI.Core;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Windows
{
    public class WindowsCacheExtension : ICustomWorkflowExtension, IDisposable
    {
        public void Initialize(IOpenRPAClient client, IWorkflow workflow, IWorkflowInstance instance)
        {
            automation = AutomationUtil.getAutomation();
            //cache = new CacheRequest();
            //cache.TreeScope = FlaUI.Core.Definitions.TreeScope.Element | FlaUI.Core.Definitions.TreeScope.Subtree | FlaUI.Core.Definitions.TreeScope.Parent;
            ////cacheRequest.TreeScope = FlaUI.Core.Definitions.TreeScope.Element;
            //cache.AutomationElementMode = FlaUI.Core.Definitions.AutomationElementMode.None;
            //cache.Add(automation.PropertyLibrary.Element.AutomationId);
            //cache.Add(automation.PropertyLibrary.Element.ProcessId);
            //cache.Add(automation.PropertyLibrary.Element.Name);
            //cache.Add(automation.PropertyLibrary.Element.ClassName);
            //cache.Add(automation.PropertyLibrary.Element.ControlType);
            //cache.Add(automation.PropertyLibrary.Element.LocalizedControlType);
            //cache.Add(automation.PropertyLibrary.Element.FrameworkId);
            //cache.Add(automation.PropertyLibrary.Element.BoundingRectangle);
            //cache.Add(automation.PropertyLibrary.Element.IsPassword);
            //// cache.Add(automation.PropertyLibrary.Element.Parent);
            //cacheObject = cache.Activate();
        }
        public AutomationBase automation { get; set; }
        public CacheRequest cache { get; set; }
        public IDisposable cacheObject { get; set; }
        private bool isDisposing = false;
        public void Dispose()
        {
            if (isDisposing) return;
            if (automation != null) automation.Dispose();
            if (cacheObject != null) cacheObject.Dispose();
            isDisposing = true;
        }
    }
}
