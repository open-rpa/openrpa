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
    }
}
