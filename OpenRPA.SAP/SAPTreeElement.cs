using FlaUI.Core.Definitions;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.SAP
{
    public class SAPTreeElement : treeelement
    {
        public SAPElement SAPElement { get; set; }
        public ControlType ControlType
        {
            get
            {
                if(SAPElement!=null)
                {
                    switch (SAPElement.Role)
                    {
                        case "panel": return ControlType.Pane;
                        case "root pane": return ControlType.Pane;
                        case "layered pane": return ControlType.Pane;
                        case "push button": return ControlType.Button;
                        case "text": return ControlType.Edit;
                        case "frame": return ControlType.Pane;
                        case "label": return ControlType.Text;
                        default:
                            break;
                    }
                }
                return ControlType.Custom;
            }
        }

        public SAPTreeElement(treeelement parent, bool expanded, SAPElement element) : base(parent)
        {
            SAPElement = element;
            IsExpanded = expanded;
            Element = element;
            Name = element.Name;
        }

        public override void AddSubElements()
        {
            foreach (var elementNode in SAPElement.Children)
            {
                var ele = elementNode;
                bool exists = false;
                foreach (var c in Children) { if (c.Element.Equals(ele)) exists = true; }
                if (!exists)
                {
                    Children.Add(new SAPTreeElement(this, false, ele));
                }
            }
        }
    }

}
