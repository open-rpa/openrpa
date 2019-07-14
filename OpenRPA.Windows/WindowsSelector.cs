using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.AutomationElements.Infrastructure;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Windows
{
    public class WindowsSelector : Selector
    {
        UIElement element { get; set; }
        public WindowsSelector(string json) : base(json) { }
        public WindowsSelector(AutomationElement element, WindowsSelector anchor, bool doEnum)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            Log.Selector(string.Format("windowsselector::begin {0:mm\\:ss\\.fff}", sw.Elapsed));

            AutomationElement root = null;
            AutomationElement baseElement = null;
            var pathToRoot = new List<AutomationElement>();
            while (element != null)
            {
                // Break on circular relationship (should not happen?)
                //if (pathToRoot.Contains(element) || element.Equals(_rootElement)) { break; }
                if (pathToRoot.Contains(element)) { break; }
                try
                {
                    if (element.Parent != null) pathToRoot.Add(element);
                    if (element.Parent == null) root = element;
                }
                catch (Exception)
                {
                    root = element;
                }
                try
                {
                    //element = _treeWalker.GetParent(element);
                    element = element.Parent;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    return;
                }
            }
            Log.Selector(string.Format("windowsselector::create pathToRoot::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            pathToRoot.Reverse();
            if (anchor != null)
            {
                var anchorlist = anchor.Where(x => x.Enabled && x.Selector == null).ToList();
                for (var i = 0; i < anchorlist.Count(); i++)
                {
                    if(WindowsSelectorItem.Match(anchorlist[i], pathToRoot[0]))
                    //if (((WindowsSelectorItem)anchorlist[i]).Match(pathToRoot[0]))
                    {
                        pathToRoot.Remove(pathToRoot[0]);
                    }
                    else
                    {
                        Log.Selector("Element does not match the anchor path");
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
            Log.Selector(string.Format("windowsselector::remove anchor if needed::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            WindowsSelectorItem item;
            if (anchor == null)
            {
                item = new WindowsSelectorItem(baseElement, true);
                item.Enabled = true;
                //item.canDisable = false;
                Items.Add(item);
            }
            bool isStartmenu = false;
            for (var i = 0; i < pathToRoot.Count(); i++)
            {
                var o = pathToRoot[i];
                item = new WindowsSelectorItem(o, false);
                if (i == 0 || i == (pathToRoot.Count() - 1)) item.canDisable = false;
                foreach (var p in item.Properties) // TODO: Ugly, ugly inzuBiz hack !!!!
                {
                    int idx = p.Value.IndexOf(".");
                    if (p.Name == "ClassName" && idx > -1)
                    {
                        int idx2 = p.Value.IndexOf(".", idx + 1);
                        if (idx2 > idx) p.Value = p.Value.Substring(0, idx2 + 1) + "*";
                    }
                    //if (p.Name == "ClassName" && p.Value.StartsWith("WindowsForms10")) p.Value = "WindowsForms10*";
                    if (p.Name == "ClassName" && p.Value.ToLower() == "shelldll_defview")
                    {
                        item.Enabled = false;
                    }
                    if (p.Name == "ClassName" && (p.Value.ToLower() == "dv2vontrolhost" || p.Value.ToLower() == "desktopprogramsmfu"))
                    {
                        isStartmenu = true;
                    }
                    if (p.Name == "ClassName" && p.Value == "#32770")
                    {
                        item.Enabled = false;
                    }
                    if (p.Name == "ControlType" && p.Value == "ListItem" && isStartmenu)
                    {
                        p.Enabled = false;
                    }
                }
                var hassyslistview32 = item.Properties.Where(p => p.Name == "ClassName" && p.Value.ToLower() == "syslistview32").ToList();
                if (hassyslistview32.Count > 0)
                {
                    var hasControlType = item.Properties.Where(p => p.Name == "ControlType").ToList();
                    if(hasControlType.Count> 0) { hasControlType[0].Enabled = false; }
                }

                if (doEnum) item.EnumNeededProperties(o, o.Parent);
                Items.Add(item);
            }
            pathToRoot.Reverse();
            Log.Selector(string.Format("windowsselector::EnumNeededProperties::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }
        public override IElement[] GetElements(IElement fromElement = null, int maxresults = 1)
        {
            return WindowsSelector.GetElementsWithuiSelector(this, fromElement, maxresults);
        }
        public static UIElement[] GetElementsWithuiSelector(WindowsSelector selector, IElement fromElement = null, int maxresults = 1)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            Log.Selector(string.Format("GetElementsWithuiSelector::begin {0:mm\\:ss\\.fff}", sw.Elapsed));

            UIElement _fromElement = fromElement as UIElement;
            var selectors = selector.Where(x => x.Enabled == true && x.Selector == null).ToList();

            var current = new List<UIElement>();
            var automation = AutomationUtil.getAutomation();

            UIElement[] result = null;
            using (automation)
            {
                var _treeWalker = automation.TreeWalkerFactory.GetControlViewWalker();
                AutomationElement startfrom = null;
                if (_fromElement != null) startfrom = _fromElement.RawElement;
                Log.Selector("automation.GetDesktop");
                if (startfrom == null) startfrom = automation.GetDesktop();

                current.Add(new UIElement(startfrom));
                for (var i = 0; i < selectors.Count; i++)
                {
                    var s = new WindowsSelectorItem(selectors[i]);
                    var elements = new List<UIElement>();
                    elements.AddRange(current);
                    current.Clear();
                    int failcounter = 0;
                    do
                    {
                        foreach (var _element in elements)
                        {
                            var matches = ((WindowsSelectorItem)s).matches(automation, _element.RawElement, _treeWalker, (i == 0 ? 1: maxresults));
                            var uimatches = new List<UIElement>();
                            foreach (var m in matches)
                            {
                                var ui = new UIElement(m);
                                var list = selectors.Take(i).ToList();
                                list.Add(new WindowsSelectorItem(m, false));
                                uimatches.Add(ui);
                            }
                            current.AddRange(uimatches.ToArray());
                        }
                        if (current.Count == 0)
                        {
                            ++failcounter;
                            foreach (var element in elements)
                            {
                                var message = "needed to find " + Environment.NewLine + selectors[i].ToString() + Environment.NewLine + "but found only: " + Environment.NewLine;
                                var children = element.RawElement.FindAllChildren();
                                foreach (var c in children)
                                {
                                    try
                                    {
                                        message += new UIElement(c).ToString() + Environment.NewLine;
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                                Log.Selector(message);
                            }
                        }
                        else
                        {
                            Log.Selector(string.Format("Found " + current.Count + " hits for selector # " + i + " {0:mm\\:ss\\.fff}", sw.Elapsed));
                        }
                    } while (failcounter < 2 && current.Count == 0);

                    if (i == (selectors.Count - 1)) result = current.ToArray();
                    if (current.Count == 0)
                    {
                        var message = "needed to find " + Environment.NewLine + selectors[i].ToString() + Environment.NewLine + "but found only: " + Environment.NewLine;
                        foreach (var element in elements)
                        {
                            var children = element.RawElement.FindAllChildren();
                            foreach (var c in children)
                            {
                                try
                                {
                                    message += new UIElement(c).ToString() + Environment.NewLine;
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }
                        Log.Warning(message);
                        return new UIElement[] { };
                    }
                }
            }
            if (result == null)
            {
                Log.Selector(string.Format("GetElementsWithuiSelector::ended with 0 results after {0:mm\\:ss\\.fff}", sw.Elapsed));
                return new UIElement[] { };
            }
            Log.Selector(string.Format("GetElementsWithuiSelector::ended with " + result.Count() + " results after {0:mm\\:ss\\.fff}", sw.Elapsed));
            return result;
        }


    }
}
