using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Windows
{
    class PluginConfig
    {
        private static string pluginname => "Windows";
        private static Config _globallocal = null;
        public static Config globallocal
        {
            get
            {
                if (_globallocal == null)
                {
                    _globallocal = Config.local;
                }
                return _globallocal;
            }
        }
        public static bool allow_child_searching { get { return globallocal.GetProperty(pluginname, false); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool allow_multiple_hits_mid_selector { get { return globallocal.GetProperty(pluginname, false); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool get_elements_in_different_thread { get { return globallocal.GetProperty(pluginname, true); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool traverse_selector_both_ways { get { return globallocal.GetProperty(pluginname, true); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool enum_selector_properties { get { return globallocal.GetProperty(pluginname, false); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool search_descendants { get { return globallocal.GetProperty(pluginname, false); } set { globallocal.SetProperty(pluginname, value); } }
        public static TimeSpan cache_timeout { get { return globallocal.GetProperty(pluginname, TimeSpan.FromMinutes(5)); } set { globallocal.SetProperty(pluginname, value); } }
        public static TimeSpan search_timeout { get { return globallocal.GetProperty(pluginname, TimeSpan.FromSeconds(5)); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool enable_cache { get { return globallocal.GetProperty(pluginname, true); } set { globallocal.SetProperty(pluginname, value); } }

    }
}
