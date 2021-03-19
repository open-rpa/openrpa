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
    public class MatchCacheItem
    {
        public DateTime Created { get; set; }
        public AutomationElement[] Result { get; set; }
        public AutomationElement Root { get; set; }
        public string Conditions { get; set; }
        public int Ident { get; set; }
        public override string ToString()
        {
            if(Result == null) return "[0] " + Conditions;
            return "[" + Result.Length + "] " + Conditions;
        }
    }
    public class WindowsSelectorItem : SelectorItem
    {
        public WindowsSelectorItem() { }
        public WindowsSelectorItem(SelectorItem item)
        {
            SetBackingFieldValues(item._backingFieldValues);
            Properties = item.Properties;
            Element = item.Element;
        }
        public bool search_descendants
        {
            get
            {
                if (Properties == null) return PluginConfig.search_descendants;
                var v = Properties.Where(x => x.Name == "search_descendants").FirstOrDefault();
                if (v == null) Properties.Where(x => x.Name == "SearchDescendants").FirstOrDefault();
                if (v == null) return PluginConfig.search_descendants;
                return bool.Parse(v.Value);
            }
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
                var w = element.AsWindow();
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
                        if (info.Filename.ToLower().Contains("system32\\conhost.exe"))
                        {
                            info.Filename = "%windir%\\system32\\cmd.exe";
                        }
                        Properties.Add(new SelectorItemProperty("filename", info.Filename));
                        Properties.Add(new SelectorItemProperty("processname", info.ProcessName));
                        Properties.Add(new SelectorItemProperty("arguments", info.Arguments));
                    }
                    Properties.Add(new SelectorItemProperty("Selector", "Windows"));
                    Properties.Add(new SelectorItemProperty("search_descendants", PluginConfig.search_descendants.ToString()));
                    // if(!PluginConfig.search_descendants) Properties.Add(new SelectorItemProperty("search_descendants", "false"));
                    // Properties.Add(new SelectorItemProperty("SearchDescendants", PluginConfig.search_descendants.ToString()));
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
                    if (element.Properties.ControlType.IsSupported && !string.IsNullOrEmpty(element.Properties.ControlType.Value.ToString()))
                    {

                        if (element.Properties.ControlType.Value != FlaUI.Core.Definitions.ControlType.Unknown)
                        {
                            Properties.Add(new SelectorItemProperty("ControlType", element.Properties.ControlType.Value.ToString()));
                        }
                    }
                    if (element.Properties.AutomationId.IsSupported && !string.IsNullOrEmpty(element.Properties.AutomationId)) Properties.Add(new SelectorItemProperty("AutomationId", element.Properties.AutomationId.Value));
                    if (element.Properties.FrameworkId.IsSupported && !string.IsNullOrEmpty(element.Properties.FrameworkId)) Properties.Add(new SelectorItemProperty("FrameworkId", element.Properties.FrameworkId.Value));
                    if (IndexInParent > -1) Properties.Add(new SelectorItemProperty("IndexInParent", IndexInParent.ToString()));
                }
                catch (Exception)
                {
                }
                //Enabled = (Properties.Count > 1);
                //canDisable = true;
                if (Properties.Count == 0)
                {
                    try
                    {
                        var c = element.Properties.ControlType.ValueOrDefault;
                        if(c.ToString() != "Unknown") Properties.Add(new SelectorItemProperty("ControlType", c.ToString()));
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
            int i = props.Length;
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
        public AutomationElement[] matches_new(AutomationBase automation, AutomationElement element, ITreeWalker _treeWalker, int count, bool isDesktop, TimeSpan timeout, bool search_descendants)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var matchs = new List<AutomationElement>();
            var c = Extensions.GetConditionsWithoutStar(this);
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
        private static List<MatchCacheItem> MatchCache = new List<MatchCacheItem>();
        private static object cache_lock = new object();
        public static AutomationElement[] GetFromCache(AutomationElement root, int ident, string Conditions)
        {
            if (!PluginConfig.enable_cache) return null;
            var now = DateTime.Now;
            var timeout = PluginConfig.cache_timeout;
            MatchCacheItem result = null;
            MatchCacheItem[] _list = null;
            lock (cache_lock) _list = MatchCache.ToArray();
            try
            {
                for(var i = _list.Length-1; i >= 0; i--)
                {
                    try
                    {
                        if (now - _list[i].Created > timeout) RemoveFromCache(_list[i]);
                        if(_list[i].Conditions == Conditions && _list[i].Root.Equals(root) && _list[i].Ident == ident)
                        {
                            result = _list[i];
                        }
                    }
                    catch (Exception)
                    {
                        RemoveFromCache(_list[i]);
                    }
                }
            }
            catch (Exception)
            {
            }
            if (result != null)
            {
                try
                {
                    foreach (var e in result.Result)
                    {
                        // _ = e.Parent;
                        if (!e.IsAvailable)
                        {
                            RemoveFromCache(result);
                            return null;
                        }
                        else if (!e.Properties.BoundingRectangle.IsSupported || e.Properties.BoundingRectangle.Value == System.Drawing.Rectangle.Empty)
                        {
                            RemoveFromCache(result);
                            return null;
                        }
                        //else if ((e.ControlType == FlaUI.Core.Definitions.ControlType.Button ||
                        //    e.ControlType == FlaUI.Core.Definitions.ControlType.CheckBox ||
                        //    e.ControlType == FlaUI.Core.Definitions.ControlType.ComboBox ||
                        //    e.ControlType == FlaUI.Core.Definitions.ControlType.Text ||
                        //    e.ControlType == FlaUI.Core.Definitions.ControlType.RadioButton
                        //    ) &&e.IsOffscreen)
                        //{
                        //    RemoveFromCache(result);
                        //    return null;
                        //}
                    }
                }
                catch (Exception)
                {
                    RemoveFromCache(result);
                }
                return result.Result;
            }
            return null;
        }
        public static void RemoveFromCache(MatchCacheItem item)
        {
            try
            {
                lock(cache_lock)
                {
                    var items = MatchCache.Where(x => x.Root.Equals(item.Root) && x.Ident >= item.Ident).ToList();
                    foreach (var e in items) MatchCache.Remove(e);
                }
            }
            catch (Exception)
            {
            }
            lock (cache_lock) MatchCache.Remove(item);
        }
        public static void ClearCache()
        {
            lock (cache_lock) MatchCache.Clear();
        }
        public static void AddToCache(AutomationElement root, int ident, string Conditions, AutomationElement[] Result)
        {
            if (!PluginConfig.enable_cache) return;
            MatchCacheItem result = null;
            try
            {
                lock (cache_lock) result = MatchCache.Where(x => x.Conditions == Conditions && x.Ident == ident && x.Root.Equals(root)).FirstOrDefault();
            }
            catch (Exception)
            {
            }
            if (result != null)
            {
                result.Result = Result;
                result.Created = DateTime.Now;
                return;
            }
            result = new MatchCacheItem() { Conditions = Conditions, Created = DateTime.Now, Root = root, Ident = ident, Result = Result };
            lock (cache_lock) MatchCache.Add(result);
        }
        public AutomationElement[] matches(AutomationElement root, int ident, AutomationElement element, int count, bool isDesktop, bool search_descendants)
        {
            var matchs = new List<AutomationElement>();
            var Conditions = Extensions.GetConditionsWithoutStar(this);
            if (isDesktop || !isDesktop)
            {
                var cache = GetFromCache(root, ident, Conditions.ToString());
                if (cache != null)
                {
                    Log.Selector("matches::FindAllChildren: found in AppWindowCache");
                    var result = new List<AutomationElement>();
                    foreach (var elementNode in cache)
                    {
                        if (Match(elementNode)) result.Add(elementNode);
                    }
                    return result.ToArray();
                }
            }
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            Log.SelectorVerbose("matches::FindAllChildren.isDesktop(" + isDesktop + ")::begin");

            if (search_descendants)
            {
                Log.SelectorVerbose("AutomationElement.matches: Searching for " + Conditions.ToString());
                AutomationElement[] elements = null;
                if (isDesktop)
                {
                    elements = element.FindAllChildren(Conditions);
                }
                else
                {
                    elements = element.FindAllDescendants(Conditions);
                }
                Log.SelectorVerbose(string.Format("AutomationElement.matches::found " + elements.Count() + " elements {0:mm\\:ss\\.fff}", sw.Elapsed));
                // var elements = element.FindAllChildren();
                foreach (var elementNode in elements)
                {
                    Log.SelectorVerbose("matches::match");
                    if (Match(elementNode) && matchs.Count < count) matchs.Add(elementNode);
                }
                Log.Selector(string.Format("AutomationElement.matches::complete, with " + elements.Count() + " elements {0:mm\\:ss\\.fff}", sw.Elapsed));
            }
            else
            {
                Log.SelectorVerbose(string.Format("AutomationElement.matches.FindAllChildren::begin"));
                var elements = element.FindAllChildren(Conditions);
                var manualcheck = Properties.Where(x => x.Enabled == true && x.Value != null && (x.Name == "IndexInParent" || x.Value.Contains("*"))).Count();
                if (manualcheck > 0)
                {
                    foreach (var elementNode in elements)
                    {
                        if (Match(elementNode) && matchs.Count < count) matchs.Add(elementNode);
                    }
                    Log.SelectorVerbose(string.Format("AutomationElement.matches.manualcheck(" + isDesktop + ")::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                }
                else
                {
                    //matchs.AddRange(elements);
                    foreach (var elementNode in elements) if (matchs.Count < count) matchs.Add(elementNode);
                    Log.SelectorVerbose(string.Format("AutomationElement.matches.puresearch::FindAllChildren::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                }
            }
            if ((isDesktop || !isDesktop) && matchs.Count > 0)
            {
                foreach (var e in matchs) { _ = e.Parent; }
                AddToCache(root, ident, Conditions.ToString(), matchs.ToArray());
            }
            Log.Selector(string.Format("matches::matches::complete {0:mm\\:ss\\.fff}", sw.Elapsed));
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
                        if (m.Properties.ControlType.IsSupported)
                        {
                            v = m.Properties.ControlType.ValueOrDefault.ToString();
                        }
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
