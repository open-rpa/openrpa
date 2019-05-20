using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.NM
{
    class NMSelector : Selector
    {
        NMElement element { get; set; }
        public NMSelector(string json) : base(json) { }
        public NMSelector(NMElement element, NMSelector anchor, bool doEnum)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            Log.Selector(string.Format("NMselector::AutomationElement::begin {0:mm\\:ss\\.fff}", sw.Elapsed));
            Log.Selector(string.Format("NMselector::GetControlVNMwWalker::end {0:mm\\:ss\\.fff}", sw.Elapsed));

            NMSelectorItem item;
            if (anchor == null)
            {
                item = new NMSelectorItem(element, true);
                item.Enabled = true;
                item.canDisable = false;
                Items.Add(item);
            }
            item = new NMSelectorItem(element, false);
            item.Enabled = true;
            item.canDisable = false;
            Items.Add(item);

            Log.Selector(string.Format("NMselector::EnumNeededProperties::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }
        public override IElement[] GetElements(IElement fromElement = null, int maxresults = 1)
        {
            return NMSelector.GetElementsWithuiSelector(this, fromElement, maxresults);
        }
        //private static NMElement[] GetElementsWithuiSelector(WindowsAccessBridgeInterop.AccessibleJvm jvm, NMSelector selector, IElement fromElement, int maxresults)
        //{
        //    NMElement[] result = null;
        //    NMElement _fromElement = fromElement as NMElement;
        //    var selectors = selector.Where(x => x.Enabled == true && x.Selector == null).ToList();
        //    var current = new List<NMElement>();
        //    NMElement startfrom = null;
        //    if (_fromElement != null) startfrom = _fromElement;
        //    if (startfrom == null) startfrom = new NMElement(jvm);
        //    current.Add(startfrom);
        //    for (var i = 0; i < selectors.Count; i++)
        //    {
        //        var sw = new System.Diagnostics.Stopwatch();
        //        sw.Start();
        //        var s = new NMSelectorItem(selectors[i]);
        //        Log.Selector(string.Format("OpenRPA.NM::GetElementsWithuiSelector::Find for selector {0} {1}", i, s.ToString()));
        //        var elements = new List<NMElement>();
        //        elements.AddRange(current);
        //        current.Clear();
        //        foreach (var _element in elements)
        //        {
        //            result = ((NMSelectorItem)s).matches(_element);
        //            current.AddRange(result);
        //        }
        //        if (i == (selectors.Count - 1)) result = current.ToArray();
        //        if (current.Count == 0)
        //        {
        //            var _c = new NMSelectorItem(selectors[i]);
        //            var message = "needed to find " + Environment.NewLine + _c.ToString() + Environment.NewLine + "but found only: " + Environment.NewLine;
        //            foreach (var element in elements)
        //            {
        //                var children = element.Children;
        //                foreach (var c in children)
        //                {
        //                    try
        //                    {
        //                        message += c.ToString() + Environment.NewLine;
        //                    }
        //                    catch (Exception)
        //                    {
        //                    }
        //                }
        //            }
        //            Log.Selector(message);
        //            return new NMElement[] { };
        //        }
        //        Log.Selector(string.Format("OpenRPA.NM::GetElement::found {1} for selector {2} in {0:mm\\:ss\\.fff}", sw.Elapsed, elements.Count(), i));
        //    }
        //    if (result == null) return new NMElement[] { };
        //    return result;
        //}
        public static NMElement[] GetElementsWithuiSelector(NMSelector selector, IElement fromElement = null, int maxresults = 1)
        {
            var results = new List<NMElement>();
            var first = selector[0];
            var second = selector[1];
            var p = first.Properties.Where(x => x.Name == "browser").FirstOrDefault();
            string browser = "";
            if (p != null) { browser = p.Value; }
            p = second.Properties.Where(x => x.Name == "xpath").FirstOrDefault();
            string xpath = "";
            if (p != null) { xpath = p.Value; }
            p = second.Properties.Where(x => x.Name == "cssselector").FirstOrDefault();
            string cssselector = "";
            if (p != null) { cssselector = p.Value; }

            NMHook.checkForPipes(true, true);
            NMHook.reloadtabs();
            var tabs = NMHook.tabs;
            if (!string.IsNullOrEmpty(browser)) { tabs = NMHook.tabs.Where(x => x.browser == browser).ToList(); }
            foreach (var tab in tabs)
            {
                NativeMessagingMessage subresult = null;

                var getelement = new NativeMessagingMessage("getelement");
                getelement.xPath = xpath;
                getelement.cssPath = cssselector;
                subresult = NMHook.sendMessageResult(getelement, false);
                foreach (var b in subresult.results)
                {
                    if (b.cssPath == "true" || b.xPath == "true")
                    {
                        results.Add(new NMElement(b));
                    }
                }
            }
            return results.ToArray();
            //NMHook.Instance.refreshJvms();
            //NMElement[] result = null;
            //foreach (var jvm in NMHook.Instance.jvms)
            //{
            //    result = GetElementsWithuiSelector(jvm, selector, fromElement, maxresults);
            //    if (result.Count() > 0) return result;
            //}

            //if (result == null) return new NMElement[] { };
            //return result;
            //return new NMElement[] { };
        }


    }
}
