using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
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
    class WindowsSelectorItem : SelectorItem
    {
        public WindowsSelectorItem() { }
        public WindowsSelectorItem(SelectorItem item)
        {
            SetBackingFieldValues(item._backingFieldValues);
            Properties = item.Properties;
            Element = item.Element;
        }
        public string Name
        {
            get
            {
                var e = Properties.Where(x => x.Name == "Name").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }
        public string ControlType
        {
            get
            {
                var e = Properties.Where(x => x.Name == "ControlType").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }
        public string ClassName
        {
            get
            {
                var e = Properties.Where(x => x.Name == "ClassName").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }
        public string AutomationId
        {
            get
            {
                var e = Properties.Where(x => x.Name == "AutomationId").FirstOrDefault();
                if (e == null) return null;
                return e.Value;
            }
        }
        public WindowsSelectorItem(AutomationElement element, bool isRoot, int IndexInParent = -1)
        {
            this.Element = new UIElement(element);
            Properties = new ObservableCollection<SelectorItemProperty>();
            if (isRoot)
            {
                if (element.Properties.ProcessId.IsSupported)
                {
                    var info = element.GetProcessInfo();
                    if (info.IsImmersiveProcess)
                    {
                        Properties.Add(new SelectorItemProperty("isImmersiveProcess", info.IsImmersiveProcess.ToString()));
                        Properties.Add(new SelectorItemProperty("applicationUserModelId", info.ApplicationUserModelId));
                    }
                    else
                    {
                        Properties.Add(new SelectorItemProperty("filename", info.Filename));
                        Properties.Add(new SelectorItemProperty("processname", info.ProcessName));
                        Properties.Add(new SelectorItemProperty("arguments", info.Arguments));
                    }
                    Properties.Add(new SelectorItemProperty("Selector", "Windows"));
                    Properties.Add(new SelectorItemProperty("SearchDescendants", PluginConfig.search_descendants.ToString()));
                    //Properties.Add(new SelectorItemProperty("", info.));
                }
                foreach (var p in Properties)
                {
                    p.Enabled = true;
                    p.canDisable = false;
                };
            }
            else
            {
                try
                {
                    if (element.Properties.Name.IsSupported && !string.IsNullOrEmpty(element.Properties.Name.Value)) Properties.Add(new SelectorItemProperty("Name", element.Properties.Name.Value));
                    if (element.Properties.ClassName.IsSupported && !string.IsNullOrEmpty(element.Properties.ClassName)) Properties.Add(new SelectorItemProperty("ClassName", element.Properties.ClassName.Value));
                    if (element.Properties.ControlType.IsSupported && !string.IsNullOrEmpty(element.Properties.ControlType.Value.ToString())) Properties.Add(new SelectorItemProperty("ControlType", element.Properties.ControlType.Value.ToString()));
                    if (element.Properties.AutomationId.IsSupported && !string.IsNullOrEmpty(element.Properties.AutomationId)) Properties.Add(new SelectorItemProperty("AutomationId", element.Properties.AutomationId.Value));
                    if (element.Properties.FrameworkId.IsSupported && !string.IsNullOrEmpty(element.Properties.FrameworkId)) Properties.Add(new SelectorItemProperty("FrameworkId", element.Properties.FrameworkId.Value));
                    if (IndexInParent > -1) Properties.Add(new SelectorItemProperty("IndexInParent", IndexInParent.ToString()));
                }
                catch (Exception)
                {
                }
                //Enabled = (Properties.Count > 1);
                //canDisable = true;
                if(Properties.Count == 0)
                {
                    try
                    {
                        var c = element.Properties.ControlType.ValueOrDefault;
                        Properties.Add(new SelectorItemProperty("ControlType", c.ToString()));
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                foreach (var p in Properties)
                {
                    p.Enabled = true;
                    p.canDisable = (Properties.Count > 1);
                };
                Enabled = true;
                canDisable = true;
            }
            foreach (var p in Properties) p.PropertyChanged += (sender, e) =>
            {
                OnPropertyChanged("Displayname");
                OnPropertyChanged("json");
            };
        }
        private string[] GetProperties()
        {
            var result = new List<string>();
            if (Properties.Where(x => x.Name == "ControlType").Count() == 1) result.Add("ControlType");
            if (Properties.Where(x => x.Name == "ClassName").Count() == 1) result.Add("ClassName");
            if (Properties.Where(x => x.Name == "AutomationId").Count() == 1) result.Add("AutomationId");
            if (Properties.Where(x => x.Name == "Name").Count() == 1) result.Add("Name");
            //if (!string.IsNullOrEmpty(role)) result.Add("role");
            //if (!string.IsNullOrEmpty(id)) result.Add("id");
            //if (!string.IsNullOrEmpty(title)) result.Add("title");
            if (Properties.Where(x => x.Name == "Index").Count() == 1) result.Add("Index");
            return result.ToArray();
        }
        public void EnumNeededProperties(AutomationElement element, AutomationElement parent)
        {
            string name = null;
            if (element.Properties.Name.IsSupported) name = element.Properties.Name.Value;
            var props = GetProperties();
            // int i = props.Length - 1;
            int i = props.Length ;
            int matchcounter = 0;
            var automation = AutomationUtil.getAutomation();
            var cacheRequest = new CacheRequest();
            cacheRequest.TreeScope = FlaUI.Core.Definitions.TreeScope.Element | FlaUI.Core.Definitions.TreeScope.Subtree;
            //cacheRequest.TreeScope = FlaUI.Core.Definitions.TreeScope.Element;
            cacheRequest.AutomationElementMode = FlaUI.Core.Definitions.AutomationElementMode.None;
            cacheRequest.Add(automation.PropertyLibrary.Element.AutomationId);
            cacheRequest.Add(automation.PropertyLibrary.Element.ProcessId);
            cacheRequest.Add(automation.PropertyLibrary.Element.Name);
            cacheRequest.Add(automation.PropertyLibrary.Element.ClassName);
            cacheRequest.Add(automation.PropertyLibrary.Element.ControlType);
            using (cacheRequest.Activate())
            {
                do
                {
                    var selectedProps = props.Take(i).ToArray();
                    foreach (var p in Properties) p.Enabled = selectedProps.Contains(p.Name);
                    var c = GetConditions(props.Take(i).ToArray());
                    matchcounter = parent.FindAllChildren(c).Count();
                    // if (matchcounter > 1) break;
                    if (matchcounter != 1)
                    {
                        Log.SelectorVerbose("EnumNeededProperties match with " + i + " gave more than 1 result");
                        ++i;
                        if (i >= props.Count()) break;
                    }
                } while (matchcounter != 1 && i < props.Count());

                //Log.SelectorVerbose("EnumNeededProperties match with " + i + " gave " + matchcounter + " result");
                Properties.ForEach((e) => e.Enabled = false);
                foreach (var p in props.Take(i).ToArray())
                {
                    Properties.Where(x => x.Name == p).First().Enabled = true;
                }
            }
        }
        private AndCondition GetConditions(string[] properties)
        {
            var automation = AutomationUtil.getAutomation();
            var cond = new List<ConditionBase>();
            foreach (var p in properties)
            {
                //if (p == "ControlType") cond.Add(element.ConditionFactory.ByControlType((ControlType)Enum.Parse(typeof(ControlType), ControlType)));
                //if (p == "Name") cond.Add(element.ConditionFactory.ByName(Name));
                //if (p == "ClassName") cond.Add(element.ConditionFactory.ByClassName(ClassName));
                //if (p == "AutomationId") cond.Add(element.ConditionFactory.ByAutomationId(AutomationId));
                if (p == "ControlType")
                {
                    ControlType ct = (ControlType)Enum.Parse(typeof(ControlType), ControlType);
                    cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.ControlType, ct));
                }
                if (p == "Name") cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.Name, Name));
                if (p == "AutomationId") cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.AutomationId, AutomationId));
                if (p == "ClassName") cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.ClassName, ClassName));
            }
            return new AndCondition(cond);
        }
        private AndCondition GetConditionsWithoutStar()
        {
            using (var automation = AutomationUtil.getAutomation())
            {
                var cond = new List<ConditionBase>();
                foreach (var p in Properties.Where(x => x.Enabled == true && (x.Value != null && !x.Value.Contains("*"))))
                {
                    //if (p == "ControlType") cond.Add(element.ConditionFactory.ByControlType((ControlType)Enum.Parse(typeof(ControlType), ControlType)));
                    //if (p == "Name") cond.Add(element.ConditionFactory.ByName(Name));
                    //if (p == "ClassName") cond.Add(element.ConditionFactory.ByClassName(ClassName));
                    //if (p == "AutomationId") cond.Add(element.ConditionFactory.ByAutomationId(AutomationId));
                    if (p.Name == "ControlType")
                    {
                        ControlType ct = (ControlType)Enum.Parse(typeof(ControlType), ControlType);
                        cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.ControlType, ct));
                    }
                    if (p.Name == "Name") cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.Name, Name));
                    if (p.Name == "ClassName") cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.ClassName, ClassName));
                    if (p.Name == "AutomationId") cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.AutomationId, AutomationId));
                }
                return new AndCondition(cond);
            }
        }
        private static Dictionary<string, AutomationElement> AppWindowCache = new Dictionary<string, AutomationElement>();
        public AutomationElement[] matches_new(AutomationBase automation, AutomationElement element, ITreeWalker _treeWalker, int count, bool isDesktop, TimeSpan timeout, bool search_descendants)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var matchs = new List<AutomationElement>();
            var c = GetConditionsWithoutStar();
            Log.Selector("AutomationElement.matches: Searching for " + c.ToString());
            AutomationElement[] elements = null;
            if (isDesktop)
            {
                elements = element.FindAllChildren(c);
            }
            else
            {
                elements = element.FindAllDescendants(c);
            }
            Log.Selector(string.Format("AutomationElement.matches::found " + elements.Count() + " elements {0:mm\\:ss\\.fff}", sw.Elapsed));
            // var elements = element.FindAllChildren();
            foreach (var elementNode in elements)
            {
                Log.SelectorVerbose("matches::match");
                if (Match(elementNode)) matchs.Add(elementNode);
            }
            Log.Selector(string.Format("AutomationElement.matches::complete, with " + elements.Count() + " elements {0:mm\\:ss\\.fff}", sw.Elapsed));

            return matchs.ToArray();
        }
        public AutomationElement[] matches(AutomationBase automation, AutomationElement element, ITreeWalker _treeWalker, int count, bool isDesktop, TimeSpan timeout, bool search_descendants)
        {
            var matchs = new List<AutomationElement>();
            var Conditions = GetConditionsWithoutStar();
            if(isDesktop)
            {
                if(AppWindowCache.ContainsKey(Conditions.ToString()))
                {
                    var _element = AppWindowCache[Conditions.ToString()];
                    try
                    {
                        if (_element.Properties.IsOffscreen.IsSupported && !_element.IsOffscreen)
                        {
                            if (Match(_element))
                            {
                                Log.Selector("matches::FindAllChildren: found in AppWindowCache");
                                return new AutomationElement[] { _element };
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Log.SelectorVerbose("matches::FindAllChildren: Removing from AppWindowCache " + Conditions.ToString());
                        AppWindowCache.Remove(Conditions.ToString());
                    }
                }
            }
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            Log.SelectorVerbose("matches::FindAllChildren.isDesktop(" + isDesktop + ")::begin");

            if(search_descendants)
            {
                Log.Selector("AutomationElement.matches: Searching for " + Conditions.ToString());
                AutomationElement[] elements = null;
                if (isDesktop)
                {
                    elements = element.FindAllChildren(Conditions);
                }
                else
                {
                    elements = element.FindAllDescendants(Conditions);
                }
                Log.Selector(string.Format("AutomationElement.matches::found " + elements.Count() + " elements {0:mm\\:ss\\.fff}", sw.Elapsed));
                // var elements = element.FindAllChildren();
                foreach (var elementNode in elements)
                {
                    Log.SelectorVerbose("matches::match");
                    if (Match(elementNode)) matchs.Add(elementNode);
                }
                Log.Selector(string.Format("AutomationElement.matches::complete, with " + elements.Count() + " elements {0:mm\\:ss\\.fff}", sw.Elapsed));
            }
            else
            {
                System.Threading.ManualResetEvent syncEvent = new System.Threading.ManualResetEvent(false);
                Action action = () =>
                {
                    var manualcheck = Properties.Where(x => x.Enabled == true && x.Value != null && (x.Name == "IndexInParent" || x.Value.Contains("*"))).Count();
                    if(manualcheck > 0)
                    {
                        var nodes = new List<AutomationElement>();
                        Log.Selector(string.Format("AutomationElement.matches.isDesktop(" + isDesktop + ")::GetFirstChild {0:mm\\:ss\\.fff}", sw.Elapsed));
                        var elementNode = _treeWalker.GetFirstChild(element);
                        var i = 0;
                        while (elementNode != null)
                        {
                            nodes.Add(elementNode);
                            i++;
                            if (Match(elementNode)) matchs.Add(elementNode);
                            if (matchs.Count >= count) break;
                            Log.Selector(string.Format("AutomationElement.matches.isDesktop(" + isDesktop + ")::GetNextSibling {0:mm\\:ss\\.fff}", sw.Elapsed));
                            elementNode = _treeWalker.GetNextSibling(elementNode);
                        }
                    } 
                    else
                    {
                        Log.Selector(string.Format("AutomationElement.matches.isDesktop(" + isDesktop + ")::FindAllChildren::begin {0:mm\\:ss\\.fff}", sw.Elapsed));
                        var elements = element.FindAllChildren(Conditions);
                        Log.Selector(string.Format("AutomationElement.matches.isDesktop(" + isDesktop + ")::FindAllChildren::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                        foreach (var elementNode in elements) matchs.Add(elementNode);
                    }
                    if (syncEvent != null)
                    {
                        syncEvent.Set();
                    }
                };
                // if (isDesktop && PluginConfig.get_elements_in_different_thread)
                if (PluginConfig.get_elements_in_different_thread)
                {
                    //Task.Run(action);
                    //syncEvent.WaitOne(timeout, true);
                    //if(matchs.Count > 0)
                    //{
                    //    matchs.Clear();
                    action();
                    //}
                }
                else
                {
                    action();
                }
            }
            Log.SelectorVerbose(string.Format("matches::FindAllChildren.isDesktop(" + isDesktop + ")::complete {0:mm\\:ss\\.fff}", sw.Elapsed));
            if(isDesktop && matchs.Count == 1)
            {
                if (matchs[0].Properties.IsOffscreen.IsSupported)
                {
                    if(!AppWindowCache.ContainsKey(Conditions.ToString()))
                    {
                        AppWindowCache.Add(Conditions.ToString(), matchs[0]);
                    } else
                    {
                        AppWindowCache[Conditions.ToString()] = matchs[0];
                    }
                    
                }
                    
            }
            return matchs.ToArray();
        }
        public bool Match(AutomationElement m)
        {
            return Match(this, m);
        }
        public static bool Match(SelectorItem item, AutomationElement m)
        {
            foreach (var p in item.Properties.Where(x => x.Enabled == true && x.Value != null))
            {
                if (p.Name == "ControlType")
                {
                    string v = null;
                    try
                    {
                        v = m.Properties.ControlType.ValueOrDefault.ToString();
                    }
                    catch (Exception)
                    {
                    }
                    if (!string.IsNullOrEmpty(v))
                    {
                        // var v = m.Properties.ControlType.Value.ToString();
                        if (!PatternMatcher.FitsMask(v, p.Value))
                        {
                            Log.SelectorVerbose(p.Name + " mismatch '" + v + "' / '" + p.Value + "'");
                            return false;
                        }
                    }
                    else
                    {
                        Log.SelectorVerbose(p.Name + " does not exists, but needed value '" + p.Value + "'");
                        return false;
                    }
                }
                if (p.Name == "Name")
                {
                    if (m.Properties.Name.IsSupported)
                    {
                        var v = m.Properties.Name.Value;
                        if (!PatternMatcher.FitsMask(m.Properties.Name.Value, p.Value))
                        {
                            Log.SelectorVerbose(p.Name + " mismatch '" + v + "' / '" + p.Value + "'");
                            return false;
                        }
                    }
                    else
                    {
                        Log.SelectorVerbose(p.Name + " does not exists, but needed value '" + p.Value + "'");
                        return false;
                    }
                }
                if (p.Name == "ClassName")
                {

                    if (m.Properties.ClassName.IsSupported)
                    {
                        var v = m.Properties.ClassName.Value;
                        if (!PatternMatcher.FitsMask(m.Properties.ClassName.Value, p.Value))
                        {
                            Log.SelectorVerbose(p.Name + " mismatch '" + v + "' / '" + p.Value + "'");
                            return false;
                        }
                    }
                    else
                    {
                        Log.SelectorVerbose(p.Name + " does not exists, but needed value '" + p.Value + "'");
                        return false;
                    }
                }
                if (p.Name == "AutomationId")
                {
                    if (m.Properties.AutomationId.IsSupported)
                    {
                        var v = m.Properties.AutomationId.Value;
                        if (!PatternMatcher.FitsMask(m.Properties.AutomationId.Value, p.Value))
                        {
                            Log.SelectorVerbose(p.Name + " mismatch '" + v + "' / '" + p.Value + "'");
                            return false;
                        }
                    }
                    else
                    {
                        Log.SelectorVerbose(p.Name + " does not exists, but needed value '" + p.Value + "'");
                        return false;
                    }
                }
                if (p.Name == "FrameworkId")
                {
                    if (m.Properties.FrameworkId.IsSupported)
                    {
                        var v = m.Properties.FrameworkId.Value;
                        if (!PatternMatcher.FitsMask(m.Properties.FrameworkId.Value, p.Value))
                        {
                            Log.SelectorVerbose(p.Name + " mismatch '" + v + "' / '" + p.Value + "'");
                            return false;
                        }
                    }
                    else
                    {
                        Log.SelectorVerbose(p.Name + " does not exists, but needed value '" + p.Value + "'");
                        return false;
                    }
                }
                if (p.Name == "IndexInParent")
                {
                    int IndexInParent = -1;
                    int.TryParse(p.Value, out IndexInParent);
                    if (IndexInParent > -1)
                    {
                        var c = m.Parent.FindAllChildren();
                        if (c.Count() <= IndexInParent)
                        {
                            Log.SelectorVerbose(p.Name + " is " + IndexInParent + " but found only " + c.Count() + " elements in parent");
                            return false;
                        }
                        if (!m.Equals(c[IndexInParent]))
                        {
                            Log.SelectorVerbose(p.Name + " mismatch, element is not equal to element " + IndexInParent + " in parent");
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        public override string ToString()
        {
            return "AutomationId:" + AutomationId + " Name:" + Name + " ClassName: " + ClassName + " ControlType: " + ControlType;
        }
    }
}
