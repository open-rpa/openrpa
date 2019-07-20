using OpenRPA.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public class global
    {
        public static WebSocketClient webSocketClient = null;
        public static bool isConnected
        {
            get
            {
                if (webSocketClient == null || !webSocketClient.isConnected) return false;
                return true;
            }
        }
    }
}
