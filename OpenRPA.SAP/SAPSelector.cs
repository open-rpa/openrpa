using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.SAP
{
    class SAPSelector : Selector
    {
        SAPElement element { get; set; }
        public SAPSelector(string json) : base(json) { }
        public SAPSelector(SAPElement element, SAPSelector anchor, bool doEnum)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            Log.Selector(string.Format("SAPselector::AutomationElement::begin {0:mm\\:ss\\.fff}", sw.Elapsed));
            Log.Selector(string.Format("SAPselector::GetControlVSAPwWalker::end {0:mm\\:ss\\.fff}", sw.Elapsed));

            SAPElement root = null;
            SAPElement baseElement = null;
            var pathToRoot = new List<SAPElement>();
            while (element != null)
            {
                // Break on circular relationship (should not happen?)
                //if (pathToRoot.Contains(element) || element.Equals(_rootElement)) { break; }
                if (pathToRoot.Contains(element)) { break; }
                if (element.Parent != null) pathToRoot.Add(element);
                if (element.Parent == null) root = element;
                try
                {
                    element = element.Parent;
                    root = element.Parent;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    return;
                }
            }
            pathToRoot.Reverse();

            if (anchor != null)
            {
                var anchorlist = anchor.Where(x => x.Enabled && x.Selector == null).ToList();
                for (var i = 0; i < anchorlist.Count; i++)
                {
                    //if (((SAPSelectorItem)anchorlist[i]).Match(pathToRoot[0]))
                    if (SAPSelectorItem.Match(anchorlist[i], pathToRoot[0]))
                    {
                        pathToRoot.Remove(pathToRoot[0]);
                    }
                    else
                    {
                        Log.Warning("Element does not match the anchor path");
                        return;
                    }
                }
            }
            if (pathToRoot.Count == 0)
            {
                Log.Error("Element is same as annchor");
                return;
            }

            baseElement = pathToRoot.First();
            element = pathToRoot.Last();
            Clear();
            SAPSelectorItem item;
            if (anchor == null)
            {

                item = new SAPSelectorItem(root, true);
                item.Enabled = true;
                item.canDisable = false;
                Items.Add(item);
            }
            item = new SAPSelectorItem(element, false);
            item.Enabled = true; item.canDisable = false;
            Items.Add(item);
            //for (var i = 0; i < pathToRoot.Count(); i++)
            //{
            //    var o = pathToRoot[i];
            //    item = new SAPSelectorItem(o, false);
            //    if (i == 0 || i == (pathToRoot.Count() - 1)) item.canDisable = false;
            //    if (doEnum) { item.EnumNeededProperties(o, o.Parent); }
            //    Items.Add(item);
            //}
            //pathToRoot.Reverse();

            Log.Selector(string.Format("SAPselector::EnumNeededProperties::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }
        public override IElement[] GetElements(IElement fromElement = null, int maxresults = 1)
        {
            return SAPSelector.GetElementsWithuiSelector(this, fromElement, maxresults);
        }
        private static SAPElement[] GetElementsWithuiSelector(SAPSession session, SAPSelector selector, IElement fromElement, int maxresults)
        {
            var result = new List<SAPElement>();
            SAPElement _fromElement = fromElement as SAPElement;

            var root = new SAPSelectorItem(selector[0]);
            var sel = new SAPSelectorItem(selector[1]);
            var SystemName = root.SystemName;
            var id = sel.id;

            var msg = new SAPEvent("getitem");
            msg.Set(new SAPEventElement() { Id = id, SystemName = SystemName });
            msg = SAPhook.Instance.SendMessage(msg, TimeSpan.FromSeconds(5));
            if (msg != null)
            {
                var ele = msg.Get<SAPEventElement>();
                var Parent = new SAPElement(null, ele);
                result.Add(Parent);
            }
            return result.ToArray();
        }
        public static SAPElement[] GetElementsWithuiSelector( SAPSelector selector, IElement fromElement = null, int maxresults = 1)
        {
            var result = new List<SAPElement>();
            var root = new SAPSelectorItem(selector[0]);
            var SystemName = root.SystemName;
            foreach (var session in SAPhook.Instance.Sessions)
            {
                if(string.IsNullOrEmpty(SystemName) || (SystemName == session.Info.SystemName))
                {
                    result.AddRange(GetElementsWithuiSelector(session, selector, fromElement, maxresults));
                    if (result.Count > maxresults) return result.ToArray();
                }
            }
            return result.ToArray();
        }
    }
}
