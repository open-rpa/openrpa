using FlaUI.Core.AutomationElements;
using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.IE
{
    public class Browser
    {
        public string title { get; set; }
        public SHDocVw.WebBrowser wBrowser { get; set; }
        public mshtml.HTMLDocument Document { get; set; }
        // public SHDocVw.InternetExplorer IE { get; set; }
        
        public AutomationElement panel { get; set; }
        public string PageSource
        {
            get
            {
                mshtml.IHTMLElementCollection oColl;
                mshtml.IHTMLElement oHTML;
                oColl = Document.getElementsByTagName("HTML");
                if (oColl != null && oColl.length > 0)
                {
                    oHTML = oColl.item(null, 0); // (mshtml.IHTMLElement)oColl.GetEnumerator().Current;
                    return oHTML.outerHTML;
                }
                return string.Empty;
            }
        }
        public static Browser GetBrowser(string url = null)
        {
            var result = new Browser();
            SHDocVw.ShellWindows shellWindows = new SHDocVw.ShellWindowsClass();
            foreach (SHDocVw.InternetExplorer _ie in shellWindows)
            {
                var filename = System.IO.Path.GetFileNameWithoutExtension(_ie.FullName).ToLower();

                if (filename.Equals("iexplore"))
                {
                    //Debug.WriteLine("Web Site   : {0}", _ie.LocationURL);
                    try
                    {
                        result.wBrowser = _ie as SHDocVw.WebBrowser;
                        // result.IE = _ie;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "");
                    }
                }
            }
            if (result.wBrowser == null && !string.IsNullOrEmpty(url))
            {
                object m = null;
                SHDocVw.InternetExplorer ie = new SHDocVw.InternetExplorer();
                // Open the URL
                ie.Navigate2(url, ref m, ref m, ref m, ref m);
                ie.Visible = true;
                var sw = new Stopwatch();
                sw.Start();
                while (result.wBrowser == null)
                {
                    try
                    {
                        var doc = ie.Document as mshtml.HTMLDocument;
                        var timeout = TimeSpan.FromSeconds(5);
                        while (sw.Elapsed < timeout && doc.readyState != "complete" && doc.readyState != "interactive")
                        {
                            Log.Debug("pending complete, readyState: " + doc.readyState);
                            System.Threading.Thread.Sleep(100);
                        }
                        result.wBrowser = ie as SHDocVw.WebBrowser;
                    }
                    catch (Exception)
                    {
                    }
                }

            }
            if (result.wBrowser == null) return null;
            result.Document = result.wBrowser.Document as mshtml.HTMLDocument;
            result.title = result.Document.title;
            return result;
        }

        internal void Show()
        {
            Interfaces.GenericTools.ShowWindow(new IntPtr(wBrowser.HWND), Interfaces.GenericTools.SW_SHOWNORMAL);
            Interfaces.GenericTools.SetForegroundWindow(new IntPtr(wBrowser.HWND));
        }

        private Browser() { }
        public Browser(AutomationElement Element)
        {
            var HWNDs = new Dictionary<Int32, AutomationElement>();
            var ele = Element;
            while (ele != null)
            {
                if (ele.Properties.NativeWindowHandle.IsSupported)
                {
                    var HWND = ele.Properties.NativeWindowHandle.Value.ToInt32();
                    HWNDs.Add(HWND, ele);
                }
                ele = ele.Parent;
            }
            var wbs = new SHDocVw.ShellWindowsClass().Cast<SHDocVw.WebBrowser>().ToList();
            foreach (var w in wbs)
            {
                using (var automation = Interfaces.AutomationUtil.getAutomation())
                {
                    var doc = (w.Document as mshtml.HTMLDocument);
                    if (doc != null)
                    {
                        wBrowser = w as SHDocVw.WebBrowser;
                        var _Document = (wBrowser.Document as mshtml.HTMLDocument);
                        var _Document2 = (wBrowser.Document as mshtml.IHTMLDocument2);

                        var _ele = automation.FromHandle(new IntPtr(w.HWND));

                        panel = _ele.FindFirst(TreeScope.Descendants,
                            new AndCondition(new PropertyCondition(automation.PropertyLibrary.Element.ControlType, ControlType.Pane),
                            new PropertyCondition(automation.PropertyLibrary.Element.ClassName, "TabWindowClass"))); // Frame Tab
                        elementx = Convert.ToInt32(panel.BoundingRectangle.X);
                        elementy = Convert.ToInt32(panel.BoundingRectangle.Y);
                    }
                }
            }
            Document = wBrowser.Document as mshtml.HTMLDocument;
            title = Document.title;
        }

        public mshtml.IHTMLElement ElementFromPoint(int X, int Y)
        {
            mshtml.IHTMLElement htmlelement = Document.elementFromPoint(X - elementx, Y - elementy);

            return htmlelement;
        }
        public int elementx { get; set; } = 0;
        public int elementy { get; set; } = 0;
        public int frameoffsetx { get; set; } = 0;
        public int frameoffsety { get;  set; } = 0;
        public mshtml.IHTMLElement ElementFromPoint(mshtml.IHTMLElement frame , int X, int Y)
        {
            frame = Document.elementFromPoint(X - elementx, Y - elementy);
            frameoffsetx = 0;
            frameoffsety = 0;
            bool isFrame = (frame.tagName == "FRAME");
            while (isFrame)
            {
                var web = frame as SHDocVw.IWebBrowser2;
                elementx += frame.offsetLeft;
                elementy += frame.offsetTop;
                frameoffsetx += frame.offsetLeft;
                frameoffsety += frame.offsetTop;
                //int elementw = el.offsetWidth;
                //int elementh = el.offsetHeight;

                var dd = (mshtml.HTMLDocument)web.Document;
                frame = dd.elementFromPoint(X - elementx, Y - elementy);
                if(frame==null) frame = dd.elementFromPoint(X, Y );
                var tag = frame.tagName;
                var html = frame.innerHTML;
                System.Diagnostics.Debug.WriteLine("tag: " + tag);
                System.Diagnostics.Debug.WriteLine("html: " + html);
                System.Diagnostics.Debug.WriteLine("-----");

                isFrame = (frame.tagName == "FRAME");
            }
            return frame as mshtml.IHTMLElement;

            //mshtml.IHTMLElement htmlelement;
            //// Document = (mshtml.DispHTMLDocument)((SHDocVw.IWebBrowser2)frame).Document;
            //Document = (mshtml.HTMLDocument)((SHDocVw.IWebBrowser2)frame).Document;
            //elementx += frame.offsetLeft;
            //elementy += frame.offsetTop;
            //frameoffsetx += frame.offsetLeft;
            //frameoffsety += frame.offsetTop;
            //htmlelement = Document.elementFromPoint(X - elementx, Y - elementy);
            //if (htmlelement == null) throw new Exception("Nothing found at " + (X - elementx) + "," + (Y - elementy));
            //return htmlelement;
        }
    }
}
