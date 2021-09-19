using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.PS
{
    public class OpenRPACmdlet : AsyncCmdlet
    {
        internal static string[] _Collections = null;
        private static bool hasOpenRPAConfig()
        {
            var filename = Config.SettingsFile;
            if (System.IO.File.Exists(filename)) return true;
            return false;
        }
        protected async Task Initialize()
        {
            if (hasOpenRPAConfig() && NetworkInterface.GetIsNetworkAvailable() && new Ping().Send(new IPAddress(new byte[] { 8, 8, 8, 8 }), 2000).Status == IPStatus.Success)
            {
                if (global.webSocketClient == null)
                {
                    global.webSocketClient = OpenRPA.Net.WebSocketClient.Get(Config.local.wsurl);
                    global.webSocketClient.OnClose += WebSocketClient_OnClose;
                }
                if (!global.webSocketClient.isConnected)
                {
                    await global.webSocketClient.Connect();
                }
                if (global.webSocketClient.isConnected && global.webSocketClient.user == null)
                {
                    try
                    {
                        if (Config.local.jwt != null && Config.local.jwt.Length > 0)
                        {
                            await global.webSocketClient.Signin(Config.local.UnprotectString(Config.local.jwt), "powershell");
                        }
                        else if (!string.IsNullOrEmpty(Config.local.username) && Config.local.password != null && Config.local.password.Length > 0)
                        {
                            await global.webSocketClient.Signin(Config.local.username, Config.local.UnprotectString(Config.local.password), "powershell");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        throw;
                    }
                }
                if (_Collections == null)
                {
                    _Collections = (await global.webSocketClient.ListCollections()).Select(x => x.name).ToArray();
                    if (!_Collections.Contains("files")) _Collections = _Collections.Concat(new string[] { "files" }).ToArray();
                }

            }
        }
        private void WebSocketClient_OnClose(string obj)
        {
            psqueue = null;
            global.webSocketClient.Close();
        }
        protected async override Task BeginProcessingAsync()
        {
            await Initialize();
            await base.BeginProcessingAsync();
        }
        internal static string psqueue = null;
        internal async Task RegisterQueue()
        {
            if (psqueue == null && global.webSocketClient != null)
            {
                psqueue = await global.webSocketClient.RegisterQueue(null);
            }

        }
    }
}
