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
            rawElement = Element;
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
                mshtml.IHTMLUniqueName id = rawElement as mshtml.IHTMLUniqueName;
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
                    if (id.uniqueID == uniqueID) { IndexInParent = i; break; }
                }
            }
            var rect = Rectangle;
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
                mshtml.IHTMLElement2 ele = rawElement as mshtml.IHTMLElement2;
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
        public mshtml.IHTMLElement rawElement { get; private set; }

        //public HTMLElement rawElement { get; private set; }
        public void Click()
        {
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
            rawElement.click();
        }
        public void Focus()
        {
        }
        private Interfaces.Overlay.OverlayWindow _overlayWindow;
        public void Highlight(bool Blocking, System.Drawing.Color Color, TimeSpan Duration)
        {
            if (_overlayWindow == null) { _overlayWindow = new Interfaces.Overlay.OverlayWindow(); }
            _overlayWindow.Visible = true;
            _overlayWindow.SetTimeout(Duration);
            if(_Rectangle == null)
            {
                Console.WriteLine("whut ??");
            }
            _overlayWindow.Bounds = Rectangle;
        }
        public string Value
        {
            get
            {
                if (rawElement.tagName.ToLower() == "input")
                {
                    var ele = (mshtml.IHTMLInputElement)rawElement;
                    return ele.value;
                }
                else
                {
                    return rawElement.innerText;
                }
                // return null;
            }
            set
            {
                if (rawElement.tagName.ToLower() == "input")
                {
                    var ele = (mshtml.IHTMLInputElement)rawElement;
                    ele.value = value;
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
            if (rawElement.sourceIndex == e.rawElement.sourceIndex) return true;
            if (rawElement.GetHashCode() == e.rawElement.GetHashCode()) return true;
            return false;
            //return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
