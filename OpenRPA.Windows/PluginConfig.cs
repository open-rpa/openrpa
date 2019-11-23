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
        public static bool allow_child_searching { get { return globallocal.GetProperty<bool>(pluginname, false); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool allow_multiple_hits_mid_selector { get { return globallocal.GetProperty<bool>(pluginname, false); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool enum_properties { get { return globallocal.GetProperty<bool>(pluginname, false); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool get_elements_in_different_thread { get { return globallocal.GetProperty<bool>(pluginname, false); } set { globallocal.SetProperty(pluginname, value); } }
    }
}
