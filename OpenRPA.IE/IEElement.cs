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
        public IEElement(Browser browser, MSHTML.IHTMLElement Element)
        {
            Browser = browser;
            RawElement = Element;
            ClassName = Element.className;
            Id = Element.id;
            TagName = Element.tagName.ToLower();
            Name = "";
            try
            {
                if (!(RawElement.getAttribute("Name") is System.DBNull))
                {
                    Name = (string)RawElement.getAttribute("Name");
                }
            }
            catch (Exception)
            {
            }
            if(TagName == "option")
            {
                var option = Element as MSHTML.IHTMLOptionElement;
                Name = option.text;
            }
            if (TagName == "input")
            {
                MSHTML.IHTMLInputElement inputelement = Element as MSHTML.IHTMLInputElement;
                Type = inputelement.type.ToLower();
            }
            try
            {
                MSHTML.IHTMLUniqueName id = RawElement as MSHTML.IHTMLUniqueName;
                UniqueID = id.uniqueID;
            }
            catch (Exception)
            {
            }
            IndexInParent = -1;
            if (Element.parentElement != null && !string.IsNullOrEmpty(UniqueID))
            {
                MSHTML.IHTMLElementCollection children = (MSHTML.IHTMLElementCollection)Element.parentElement.children;
                for (int i = 0; i < children.length; i++)
                {
                    if (children.item(i) is MSHTML.IHTMLUniqueName id && id.uniqueID == UniqueID) { IndexInParent = i; break; }
                }
            }
        }
        public HtmlAgilityPack.HtmlNode node { get; set; }
        public IEElement(Browser browser, HtmlAgilityPack.HtmlNode node)
        {
            Browser = browser;
            this.node = node;
            this.xpath = node.XPath.Replace("[1]", "");
            //var Element = GetLiveElement(node, browser);
            //RawElement = Element;
            ClassName = string.Join(" ", node.GetClasses());
            Id = node.Id;
            TagName = node.Name.ToLower();
            Name = "";
            try
            {
                if (node.Attributes.Contains("Name"))
                {
                    Name = node.Attributes["Name"].Value;
                }
            }
            catch (Exception)
            {
            }
            if (TagName == "input")
            {
                if (node.Attributes.Contains("type"))
                {
                    Type = node.Attributes["type"].Value.ToLower();
                }
                else { Type = "text"; }
            }
            IndexInParent = -1;
            if (node.ParentNode != null)
            {

                var children = node.ParentNode.ChildNodes;
                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i].Equals(node))
                    {
                        IndexInParent = i; break;
                    }
                }
            }
        }
        public string Name { get; set; }
        public IEElement[] Children
        {
            get
            {
                var result = new List<IEElement>();
                MSHTML.IHTMLElementCollection children = (MSHTML.IHTMLElementCollection)RawElement.children;
                foreach (MSHTML.IHTMLElement c in children)
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
                int elementx;
                int elementy;
                int elementw;
                int elementh;
                if (!(RawElement is MSHTML.IHTMLElement2 ele)) return _Rectangle.Value;
                var col = ele.getClientRects();
                if (col == null) return _Rectangle.Value;
                try
                {
                    var _rect = col.item(0);
                    var left = ((dynamic)_rect).left;
                    var top = ((dynamic)_rect).top;
                    var right = ((dynamic)_rect).right;
                    var bottom = ((dynamic)_rect).bottom;
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
            set { }
        }
        public Browser Browser { get; set; }
        public string ClassName { get; set; }
        public string UniqueID { get; set; }
        public string Id { get; set; }
        public string xpath { get; set; }
        public string TagName { get; set; }
        public string Type { get; set; }
        public int IndexInParent { get; set; }
        private MSHTML.IHTMLElement _RawElement;
        public MSHTML.IHTMLElement RawElement
        {
            get
            {
                if (_RawElement != null) return _RawElement;
                if (node != null)
                {
                    var sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    string csspath = null;
                    if (!string.IsNullOrEmpty(xpath))
                    {
                        GenericTools.MainWindow.Dispatcher.Invoke(() =>
                        {
                            //csspath = CSSPath.getCSSPath(node, true);
                            csspath = CSSPath.getCSSPath(node, false);
                            while (_RawElement == null)
                            {
                                try
                                {
                                    _RawElement = Browser.Document.querySelector(csspath);
                                }
                                catch (Exception)
                                {
                                }
                            }
                        });
                        if (_RawElement != null)
                        {
                            Log.SelectorVerbose(string.Format("IEElement.RawElement::Found with querySelector::end (" + csspath + ") {0:mm\\:ss\\.fff}", sw.Elapsed));
                            return _RawElement;
                        }
                    }
                    _RawElement = GetLiveElement(node, Browser);
                    Log.SelectorVerbose(string.Format("IEElement.RawElement::Found with GetLiveElement::end (" + csspath + ") {0:mm\\:ss\\.fff}", sw.Elapsed));
                    return _RawElement;
                }
                return null;
            }
            set
            {
                _RawElement = value;
            }

        }
        object IElement.RawElement { get => RawElement; set => RawElement = value as MSHTML.IHTMLElement; }
        public void Click(bool VirtualClick, Input.MouseButton Button, int OffsetX, int OffsetY, bool DoubleClick, bool AnimateMouse)
        {
            if (Button != Input.MouseButton.Left) { VirtualClick = false; }
            if (VirtualClick)
            {
                RawElement.click();
                if (DoubleClick) RawElement.click();
            }
            else
            {
                NativeMethods.SetCursorPos(Rectangle.X + OffsetX, Rectangle.Y + OffsetY);
                Input.InputDriver.Click(Button);
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        public Task _Highlight(System.Drawing.Color Color, TimeSpan Duration)
        {
            using (Interfaces.Overlay.OverlayWindow _overlayWindow = new Interfaces.Overlay.OverlayWindow(true))
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
                    var ele = (MSHTML.IHTMLInputElement)RawElement;
                    return ele.value;
                }
                else if (RawElement.tagName.ToLower() == "select")
                {
                    var ele = (MSHTML.IHTMLSelectElement)RawElement;
                    return ele.value;
                }
                if (RawElement.tagName.ToLower() == "option")
                {
                    var ele = (MSHTML.IHTMLOptionElement)RawElement;
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
                    var ele = (MSHTML.HTMLInputElement)RawElement;
                    var name = (string)RawElement.getAttribute("Name");
                    var id = (string)RawElement.id;
                    Log.Verbose("IEElement: tagName: " + RawElement.tagName + " name: " + name + " id: " + id);
                    Log.Verbose("IE update value from '" + ele.value + "' => '" + value + "'");
                    ele.value = value;
                }
                if (RawElement.tagName.ToLower() == "select")
                {
                    var ele = (MSHTML.IHTMLSelectElement)RawElement;
                    foreach (MSHTML.IHTMLOptionElement e in (dynamic)((dynamic)ele.options))
                    {
                        if (e.value == value)
                        {
                            ele.value = value;
                        }
                        else if (e.text == value)
                        {
                            ele.value = e.value;
                        }
                    }
                }
                if (RawElement.tagName.ToLower() == "option")
                {
                    var ele = (MSHTML.IHTMLOptionElement)RawElement;
                    ele.value = value;
                }
                if (Value != value) throw new Exception("Failed updating value!");
            }
        }
        public string Text
        {
            get
            {
                if (RawElement.tagName.ToLower() == "input")
                {
                    var ele = (MSHTML.IHTMLInputElement)RawElement;
                    return ele.value;
                }
                else if (RawElement.tagName.ToLower() == "select")
                {
                    try
                    {
                        var ele = (MSHTML.IHTMLSelectElement)RawElement;
                        foreach (MSHTML.IHTMLOptionElement e in (dynamic)((dynamic)ele.options))
                        {
                            if (e.value == ele.value)
                            {
                                return e.text;
                            }
                            else if (e.text == ele.value)
                            {
                                return e.text;
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                    return null;
                }
                if (RawElement.tagName.ToLower() == "option")
                {
                    var ele = (MSHTML.IHTMLOptionElement)RawElement;
                    return ele.text;
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
                    var ele = (MSHTML.HTMLInputElement)RawElement;
                    var name = (string)RawElement.getAttribute("Name");
                    var id = (string)RawElement.id;
                    Log.Verbose("IEElement: tagName: " + RawElement.tagName + " name: " + name + " id: " + id);
                    Log.Verbose("IE update value from '" + ele.value + "' => '" + value + "'");
                    ele.value = value;
                }
                if (RawElement.tagName.ToLower() == "select")
                {
                    var ele = (MSHTML.IHTMLSelectElement)RawElement;
                    foreach (MSHTML.IHTMLOptionElement e in (dynamic)((dynamic)ele.options))
                    {
                        if (e.text == value)
                        {
                            ele.value = e.value;
                        }
                        else if (e.text == value)
                        {
                            ele.value = e.value;
                        }
                    }
                }
                if (RawElement.tagName.ToLower() == "option")
                {
                    var ele = (MSHTML.IHTMLOptionElement)RawElement;
                    ele.text = value;
                }
                if (Text != value) throw new Exception("Failed updating value!");
            }
        }
        public override string ToString()
        {
            return TagName + " " + (!string.IsNullOrEmpty(Id) ? Id : ClassName);
        }
        public override bool Equals(object obj)
        {
            if (!(obj is IEElement e)) return false;
            if (e.UniqueID == UniqueID) return true;
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
                return Interfaces.Image.Util.Bitmap2Base64(image);
            }
        }
        public string Href
        {
            get
            {
                if (RawElement.getAttribute("href") is System.DBNull) return null;
                return (string)RawElement.getAttribute("href");
            }
        }
        public string Src
        {
            get
            {
                if (RawElement.getAttribute("src") is System.DBNull) return null;
                return (string)RawElement.getAttribute("src");
            }
        }
        public string Alt
        {
            get
            {
                if (RawElement.getAttribute("alt") is System.DBNull) return null;
                return (string)RawElement.getAttribute("alt");
            }
        }
        public IElement[] Items
        {
            get
            {
                var result = new List<IElement>();
                if (RawElement.tagName.ToLower() == "select")
                {
                    var ele = (MSHTML.IHTMLSelectElement)RawElement;
                    foreach (MSHTML.IHTMLOptionElement e in (dynamic)((dynamic)ele.options))
                    {
                        result.Add(new IEElement(Browser, e as MSHTML.IHTMLElement));
                    }

                }
                return result.ToArray();
            }
        }
        static public MSHTML.IHTMLElement GetLiveElement(HtmlAgilityPack.HtmlNode node, Browser browser)
        {
            var pattern = @"/(.*?)\[(.*?)\]"; // like div[1]
                                              // Parse the XPath to extract the nodes on the path
            var matches = System.Text.RegularExpressions.Regex.Matches(node.XPath, pattern);
            List<DocNode> PathToNode = new List<DocNode>();
            foreach (System.Text.RegularExpressions.Match m in matches) // Make a path of nodes
            {
                DocNode n = new DocNode();
                n.Name = n.Name = m.Groups[1].Value;
                n.Pos = Convert.ToInt32(m.Groups[2].Value) - 1;
                if (n.Name.ToLower() != "html") PathToNode.Add(n); // add the node to path 
            }

            MSHTML.IHTMLElement elem = null; //Traverse to the element using the path
            if (PathToNode.Count > 0)
            {
                //elem = doc.Body; //begin from the body
                elem = browser.Document.documentElement;
                foreach (DocNode n in PathToNode)
                {
                    //Find the corresponding child by its name and position
                    elem = GetChild(elem, n);
                }
            }
            return elem;
        }
        public static MSHTML.IHTMLElement GetChild(MSHTML.IHTMLElement el, DocNode node)
        {
            // Find corresponding child of the elemnt 
            // based on the name and position of the node
            int childPos = 0;
            MSHTML.IHTMLElementCollection children = (MSHTML.IHTMLElementCollection)el.children;
            for (int i = 0; i < children.length; i++)
            {
                var child = children.item(i) as MSHTML.IHTMLElement;
                if (child.tagName.Equals(node.Name,
                   StringComparison.OrdinalIgnoreCase))
                {
                    if (childPos == node.Pos)
                    {
                        return child;
                    }
                    childPos++;
                }
            }
            return null;
        }

    }
    public struct DocNode
    {
        public string Name;
        public int Pos;
    }
}
