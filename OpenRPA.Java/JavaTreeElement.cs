using FlaUI.Core.Definitions;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Java
{
    public class JavaTreeElement : treeelement
    {
        public JavaElement JavaElement { get; set; }
        public ControlType ControlType
        {
            get
            {
                if(JavaElement!=null)
                {
                    switch (JavaElement.role)
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

        public JavaTreeElement(treeelement parent, bool expanded, JavaElement element) : base(parent)
        {
            JavaElement = element;
            IsExpanded = expanded;
            Element = element;
            Name = element.title;
        }

        public override void AddSubElements()
        {
            foreach(var elementNode in JavaElement.Children)
            {
                var ele = elementNode;
                //Children.Add(new JavaTreeElement(this, false, ele));
                bool exists = false;
                foreach (var c in Children) { if (c.Element.Equals(ele)) exists = true; }
                //var exists = Children.Where(x => !ele.Equals( ((JavaTreeElement)x).Element) ).FirstOrDefault();
                if (!exists)
                {
                    Console.WriteLine("Adding " + ele.ToString());
                    Children.Add(new JavaTreeElement(this, false, ele));
                }
            }
        }
    }

}
