using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.NM
{
    class PluginConfig
    {
        private static string pluginname => "NM";
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
        public static bool wait_for_tab_after_set_value { get { return globallocal.GetProperty(pluginname, false); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool wait_for_tab_click { get { return globallocal.GetProperty(pluginname, false); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool compensate_for_old_addon { get { return globallocal.GetProperty(pluginname, false); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool debug_console_output { get { return globallocal.GetProperty(pluginname, false); } set { globallocal.SetProperty(pluginname, value); } }
        public static string[] unique_xpath_ids { get { return globallocal.GetProperty(pluginname, new string[] { "ng-model", "ng-reflect-name", "data-control-name" }); } set { globallocal.SetProperty(pluginname, value); } }
    }
}
