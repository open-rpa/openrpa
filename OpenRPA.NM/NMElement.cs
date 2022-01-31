using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
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
        public NMElement[] Children
        {
            get
            {
                var result = new List<NMElement>();
                if (Attributes.ContainsKey("content"))
                {
                    if (!(Attributes["content"] is JArray content)) return result.ToArray();
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
            set { }
        }
        public NMElement Parent { get; set; }
        public bool SupportInput
        {
            get
            {
                if (tagname.ToLower() != "input" && tagname.ToLower() != "select") return false;
                if (tagname.ToLower() == "input")
                {
                    if (type == null) return true;
                    if (type.ToLower() == "text" || type.ToLower() == "password") return true;
                    return false;
                }
                else
                {
                    return true;
                }

            }
        }
        public Dictionary<string, object> Attributes { get; set; }
        public string xpath { get; set; }
        public string cssselector { get; set; }
        public string tagname { get; set; }
        public string classname { get; set; }
        public bool IsVisible { get; set; }
        public string Display { get; set; }
        public bool isVisibleOnScreen { get; set; }
        public bool Disabled { get; set; }
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
                Attributes = new Dictionary<string, object>();
                if (c != null)
                    foreach (var kp in c)
                    {
                        if (Attributes.ContainsKey(kp.Key.ToLower()))
                        {
                            Attributes[kp.Key.ToLower()] = kp.Value;
                        }
                        else
                        {
                            Attributes.Add(kp.Key.ToLower(), kp.Value);
                        }

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
                if (Attributes.ContainsKey("name")) Name = Attributes["name"].ToString();
                if (Attributes.ContainsKey("id")) id = Attributes["id"].ToString();
                if (Attributes.ContainsKey("tagname")) tagname = Attributes["tagname"].ToString();
                if (Attributes.ContainsKey("classname")) classname = Attributes["classname"].ToString();
                if (Attributes.ContainsKey("type")) type = Attributes["type"].ToString();
                if (Attributes.ContainsKey("xpath")) xpath = Attributes["xpath"].ToString();
                if (Attributes.ContainsKey("cssselector")) cssselector = Attributes["cssselector"].ToString();
                if (Attributes.ContainsKey("cssPath")) cssselector = Attributes["cssPath"].ToString();
                if (Attributes.ContainsKey("csspath")) cssselector = Attributes["csspath"].ToString();
                if (Attributes.ContainsKey("zn_id")) zn_id = int.Parse(Attributes["zn_id"].ToString());

                if (Attributes.ContainsKey("isvisible")) IsVisible = bool.Parse(Attributes["isvisible"].ToString());
                if (Attributes.ContainsKey("display")) Display = Attributes["display"].ToString();
                if (Attributes.ContainsKey("isvisibleonscreen")) isVisibleOnScreen = bool.Parse(Attributes["isvisibleonscreen"].ToString());
                if (Attributes.ContainsKey("disabled")) Disabled = bool.Parse(Attributes["disabled"].ToString());
                if (string.IsNullOrEmpty(Name) && tagname == "OPTION" && Attributes.ContainsKey("content"))
                {
                    Name = Text;
                }
            }
        }
        public NMElement(NativeMessagingMessage message)
        {
            parseChromeString(message.result.ToString());
            _browser = message.browser;
            zn_id = message.zn_id;
            this.message = message;
            if (string.IsNullOrEmpty(xpath)) xpath = message.xPath;
            if (string.IsNullOrEmpty(cssselector)) cssselector = message.cssPath;
            X = message.uix;
            Y = message.uiy;
            Width = message.uiwidth;
            Height = message.uiheight;
        }
        public NMElement(NativeMessagingMessage message, string _chromeelement)
        {
            this.message = message;
            _browser = message.browser;
            parseChromeString(_chromeelement);
        }
        [Newtonsoft.Json.JsonIgnore]
        private readonly string _browser = null;
        public string browser
        {
            get
            {
                return _browser;
            }
        }
        object IElement.RawElement { get => message; set => message = value as NativeMessagingMessage; }
        public string Text
        {
            get
            {
                if (!string.IsNullOrEmpty(tagname) && tagname.ToLower() == "select")
                {
                    if (Attributes.ContainsKey("text")) return Attributes["text"].ToString();
                    if (Attributes.ContainsKey("innertext")) return Attributes["innertext"].ToString();
                }
                if (Attributes.ContainsKey("text")) return Attributes["text"].ToString();
                if (Attributes.ContainsKey("innertext")) return Attributes["innertext"].ToString();
                if (!hasRefreshed)
                {
                    Refresh();
                    if (Attributes.ContainsKey("text")) return Attributes["text"].ToString();
                    if (Attributes.ContainsKey("innertext")) return Attributes["innertext"].ToString();
                }
                return null;
            }
            set
            {
                if (NMHook.connected)
                {

                    if (tagname.ToLower() == "select")
                    {
                        foreach (var item in Children)
                        {
                            if ((!string.IsNullOrEmpty(item.Text) && !string.IsNullOrEmpty(value) && item.Text.ToLower() == value.ToLower()) || (string.IsNullOrEmpty(item.Text) && string.IsNullOrEmpty(value)))
                            {
                                Value = item.Value;
                                return;
                            }
                        }
                    }
                }
            }
        }
        public string SendText
        {
            get
            {
                string result = null;
                if (Attributes.ContainsKey("value")) result = Attributes["value"].ToString();
                if (Attributes.ContainsKey("innertext") && string.IsNullOrEmpty(result)) result = Attributes["innertext"].ToString();
                if (string.IsNullOrEmpty(result)) result = Text;
                return result;
            }
            set
            {
                if (NMHook.connected)
                {
                    var tab = NMHook.FindTabById(browser, message.tabid);
                    if (tab == null) throw new ElementNotFoundException("Unknown tabid " + message.tabid);
                    var updateelement = new NativeMessagingMessage("sendtext", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids)
                    {
                        browser = message.browser,
                        zn_id = zn_id,
                        tabid = message.tabid,
                        frameId = message.frameId,
                        data = Interfaces.Extensions.Base64Encode(value),
                        result = "value"
                    };
                    var temp = Interfaces.Extensions.Base64Decode(updateelement.data);
                    if (value == null) updateelement.data = null;
                    var subsubresult = NMHook.sendMessageResult(updateelement, PluginConfig.protocol_timeout);
                    if (subsubresult == null) throw new Exception("Failed setting html element value");
                    if (PluginConfig.wait_for_tab_after_set_value)
                    {
                        NMHook.WaitForTab(updateelement.tabid, updateelement.browser, TimeSpan.FromSeconds(5));
                    }
                    return;
                }
            }
        }
        public string SetText
        {
            get
            {
                string result = null;
                if (Attributes.ContainsKey("value")) result = Attributes["value"].ToString();
                if (Attributes.ContainsKey("innertext") && string.IsNullOrEmpty(result)) result = Attributes["innertext"].ToString();
                if (string.IsNullOrEmpty(result)) result = Text;
                return result;
            }
            set
            {
                if (NMHook.connected)
                {
                    var tab = NMHook.FindTabById(browser, message.tabid);
                    if (tab == null) throw new ElementNotFoundException("Unknown tabid " + message.tabid);
                    var updateelement = new NativeMessagingMessage("settext", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids)
                    {
                        browser = message.browser,
                        zn_id = zn_id,
                        tabid = message.tabid,
                        frameId = message.frameId,
                        data = Interfaces.Extensions.Base64Encode(value),
                        result = "value"
                    };
                    var temp = Interfaces.Extensions.Base64Decode(updateelement.data);
                    if (value == null) updateelement.data = null;
                    var subsubresult = NMHook.sendMessageResult(updateelement, PluginConfig.protocol_timeout);
                    if (subsubresult == null) throw new Exception("Failed setting html element value");
                    if (PluginConfig.wait_for_tab_after_set_value)
                    {
                        NMHook.WaitForTab(updateelement.tabid, updateelement.browser, TimeSpan.FromSeconds(5));
                    }
                    return;
                }
            }
        }
        public string SendKeys
        {
            get
            {
                string result = null;
                if (Attributes.ContainsKey("value")) result = Attributes["value"].ToString();
                if (Attributes.ContainsKey("innertext") && string.IsNullOrEmpty(result)) result = Attributes["innertext"].ToString();
                if (string.IsNullOrEmpty(result)) result = Text;
                return result;
            }
            set
            {
                if (NMHook.connected)
                {
                    var tab = NMHook.FindTabById(browser, message.tabid);
                    if (tab == null) throw new ElementNotFoundException("Unknown tabid " + message.tabid);
                    var updateelement = new NativeMessagingMessage("sendkeys", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids)
                    {
                        browser = message.browser,
                        zn_id = zn_id,
                        tabid = message.tabid,
                        frameId = message.frameId,
                        data = Interfaces.Extensions.Base64Encode(value),
                        result = "value"
                    };
                    var temp = Interfaces.Extensions.Base64Decode(updateelement.data);
                    if (value == null) updateelement.data = null;
                    var subsubresult = NMHook.sendMessageResult(updateelement, PluginConfig.protocol_timeout);
                    if (subsubresult == null) throw new Exception("Failed setting html element value");
                    if (PluginConfig.wait_for_tab_after_set_value)
                    {
                        NMHook.WaitForTab(updateelement.tabid, updateelement.browser, TimeSpan.FromSeconds(5));
                    }
                    return;
                }
            }
        }
        public string[] Values
        {
            get
            {
                string[] result = new string[] { };
                if (Attributes.ContainsKey("values"))
                {
                    var json = Attributes["values"].ToString();
                    result = JsonConvert.DeserializeObject<string[]>(json);
                }
                if (result == null || result.Length == 0)
                {
                    if (!string.IsNullOrEmpty(Value))
                    {
                        result = new string[] { Value };
                    }
                }
                return result;
            }
            set
            {
                if (NMHook.connected)
                {
                    var tab = NMHook.FindTabById(browser, message.tabid);
                    if (tab == null) throw new ElementNotFoundException("Unknown tabid " + message.tabid);
                    // NMHook.HighlightTab(tab);

                    var updateelement = new NativeMessagingMessage("updateelementvalues", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids)
                    {
                        browser = message.browser,
                        //cssPath = cssselector,
                        //xPath = xpath,
                        //tabid = message.tabid,
                        //frameId = message.frameId,
                        //data = value
                        zn_id = zn_id,
                        tabid = message.tabid,
                        frameId = message.frameId,
                        data = Interfaces.Extensions.Base64Encode(JsonConvert.SerializeObject(value))
                    };
                    var subsubresult = NMHook.sendMessageResult(updateelement, PluginConfig.protocol_timeout);
                    if (subsubresult == null) throw new Exception("Failed setting html element value");
                    //System.Threading.Thread.Sleep(500);
                    if (PluginConfig.wait_for_tab_after_set_value)
                    {
                        NMHook.WaitForTab(updateelement.tabid, updateelement.browser, PluginConfig.protocol_timeout);
                    }
                    return;
                }
            }
        }
        public string innerHTML
        {
            get
            {
                string result = null;
                if (!Attributes.ContainsKey("innerhtml"))
                {
                    var getelement2 = new NativeMessagingMessage("getelement", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids)
                    {
                        browser = message.browser,
                        zn_id = zn_id,
                        tabid = message.tabid,
                        frameId = message.frameId,
                        data = "innerhtml"
                    };
                    NativeMessagingMessage subsubresult = NMHook.sendMessageResult(getelement2, PluginConfig.protocol_timeout);
                    if (subsubresult == null) throw new Exception("Failed locating element again (zn_id " + zn_id + ")");
                    parseChromeString(subsubresult.result.ToString());
                    if (Attributes.ContainsKey("innerhtml"))
                    {
                        result = Attributes["innerhtml"].ToString();
                        return result;
                    }

                }
                if (Attributes.ContainsKey("innerhtml") && string.IsNullOrEmpty(result)) result = Attributes["innerhtml"].ToString();
                if (Attributes.ContainsKey("value")) result = Attributes["value"].ToString();
                if (Attributes.ContainsKey("innertext") && string.IsNullOrEmpty(result)) result = Attributes["innertext"].ToString();
                if (string.IsNullOrEmpty(result)) result = Text;
                return result;
            }
            set
            {
                if (NMHook.connected)
                {
                    var tab = NMHook.FindTabById(browser, message.tabid);
                    if (tab == null) throw new ElementNotFoundException("Unknown tabid " + message.tabid);
                    // NMHook.HighlightTab(tab);

                    var updateelement = new NativeMessagingMessage("updateelementvalue", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids)
                    {
                        browser = message.browser,
                        //cssPath = cssselector,
                        //xPath = xpath,
                        //tabid = message.tabid,
                        //frameId = message.frameId,
                        //data = value
                        zn_id = zn_id,
                        tabid = message.tabid,
                        frameId = message.frameId,
                        data = Interfaces.Extensions.Base64Encode(value),
                        result = "innerhtml"
                    };
                    var temp = Interfaces.Extensions.Base64Decode(updateelement.data);
                    if (value == null) updateelement.data = null;
                    var subsubresult = NMHook.sendMessageResult(updateelement, PluginConfig.protocol_timeout);
                    if (subsubresult == null) throw new Exception("Failed setting html element value");
                    //System.Threading.Thread.Sleep(500);
                    if (PluginConfig.wait_for_tab_after_set_value)
                    {
                        NMHook.WaitForTab(updateelement.tabid, updateelement.browser, TimeSpan.FromSeconds(5));
                    }
                    return;
                }
            }
        }
        public string Value
        {
            get
            {
                string result = null;
                if (Attributes.ContainsKey("value")) result = Attributes["value"].ToString();
                if (Attributes.ContainsKey("innertext") && string.IsNullOrEmpty(result)) result = Attributes["innertext"].ToString();
                if (string.IsNullOrEmpty(result)) result = Text;
                return result;
            }
            set
            {
                if (NMHook.connected)
                {
                    var tab = NMHook.FindTabById(browser, message.tabid);
                    if (tab == null) throw new ElementNotFoundException("Unknown tabid " + message.tabid);
                    // NMHook.HighlightTab(tab);

                    var updateelement = new NativeMessagingMessage("updateelementvalue", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids)
                    {
                        browser = message.browser,
                        //cssPath = cssselector,
                        //xPath = xpath,
                        //tabid = message.tabid,
                        //frameId = message.frameId,
                        //data = value
                        zn_id = zn_id,
                        tabid = message.tabid,
                        frameId = message.frameId,
                        data = Interfaces.Extensions.Base64Encode(value),
                        result = "value"
                    };
                    var temp = Interfaces.Extensions.Base64Decode(updateelement.data);
                    if (value == null) updateelement.data = null;
                    var subsubresult = NMHook.sendMessageResult(updateelement, PluginConfig.protocol_timeout);
                    if (subsubresult == null) throw new Exception("Failed setting html element value");
                    if (PluginConfig.wait_for_tab_after_set_value)
                    {
                        NMHook.WaitForTab(updateelement.tabid, updateelement.browser, TimeSpan.FromSeconds(5));
                    }
                    return;
                }
            }
        }
        public void Click(bool VirtualClick, Input.MouseButton Button, int OffsetX, int OffsetY, bool DoubleClick, bool AnimateMouse)
        {
            if (Button != Input.MouseButton.Left) { VirtualClick = false; }
            if (type == "file") { VirtualClick = false; }
            if (!VirtualClick)
            {
                if (AnimateMouse)
                {
                    FlaUI.Core.Input.Mouse.MoveTo(new System.Drawing.Point(Rectangle.X + OffsetX, Rectangle.Y + OffsetY));
                }
                else
                {
                    NativeMethods.SetCursorPos(Rectangle.X + OffsetX, Rectangle.Y + OffsetY);
                }
                Log.Debug("Click at  " + OffsetX + "," + OffsetY + " in area " + Rectangle.ToString());
                Input.InputDriver.Click(Button);
                if (DoubleClick) Input.InputDriver.Click(Button);
                return;
            }
            bool virtualClick = true;
            NMHook.checkForPipes(true, true, true);
            if (NMHook.connected)
            {
                if (virtualClick)
                {
                    var getelement2 = new NativeMessagingMessage("clickelement", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids)
                    {
                        browser = message.browser,
                        //cssPath = cssselector,
                        //xPath = xpath,
                        zn_id = zn_id,
                        tabid = message.tabid,
                        frameId = message.frameId
                    };
                    NativeMessagingMessage subsubresult = NMHook.sendMessageResult(getelement2, PluginConfig.protocol_timeout);
                    if (subsubresult == null) throw new Exception("Failed clicking html element");
                    //System.Threading.Thread.Sleep(500);
                    if (PluginConfig.wait_for_tab_click)
                    {
                        NMHook.WaitForTab(getelement2.tabid, getelement2.browser, TimeSpan.FromSeconds(5));
                    }
                    return;
                }
                NativeMessagingMessage subresult = null;
                var getelement = new NativeMessagingMessage("getelement", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids)
                {
                    browser = message.browser,
                    zn_id = zn_id,
                    //cssPath = cssselector,
                    //xPath = xpath,
                    tabid = message.tabid,
                    frameId = message.frameId
                };
                if (NMHook.connected) subresult = NMHook.sendMessageResult(getelement, PluginConfig.protocol_timeout);
                if (subresult == null) throw new Exception("Failed clicking html element, element not found");
                int hitx = subresult.uix;
                int hity = subresult.uiy;
                int width = subresult.uiwidth;
                int height = subresult.uiheight;
                hitx += OffsetX;
                hity += OffsetY;
                if ((OffsetX == 0 && OffsetY == 0) || hitx < 0 || hity < 0)
                {
                    hitx += (width / 2);
                    hity += (height / 2);
                }
                if (AnimateMouse)
                {
                    FlaUI.Core.Input.Mouse.MoveTo(new Point(hitx, hity));
                }
                else
                {
                    NativeMethods.SetCursorPos(hitx, hity);
                }
                Input.InputDriver.Click(Button);
                if (DoubleClick) Input.InputDriver.Click(Button);
                //System.Threading.Thread.Sleep(500);
                if (PluginConfig.wait_for_tab_click)
                {
                    NMHook.WaitForTab(getelement.tabid, getelement.browser, TimeSpan.FromSeconds(5));
                }

            }
        }
        public void ClickAndWait()
        {
            NMHook.checkForPipes(true, true, true);
            if (NMHook.connected)
            {
                var tab = NMHook.GetCurrentTab(browser);
                var getelement2 = new NativeMessagingMessage("clickelement", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids)
                {
                    browser = message.browser,
                    //cssPath = cssselector,
                    //xPath = xpath,
                    zn_id = zn_id,
                    tabid = message.tabid,
                    frameId = message.frameId
                };
                NativeMessagingMessage subsubresult = NMHook.sendMessageResult(getelement2, PluginConfig.protocol_timeout);
                if (subsubresult == null) throw new Exception("Failed clicking html element");
                if (PluginConfig.wait_for_tab_click)
                {
                    NMHook.WaitForTab(subsubresult.tabid, subsubresult.browser, TimeSpan.FromSeconds(5), tab.lastready);
                }

            }
        }
        private bool hasRefreshed = false;
        public bool Refresh()
        {
            try
            {
                var getelement = new NativeMessagingMessage("getelement", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids)
                {
                    browser = browser,
                    cssPath = cssselector,
                    xPath = xpath,
                    frameId = this.message.frameId,
                    windowId = this.message.windowId,
                    zn_id = this.zn_id
                };
                NativeMessagingMessage message = null;
                // getelement.data = "getdom";
                if (NMHook.connected) message = NMHook.sendMessageResult(getelement, PluginConfig.protocol_timeout);
                if (message == null)
                {
                    Log.Error("Failed getting html element");
                    return false;
                }
                if (message.result == null)
                {
                    Log.Error("Failed getting html element");
                    return false;
                }

                parseChromeString(message.result.ToString());
                zn_id = message.zn_id;
                this.message = message;
                if (!string.IsNullOrEmpty(message.xPath)) xpath = message.xPath;
                if (!string.IsNullOrEmpty(message.cssPath)) cssselector = message.cssPath;
                X = message.uix;
                Y = message.uiy;
                Width = message.uiwidth;
                Height = message.uiheight;
                hasRefreshed = true;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        public void Focus()
        {
            var tab = NMHook.FindTabById(browser, message.tabid);
            if (tab == null) throw new ElementNotFoundException("Unknown tabid " + message.tabid);
            var updateelement = new NativeMessagingMessage("focuselement", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids)
            {
                browser = message.browser,
                //cssPath = cssselector,
                //xPath = xpath,
                //tabid = message.tabid,
                //frameId = message.frameId,
                //data = value
                zn_id = zn_id,
                tabid = message.tabid,
                frameId = message.frameId
            };
            var subsubresult = NMHook.sendMessageResult(updateelement, PluginConfig.protocol_timeout);
            if (subsubresult == null) throw new Exception("Failed setting html element value");
            //System.Threading.Thread.Sleep(500);
            if (PluginConfig.wait_for_tab_after_set_value)
            {
                NMHook.WaitForTab(updateelement.tabid, updateelement.browser, TimeSpan.FromSeconds(5));
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        public Task _Highlight(System.Drawing.Color Color, TimeSpan Duration)
        {
            using (Interfaces.Overlay.OverlayWindow _overlayWindow = new Interfaces.Overlay.OverlayWindow(true))
            {
                _overlayWindow.BackColor = Color;
                _overlayWindow.Visible = true;
                _overlayWindow.SetTimeout(Duration);
                _overlayWindow.Bounds = Rectangle;
                Log.Debug("Highlight area " + Rectangle.ToString());
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
                return Interfaces.Image.Util.Bitmap2Base64(image);
            }
        }
        [Newtonsoft.Json.JsonIgnore]
        public string href
        {
            get
            {
                if (Attributes != null)
                {
                    if (Attributes.ContainsKey("href")) return Attributes["href"].ToString();
                    return null;
                }
                return null;
            }
        }
        [Newtonsoft.Json.JsonIgnore]
        public string src
        {
            get
            {
                if (Attributes != null)
                {
                    if (Attributes.ContainsKey("src")) return Attributes["src"].ToString();
                    return null;
                }
                return null;
            }
        }
        [Newtonsoft.Json.JsonIgnore]
        public string alt
        {
            get
            {
                if (Attributes != null)
                {
                    if (Attributes.ContainsKey("alt")) return (string)Attributes["alt"];
                    return null;
                }
                return null;
            }
        }
        public IElement[] Items
        {
            get
            {
                var result = new List<IElement>();
                if (tagname.ToLower() == "select")
                {
                    foreach (var item in Children)
                    {
                        item.Refresh();
                        result.Add(item);
                    }

                }
                return result.ToArray();
            }
        }
        public bool WaitForVanish(TimeSpan Timeout, bool IsVisible = true, bool isVisibleOnScreen = false)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            do
            {
                System.Threading.Thread.Sleep(100);
                if (!Refresh()) return true;
                if (IsVisible && !this.IsVisible) return true;
                if (isVisibleOnScreen && !this.isVisibleOnScreen) return true;
            } while (sw.Elapsed < Timeout);
            return false;
        }
        public override bool Equals(object obj)
        {
            //if (obj is NMElement nm)
            //{
            //    var eq = new Activities.NMEqualityComparer();
            //    return eq.Equals(this, nm);
            //}
            //return base.Equals(obj);
            return hashCode == obj.GetHashCode();
        }
        private int hashCode = 0;
        public override int GetHashCode()
        {
            if (hashCode > 0) return hashCode;
            int hCode = 0;

            if (!string.IsNullOrEmpty(xpath)) hCode += xpath.GetHashCode();
            if (!string.IsNullOrEmpty(id)) hCode += id.GetHashCode();
            if (!string.IsNullOrEmpty(cssselector)) hCode += cssselector.GetHashCode();
            if (!string.IsNullOrEmpty(classname)) hCode += classname.GetHashCode();
            if (!string.IsNullOrEmpty(Text)) hCode += Text.GetHashCode();
            if (zn_id > 0) hCode += Convert.ToInt32(zn_id);
            hashCode = hCode.GetHashCode();
            return hashCode;
        }
        public bool IsChecked
        {
            get
            {
                if (Attributes.ContainsKey("checked"))
                {
                    if (Attributes["checked"].ToString() == "true") return true;
                    if (Attributes["checked"].ToString() == "True") return true;
                }
                return false;
            }
            set
            {
                if (NMHook.connected)
                {
                    var tab = NMHook.FindTabById(browser, message.tabid);
                    if (tab == null) throw new ElementNotFoundException("Unknown tabid " + message.tabid);
                    // NMHook.HighlightTab(tab);

                    var updateelement = new NativeMessagingMessage("updateelementvalue", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids)
                    {
                        browser = message.browser,
                        zn_id = zn_id,
                        tabid = message.tabid,
                        frameId = message.frameId,
                        data = Interfaces.Extensions.Base64Encode(value.ToString()),
                        result = "value"
                    };
                    var subsubresult = NMHook.sendMessageResult(updateelement, PluginConfig.protocol_timeout);
                    if (subsubresult == null) throw new Exception("Failed setting html element value");
                    //System.Threading.Thread.Sleep(500);
                    if (PluginConfig.wait_for_tab_after_set_value)
                    {
                        NMHook.WaitForTab(updateelement.tabid, updateelement.browser, TimeSpan.FromSeconds(5));
                    }
                    return;
                }
            }
        }

    }
}
