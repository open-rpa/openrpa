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
                // Break on circular relationship(should not happen?)
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
            AutomationElement win = null;
            for(var i = 0; i < pathToRoot.Count; i++)
            {
                var _item = pathToRoot[i];
                FlaUI.Core.Definitions.ControlType ct;
                if(_item.Properties.ControlType.TryGetValue(out ct))
                {
                    if (ct == FlaUI.Core.Definitions.ControlType.Window) win = _item;
                }
            }
            if(win != null)
            {
                var indexof = pathToRoot.IndexOf(win) + 1;
                pathToRoot.RemoveRange(indexof, pathToRoot.Count - indexof);
            }
            pathToRoot.Reverse();
            if (anchor != null)
            {
                bool SearchDescendants = anchor.First().SearchDescendants();
                if(SearchDescendants)
                {
                    var a = anchor.Last();
                    var idx = -1;
                    for (var i = 0; i < pathToRoot.Count(); i++)
                    {
                        if (WindowsSelectorItem.Match(a, pathToRoot[i]))
                        {
                            idx = i;
                            // break;
                        }
                    }
                    pathToRoot.RemoveRange(0, idx);

                }
                else
                {
                    var anchorlist = anchor.Where(x => x.Enabled && x.Selector == null).ToList();
                    for (var i = 0; i < anchorlist.Count(); i++)
                    {
                        if (WindowsSelectorItem.Match(anchorlist[i], pathToRoot[0]))
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

            }
            WindowsSelectorItem item;
            if(PluginConfig.traverse_selector_both_ways)
            {
                Log.Selector(string.Format("windowsselector::create traverse_selector_both_ways::begin {0:mm\\:ss\\.fff}", sw.Elapsed));
                var temppathToRoot = new List<AutomationElement>();
                var newpathToRoot = new List<AutomationElement>();
                foreach (var e in pathToRoot) temppathToRoot.Add(e);
                Log.Selector(string.Format("windowsselector::traverse back to element from root::begin {0:mm\\:ss\\.fff}", sw.Elapsed));
                using (var automation = AutomationUtil.getAutomation())
                {
                    bool isDesktop = true;
                    AutomationElement parent = null;
                    if (anchor != null) { parent = temppathToRoot[0].Parent; isDesktop = false; }
                    else { parent = automation.GetDesktop(); }
                    int count = temppathToRoot.Count;
                    while (temppathToRoot.Count > 0)
                    {
                        count--;
                        var i = temppathToRoot.First();
                        temppathToRoot.Remove(i);
                        item = new WindowsSelectorItem(i, false);
                        if(parent!=null)
                        {
                            var m = item.matches(root, count, parent, 2, isDesktop, false);
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
                }
                if (newpathToRoot.Count != pathToRoot.Count)
                {
                    Log.Information("Selector had " + pathToRoot.Count + " items to root, but traversing children only matched " + newpathToRoot.Count);
                    pathToRoot = newpathToRoot;
                }
                Log.Selector(string.Format("windowsselector::create traverse_selector_both_ways::end {0:mm\\:ss\\.fff}", sw.Elapsed));
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
                Log.Selector(string.Format("windowsselector::create root element::begin {0:mm\\:ss\\.fff}", sw.Elapsed));
                item = new WindowsSelectorItem(baseElement, true);
                item.Enabled = true;
                //item.canDisable = false;
                Items.Add(item);
                Log.Selector(string.Format("windowsselector::create root element::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            }

            bool isStartmenu = false;
            for (var i = 0; i < pathToRoot.Count(); i++)
            {
                Log.Selector(string.Format("windowsselector::search_descendants::loop element " + i + ":begin {0:mm\\:ss\\.fff}", sw.Elapsed));
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
                if (_IndexInParent != null) _IndexInParent.Enabled = false;
                if (i == 0 || i == (pathToRoot.Count() - 1)) item.canDisable = false;
                foreach (var p in item.Properties)
                {
                    int idx = p.Value.IndexOf(".");
                    if (p.Name == "ClassName" && idx > -1)
                    {
                        var FrameworkId = item.Properties.Where(x => x.Name == "FrameworkId").FirstOrDefault();
                        //if (FrameworkId!=null && (FrameworkId.Value == "XAML" || FrameworkId.Value == "WinForm") && _IndexInParent != null)
                        //{
                        //    item.Properties.ForEach(x => x.Enabled = false);
                        //    _IndexInParent.Enabled = true;
                        //    p.Enabled = true;
                        //}
                        int idx2 = p.Value.IndexOf(".", idx + 1);
                        // if (idx2 > idx) p.Value = p.Value.Substring(0, idx2 + 1) + "*";
                        if (idx2 > idx && item.Properties.Count > 1)
                        {
                            p.Enabled = false;
                        }
                                
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
                    //if (p.Name == "ClassName" && p.Value == "#32770")
                    //{
                    //    item.Enabled = false;
                    //}
                    if (p.Name == "ControlType" && p.Value == "ListItem" && isStartmenu)
                    {
                        p.Enabled = false;
                    }
                }
                var hassyslistview32 = item.Properties.Where(p => p.Name == "ClassName" && p.Value.ToLower() == "syslistview32").ToList();
                if (hassyslistview32.Count > 0)
                {
                    var hasControlType = item.Properties.Where(p => p.Name == "ControlType").ToList();
                    if (hasControlType.Count > 0) { hasControlType[0].Enabled = false; }
                }

                if (doEnum) item.EnumNeededProperties(o, o.Parent);
                Items.Add(item);
                Log.Selector(string.Format("windowsselector::search_descendants::loop element " + i + ":end {0:mm\\:ss\\.fff}", sw.Elapsed));
            }
            pathToRoot.Reverse();
            Log.Selector(string.Format("windowsselector::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }
        public override IElement[] GetElements(IElement fromElement = null, int maxresults = 1)
        {
            return WindowsSelector.GetElementsWithuiSelector(this, fromElement, maxresults, null);
        }
        public static UIElement[] GetElementsWithuiSelectorItem(int ident, AutomationBase automation, WindowsSelectorItem sel, UIElement[] parents, int maxresults, bool LastItem, bool search_descendants)
        {
            var _current = new List<UIElement>();
            var cond = sel.GetConditionsWithoutStar();
            foreach (var _ele in parents)
            {
                if (PluginConfig.enable_cache && cond.ChildCount > 0)
                {
                    var cache = WindowsSelectorItem.GetFromCache(_ele.RawElement, ident, cond.ToString());
                    if (cache != null)
                    {
                        Log.Debug("GetElementsWithuiSelector: found in AppWindowCache " + cond.ToString());
                        foreach (var elementNode in cache)
                        {
                            if (WindowsSelectorItem.Match(sel, elementNode)) _current.Add(new UIElement(elementNode));
                        }
                    }
                }
                if (!string.IsNullOrEmpty(sel.processname()))
                {
                    if (!string.IsNullOrEmpty(sel.processname()) && (sel.processname().ToLower() != "startmenuexperiencehost" && sel.processname().ToLower() != "explorer"))
                    {
                        var me = System.Diagnostics.Process.GetCurrentProcess();
                        var ps = System.Diagnostics.Process.GetProcessesByName(sel.processname()).Where(_p => _p.SessionId == me.SessionId).ToArray();
                        if (ps.Length == 0)
                        {
                            Log.Selector(string.Format("GetElementsWithuiSelector::Process " + sel.processname() + " not found, end with 0 results"));
                            return new UIElement[] { };
                        }
                        var psids = ps.Select(x => x.Id).ToArray();

                        var condition = new FlaUI.Core.Conditions.ConditionFactory(automation.PropertyLibrary);
                        var ors = new List<FlaUI.Core.Conditions.ConditionBase>();
                        foreach (var p in ps)
                        {
                            ors.Add(condition.ByProcessId(p.Id));
                        }
                        var con = new FlaUI.Core.Conditions.OrCondition(ors);
                        if (PluginConfig.enable_cache && con.ChildCount > 0)
                        {
                            var cache = WindowsSelectorItem.GetFromCache(_ele.RawElement, ident, con.ToString());
                            if (cache != null)
                            {
                                Log.Debug("GetElementsWithuiSelector: found in AppWindowCache " + con.ToString());
                                foreach (var elementNode in cache)
                                {
                                    if (WindowsSelectorItem.Match(sel, elementNode)) _current.Add(new UIElement(elementNode));
                                }
                            }
                        }

                        if (_current.Count == 0)
                        {
                            Log.Debug(string.Format("GetElementsWithuiSelector::Searchin for all " + con.ToString()));
                            // var ___treeWalker = automation.TreeWalkerFactory.GetCustomTreeWalker(con);
                            var ___treeWalker = automation.TreeWalkerFactory.GetControlViewWalker();
                            int retries = 0;
                            AutomationElement win = null;
                            bool hasError = false;
                            do
                            {
                                hasError = false;
                                try
                                {
                                    win = ___treeWalker.GetFirstChild(_ele.RawElement);
                                }
                                catch (Exception ex)
                                {
                                    Log.Debug(ex.ToString());
                                    retries++;
                                    hasError = true;
                                    // throw;
                                }

                            } while (hasError && retries < 10);
                            
                            while (win != null)
                            {
                                bool addit = false;
                                if (win.Properties.ProcessId.IsSupported && psids.Contains(win.Properties.ProcessId))
                                {
                                    addit = true;
                                }
                                if (addit)
                                {
                                    var uiele = new UIElement(win);
                                    Log.Debug(string.Format("GetElementsWithuiSelector::Adding element " + uiele.ToString() ));
                                    _current.Add(uiele);
                                    if (win.Patterns.Window.TryGetPattern(out var winPattern))
                                    {
                                        if (winPattern.WindowVisualState.Value == FlaUI.Core.Definitions.WindowVisualState.Minimized)
                                        {
                                            IntPtr handle = win.Properties.NativeWindowHandle.Value;
                                            winPattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Normal);
                                        }
                                    }
                                    if (PluginConfig.allow_multiple_hits_mid_selector || ident == 0) // do all
                                    {
                                        win = ___treeWalker.GetNextSibling(win);
                                    }
                                    else
                                    {
                                        win = null;
                                    }
                                }
                                else
                                {
                                    win = ___treeWalker.GetNextSibling(win);
                                }
                            }
                            if (_current.Count > 0 && PluginConfig.enable_cache && con.ChildCount > 0)
                            {
                                var elements = _current.Select(x => x.RawElement).ToArray();
                                WindowsSelectorItem.AddToCache(_ele.RawElement, ident, con.ToString(), elements);
                            }
                        }
                    }
                    return _current.ToArray();
                }
                if (_current.Count == 0 && string.IsNullOrEmpty(sel.Selector))
                {
                    Log.Debug("GetElementsWithuiSelector::Searchin for " + cond.ToString());
                    ITreeWalker _treeWalker = default(ITreeWalker);
                    if (search_descendants)
                    {
                        var hasStar = sel.Properties.Where(x => x.Enabled == true && (x.Value != null && x.Value.Contains("*"))).ToArray();
                        _treeWalker = automation.TreeWalkerFactory.GetCustomTreeWalker(cond);
                    }
                    else
                    {
                        _treeWalker = automation.TreeWalkerFactory.GetControlViewWalker();
                    }
                    int retries = 0;
                    AutomationElement ele = null;
                    bool hasError = false;
                    do
                    {
                        hasError = false;
                        try
                        {
                            ele = _treeWalker.GetFirstChild(_ele.RawElement);
                        }
                        catch (Exception ex)
                        {
                            Log.Debug(ex.ToString());
                            retries++;
                            hasError = true;
                            // throw;
                        }

                    } while (hasError && retries < 10);

                    
                    if (ele != null)
                    {
                        do
                        {
                            // Log.Debug(string.Format("GetElementsWithuiSelector::Match element {0:mm\\:ss\\.fff}", sw.Elapsed));
                            if (WindowsSelectorItem.Match(sel, ele))
                            {
                                var uiele = new UIElement(ele);
                                Log.Debug(string.Format("GetElementsWithuiSelector::Adding element " + uiele.ToString()));
                                _current.Add(uiele);
                            }

                            bool getmore = false;
                            if (LastItem)
                            {
                                if (_current.Count < maxresults) getmore = true;
                            }
                            else
                            {
                                if (_current.Count == 0)
                                {
                                    getmore = true;
                                }
                                else if (PluginConfig.allow_multiple_hits_mid_selector)
                                {
                                    getmore = true;
                                }
                            }
                            if (getmore) ele = _treeWalker.GetNextSibling(ele);
                            if (!getmore) ele = null;
                        } while (ele != null);
                    }
                    if (_current.Count > 0 && PluginConfig.enable_cache && cond.ChildCount > 0)
                    {
                        var elements = _current.Select(x => x.RawElement).ToArray();
                        WindowsSelectorItem.AddToCache(_ele.RawElement, ident, cond.ToString(), elements);
                    }

                }
            }
            return _current.ToArray();
        }
        public static UIElement[] GetElementsWithuiSelector(WindowsSelector selector, IElement fromElement, int maxresults, WindowsCacheExtension ext)
        {
            TimeSpan timeout = TimeSpan.FromMilliseconds(5000);
            timeout = TimeSpan.FromMilliseconds(20000);
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            Log.Selector(string.Format("GetElementsWithuiSelector::begin {0:mm\\:ss\\.fff}", sw.Elapsed));

            UIElement _fromElement = fromElement as UIElement;
            // var selectors = selector.Where(x => x.Enabled == true && x.Selector == null).ToList();
            var selectors = selector.ToList();
            AutomationElement startfrom = null;
            if (_fromElement != null) startfrom = _fromElement.RawElement;


            var _current = new List<UIElement>();
            // var automation = AutomationUtil.getAutomation();
            AutomationBase automation = null;
            if (ext != null) automation = ext.automation;
            if(automation == null) automation = AutomationUtil.getAutomation();

            UIElement[] result = null;
            // AutomationElement ele = null;

            bool search_descendants = selectors[0].SearchDescendants();
            if (startfrom == null) startfrom = automation.GetDesktop();
            _current.Add(new UIElement(startfrom));
            for (var i = 0; i < selectors.Count; i++)
            {
                var _sel = selectors[i];
                var sel = new WindowsSelectorItem(_sel);
                var current = _current.ToArray();
                _current.Clear();
                // if(i == 1 && current.Length == 1 && current.First().ControlType == sel.ControlType)
                if(i == 1)
                {
                    foreach(var e in current)
                    {
                        if (WindowsSelectorItem.Match(sel, e.RawElement))
                        {
                            _current.Add(e);
                        }
                    }
                    if (_current.Count > 0) continue;
                    //_current = GetElementsWithuiSelectorItem(automation, sel, current, maxresults, i == (selectors.Count - 1)).ToList();
                    //if(_current.Count == 0) _current = current.ToList();
                    //_current = current.ToList();
                } 
                _current = GetElementsWithuiSelectorItem(i, automation, sel, current, maxresults, i == (selectors.Count - 1), search_descendants).ToList();
                if(i == 0 && _current.Count == 0) _current = current.ToList();
            }
            Log.Debug(string.Format("GetElementsWithuiSelector::completed with " + _current.Count + " results {0:mm\\:ss\\.fff}", sw.Elapsed));
            if (_current.Count > 0)
            {
                result = _current.ToArray();
                if (result.Count() > maxresults)
                {
                    Console.WriteLine("found " + result.Count() + " but only needed " + maxresults);
                    result = result.Take(maxresults).ToArray();
                }
                return result;
            }
            if (result == null)
            {
                Log.Selector(string.Format("GetElementsWithuiSelector::ended with 0 results after {0:mm\\:ss\\.fff}", sw.Elapsed));
                return new UIElement[] { };
            }
            if (result.Count() > maxresults) result = result.Take(maxresults).ToArray();
            Log.Selector(string.Format("GetElementsWithuiSelector::ended with " + result.Length + " results after {0:mm\\:ss\\.fff}", sw.Elapsed));
            return result;

        }
    }
}
