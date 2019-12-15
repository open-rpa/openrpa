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
        public MSHTML.HTMLDocument Document { get; set; }
        // public SHDocVw.InternetExplorer IE { get; set; }
        
        public AutomationElement panel { get; set; }
        public string PageSource
        {
            get
            {
                MSHTML.IHTMLElementCollection oColl;
                MSHTML.IHTMLElement oHTML;
                oColl = Document.getElementsByTagName("HTML");
                if (oColl != null && oColl.length > 0)
                {
                    oHTML = (MSHTML.IHTMLElement)oColl.item(null, 0); // (MSHTML.IHTMLElement)oColl.GetEnumerator().Current;
                    return oHTML.outerHTML;
                }
                return string.Empty;
            }
        }
        public static Browser GetBrowser(string url = null)
        {
            var result = new Browser();
            SHDocVw.ShellWindows shellWindows = new SHDocVw.ShellWindows();
            foreach (SHDocVw.InternetExplorer _ie in shellWindows)
            {
                var filename = System.IO.Path.GetFileNameWithoutExtension(_ie.FullName).ToLower();

                if (filename.Equals("iexplore"))
                {
                    // Log.Debug(string.Format("Web Site : {0}", _ie.LocationURL));
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
                        var doc = ie.Document as MSHTML.HTMLDocument;
                        var timeout = TimeSpan.FromSeconds(5);
                        while (sw.Elapsed < timeout && doc.readyState != "complete" && doc.readyState != "interactive")
                        {
                            // Log.Debug("pending complete, readyState: " + doc.readyState);
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
            result.Document = result.wBrowser.Document as MSHTML.HTMLDocument;
            result.title = result.Document.title;
            result.findPanel();
            return result;
        }

        internal void Show()
        {
            NativeMethods.ShowWindow(new IntPtr(wBrowser.HWND), NativeMethods.SW_SHOWNORMAL);
            NativeMethods.SetForegroundWindow(new IntPtr(wBrowser.HWND));
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
            findBrowser();
            if (wBrowser.Document == null) throw new Exception("Failed initializing Internet Eexplorer");
            Document = wBrowser.Document as MSHTML.HTMLDocument;
            title = Document.title;
        }
        private void findBrowser()
        {
            var wbs = new SHDocVw.ShellWindows().Cast<SHDocVw.WebBrowser>().ToList();
            foreach (var w in wbs)
            {
                try
                {
                    var doc = (w.Document as MSHTML.HTMLDocument);
                    if (doc != null)
                    {
                        wBrowser = w as SHDocVw.WebBrowser;
                        var _Document = (wBrowser.Document as MSHTML.HTMLDocument);
                        var _Document2 = (wBrowser.Document as MSHTML.IHTMLDocument2);
                        findPanel();
                    }
                }
                catch (Exception)
                {
                }
            }
        }
        private void findPanel()
        {
            using (var automation = Interfaces.AutomationUtil.getAutomation())
            {
                var _ele = automation.FromHandle(new IntPtr(wBrowser.HWND));

                panel = _ele.FindFirst(TreeScope.Descendants,
                    new AndCondition(new PropertyCondition(automation.PropertyLibrary.Element.ControlType, ControlType.Pane),
                    new PropertyCondition(automation.PropertyLibrary.Element.ClassName, "TabWindowClass"))); // Frame Tab
                elementx = Convert.ToInt32(panel.BoundingRectangle.X);
                elementy = Convert.ToInt32(panel.BoundingRectangle.Y);
            }
        }
        public MSHTML.IHTMLElement ElementFromPoint(int X, int Y)
        {
            MSHTML.IHTMLElement htmlelement = Document.elementFromPoint(X - elementx, Y - elementy);

            return htmlelement;
        }
        public int elementx { get; set; } = 0;
        public int elementy { get; set; } = 0;
        public int frameoffsetx { get; set; } = 0;
        public int frameoffsety { get;  set; } = 0;
        public MSHTML.IHTMLElement ElementFromPoint(MSHTML.IHTMLElement frame , int X, int Y)
        {
            frame = Document.elementFromPoint(X - elementx, Y - elementy);
            frameoffsetx = 0;
            frameoffsety = 0;
            bool isFrame = (frame.tagName == "FRAME" || frame.tagName == "IFRAME");
            while (isFrame)
            {
                var web = frame as SHDocVw.IWebBrowser2;
                elementx += frame.offsetLeft;
                elementy += frame.offsetTop;
                frameoffsetx += frame.offsetLeft;
                frameoffsety += frame.offsetTop;
                //int elementw = el.offsetWidth;
                //int elementh = el.offsetHeight;

                var dd = (MSHTML.HTMLDocument)web.Document;
                frame = dd.elementFromPoint(X - elementx, Y - elementy);
                if(frame==null) frame = dd.elementFromPoint(X, Y );
                var tag = frame.tagName;
                var html = frame.innerHTML;
                Log.Debug("tag: " + tag);
                Log.Debug("html: " + html);
                Log.Debug("-----");

                isFrame = (frame.tagName == "FRAME" || frame.tagName == "IFRAME");
            }
            return frame as MSHTML.IHTMLElement;

            //MSHTML.IHTMLElement htmlelement;
            //// Document = (MSHTML.DispHTMLDocument)((SHDocVw.IWebBrowser2)frame).Document;
            //Document = (MSHTML.HTMLDocument)((SHDocVw.IWebBrowser2)frame).Document;
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
