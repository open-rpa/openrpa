using FlaUI.Core;
using FlaUI.Core.AutomationElements.Infrastructure;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Windows
{
    class WindowsSelector : Selector
    {
        UIElement element { get; set; }
        public WindowsSelector(string json) : base(json) { }
        public WindowsSelector(AutomationElement element, WindowsSelector anchor, bool doEnum)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            Log.Debug(string.Format("windowsselector::AutomationElement::begin {0:mm\\:ss\\.fff}", sw.Elapsed));
            Log.Debug(string.Format("windowsselector::GetControlViewWalker::end {0:mm\\:ss\\.fff}", sw.Elapsed));



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
            Log.Debug("pathToRoot.Count: " + pathToRoot.Count);
            Log.Debug(string.Format("windowsselector::create pathToRoot::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            pathToRoot.Reverse();
            if (anchor != null)
            {
                var anchorlist = anchor.Where(x => x.Enabled && x.Selector == null).ToList();
                for (var i = 0; i < anchorlist.Count(); i++)
                {
                    if (((WindowsSelectorItem)anchorlist[i]).match(pathToRoot[0]))
                    {
                        pathToRoot.Remove(pathToRoot[0]);
                    }
                    else
                    {
                        Log.Debug("Element does not match the anchor path");
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
            Log.Debug(string.Format("windowsselector::remove anchor if needed::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            WindowsSelectorItem item;
            if (anchor == null)
            {
                item = new WindowsSelectorItem(baseElement, true);
                item.Enabled = true;
                //item.canDisable = false;
                Items.Add(item);
                item.PropertyChanged += SelectorChanged;
            }
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
                }
                if (doEnum) item.EnumNeededProperties(o, o.Parent);

                Items.Add(item);
                item.PropertyChanged += SelectorChanged;
            }
            pathToRoot.Reverse();
            Log.Debug(string.Format("windowsselector::EnumNeededProperties::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }

        public static UIElement[] GetElementsWithuiSelector(WindowsSelector selector, IElement fromElement = null)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            UIElement _fromElement = fromElement as UIElement;
            var selectors = selector.Where(x => x.Enabled == true && x.Selector == null).ToList();

            var current = new List<UIElement>();

            UIElement[] result = null;
            using (var automation = AutomationUtil.getAutomation())
            {
                var _treeWalker = automation.TreeWalkerFactory.GetControlViewWalker();

                AutomationElement startfrom = null;
                if (_fromElement != null) startfrom = _fromElement.rawElement;
                if (startfrom == null) startfrom = automation.GetDesktop();
                current.Add(new UIElement(startfrom));
                for (var i = 1; i < selectors.Count; i++)
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
                            var matches = ((WindowsSelectorItem)s).matches(_element.rawElement, _treeWalker);
                            var uimatches = new List<UIElement>();
                            foreach (var m in matches)
                            {
                                var ui = new UIElement(m);
                                var list = selectors.Take(i).ToList();
                                list.Add(new WindowsSelectorItem(m, false));
                                uimatches.Add(ui);
                            }

                            //result = uimatches.ToArray();
                            current.AddRange(uimatches.ToArray());
                            Log.Debug("add " + uimatches.Count + " matches to current");
                        }
                        if (current.Count == 0)
                        {
                            ++failcounter;
                            Log.Debug(string.Format("Failer # " + failcounter + " finding any hits for selector # " + i + " {0:mm\\:ss\\.fff}", sw.Elapsed));
                            foreach (var element in elements)
                            {
                                Log.Debug(element.ToString());
                                var message = "needed to find " + Environment.NewLine + selectors[i].ToString() + Environment.NewLine + "but found only: " + Environment.NewLine;
                                var children = element.rawElement.FindAllChildren();
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
                                Log.Debug(message);

                            }
                        }
                        else
                        {
                            Log.Debug(string.Format("Found " + current.Count + " hits for selector # " + i + " {0:mm\\:ss\\.fff}", sw.Elapsed));
                        }
                    } while (failcounter < 2 && current.Count == 0);

                    if (i == (selectors.Count - 1)) result = current.ToArray();
                    if (current.Count == 0)
                    {
                        var message = "needed to find " + Environment.NewLine + selectors[i].ToString() + Environment.NewLine + "but found only: " + Environment.NewLine;
                        foreach (var element in elements)
                        {
                            var children = element.rawElement.FindAllChildren();
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
            if (result == null) return new UIElement[] { };
            Log.Debug(string.Format("GetElementsWithuiSelector::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            return result;
        }


    }
}
