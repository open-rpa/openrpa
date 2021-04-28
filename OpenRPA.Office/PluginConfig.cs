using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Office
{
    class PluginConfig
    {
        private static string pluginname => "Office";
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
        public static int get_emails_max_folders { get { return globallocal.GetProperty(pluginname, 50); } set { globallocal.SetProperty(pluginname, value); } }
    }
}
