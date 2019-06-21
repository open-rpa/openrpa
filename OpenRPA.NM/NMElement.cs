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
        public System.Drawing.Rectangle Rectangle
        {
            get
            {
                return new System.Drawing.Rectangle(X, Y, Width, Height);
            }
        }
        public NMElement Parent { get; set; }
        public bool SupportInput {
            get
            {
                if (tagname.ToLower() != "input" && tagname.ToLower() != "select") return false;
                if(tagname.ToLower() == "input")
                {
                    if (type.ToLower() == "text" || type.ToLower() == "password") return true;
                    return false;
                } else
                {
                    return true;
                }
                
            }
        }
        private Dictionary<string, object> chromeelement { get; set; }
        public string xpath { get; set; }
        public string cssselector { get; set; }
        public string tagname { get; set; }
        public string classname { get; set; }
        public long zn_id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        private void parseChromeString(string _chromeelement)
        {
            if (string.IsNullOrEmpty(_chromeelement)) return;
            {
                JObject c = null;
                try
                {
                    c = JObject.Parse(_chromeelement);
                }
                catch (Exception)
                {
                }
                if(c == null)
                {
                    var a = JArray.Parse(_chromeelement);
                }
                
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
            if(string.IsNullOrEmpty(xpath)) xpath = message.xPath;
            if (string.IsNullOrEmpty(cssselector)) cssselector = message.cssPath;
            X = message.uix;
            Y = message.uiy;
            Width = message.uiwidth;
            Height = message.uiheight;
        }
        public NMElement(NativeMessagingMessage message, string _chromeelement)
        {
            this.message = message;
            parseChromeString(_chromeelement);
        }
        object IElement.RawElement { get => message; set => message = value as NativeMessagingMessage; }
        public string Value
        {
            get
            {
                if (chromeelement.ContainsKey("value")) return chromeelement["value"].ToString();
                if (chromeelement.ContainsKey("innertext")) return chromeelement["innertext"].ToString();
                return null;
            }
            set
            {
                if (NMHook.connected)
                {
                    var tab = NMHook.tabs.Where(x => x.id == message.tabid).FirstOrDefault();
                    if (tab == null) throw new ElementNotFoundException("Unknown tabid " + message.tabid);
                    NMHook.HighlightTab(tab);

                    var updateelement = new NativeMessagingMessage("updateelementvalue");
                    updateelement.browser = message.browser;
                    updateelement.cssPath = cssselector;
                    updateelement.xPath = xpath;
                    updateelement.tabid = message.tabid;
                    updateelement.frameId = message.frameId;
                    updateelement.data = value;
                    var subsubresult = NMHook.sendMessageResult(updateelement, true, TimeSpan.FromSeconds(2));
                    if (subsubresult == null) throw new Exception("Failed clicking html element");
                    System.Threading.Thread.Sleep(500);
                    NMHook.WaitForTab(updateelement.tabid, updateelement.browser, TimeSpan.FromSeconds(5));
                    return;
                }
            }
        }
        public void Click(bool VirtualClick, Input.MouseButton Button, int OffsetX, int OffsetY)
        {
            if (Button != Input.MouseButton.Left) { VirtualClick = false; }
            if (!VirtualClick)
            {
                Log.Debug("MouseMove to " + Rectangle.X + "," + Rectangle.Y + " and click");
                //Input.InputDriver.Instance.MouseMove(Rectangle.X + OffsetX, Rectangle.Y + OffsetY);
                //Input.InputDriver.DoMouseClick();
                var point = new FlaUI.Core.Shapes.Point(Rectangle.X + OffsetX, Rectangle.Y + OffsetY);
                //FlaUI.Core.Input.Mouse.MoveTo(Rectangle.X + OffsetX, Rectangle.Y + OffsetY);
                FlaUI.Core.Input.MouseButton flabuttun = FlaUI.Core.Input.MouseButton.Left;
                if (Button == Input.MouseButton.Middle) flabuttun = FlaUI.Core.Input.MouseButton.Middle;
                if (Button == Input.MouseButton.Right) flabuttun = FlaUI.Core.Input.MouseButton.Right;
                FlaUI.Core.Input.Mouse.Click(flabuttun, point);
                Log.Debug("Click done");
                return;
            }
            bool virtualClick = true;
            //int OffsetX = 0;
            //int OffsetY = 0;
            bool AnimateMouse = false;
            bool DoubleClick = false;
            FlaUI.Core.Input.MouseButton button = FlaUI.Core.Input.MouseButton.Left; 
            NMHook.checkForPipes(true, true);
            if (NMHook.connected)
            {
                if (virtualClick)
                {
                    NativeMessagingMessage subsubresult = null;
                    var getelement2 = new NativeMessagingMessage("clickelement");
                    getelement2.browser = message.browser;
                    getelement2.cssPath = cssselector;
                    getelement2.xPath = xpath;
                    getelement2.tabid = message.tabid;
                    getelement2.frameId = message.frameId;
                    subsubresult = NMHook.sendMessageResult(getelement2, true, TimeSpan.FromSeconds(2));
                    if (subsubresult == null) throw new Exception("Failed clicking html element");
                    System.Threading.Thread.Sleep(500);
                    NMHook.WaitForTab(getelement2.tabid, getelement2.browser, TimeSpan.FromSeconds(5));
                    return;
                }
                NativeMessagingMessage subresult = null;
                var getelement = new NativeMessagingMessage("getelement");
                getelement.browser = message.browser;
                getelement.cssPath = cssselector;
                getelement.xPath = xpath;
                getelement.tabid = message.tabid;
                getelement.frameId = message.frameId;
                if (NMHook.connected) subresult = NMHook.sendMessageResult(getelement, true, TimeSpan.FromSeconds(2));
                if (subresult == null) throw new Exception("Failed clicking html element");
                int hitx = subresult.uix;
                int hity = subresult.uiy;
                int width = subresult.uiwidth;
                int height = subresult.uiheight;
                hitx = hitx + OffsetX;
                hity = hity + OffsetY;
                if ((OffsetX == 0 && OffsetY == 0) || hitx < 0 || hity < 0)
                {
                    hitx = hitx + (width / 2);
                    hity = hity + (height / 2);
                }
                if (AnimateMouse) FlaUI.Core.Input.Mouse.MoveTo(new FlaUI.Core.Shapes.Point(hitx, hity));
                if (DoubleClick)
                {
                    FlaUI.Core.Input.Mouse.DoubleClick(button, new FlaUI.Core.Shapes.Point(hitx, hity));
                }
                else
                {
                    FlaUI.Core.Input.Mouse.Click(button, new FlaUI.Core.Shapes.Point(hitx, hity));
                }
                System.Threading.Thread.Sleep(500);
                NMHook.WaitForTab(getelement.tabid, getelement.browser, TimeSpan.FromSeconds(5));
            }
        }
        public void Focus()
        {
        }
        public Task _Highlight(System.Drawing.Color Color, TimeSpan Duration)
        {
            using (Interfaces.Overlay.OverlayWindow _overlayWindow = new Interfaces.Overlay.OverlayWindow())
            {
                _overlayWindow.Visible = true;
                _overlayWindow.SetTimeout(Duration);
                _overlayWindow.Bounds = Rectangle;
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                do
                {
                    System.Threading.Thread.Sleep(10);
                    _overlayWindow.TopMost = true;
                } while (_overlayWindow.Visible && sw.Elapsed < Duration);
                return Task.CompletedTask;
            }
        }
        public Task Highlight(bool Blocking, Color Color, TimeSpan Duration)
        {
            if (!Blocking)
            {
                Task.Run(() => _Highlight(Color, Duration));
                return Task.CompletedTask;
            }
            return _Highlight(Color, Duration);

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
            var AddedWidth = 10;
            var AddedHeight = 10;
            var ScreenImageWidth = Rectangle.Width + AddedWidth;
            var ScreenImageHeight = Rectangle.Height + AddedHeight;
            var ScreenImagex = Rectangle.X - (AddedWidth / 2);
            var ScreenImagey = Rectangle.Y - (AddedHeight / 2);
            if (ScreenImagex < 0) ScreenImagex = 0; if (ScreenImagey < 0) ScreenImagey = 0;
            using (var image = Interfaces.Image.Util.Screenshot(ScreenImagex, ScreenImagey, ScreenImageWidth, ScreenImageHeight, Interfaces.Image.Util.ActivityPreviewImageWidth, Interfaces.Image.Util.ActivityPreviewImageHeight))
            {
                // Interfaces.Image.Util.SaveImageStamped(image, System.IO.Directory.GetCurrentDirectory(), "NMElement");
                return Interfaces.Image.Util.Bitmap2Base64(image);
            }
        }
    }
}
