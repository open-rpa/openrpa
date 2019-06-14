using FlaUI.Core.Definitions;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.NM
{
    public class NMTreeElement : treeelement
    {
        public NMElement NMElement { get; set; }
        public ControlType ControlType
        {
            get
            {
                if(NMElement!=null)
                {
                    switch (NMElement.type)
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
                return ControlType.Hyperlink;
            }
        }

        public NMTreeElement(treeelement parent, bool expanded, NMElement element) : base(parent)
        {
            NMElement = element;
            IsExpanded = expanded;
            Element = element;
            Name = element.Name;
            Name = element.ToString();
        }

        public override void AddSubElements()
        {
            foreach (var e in NMElement.Children)
            {
                var ele = NMHook.getElement(NMElement.message.tabid, NMElement.message.browser, "//*[@zn_id=\"" + e.zn_id + "\"]", TimeSpan.FromSeconds(2));
                if(ele.Length > 0)
                {
                    Children.Add(new NMTreeElement(this, false, ele[0]));
                }
            }

        }
    }

}
