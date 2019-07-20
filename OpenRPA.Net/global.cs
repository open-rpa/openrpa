using Newtonsoft.Json;
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
        private static openflowconfig _openflowconfig = null;
        public static openflowconfig openflowconfig
        {
            get
            {
                if(_openflowconfig == null)
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
    }
}
