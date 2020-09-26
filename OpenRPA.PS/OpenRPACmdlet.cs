using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.PS
{
    public class OpenRPACmdlet : AsyncCmdlet
    {
        internal static string[] _Collections = null;
        protected async Task Initialize()
        {
            if (global.webSocketClient == null)
            {
                global.webSocketClient = new OpenRPA.Net.WebSocketClient(Config.local.wsurl);
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

        protected async override Task BeginProcessingAsync()
        {
            await Initialize();
            await base.BeginProcessingAsync();
        }
    }
}
