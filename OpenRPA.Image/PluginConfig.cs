using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Image
{
    class PluginConfig
    {
        private static string pluginname => "Image";
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
        public static int recording_mouse_move_time { get { return globallocal.GetProperty(pluginname, 350); } set { globallocal.SetProperty(pluginname, value); } }
    }
}
