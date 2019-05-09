using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using OpenRPA.Input;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using Serilog;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRPA.IE
{
    class Recording : IRecording
    {
        public static treeelement[] GetRootElements()
        {
            SHDocVw.ShellWindows shellWindows = new SHDocVw.ShellWindowsClass();
            SHDocVw.WebBrowser Browser = null;
            mshtml.HTMLDocument Document = null;
            foreach (SHDocVw.InternetExplorer _ie in shellWindows)
            {
                var filename = System.IO.Path.GetFileNameWithoutExtension(_ie.FullName).ToLower();

                if (filename.Equals("iexplore"))
                {
                    //Debug.WriteLine("Web Site   : {0}", _ie.LocationURL);
                    try
                    {
                        Browser = _ie as SHDocVw.WebBrowser;
                        Document = (Browser.Document as mshtml.HTMLDocument);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "");
                    }

                }
            }
            if (Document == null)
            {
                Log.Warning("Failed locating an Internet Explore instance");
                return new treeelement[] { };
            }

            var e = new IETreeElement(null, true, new IEElement(Document.documentElement));
            return new treeelement[] { e };
        }
        public treeelement[] GetRootEelements()
        {
            return Recording.GetRootElements();
        }
        public event Action<IRecording, IRecordEvent> OnUserAction;
        public string Name { get => "IE"; }
        public void Start()
        {
            InputDriver.Instance.OnMouseUp += OnMouseUp;
        }
        public void Stop()
        {
            InputDriver.Instance.OnMouseUp -= OnMouseUp;
        }
        private void OnMouseUp(InputEventArgs e)
        {
            var thread = new Thread(new ThreadStart(() =>
            {
                Log.Debug(string.Format("IE.Recording::OnMouseUp::begin"));
                var re = new RecordEvent(); re.Button = e.Button;
                var a = new GetElement { DisplayName = e.Element.Id + "-" + e.Element.Name };

                var browser = new Browser(e.Element.rawElement);
                var htmlelement = browser.ElementFromPoint(e.X, e.Y);
                if (htmlelement == null) { return; }

                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                IESelector sel = null;
                // sel = new IESelector(e.Element.rawElement, null, true);
                sel = new IESelector(htmlelement, null, browser.Document, false);
                if (sel == null) return;
                if (sel.Count < 2) return;
                a.Selector = sel.ToString();
                re.UIElement = e.Element;
                re.Element = new IEElement(htmlelement);
                re.Selector = sel;
                re.X = e.X;
                re.Y = e.Y;

                Log.Debug(e.Element.SupportInput + " / " + e.Element.ControlType);
                re.a = new GetElementResult(a);
                if (htmlelement.tagName.ToLower() == "input")
                {
                    mshtml.IHTMLInputElement inputelement = (mshtml.IHTMLInputElement)htmlelement;
                    re.SupportInput = (inputelement.type.ToLower() == "text" || inputelement.type.ToLower() == "password");
                }

                Log.Debug(string.Format("IE.Recording::OnMouseUp::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                OnUserAction?.Invoke(this, re);
            }));
            thread.IsBackground = true;
            thread.Start();
        }
        public bool parseUserAction(ref IRecordEvent e) {
            if (e.UIElement == null) return false;
            if (e.UIElement.ProcessId < 1) return false;
            var p = System.Diagnostics.Process.GetProcessById(e.UIElement.ProcessId);
            if(p.ProcessName!="iexplore" && p.ProcessName != "iexplore.exe") return false;

            var browser = new Browser(e.UIElement.rawElement);


            var htmlelement = browser.ElementFromPoint(e.X, e.Y);
            if (htmlelement == null) { return false; }

            var selector = new IESelector(htmlelement, null, browser.Document, false);

            var a = new GetElement { DisplayName = htmlelement.id + "-" + htmlelement.tagName + "-" + htmlelement.className };
            a.Selector = selector.ToString();


            var tagName = htmlelement.tagName;
            if (string.IsNullOrEmpty(tagName)) tagName = "";
            tagName = tagName.ToLower();
            e.a = new GetElementResult(a);
            if (tagName == "input")
            {
                mshtml.IHTMLInputElement inputelement = (mshtml.IHTMLInputElement)htmlelement;
                e.SupportInput = (inputelement.type.ToLower() == "text" || inputelement.type.ToLower() == "password");
            }

            return true;
        }
        public void Initialize()
        {
        }
    }
    public class GetElementResult : IBodyActivity
    {
        public GetElementResult(GetElement activity)
        {
            Activity = activity;
        }
        public Activity Activity { get; set; }
        public void addActivity(Activity a, string Name)
        {
            //var aa = new ActivityAction<IEElement>();
            //var da = new DelegateInArgument<IEElement>();
            //da.Name = Name;
            //((GetElement)Activity).Body = aa;
            //aa.Argument = da;
            var aa = new ActivityAction<IEElement>();
            var da = new DelegateInArgument<IEElement>();
            da.Name = Name;
            aa.Handler = a;
            ((GetElement)Activity).Body = aa;
            aa.Argument = da;
        }
    }
    public class RecordEvent : IRecordEvent
    {
        // public AutomationElement Element { get; set; }
        public UIElement UIElement { get; set; }
        public IElement Element { get; set; }
        public Interfaces.Selector.Selector Selector { get; set; }
        public IBodyActivity a { get; set; }
        public bool SupportInput { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public MouseButton Button { get; set; }
    }

}
