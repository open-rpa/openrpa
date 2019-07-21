using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.IE
{
    public class IEElement : IElement
    {
        public IEElement(Browser browser, mshtml.IHTMLElement Element)
        {
            Browser = browser;
            RawElement = Element;
            className = Element.className;
            id = Element.id;
            tagName = Element.tagName.ToLower();
            if (tagName == "input")
            {
                mshtml.IHTMLInputElement inputelement = Element as mshtml.IHTMLInputElement;
                type = inputelement.type.ToLower();
            }
            try
            {
                mshtml.IHTMLUniqueName id = RawElement as mshtml.IHTMLUniqueName;
                uniqueID = id.uniqueID;
            }
            catch (Exception)
            {
            }
            IndexInParent = -1;
            if (Element.parentElement != null && !string.IsNullOrEmpty(uniqueID))
            {
                mshtml.IHTMLElementCollection children = Element.parentElement.children;
                for (int i = 0; i < children.length; i++)
                {
                    mshtml.IHTMLUniqueName id = children.item(i) as mshtml.IHTMLUniqueName;
                    if (id != null && id.uniqueID == uniqueID) { IndexInParent = i; break; }
                }
            }
        }

        public IEElement[] Children
        {
            get
            {
                var result = new List<IEElement>();
                mshtml.IHTMLElementCollection children = RawElement.children;
                foreach (mshtml.IHTMLElement c in children)
                {
                    try
                    {
                        result.Add(new IEElement(Browser, c));
                    }
                    catch (Exception)
                    {
                    }
                }
                return result.ToArray();
            }
        }

        private System.Drawing.Rectangle? _Rectangle = null;
        public System.Drawing.Rectangle Rectangle
        {
            get
            {
                if (_Rectangle != null) return _Rectangle.Value;

                _Rectangle = System.Drawing.Rectangle.Empty;
                int elementx = 0;
                int elementy = 0;
                int elementw = 0;
                int elementh = 0;
                mshtml.IHTMLElement2 ele = RawElement as mshtml.IHTMLElement2;
                if (ele == null) return _Rectangle.Value;
                var col = ele.getClientRects();
                if (col == null) return _Rectangle.Value;
                try
                {
                    var _rect = col.item(0);
                    var left = _rect.left;
                    var top = _rect.top;
                    var right = _rect.right;
                    var bottom = _rect.bottom;
                    elementx = left;
                    elementy = top;
                    elementw = right - left;
                    elementh = bottom - top;


                    elementx += Browser.frameoffsetx;
                    elementy += Browser.frameoffsety;

                    elementx += Convert.ToInt32(Browser.panel.BoundingRectangle.X);
                    elementy += Convert.ToInt32(Browser.panel.BoundingRectangle.Y);
                    //var t = Task.Factory.StartNew(() =>
                    //{
                    //});
                    //t.Wait();
                    _Rectangle = new System.Drawing.Rectangle(elementx, elementy, elementw, elementh);
                    return _Rectangle.Value;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                return _Rectangle.Value;
            }
        }
        public Browser Browser { get; set; }
        public string className { get; set; }
        public string uniqueID { get; set; }
        public string id { get; set; }
        public string tagName { get; set; }
        public string type { get; set; }
        public int IndexInParent { get; set; }
        public mshtml.IHTMLElement RawElement { get; private set; }
        object IElement.RawElement { get => RawElement; set => RawElement = value as mshtml.IHTMLElement; }
        public void Click(bool VirtualClick, Input.MouseButton Button, int OffsetX, int OffsetY)
        {
            if (Button != Input.MouseButton.Left) { VirtualClick = false; }
            //if (rawElement.tagName.ToLower() == "input")
            //{
            //    var ele = (mshtml.IHTMLInputElement)rawElement;
            //    var _ele2 = ele as mshtml.IHTMLElement;
            //    _ele2.click();
            //}
            //else if (rawElement.tagName.ToLower() == "a")
            //{
            //    var ele = (mshtml.IHTMLLinkElement)rawElement;
            //    var _ele2 = ele as mshtml.IHTMLElement;
            //    _ele2.click();
            //} else
            //{
            //}
            if (VirtualClick)
            {
                RawElement.click();
            } else
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
            }
        }
        public void Focus()
        {
        }
        public Task Highlight(bool Blocking, System.Drawing.Color Color, TimeSpan Duration)
        {
            if (!Blocking)
            {
                Task.Run(() => _Highlight(Color, Duration));
                return Task.CompletedTask;
            }
            return _Highlight(Color, Duration);
        }
        public Task _Highlight(System.Drawing.Color Color, TimeSpan Duration)
        {
            using (Interfaces.Overlay.OverlayWindow _overlayWindow = new Interfaces.Overlay.OverlayWindow())
            {
                _overlayWindow.BackColor = Color;
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
        public string Value
        {
            get
            {
                if (RawElement.tagName.ToLower() == "input")
                {
                    var ele = (mshtml.IHTMLInputElement)RawElement;
                    return ele.value;
                }
                else
                {
                    return RawElement.innerText;
                }
                // return null;
            }
            set
            {
                if (RawElement.tagName.ToLower() == "input")
                {
                    var ele = (mshtml.IHTMLInputElement)RawElement;
                    ele.value = value;
                }
                if(RawElement.tagName.ToLower() == "select")
                {
                    var ele = (mshtml.IHTMLSelectElement)RawElement;
                    foreach(mshtml.IHTMLOptionElement e in ele.options)
                    {
                        if(e.value == value)
                        {
                            ele.value = value;
                        } else if (e.text == value)
                        {
                            ele.value = e.value;
                        }
                    }
                }
            }
        }
        public override string ToString()
        {
            return tagName + " " + (!string.IsNullOrEmpty(id) ? id : className);
        }
        public override bool Equals(object obj)
        {
            var e = obj as IEElement;
            if (e == null) return false;
            if (e.uniqueID == uniqueID) return true;
            if (RawElement.sourceIndex == e.RawElement.sourceIndex) return true;
            if (RawElement.GetHashCode() == e.RawElement.GetHashCode()) return true;
            return false;
            //return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
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
                // Interfaces.Image.Util.SaveImageStamped(image, System.IO.Directory.GetCurrentDirectory(), "IEElement");
                return Interfaces.Image.Util.Bitmap2Base64(image);
            }
        }

        public string href
        {
            get
            {
                if (RawElement.getAttribute("href") is System.DBNull) return null;
                return RawElement.getAttribute("href");
            }
        }
        public string src
        {
            get
            {
                if (RawElement.getAttribute("src") is System.DBNull) return null;
                return RawElement.getAttribute("src");
            }
        }
        public string alt
        {
            get
            {
                if (RawElement.getAttribute("alt") is System.DBNull) return null;
                return RawElement.getAttribute("alt");
            }
        }

    }
}
