using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.AviRecorder
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
        private static string pluginname => "avirecorder";

        public static bool enabled { get { return globallocal.GetProperty<bool>(pluginname, false); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool keepsuccessful { get { return globallocal.GetProperty<bool>(pluginname, false); } set { globallocal.SetProperty(pluginname, value); } }
        public static string codec { get { return globallocal.GetProperty<string>(pluginname, "motionjpeg"); } set { globallocal.SetProperty(pluginname, value); } }
        public static int quality { get { return globallocal.GetProperty<int>(pluginname, 70); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool stoponidle { get { return globallocal.GetProperty<bool>(pluginname, false); } set { globallocal.SetProperty(pluginname, value); } }
        public static string folder { get { return globallocal.GetProperty<string>(pluginname, ""); } set { globallocal.SetProperty(pluginname, value); } }


    }
}
