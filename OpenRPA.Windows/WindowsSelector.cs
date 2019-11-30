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
                // if (pathToRoot.Contains(element)) { break; }
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
            WindowsSelectorItem item;

            if(PluginConfig.traverse_selector_both_ways)
            {
                var temppathToRoot = new List<AutomationElement>();
                var newpathToRoot = new List<AutomationElement>();
                foreach (var e in pathToRoot) temppathToRoot.Add(e);
                Log.Selector(string.Format("windowsselector::traverse back to element from root::begin {0:mm\\:ss\\.fff}", sw.Elapsed));
                using (var automation = AutomationUtil.getAutomation())
                {
                    var _treeWalker = automation.TreeWalkerFactory.GetControlViewWalker();
                    bool isDesktop = true;
                    var parent = automation.GetDesktop();
                    if (anchor != null) { parent = temppathToRoot[0].Parent; isDesktop = false; }
                    while (temppathToRoot.Count > 0)
                    {
                        var i = temppathToRoot.First();
                        temppathToRoot.Remove(i);
                        item = new WindowsSelectorItem(i, false);
                        var m = item.matches(automation, parent, _treeWalker, 2, isDesktop, TimeSpan.FromSeconds(250));
                        if (m.Length > 0)
                        {
                            newpathToRoot.Add(i);
                            parent = i;
                            isDesktop = false;
                        }
                        if (m.Length == 0 && Config.local.log_selector)
                        {
                            //var message = "needed to find " + Environment.NewLine + item.ToString() + Environment.NewLine + "but found only: " + Environment.NewLine;
                            //var children = parent.FindAllChildren();
                            //foreach (var c in children)
                            //{
                            //    try
                            //    {
                            //        message += new UIElement(c).ToString() + Environment.NewLine;
                            //    }
                            //    catch (Exception)
                            //    {
                            //    }
                            //}
                            //Log.Debug(message);
                        }
                    }
                }
                Log.Selector(string.Format("windowsselector::traverse back to element from root::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                if (newpathToRoot.Count != pathToRoot.Count)
                {
                    Log.Information("Selector had " + pathToRoot.Count + " items to root, but traversing children inly matched " + newpathToRoot.Count);
                    pathToRoot = newpathToRoot;
                }
            }
            if (pathToRoot.Count == 0)
            {
                Log.Error("Element has not parent, or is same as annchor");
                return;
            }
            baseElement = pathToRoot.First();
            element = pathToRoot.Last();
            Clear();
            Log.Selector(string.Format("windowsselector::remove anchor if needed::end {0:mm\\:ss\\.fff}", sw.Elapsed));
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
                int IndexInParent = -1;
                if (o.Parent != null && i > 0)
                {
                    var c = o.Parent.FindAllChildren();
                    for (var x = 0; x < c.Count(); x++)
                    {
                        if (o.Equals(c[x])) IndexInParent = x;
                    }
                }

                item = new WindowsSelectorItem(o, false, IndexInParent);
                var _IndexInParent = item.Properties.Where(x => x.Name == "IndexInParent").FirstOrDefault();
                if(_IndexInParent!=null) _IndexInParent.Enabled = false;
                if (i == 0 || i == (pathToRoot.Count() - 1)) item.canDisable = false;
                foreach (var p in item.Properties) // TODO: Ugly, ugly inzuBiz hack !!!!
                {
                    int idx = p.Value.IndexOf(".");
                    if (p.Name == "ClassName" && idx > -1)
                    {
                        var FrameworkId = item.Properties.Where(x => x.Name == "FrameworkId").FirstOrDefault();
                        if (FrameworkId!=null && (FrameworkId.Value == "XAML" || FrameworkId.Value == "WinForm") && _IndexInParent != null)
                        {
                            item.Properties.ForEach(x => x.Enabled = false);
                            _IndexInParent.Enabled = true;
                            p.Enabled = true;
                        }
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
            Log.Selector(string.Format("windowsselector::end {0:mm\\:ss\\.fff}", sw.Elapsed));
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
            var midcounter = 1;
            if(PluginConfig.allow_multiple_hits_mid_selector)
            {
                midcounter = 10;
            }
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
                Log.SelectorVerbose("automation.GetDesktop");
                bool isDesktop = false;
                if (startfrom == null)
                {
                    startfrom = automation.GetDesktop();
                    isDesktop = true;
                }

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
                            var count = maxresults;
                            if (i == 0) count = midcounter;
                            // if (i < selectors.Count) count = 500;
                            if ((i+1) < selectors.Count) count = 1;
                            var matches = ((WindowsSelectorItem)s).matches(automation, _element.RawElement, _treeWalker, count, isDesktop, TimeSpan.FromSeconds(250)); // (i == 0 ? 1: maxresults)
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
                        if (current.Count > 1)
                        {
                            if(i < selectors.Count)
                            {
                                Log.Warning("Selector had " + current.Count + " hits and not just one, at element " + i + " this selector will be slow!");
                            }
                        }
                        if (current.Count == 0 && PluginConfig.allow_child_searching)
                        {
                            Log.Warning("Selector found not hits at element " + i + ", Try searching children, this selector will be slow!");
                            if ((i+1) < selectors.Count && i > 0) {
                                i++;
                                s = new WindowsSelectorItem(selectors[i]);
                                foreach (var _element in elements)
                                {
                                    var count = maxresults;
                                    if (i == 0) count = 1;
                                    if (i < selectors.Count) count = 500;
                                    var matches = ((WindowsSelectorItem)s).matches(automation, _element.RawElement, _treeWalker, count, false, TimeSpan.FromSeconds(250)); // (i == 0 ? 1 : maxresults)
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
                                Console.WriteLine(current.Count());
                            }
                        }
                        if (current.Count == 0) ++failcounter;
                        if (current.Count == 0 && Config.local.log_selector)
                        {
                            if(isDesktop)
                            {
                                var message = "needed to find " + Environment.NewLine + selectors[i].ToString() + Environment.NewLine + "but found only: " + Environment.NewLine;
                                var windows = Win32WindowUtils.GetTopLevelWindows(automation);
                                foreach (var c in windows)
                                {
                                    try
                                    {
                                        message += new UIElement(c).ToString() + Environment.NewLine;
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                                // Log.Selector(message);
                                Log.Warning(message);
                            }
                            else
                            {
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
                                    // Log.Selector(message);
                                    Log.Warning(message);
                                }
                            }
                        }
                        else
                        {
                            Log.SelectorVerbose(string.Format("Found " + current.Count + " hits for selector # " + i + " {0:mm\\:ss\\.fff}", sw.Elapsed));
                        }
                    } while (failcounter < 2 && current.Count == 0);


                    if (i == (selectors.Count - 1)) result = current.ToArray();
                    if (current.Count == 0 && Config.local.log_selector)
                    {
                        if (isDesktop)
                        {
                            var message = "needed to find " + Environment.NewLine + selectors[i].ToString() + Environment.NewLine + "but found only: " + Environment.NewLine;
                            var windows = Win32WindowUtils.GetTopLevelWindows(automation);
                            foreach (var c in windows)
                            {
                                try
                                {
                                    message += new UIElement(c).ToString() + Environment.NewLine;
                                }
                                catch (Exception)
                                {
                                }
                            }
                            Log.Warning(message);
                        }
                        else
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
                        }
                        return new UIElement[] { };
                    }
                    isDesktop = false;
                }
            }
            if (result == null)
            {
                Log.Selector(string.Format("GetElementsWithuiSelector::ended with 0 results after {0:mm\\:ss\\.fff}", sw.Elapsed));
                return new UIElement[] { };
            }
            return result;
        }
    }
}
