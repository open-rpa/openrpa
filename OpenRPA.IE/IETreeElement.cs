using FlaUI.Core.Definitions;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.IE
{
    public class IETreeElement : treeelement
    {
        public IEElement IEElement { get; set; }
        public ControlType ControlType
        {
            get
            {
                if(IEElement!=null)
                {
                    switch (IEElement.tagName)
                    {
                        case "input":
                            if (IEElement.type == "hidden") return ControlType.Text;
                            if (IEElement.type == "text" || IEElement.type == "password") return ControlType.Edit;
                            if (IEElement.type == "button" || IEElement.type == "submit") return ControlType.Button;
                            break;
                        case "textarea":
                            return ControlType.Text;
                        case "a":
                            return ControlType.Hyperlink;
                        case "img":
                            return ControlType.Image;
                        case "div": case "body":
                            return ControlType.Pane;
                        case "style":
                            return ControlType.ToolTip;
                        case "html": case "head":
                            return ControlType.Hyperlink;
                        case "script":
                            return ControlType.DataItem;
                        case "form":
                            return ControlType.DataGrid;
                        case "center":
                            return ControlType.Text;
                        default:
                            break;
                    }
                }
                return ControlType.Hyperlink;
            }
        }

        public IETreeElement(treeelement parent, bool expanded, IEElement element) : base(parent)
        {
            IEElement = element;
            IsExpanded = expanded;
            Element = element;
            string controltype = "";
            string name = element.ToString();
            string automationid = "";
            //if (element.Properties.ControlType.IsSupported) ControlType = element.Properties.ControlType.Value;
            //if (element.Properties.ControlType.IsSupported) controltype = element.Properties.ControlType.Value.ToString();
            //if (element.Properties.Name.IsSupported) name = element.Properties.Name.Value;
            //if (element.Properties.AutomationId.IsSupported) automationid = element.Properties.AutomationId.Value;
            Name = (controltype + " " + name + " " + automationid).Trim();
        }

        public override void AddSubElements()
        {
            mshtml.IHTMLElementCollection children = IEElement.rawElement.children;
            foreach (mshtml.IHTMLElement elementNode in children) {
                var ele = new IEElement(elementNode);
                var exists = Children.Where(x => ((IEElement)x.Element).uniqueID == ele.uniqueID).FirstOrDefault();
                if(exists==null)
                {
                    Console.WriteLine("Adding " + ele.ToString());
                    Children.Add(new IETreeElement(this, false, ele));
                }
            }
        }
    }

}
