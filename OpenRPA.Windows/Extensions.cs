using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Windows
{
    public static class Extensions
    {
        public static bool SearchDescendants(this Interfaces.Selector.SelectorItem item)
        {
            var e = item.Properties.Where(x => x.Name == "SearchDescendants").FirstOrDefault();
            if (e == null || string.IsNullOrEmpty(e.Value)) return false;
            if (e.Value.ToLower() == "true") return true;
            return false;
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

    }
}
