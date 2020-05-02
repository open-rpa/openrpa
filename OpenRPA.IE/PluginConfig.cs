using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.IE
{
    class PluginConfig
    {
        private static string pluginname => "IE";
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
        public static bool enable_xpath_support { get { return globallocal.GetProperty(pluginname, false); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool enable_caching_browser { get { return globallocal.GetProperty(pluginname, false); } set { globallocal.SetProperty(pluginname, value); } }
    }
}
