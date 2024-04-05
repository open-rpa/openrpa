using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Storage.LiteDB
{
    class PluginConfig
    {
        private static string pluginname => "StorageLiteDB";
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
        public static bool enabled { get { return globallocal.GetProperty(pluginname, true); } set { globallocal.SetProperty(pluginname, value); } }

    }
}
