using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public class global
    {
        public static Interfaces.IWebSocketClient webSocketClient = null;
        public static Interfaces.IOpenRPAClient OpenRPAClient;
        public static bool isConnected
        {
            get
            {
                if (webSocketClient == null || !webSocketClient.isConnected) return false;
                return true;
            }
        }
        public static bool isSignedIn
        {
            get
            {
                if (!isConnected || webSocketClient.user == null) return false;
                return true;
            }
        }
        private static openflowconfig _openflowconfig = null;
        public static openflowconfig openflowconfig
        {
            get
            {
                if (_openflowconfig == null)
                {
                    using (System.Net.WebClient wc = new System.Net.WebClient())
                    {
                        try
                        {
                            var baseurl = new Uri(Config.local.wsurl);
                            if (baseurl.Scheme == "wss") baseurl = new Uri(Config.local.wsurl.Replace("wss:", "https:"));
                            if (baseurl.Scheme == "ws") baseurl = new Uri(Config.local.wsurl.Replace("ws:", "http:"));

                            var url = new Uri(baseurl, "/config");
                            var json = wc.DownloadString(url);
                            _openflowconfig = JsonConvert.DeserializeObject<openflowconfig>(json);
                            _openflowconfig.baseurl = baseurl.ToString();
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                return _openflowconfig;
            }
        }
        public static string CurrentDirectory
        {
            get
            {
                var asm = System.Reflection.Assembly.GetEntryAssembly();
                if (asm == null) asm = System.Reflection.Assembly.GetCallingAssembly();
                var filepath = asm.CodeBase.Replace("file:///", "");
                var path = System.IO.Path.GetDirectoryName(filepath);
                return path;
                // return Environment.CurrentDirectory;
            }
        }
        public static string version
        {
            get
            {
                try
                {
                    var asm = System.Reflection.Assembly.GetEntryAssembly();
                    if (asm == null) asm = System.Reflection.Assembly.GetCallingAssembly();
                    var _v = asm.GetName().Version;
                    var _version = _v.Major + "." + _v.Minor + "." + _v.Build;
                    return _version;
                }
                catch (Exception)
                {
                }
                return "0.0.1";
            }
        }
    }
    public class openflowconfig
    {
        public string wshost { get; set; }
        public string domain { get; set; }
        public bool allow_user_registration { get; set; }
        public bool allow_personal_nodered { get; set; }
        public string @namespace { get; set; }
        public string nodered_domain_schema { get; set; }
        public string baseurl { get; set; }
        public string getting_started_url { get; set; }
        public int websocket_package_size { get; set; }
        public string version { get; set; }
        public bool supports_watch { get; set; }
    }
}
