using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.TerminalEmulator
{
    class PluginConfig
    {
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
        private static string pluginname => "terminalemulator";
        public static bool auto_close { get { return globallocal.GetProperty<bool>(pluginname, true); } set { globallocal.SetProperty(pluginname, value); } }
    }
}
