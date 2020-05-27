using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using SAPFEWSELib;
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
            for (var i = 0; i < pathToRoot.Count(); i++)
            {
                var o = pathToRoot[i];
                item = new SAPSelectorItem(o, false);
                if (i == 0 || i == (pathToRoot.Count() - 1)) item.canDisable = false;
                if (doEnum) { item.EnumNeededProperties(o, o.Parent); }
                Items.Add(item);
            }
            pathToRoot.Reverse();

            Log.Selector(string.Format("SAPselector::EnumNeededProperties::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }
        public override IElement[] GetElements(IElement fromElement = null, int maxresults = 1)
        {
            return SAPSelector.GetElementsWithuiSelector(this, fromElement, maxresults);
        }
        private static SAPElement[] GetElementsWithuiSelector(GuiSession session, SAPSelector selector, IElement fromElement, int maxresults)
        {
            SAPElement[] result = null;
            SAPElement _fromElement = fromElement as SAPElement;
            var selectors = selector.Where(x => x.Enabled == true && x.Selector == null).ToList();
            var current = new List<SAPElement>();
            SAPElement startfrom = null;
            if (_fromElement != null) startfrom = _fromElement;
            if (startfrom == null) startfrom = new SAPElement(session);
            current.Add(startfrom);
            for (var i = 0; i < selectors.Count; i++)
            {
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                var s = new SAPSelectorItem(selectors[i]);
                Log.Selector(string.Format("OpenRPA.SAP::GetElementsWithuiSelector::Find for selector {0} {1}", i, s.ToString()));
                var elements = new List<SAPElement>();
                elements.AddRange(current);
                current.Clear();
                foreach (var _element in elements)
                {
                    result = ((SAPSelectorItem)s).matches(_element);
                    current.AddRange(result);
                }
                if (current.Count == 0)
                {
                    // TODO: Figure out, why this is needed when working with SAP Menu's
                    foreach(var _e in elements)
                    {
                        if(s.Match(_e)) current.Add(_e);
                    }
                }
                if (i == (selectors.Count - 1)) result = current.ToArray();
                if (current.Count == 0 && Config.local.log_selector)
                {
                    var _c = new SAPSelectorItem(selectors[i]);
                    var message = "needed to find " + Environment.NewLine + _c.ToString() + Environment.NewLine + "but found only: " + Environment.NewLine;
                    foreach (var element in elements)
                    {
                        var children = element.Children;
                        foreach (var c in children)
                        {
                            try
                            {
                                message += c.ToString() + Environment.NewLine;
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                    Log.Selector(message);
                    return new SAPElement[] { };
                }
                Log.Selector(string.Format("OpenRPA.SAP::GetElement::found {1} for selector {2} in {0:mm\\:ss\\.fff}", sw.Elapsed, elements.Count(), i));
            }
            if (result == null) return new SAPElement[] { };
            return result;
        }
        public static SAPElement[] GetElementsWithuiSelector( SAPSelector selector, IElement fromElement = null, int maxresults = 1)
        {
            SAPhook.Instance.RefreshSessions();
            SAPElement[] result = null;
            foreach (var session in SAPhook.Instance.Sessions)
            {
                result = GetElementsWithuiSelector(session, selector, fromElement, maxresults);
                if (result.Count() > 0) return result;
            }

            if (result == null) return new SAPElement[] { };
            return result;
        }
    }
}
