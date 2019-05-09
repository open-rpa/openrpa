using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using System;
using System.Collections.Generic;
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

                    }
                }
            }
            Document = wBrowser.Document as mshtml.HTMLDocument;
            title = Document.title;
        }

        public mshtml.IHTMLElement ElementFromPoint(int X, int Y)
        {
            int elementx = Convert.ToInt32(panel.BoundingRectangle.X);
            int elementy = Convert.ToInt32(panel.BoundingRectangle.Y);
            mshtml.IHTMLElement htmlelement = Document.elementFromPoint(X - elementx, Y - elementy);

            return htmlelement;
        }
    }
}
