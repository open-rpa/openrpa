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
        public NMTreeElement(treeelement parent, bool expanded, NMElement element) : base(parent)
        {
            NMElement = element;
            IsExpanded = expanded;
            Element = element;
            Name = element.Name;
            Name = element.ToString();
            if (parent == null) {
                if (element.message != null && element.message.frameId > 0) Name += " (" + element.message.frameId + ")";
            }
        }
        private bool NeedsReload { get; set; } = true;
        public override void AddSubElements()
        {
            if (!NeedsReload) return;
            foreach (var e in NMElement.Children)
            {
                var ele = NMHook.getElement(NMElement.message.tabid, NMElement.message.frameId, NMElement.message.browser, "//*[@zn_id=\"" + e.zn_id + "\"]", TimeSpan.FromSeconds(2));
                if(ele.Length > 0)
                {
                    Children.Add(new NMTreeElement(this, false, ele[0]));
                }
            }
            NeedsReload = false;
        }
    }

}
