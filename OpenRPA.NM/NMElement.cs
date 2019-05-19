using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.NM
{
    public class NMElement : IElement
    {
        public NativeMessagingMessage message { get; set; }

        public string Name { get; set; }
        public string id { get; set; }
        public string type { get; set; }
        public NMElement[] Children {
            get {
                var result = new List<NMElement>();
                if (chromeelement.ContainsKey("content"))
                {
                    var content = (JArray)chromeelement["content"];
                    foreach (var c in content)
                    {
                        try
                        {
                            if (c.HasValues == true)
                            {
                                //var _c = c.ToObject<Dictionary<string, object>>();
                                result.Add(new NMElement(message, c.ToString()));
                            }
                        }
                        catch (Exception)
                        {
                            //var b = true;
                            //chromeelement["innerText"] = c;
                        }

                    }
                }
                return result.ToArray();
            }
        }
        public NMElement Parent { get; set; }
        public bool SupportInput { get; set; }

        private Dictionary<string, object> chromeelement { get; set; }
        public string xpath { get; set; }
        public string cssselector { get; set; }
        public string tagname { get; set; }
        public string classname { get; set; }
        public long zn_id { get; set; }
        private void parseChromeString(string _chromeelement)
        {
            if (string.IsNullOrEmpty(_chromeelement)) return;
            {
                var c = JObject.Parse(_chromeelement);
                chromeelement = new Dictionary<string, object>();
                foreach (var kp in c )
                {
                    chromeelement.Add(kp.Key.ToLower(), kp.Value);
                }
                //if (chromeelement.ContainsKey("attributes"))
                //{
                //    JObject _c = (JObject)chromeelement["attributes"];
                //    var attributes = _c.ToObject<Dictionary<string, object>>();
                //    foreach (var a in attributes)
                //    {
                //        chromeelement[a.Key] = a.Value;
                //    }
                //}
                if (chromeelement.ContainsKey("name")) Name = chromeelement["name"].ToString();
                if (chromeelement.ContainsKey("id")) id = chromeelement["id"].ToString();
                if (chromeelement.ContainsKey("tagname")) tagname = chromeelement["tagname"].ToString();
                if (chromeelement.ContainsKey("classname")) classname = chromeelement["classname"].ToString();
                if (chromeelement.ContainsKey("type")) type = chromeelement["type"].ToString();
                if (chromeelement.ContainsKey("xpath")) xpath = chromeelement["xpath"].ToString();
                if (chromeelement.ContainsKey("cssselector")) cssselector = chromeelement["cssselector"].ToString();
                if (chromeelement.ContainsKey("zn_id")) zn_id = int.Parse(chromeelement["zn_id"].ToString());
            }
        }

        public NMElement(NativeMessagingMessage message)
        {
            parseChromeString(message.result);
            zn_id = message.zn_id;
            this.message = message;
            xpath = message.xPath;
            cssselector = message.cssPath;
            //nativebrowser = message.browser;
            //tabid = message.tabid;
            //frameId = message.frameId;
            //zn_id = message.zn_id;
            //uix = message.uix;
            //uiy = message.uiy;
            //uiwidth = message.uiwidth;
            //uiheight = message.uiheight;
            //var _chromeelement = message.result;
            //if (!string.IsNullOrEmpty(_chromeelement))
            //{
            //    var c = JObject.Parse(_chromeelement);
            //    this.chromeelement = c.ToObject<Dictionary<string, object>>();
            //    if (this.chromeelement.ContainsKey("xPath")) xpath = this.chromeelement["xPath"].ToString();
            //    if (this.chromeelement.ContainsKey("cssPath")) cssselector = this.chromeelement["cssPath"].ToString();
            //    //this.xpath = xpath;
            //    //this.cssselector = cssselector;
            //    if (chromeelement.ContainsKey("attributes"))
            //    {
            //        JObject _c = (JObject)chromeelement["attributes"];
            //        var attributes = _c.ToObject<Dictionary<string, object>>();
            //        foreach (var a in attributes)
            //        {
            //            chromeelement[a.Key] = a.Value;
            //        }
            //    }
            //}
            //if (zn_id == -1) throw new Exception("FAILED!");
        }
        public NMElement(NativeMessagingMessage message, string _chromeelement)
        {
            //this.tabid = tabid;
            //this.frameid = frameId;
            //this.nativebrowser = nativebrowser;
            this.message = message;
            parseChromeString(_chromeelement);
        }

        object IElement.RawElement { get => message; set => message = value as NativeMessagingMessage; }

        public void Click()
        {
        }

        public void Focus()
        {
        }

        public Task Highlight(bool Blocking, Color Color, TimeSpan Duration)
        {
            return Task.CompletedTask;
        }
        public override string ToString()
        {
            if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(Name)) return tagname;
            if (!string.IsNullOrEmpty(id) && string.IsNullOrEmpty(Name)) return tagname + " " + id;
            if (string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(Name)) return tagname + " " + Name;
            return "id:" + id + " TagName:" + tagname + " Name: " + Name;
        }

        public string ImageString()
        {
            return string.Empty;
            //throw new NotImplementedException();
        }
    }
}
