using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core.Definitions;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Windows
{
    public class WindowsTreeElement : treeelement
    {
        private ITreeWalker _treeWalker;
        public AutomationElement RawElement { get; set; }
        private AutomationBase automation { get; set; }
        public ControlType ControlType { get; set; }
        public WindowsTreeElement(treeelement parent, bool expanded, AutomationBase automation, AutomationElement element, ITreeWalker treeWalker) : base(parent)
        {
            Element = new UIElement(element);
            _treeWalker = treeWalker;
            this.automation = automation;
            RawElement = element;
            IsExpanded = expanded;

            string controltype = "";
            string name = "unknown";
            string automationid = "";
            ControlType = ControlType.Window;
            if (element.Properties.ControlType.IsSupported) ControlType = element.Properties.ControlType.Value;
            if (element.Properties.ControlType.IsSupported) controltype = element.Properties.ControlType.Value.ToString();
            if (element.Properties.Name.IsSupported) name = element.Properties.Name.Value;
            if (element.Properties.AutomationId.IsSupported) automationid = element.Properties.AutomationId.Value;
            Name = (controltype + " " + name + " " + automationid).Trim();
        }
        public override void AddSubElements()
        {
            var elementNode = _treeWalker.GetFirstChild(RawElement);
            while (elementNode != null)
            {
                Children.Add(new WindowsTreeElement(this, false, automation, elementNode, _treeWalker));
                try
                {
                    elementNode = _treeWalker.GetNextSibling(elementNode);
                }
                catch (Exception)
                {
                    elementNode = null;
                }
            }
        }

    }
}
