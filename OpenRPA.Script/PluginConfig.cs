using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Script
{
    public class PluginConfig
    {
        private static string pluginname => "Script";
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
        public static bool csharp_intellisense { get { return globallocal.GetProperty(pluginname, true); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool vb_intellisense { get { return globallocal.GetProperty(pluginname, true); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool use_embedded_python { get { return globallocal.GetProperty(pluginname, true); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool py_create_no_window { get { return globallocal.GetProperty(pluginname, true); } set { globallocal.SetProperty(pluginname, value); } }
        public static string python_exe_path { get { return globallocal.GetProperty(pluginname, ""); } set { globallocal.SetProperty(pluginname, value); } }

    }
}
