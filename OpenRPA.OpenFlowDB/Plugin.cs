using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OpenRPA.OpenFlowDB
{
    public class Plugin : IPlugin
    {
        public static IOpenRPAClient client;
        public UserControl editor => null;
        public string Name => "OpenFlowDB";
        public void Initialize(IOpenRPAClient client)
        {
            Plugin.client = client;
        }
    }
}
