using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    [Serializable]
    public class NativeMessagingMessageWindow
    {
        public NativeMessagingMessageWindow(NativeMessagingMessage msg)
        {
            browser = msg.browser;
            id = msg.windowId;
            try
            {
                if(msg.result == null || string.IsNullOrEmpty(msg.result.ToString()))
                {
                    Log.Warning("parsing NMElement that is not an html element (functionName: " + msg.functionName + ")");
                    return;
                }
                var obj = JObject.Parse(msg.result.ToString());
                if (obj.ContainsKey("alwaysOnTop")) alwaysOnTop = obj.Value<bool>("alwaysOnTop");
                if (obj.ContainsKey("focused")) focused = obj.Value<bool>("focused");
                if (obj.ContainsKey("heigh")) heigh = obj.Value<int>("heigh");
                if (obj.ContainsKey("incognito")) incognito = obj.Value<bool>("incognito");
                if (obj.ContainsKey("left")) left = obj.Value<int>("left");
                if (obj.ContainsKey("state")) state = obj.Value<string>("state");
                if (obj.ContainsKey("top")) top = obj.Value<int>("top");
                if (obj.ContainsKey("type")) type = obj.Value<string>("type");
                if (obj.ContainsKey("width")) width = obj.Value<int>("width");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public string browser { get; set; }
        public int id { get; set; }
        public bool alwaysOnTop { get; set; }
        public bool focused { get; set; }
        public int heigh { get; set; }
        public bool incognito { get; set; }
        public int left { get; set; }
        public string state { get; set; }
        public int top { get; set; }
        public string type { get; set; }
        public int width { get; set; }
    }
}
