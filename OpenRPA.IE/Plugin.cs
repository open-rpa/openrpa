using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using OpenRPA.Input;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OpenRPA.IE
{
    class Plugin : ObservableObject, IRecordPlugin
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        public static treeelement[] _GetRootElements(Selector anchor)
        {
            var browser = Browser.GetBrowser(true);
            if (browser == null)
            {
                Log.Warning("Failed locating an Internet Explore instance");
                return new treeelement[] { };
            }
            if (anchor != null)
            {
                if (!(anchor is IESelector ieselector)) { ieselector = new IESelector(anchor.ToString()); }
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
        public event Action<IRecordPlugin, IRecordEvent> OnUserAction;
        public event Action<IRecordPlugin, IRecordEvent> OnMouseMove
        {
            add { }
            remove { }
        }
        public string Name { get => "IE"; }
        public string Status => "";
        public int Priority { get => 50; }
        private Views.RecordPluginView view;
        public System.Windows.Controls.UserControl editor
        {
            get
            {
                if (view == null)
                {
                    view = new Views.RecordPluginView();
                }
                return view;
            }
        }
        public void Start()
        {
            OpenRPA.Input.InputDriver.Instance.CallNext = false;
            InputDriver.Instance.OnMouseUp += OnMouseUp;
        }
        public void Stop()
        {
            OpenRPA.Input.InputDriver.Instance.CallNext = true;
            InputDriver.Instance.OnMouseUp -= OnMouseUp;
        }
        private void ParseMouseUp(InputEventArgs e)
        {
            try
            {
                // if(string.IsNullOrEmpty(Thread.CurrentThread.Name)) Thread.CurrentThread.Name = "IE.Plugin.OnMouseUP";
                Log.Debug(string.Format("IE.Recording::OnMouseUp::begin"));
                var re = new RecordEvent
                {
                    Button = e.Button
                }; var a = new GetElement { DisplayName = (e.Element.Name).Replace(Environment.NewLine, "").Trim() };
                a.Variables.Add(new Variable<int>("Index", 0));
                a.Variables.Add(new Variable<int>("Total", 0));

                using (var p = System.Diagnostics.Process.GetProcessById(e.Element.ProcessId))
                {
                    if (p.ProcessName != "iexplore" && p.ProcessName != "iexplore.exe") return;
                }

                var browser = new Browser();
                var htmlelement = browser.ElementFromPoint(e.X, e.Y);
                if (htmlelement == null) { return; }

                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                IESelector sel = null;
                // sel = new IESelector(e.Element.rawElement, null, true);
                GenericTools.RunUI(() =>
                {
                    try
                    {
                        sel = new IESelector(browser, htmlelement, null, false, e.X, e.Y);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
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
                //if (htmlelement.tagName.ToLower() == "input" && htmlelement.tagName.ToLower() == "select")
                if (htmlelement.tagName.ToLower() == "input")
                {
                    MSHTML.IHTMLInputElement inputelement = (MSHTML.IHTMLInputElement)htmlelement;
                    re.SupportInput = (inputelement.type.ToLower() == "text" || inputelement.type.ToLower() == "password");
                }
                re.SupportSelect = false;
                Log.Debug(string.Format("IE.Recording::OnMouseUp::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                OnUserAction?.Invoke(this, re);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private void OnMouseUp(InputEventArgs e)
        {
            var thread = new Thread(new ThreadStart(() =>
            {
                ParseMouseUp(e);
            }));
            thread.IsBackground = true;
            thread.Start();
        }
        public bool ParseUserAction(ref IRecordEvent e)
        {
            if (e.UIElement == null) return false;
            if (e.UIElement.ProcessId < 1) return false;
            using (var p = System.Diagnostics.Process.GetProcessById(e.UIElement.ProcessId))
            {
                if (p.ProcessName != "iexplore" && p.ProcessName != "iexplore.exe") return false;
            }
            var browser = new Browser();

            var htmlelement = browser.ElementFromPoint(e.X, e.Y);
            if (htmlelement == null) { return false; }
            var selector = new IESelector(browser, htmlelement, null, true, e.X, e.Y);
            e.Selector = selector;
            e.Element = new IEElement(browser, htmlelement);

            var a = new GetElement { DisplayName = (htmlelement.id + "-" + htmlelement.tagName + "-" + htmlelement.className).Replace(Environment.NewLine, "").Trim() };
            a.Variables.Add(new Variable<int>("Index", 0));
            a.Variables.Add(new Variable<int>("Total", 0));
            a.Selector = selector.ToString();

            var last = selector.Last() as IESelectorItem;
            a.Image = last.Element.ImageString();


            var tagName = htmlelement.tagName;
            if (string.IsNullOrEmpty(tagName)) tagName = "";
            tagName = tagName.ToLower();
            e.a = new GetElementResult(a);
            //if (tagName == "input")
            //{
            //    // MSHTML.IHTMLInputElement inputelement = (MSHTML.IHTMLInputElement)htmlelement;
            //    e.SupportInput = (last.type.ToLower() == "text" || last.type.ToLower() == "password");
            //}
            // if (htmlelement.tagName.ToLower() == "input" && htmlelement.tagName.ToLower() == "select")
            if (htmlelement.tagName.ToLower() == "input")
            {
                MSHTML.IHTMLInputElement inputelement = (MSHTML.IHTMLInputElement)htmlelement;
                e.SupportInput = (inputelement.type.ToLower() == "text" || inputelement.type.ToLower() == "password");
            }
            e.SupportSelect = htmlelement.tagName.ToLower() == "select";
            return true;
        }
        public static IOpenRPAClient client { get; set; }
        public void Initialize(IOpenRPAClient client)
        {
            Plugin.client = client;
            _ = PluginConfig.enable_xpath_support;
        }
        public IElement[] GetElementsWithSelector(Selector selector, IElement fromElement = null, int maxresults = 1)
        {
            if (!(selector is IESelector ieselector)) { ieselector = new IESelector(selector.ToString()); }
            var result = IESelector.GetElementsWithuiSelector(ieselector, fromElement, maxresults);
            return result;
        }
        public IElement LaunchBySelector(Selector selector, bool CheckRunning, TimeSpan timeout)
        {
            if (selector == null || selector.Count == 0) return null;
            var f = selector.First();
            var p = f.Properties.Where(x => x.Name == "url").FirstOrDefault();
            if (p == null) return null;
            var url = p.Value;
            if (string.IsNullOrEmpty(url)) return null;
            GenericTools.RunUI(() =>
            {
                try
                {
                    var browser = Browser.GetBrowser(true, url);
                    var doc = browser.Document;
                    if (url != doc.url) doc.url = url;
                    browser.Show();
                    var sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    while (sw.Elapsed < timeout)
                    {
                        try
                        {
                            Log.Debug("pending complete, readyState: " + doc.readyState);
                            Thread.Sleep(100);
                            if (doc.readyState != "complete" && doc.readyState != "interactive") break;
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });

            var wbs = new SHDocVw.ShellWindows().Cast<SHDocVw.WebBrowser>().ToList();
            foreach (var w in wbs)
            {
                try
                {
                    var doc = (w.Document as MSHTML.HTMLDocument);
                    if (doc != null)
                    {
                        var wBrowser = w as SHDocVw.WebBrowser;
                        var automation = Interfaces.AutomationUtil.getAutomation();
                        var _ele = automation.FromHandle(new IntPtr(wBrowser.HWND));
                        if (_ele != null)
                        {
                            var ui = new UIElement(_ele);
                            var window = ui.GetWindow();
                            if (window != null) return new UIElement(window);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            return null;
        }
        public void CloseBySelector(Selector selector, TimeSpan timeout, bool Force)
        {
            string url = "";
            var f = selector.First();
            var p = f.Properties.Where(x => x.Name == "url").FirstOrDefault();
            if (p != null) url = p.Value;
            SHDocVw.ShellWindows shellWindows = new SHDocVw.ShellWindows();
            foreach (SHDocVw.InternetExplorer _ie in shellWindows)
            {
                var filename = System.IO.Path.GetFileNameWithoutExtension(_ie.FullName).ToLower();
                if (filename.Equals("iexplore"))
                {
                    try
                    {
                        var wBrowser = _ie as SHDocVw.WebBrowser;
                        if (wBrowser.LocationURL == url || string.IsNullOrEmpty(url))
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
                        Log.Error(ex.ToString());
                    }
                }
            }
        }
        public bool Match(SelectorItem item, IElement m)
        {
            var p = item.Properties.Where(x => x.Name == "xpath").FirstOrDefault();
            //if(p != null)
            //{
            //    var xpath = p.Value;
            //    var browser = Browser.GetBrowser();
            //    if(browser != null)
            //    {
            //        MSHTML.IHTMLDocument3 sourceDoc = (MSHTML.IHTMLDocument3)browser.Document;
            //        string documentContents = sourceDoc.documentElement.outerHTML;
            //        HtmlAgilityPack.HtmlDocument targetDoc = new HtmlAgilityPack.HtmlDocument();
            //        targetDoc.LoadHtml(documentContents);
            //        HtmlAgilityPack.HtmlNode node = targetDoc.DocumentNode.SelectSingleNode(xpath);
            //        if(node != null)
            //        {
            //            var ele = new IEElement(browser, node);

            //        }
            //    }
            //}
            return IESelectorItem.Match(item, m.RawElement as MSHTML.IHTMLElement);
        }
        public bool ParseMouseMoveAction(ref IRecordEvent e)
        {
            if (e.UIElement == null) return false;
            if (e.Process == null || e.UIElement.ProcessId < 1) return false;
            if (e.Process.ProcessName != "iexplore" && e.Process.ProcessName != "iexplore.exe") return false;
            return true;
        }
        void IRecordPlugin.StatusTextMouseUp()
        {
            var design = Plugin.client.CurrentDesigner;
            if (design == null) return;
            var browser = Browser.GetBrowser(true);
            if (browser == null) return;
            design.AddRecordingActivity(new OpenURL { DisplayName = "Open URL", Url = browser.wBrowser.LocationURL }, this);
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
            var da = new DelegateInArgument<IEElement>
            {
                Name = Name
            };
            aa.Handler = a;
            ((GetElement)Activity).Body = aa;
            aa.Argument = da;
        }
        public void AddInput(string value, IElement element)
        {
            try
            {
                AddActivity(new System.Activities.Statements.Assign<string>
                {
                    To = new Microsoft.VisualBasic.Activities.VisualBasicReference<string>("item.value"),
                    Value = value
                }, "item");
                element.Value = value;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
    public class RecordEvent : IRecordEvent
    {
        public RecordEvent() { SupportVirtualClick = true; }
        // public AutomationElement Element { get; set; }
        public UIElement UIElement { get; set; }
        public IElement Element { get; set; }
        public Process Process { get; set; }
        public Interfaces.Selector.Selector Selector { get; set; }
        public IBodyActivity a { get; set; }
        public bool SupportInput { get; set; }
        public bool SupportSelect { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }
        public bool ClickHandled { get; set; }
        public bool SupportVirtualClick { get; set; }
        public MouseButton Button { get; set; }
    }

}
