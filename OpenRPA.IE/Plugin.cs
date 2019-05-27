using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using OpenRPA.Input;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRPA.IE
{
    class Plugin : IPlugin
    {
        public static treeelement[] _GetRootElements(Selector anchor)
        {
            var browser = Browser.GetBrowser();
            if (browser == null)
            {
                Log.Warning("Failed locating an Internet Explore instance");
                return new treeelement[] { };
            }
            if(anchor != null)
            {
                IESelector ieselector = anchor as IESelector;
                if (ieselector == null) { ieselector = new IESelector(anchor.ToString()); }
                var elements = IESelector.GetElementsWithuiSelector(ieselector, null, 5);
                var result = new List<treeelement>();
                foreach (var _ele in elements)
                {
                    var e = new IETreeElement(null, true, _ele);
                    result.Add(e);

                }
                return result.ToArray();
            }
            else
            {
                var e = new IETreeElement(null, true, new IEElement(browser, browser.Document.documentElement));
                return new treeelement[] { e };
            }
        }
        public treeelement[] GetRootElements(Selector anchor)
        {
            return Plugin._GetRootElements(anchor);
        }
        public Interfaces.Selector.Selector GetSelector(Selector anchor, Interfaces.Selector.treeelement item)
        {
            var ieitem = item as IETreeElement;
            IESelector ieanchor = anchor as IESelector;
            if (ieanchor == null && anchor != null)
            {
                ieanchor = new IESelector(anchor.ToString());
            }
            return new IESelector(ieitem.IEElement.Browser, ieitem.IEElement.RawElement, ieanchor, true, 0, 0);
        }

        public event Action<IPlugin, IRecordEvent> OnUserAction;
        public string Name { get => "IE"; }
        public string Status => _status;
        private string _status = "offline";
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
                var a = new GetElement { DisplayName = (e.Element.Id + "-" + e.Element.Name).Replace(Environment.NewLine, "").Trim() };

                var browser = new Browser(e.Element.RawElement);
                var htmlelement = browser.ElementFromPoint(e.X, e.Y);
                if (htmlelement == null) { return; }

                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                IESelector sel = null;
                // sel = new IESelector(e.Element.rawElement, null, true);
                GenericTools.RunUI(() =>
                {
                    sel = new IESelector(browser, htmlelement, null, false, e.X, e.Y);
                });
                if (sel == null) return;
                if (sel.Count < 2) return;
                a.Selector = sel.ToString();
                a.Image = sel.Last().Element.ImageString();
                re.UIElement = e.Element;
                re.Element = new IEElement(browser, htmlelement);
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

            var browser = new Browser(e.UIElement.RawElement);


            var htmlelement = browser.ElementFromPoint(e.X, e.Y);
            if (htmlelement == null) { return false; }

            var selector = new IESelector(browser, htmlelement, null, true, e.X, e.Y);
            e.Selector = selector;
            e.Element = new IEElement(browser, htmlelement);

            var a = new GetElement { DisplayName = (htmlelement.id + "-" + htmlelement.tagName + "-" + htmlelement.className).Replace(Environment.NewLine, "").Trim() };
            a.Selector = selector.ToString();
            a.Image = selector.Last().Element.ImageString();
            var last = selector.Last() as IESelectorItem;


            var tagName = last.tagName;
            if (string.IsNullOrEmpty(tagName)) tagName = "";
            tagName = tagName.ToLower();
            e.a = new GetElementResult(a);
            if (tagName == "input")
            {
                // mshtml.IHTMLInputElement inputelement = (mshtml.IHTMLInputElement)htmlelement;
                e.SupportInput = (last.type.ToLower() == "text" || last.type.ToLower() == "password");
            }

            return true;
        }
        public void Initialize()
        {
        }
        public IElement[] GetElementsWithSelector(Selector selector, IElement fromElement = null, int maxresults = 1)
        {
            IESelector ieselector = selector as IESelector;
            if(ieselector == null) { ieselector = new IESelector(selector.ToString());  }
            var result = IESelector.GetElementsWithuiSelector(ieselector, fromElement, maxresults);
            return result;
        }
        public void LaunchBySelector(Selector selector, TimeSpan timeout)
        {
            if (selector == null || selector.Count == 0) return;
            var f = selector.First();
            var p = f.Properties.Where(x => x.Name == "url").FirstOrDefault();
            if (p == null) return;
            var url = p.Value;
            if (string.IsNullOrEmpty(url)) return;
            GenericTools.RunUI(() =>
            {
                var browser = Browser.GetBrowser(url);
                var doc = browser.Document;
                if (url != doc.url) doc.url = url;
                browser.Show();
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                while (sw.Elapsed < timeout && doc.readyState != "complete" && doc.readyState != "interactive")
                {
                    Log.Debug("pending complete, readyState: " + doc.readyState);
                    Thread.Sleep(100);
                }
            });
        }
        public void CloseBySelector(Selector selector, TimeSpan timeout, bool Force)
        {
            string url = "";
            var f = selector.First();
            var p = f.Properties.Where(x => x.Name == "url").FirstOrDefault();
            if (p != null) url = p.Value;
            SHDocVw.ShellWindows shellWindows = new SHDocVw.ShellWindowsClass();
            foreach (SHDocVw.InternetExplorer _ie in shellWindows)
            {
                var filename = System.IO.Path.GetFileNameWithoutExtension(_ie.FullName).ToLower();
                if (filename.Equals("iexplore"))
                {
                    try
                    {
                        var wBrowser = _ie as SHDocVw.WebBrowser;
                        if(wBrowser.LocationURL == url || string.IsNullOrEmpty(url))
                        {
                            using (var automation = Interfaces.AutomationUtil.getAutomation())
                            {
                                var _ele = automation.FromHandle(new IntPtr(_ie.HWND));
                                using (var app = new FlaUI.Core.Application(_ele.Properties.ProcessId.Value, false))
                                {
                                    app.Close();
                                }

                            }
                        }
                    }
                    catch (Exception ex)    
                    {
                        Log.Error(ex, "");
                    }
                }
            }
        }
        public bool Match(SelectorItem item, IElement m)
        {
            return IESelectorItem.Match(item, m.RawElement as mshtml.IHTMLElement);
        }
    }
    public class GetElementResult : IBodyActivity
    {
        public GetElementResult(GetElement activity)
        {
            Activity = activity;
        }
        public Activity Activity { get; set; }
        public void AddActivity(Activity a, string Name)
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
        public void AddInput(string value, IElement element)
        {
            AddActivity(new System.Activities.Statements.Assign<string>
            {
                To = new Microsoft.VisualBasic.Activities.VisualBasicReference<string>("item.value"),
                Value = value
            }, "item");
            element.Value = value;
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
        public bool ClickHandled { get; set; }
        public MouseButton Button { get; set; }
    }

}
