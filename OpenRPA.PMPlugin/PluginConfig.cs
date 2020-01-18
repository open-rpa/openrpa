using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.PMPlugin
{
    class PluginConfig
    {
        private static string pluginname => "PMPlugin";
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
        public static bool enabled_mouse_recording { get { return globallocal.GetProperty(pluginname, false); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool enabled_keyboard_recording { get { return globallocal.GetProperty(pluginname, false); } set { globallocal.SetProperty(pluginname, value); } }
        public static string collectionname { get { return globallocal.GetProperty(pluginname, "pm"); } set { globallocal.SetProperty(pluginname, value); } }

    }
}
