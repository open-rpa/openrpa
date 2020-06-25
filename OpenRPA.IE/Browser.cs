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
        private static Browser browser;
        private static DateTime browser_at;
        private static TimeSpan browser_for = TimeSpan.FromSeconds(5);
        public static Browser GetBrowser(bool forcenew, string url = null)
        {
            if (!PluginConfig.enable_caching_browser) browser = null;
            var sw = new Stopwatch(); sw.Start();
            if (browser != null)
            {
                try
                {
                    if((DateTime.Now - browser_at) > browser_for)
                    {
                        browser = null;
                    }
                    else
                    {
                        browser_at = DateTime.Now;
                        //browser.findBrowser();
                        browser.Document = browser.wBrowser.Document as MSHTML.HTMLDocument;
                        var _url = browser.Document.url;
                        Log.Debug(string.Format("GetBrowser " + _url + "{0:mm\\:ss\\.fff}", sw.Elapsed));
                        return browser;
                    }
                }
                catch (Exception)
                {
                    browser = null;
                }                
            }
            var result = new Browser();
            SHDocVw.ShellWindows shellWindows = new SHDocVw.ShellWindows();
            SHDocVw.InternetExplorer ie = null;
            if (result.wBrowser == null && !string.IsNullOrEmpty(url))
            {
                object m = null;
                if (ie == null) ie = new SHDocVw.InternetExplorer();
                // Open the URL
                ie.Navigate2(url, ref m, ref m, ref m, ref m);
                ie.Visible = true;
                while (result.wBrowser == null && sw.Elapsed < TimeSpan.FromSeconds(5))
                {
                    var timeout = TimeSpan.FromSeconds(10);
                    result.findBrowser();
                    if(result.wBrowser!=null && result.Document != null)
                    {
                        while (sw.Elapsed < timeout && result.Document.readyState != "complete" && result.Document.readyState != "interactive")
                        {
                            Log.Debug("pending complete, readyState: " + result.Document.readyState);
                            System.Threading.Thread.Sleep(100);
                        }
                    } else
                    {
                        Log.Debug("pending document object");
                        System.Threading.Thread.Sleep(100);
                    }
                }

            }
            if (result.wBrowser == null) throw new Exception("Failed launching Internet Explorer");
            result.findPanel();
            browser = result;
            browser_at = DateTime.Now;
            Log.Debug(string.Format("GetBrowser {0:mm\\:ss\\.fff}", sw.Elapsed));
            return result;
        }
        //MSHTML.HTMLDocument doc2 = browser.Document;
        //MSHTML.IHTMLWindow2 win = browser.wBrowser as MSHTML.IHTMLWindow2;
        internal void Show()
        {
            
            NativeMethods.ShowWindow(new IntPtr(wBrowser.HWND), NativeMethods.SW_SHOWNORMAL);
            NativeMethods.SetForegroundWindow(new IntPtr(wBrowser.HWND));
            
        }
        internal Browser() {
            findBrowser();
            if (wBrowser == null || wBrowser.Document == null) return;
            // if (wBrowser.Document == null) throw new Exception("Failed initializing Internet Eexplorer");
            Document = wBrowser.Document as MSHTML.HTMLDocument;
            title = Document.title;
        }
        private void findBrowser()
        {
            var sw = new Stopwatch(); sw.Start();
            var wbs = new SHDocVw.ShellWindows().Cast<SHDocVw.WebBrowser>().ToList();
            foreach (var w in wbs)
            {
                try
                {
                    var doc = (w.Document as MSHTML.HTMLDocument);
                    if (doc != null)
                    {
                        wBrowser = w;
                        Document = (wBrowser.Document as MSHTML.HTMLDocument);
                        var _Document2 = (wBrowser.Document as MSHTML.IHTMLDocument2);
                        findPanel();
                    }
                }
                catch (Exception)
                {
                }
            }
            if (wbs.Count == 0)
            {
                wBrowser = null;
                Document = null;
            }
            Log.Debug(string.Format("findBrowser {0:mm\\:ss\\.fff}", sw.Elapsed));
        }
        private void findPanel()
        {
            using (var automation = Interfaces.AutomationUtil.getAutomation())
            {
                var _ele = automation.FromHandle(new IntPtr(wBrowser.HWND));
                panel = _ele.FindFirst(TreeScope.Descendants,
                    new AndCondition(new PropertyCondition(automation.PropertyLibrary.Element.ControlType, ControlType.Pane),
                    new PropertyCondition(automation.PropertyLibrary.Element.ClassName, "TabWindowClass"))); // Frame Tab
                if(panel==null)
                {
                    var win = _ele.AsWindow();
                    GenericTools.Restore(new IntPtr(wBrowser.HWND));
                    win.SetForeground();

                    var sw = new Stopwatch(); sw.Start();

                    while (panel == null && sw.Elapsed < TimeSpan.FromSeconds(5))
                    {
                        panel = _ele.FindFirst(TreeScope.Descendants,
                        new AndCondition(new PropertyCondition(automation.PropertyLibrary.Element.ControlType, ControlType.Pane),
                        new PropertyCondition(automation.PropertyLibrary.Element.ClassName, "TabWindowClass"))); // Frame Tab
                    }
                }
                if (panel == null) throw new Exception("Failed tab inside IE window " + wBrowser.HWND.ToString());
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
