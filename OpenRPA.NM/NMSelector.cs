using Newtonsoft.Json.Linq;
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
        public NMSelector(NMElement element, NMSelector anchor, bool doEnum, NMElement anchorelement)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            Log.Selector(string.Format("NMselector::AutomationElement::begin {0:mm\\:ss\\.fff}", sw.Elapsed));
            Log.Selector(string.Format("NMselector::GetControlVNMwWalker::end {0:mm\\:ss\\.fff}", sw.Elapsed));

            NMSelectorItem item;
            if (anchor == null)
            {
                item = new NMSelectorItem(element, true, false);
                item.Enabled = true;
                item.canDisable = false;
                Items.Add(item);
            }
            else
            {
                var anchorarray = anchorelement.cssselector.Split('>');
                var elementarray = element.cssselector.Split('>');
                elementarray = elementarray.Skip(anchorarray.Length).ToArray();
                element.cssselector = string.Join(">", elementarray);
                // element.cssselector = element.cssselector.Substring(anchorelement.cssselector.Length);
            }
            item = new NMSelectorItem(element, false, (anchor != null));
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
        public static NMElement[] GetElementsWithuiSelector(NMSelector selector, IElement fromElement = null, int maxresults = 1)
        {
            var results = new List<NMElement>();
            SelectorItem first = null;
            SelectorItem second = null;
            string browser = "";
            SelectorItemProperty p = null;
            if (selector.Count > 1)
            {
                first = selector[0];
                second = selector[1];
                p = first.Properties.Where(x => x.Name == "browser").FirstOrDefault();
                if (p != null) { browser = p.Value; }
            }
            else if (fromElement == null)
            {
                throw new ArgumentException("Invalid select with only 1 child and no anchor");
            } else
            {
                second = selector[0];
            }
            p = second.Properties.Where(x => x.Name == "xpath").FirstOrDefault();
            string xpath = "";
            if (p != null) { xpath = p.Value; }
            p = second.Properties.Where(x => x.Name == "cssselector").FirstOrDefault();
            string cssselector = "";
            if (p != null) { cssselector = p.Value; }
            NMElement fromNMElement = fromElement as NMElement;
            string fromcssPath = "";
            string fromxPath = "";
            if (fromElement != null)
            {
                fromcssPath = fromNMElement.cssselector;
                fromxPath = fromNMElement.xpath;
            }
            //NMHook.checkForPipes(true, true);
            //NMHook.reloadtabs();
            //var tabs = NMHook.tabs.ToList();
            //if (!string.IsNullOrEmpty(browser)) { 
            //    lock(NMHook.tabs)
            //    {
            //        tabs = NMHook.tabs.Where(x => x.browser == browser).ToList();
            //    }
            //}
            //foreach (var tab in tabs)
            //{

            //}
            NativeMessagingMessage subresult = null;

            var getelement = new NativeMessagingMessage("getelements", PluginConfig.debug_console_output);
            getelement.browser = browser;
            getelement.xPath = xpath;
            getelement.cssPath = cssselector;
            getelement.fromxPath = fromxPath;
            getelement.fromcssPath = fromcssPath;
            if (fromElement != null && fromElement is NMElement)
            {
                getelement.windowId = ((NMElement)fromElement).message.windowId;
                getelement.tabid = ((NMElement)fromElement).message.tabid;
                getelement.frameId = ((NMElement)fromElement).message.frameId;
            }
                subresult = NMHook.sendMessageResult(getelement, false, TimeSpan.FromSeconds(2));
            if (subresult != null)
                if (subresult.results != null)
                    foreach (var b in subresult.results)
                    {
                        if (b.cssPath == "true" || b.xPath == "true")
                        {
                            if (results.Count > maxresults) continue;
                            results.Add(new NMElement(b));
                            //var data = b.result;
                            //var arr = JArray.Parse(data.ToString());
                            //foreach (var _e in arr)
                            //{
                            //    if (results.Count > maxresults) continue;
                            //    var json = _e.ToString();
                            //    var subsubresult = Newtonsoft.Json.JsonConvert.DeserializeObject<NativeMessagingMessage>(json);
                            //    subsubresult.browser = browser;
                            //    subsubresult.result = json;
                            //    subsubresult.tabid = b.tabid;
                            //    subsubresult.tab = b.tab;
                            //    results.Add(new NMElement(subsubresult));
                            //}
                        }
                    }
            return results.ToArray();
        }


    }
}
