using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Windows
{
    public static class Extensions
    {
        public static AndCondition GetConditionsWithoutStar(this Interfaces.Selector.SelectorItem item)
        {
            using (var automation = Interfaces.AutomationUtil.getAutomation())
            {
                var cond = new List<ConditionBase>();
                foreach (var p in item.Properties.Where(x => x.Enabled == true && (x.Value != null && !x.Value.Contains("*"))))
                {
                    //if (p == "ControlType") cond.Add(element.ConditionFactory.ByControlType((ControlType)Enum.Parse(typeof(ControlType), ControlType)));
                    //if (p == "Name") cond.Add(element.ConditionFactory.ByName(Name));
                    //if (p == "ClassName") cond.Add(element.ConditionFactory.ByClassName(ClassName));
                    //if (p == "AutomationId") cond.Add(element.ConditionFactory.ByAutomationId(AutomationId));
                    var v = item.Properties.Where(x => x.Name == p.Name).FirstOrDefault();
                    if(v != null)
                    {
                        if (p.Name == "ControlType")
                        {
                            ControlType ct = (ControlType)Enum.Parse(typeof(ControlType), v.Value);
                            cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.ControlType, ct));
                        }
                        if (p.Name == "Name") cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.Name, v.Value));
                        if (p.Name == "ClassName") cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.ClassName, v.Value));
                        if (p.Name == "AutomationId") cond.Add(new PropertyCondition(automation.PropertyLibrary.Element.AutomationId, v.Value));
                    }
                }
                return new AndCondition(cond);
            }
        }
        public static string Title(this Interfaces.Selector.SelectorItem item)
        {
            var e = item.Properties.Where(x => x.Name == "Title").FirstOrDefault();
            if (e == null) return null;
            return e.Value;
        }
        public static bool Index(this Interfaces.Selector.SelectorItem item)
        {
            var e = item.Properties.Where(x => x.Name == "Index").FirstOrDefault();
            if (e == null || string.IsNullOrEmpty(e.Value)) return false;
            if (e.Value.ToLower() == "true") return true;
            return false;
        }
        public static bool SearchDescendants(this Interfaces.Selector.SelectorItem item)
        {
            var e = item.Properties.Where(x => x.Name == "SearchDescendants").FirstOrDefault();
            if (e == null) e = item.Properties.Where(x => x.Name == "search_descendants").FirstOrDefault();
            if (e == null) return PluginConfig.search_descendants;
            return bool.Parse(e.Value);
        }
        public static string processname(this Interfaces.Selector.SelectorItem item)
        {
            var e = item.Properties.Where(x => x.Name == "processname").FirstOrDefault();
            if (e == null) return null;
            return e.Value;
        }
        public static bool isImmersiveProcess(this Interfaces.Selector.SelectorItem item)
        {
            var e = item.Properties.Where(x => x.Name == "isImmersiveProcess").FirstOrDefault();
            if (e == null || string.IsNullOrEmpty(e.Value)) return false;
            if (e.Value.ToLower() == "true") return true;
            return false;
        }
        public static string ControlType(this Interfaces.Selector.SelectorItem item)
        {
            var e = item.Properties.Where(x => x.Name == "ControlType").FirstOrDefault();
            if (e == null) return null;
            return e.Value;
        }
        
    }
}
