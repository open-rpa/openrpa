using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRPA.Interfaces;
using System.Management.Automation;
namespace OpenRPA.PS
{
    [Cmdlet(VerbsCommon.Set, "CurrentUser")]
    public class SetCurrentUser : OpenRPACmdlet
    {
        [Parameter()] public string Username { get; set; }
        [Parameter()] public string Password { get; set; }
        [Parameter()] public string JWT { get; set; }
        [Parameter()] public string WSURL { get; set; }
        [Parameter()] public SwitchParameter Save { get; set; }
        protected override async Task ProcessRecordAsync()
        {
            if (global.webSocketClient == null || !string.IsNullOrEmpty(WSURL))
            {
                if (global.webSocketClient != null)
                {
                    await global.webSocketClient.Close();
                    global.webSocketClient = null;
                }
                if (string.IsNullOrEmpty(WSURL)) WSURL = Config.local.wsurl;
                global.webSocketClient = OpenRPA.Net.WebSocketClient.Get(WSURL);
                await global.webSocketClient.Connect();
            }
            if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
            {
                await global.webSocketClient.Signin(Username, new System.Net.NetworkCredential("", Password).SecurePassword, "powershell");
            }
            else if (!string.IsNullOrEmpty(JWT))
            {
                await global.webSocketClient.Signin(JWT, "powershell");
            }
            else if (Config.local.jwt != null && Config.local.jwt.Length > 0)
            {
                await global.webSocketClient.Signin(Config.local.UnprotectString(Config.local.jwt), "powershell");
            }
            else if (!string.IsNullOrEmpty(Config.local.username) && Config.local.password != null && Config.local.password.Length > 0)
            {
                await global.webSocketClient.Signin(Config.local.username, Config.local.UnprotectString(Config.local.password), "powershell");
            }
            if (Save.IsPresent)
            {
                Config.local.wsurl = WSURL;
                var longjwt = await global.webSocketClient.Signin(true, true, "powershell");
                if (!string.IsNullOrEmpty(longjwt))
                {
                    Config.local.jwt = Config.local.ProtectString(longjwt);
                    Config.Save();
                }
            }
        }
    }
}
