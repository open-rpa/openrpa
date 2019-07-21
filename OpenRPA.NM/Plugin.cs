using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.NM
{
    public class Plugin : ObservableObject, IPlugin
    {
        public NMElement lastElement { get; set; }
        public string Name => "NM";
        public string Status => (NMHook.connected ? "online" : "offline");
        public event Action<IPlugin, IRecordEvent> OnUserAction;
        public IElement[] GetElementsWithSelector(Selector selector, IElement fromElement = null, int maxresults = 1)
        {
            NMSelector nmselector = selector as NMSelector;
            if (nmselector == null)
            {
                nmselector = new NMSelector(selector.ToString());
            }
            var result = NMSelector.GetElementsWithuiSelector(nmselector, fromElement, maxresults);
            return result;
        }
        public static treeelement[] _GetRootElements(Selector anchor)
        {
            var rootelements = new List<treeelement>();

            NMHook.reloadtabs();
            // var tab = NMHook.tabs.Where(x => x.highlighted == true && x.browser == "chrome").FirstOrDefault();
            var tab = NMHook.tabs.Where(x => x.highlighted == true).FirstOrDefault();
            if (tab == null)
            {
                // tab = NMHook.tabs.Where(x => x.browser == "chrome").FirstOrDefault();
                tab = NMHook.tabs.FirstOrDefault();
            }
            if(NMHook.tabs.Count==0) { return rootelements.ToArray(); }
            // getelement.data = "getdom";
            var getelement = new NativeMessagingMessage("getelement");
            getelement.browser = tab.browser;
            getelement.tabid = tab.id;
            getelement.xPath = "/html";
            NativeMessagingMessage result = null;
            try
            {
                result = NMHook.sendMessageResult(getelement, true, TimeSpan.FromSeconds(2));
            }
            catch (Exception)
            {
            }
            if (result != null && result.result != null && result.results == null)
            {
                result.results = new NativeMessagingMessage[] { result };
            }
            if (result != null && result.results != null && result.results.Count() > 0)
            {
                foreach (var res in result.results)
                {
                    if (res.result != null)
                    {
                        //var html = new HtmlElement(getelement.xPath, getelement.cssPath, res.tabid, res.frameId, res.result);
                        var html = new NMElement(res);
                        rootelements.Add(new NMTreeElement(null, true, html));
                    }
                }
                //result = result.results[0];
            }
            return rootelements.ToArray();
        }
        public treeelement[] GetRootElements(Selector anchor)
        {
            return Plugin._GetRootElements(anchor);
        }
        public Selector GetSelector(Selector anchor, treeelement item)
        {
            var nmitem = item as NMTreeElement;
            NMSelector nmanchor = anchor as NMSelector;
            if (nmitem == null && anchor != null)
            {
                nmanchor = new NMSelector(anchor.ToString());
            }
            return new NMSelector(nmitem.NMElement, nmanchor, true);
        }
        public void Initialize()
        {
            NMHook.registreChromeNativeMessagingHost(false);
            NMHook.registreffNativeMessagingHost(false);
            NMHook.checkForPipes(true, true);
            NMHook.onMessage += onMessage;
            NMHook.Connected += omConnected;
            NMHook.onDisconnected += onDisconnected;
        }
        private void omConnected(string obj)
        {
            NotifyPropertyChanged("Status");
        }
        private void onDisconnected(string obj)
        {
            NotifyPropertyChanged("Status");
        }
        private void onMessage(NativeMessagingMessage message)
        {
            if (message.uiy > 0 && message.uix > 0 && message.uiwidth > 0 && message.uiheight > 0)
            {
                if (!string.IsNullOrEmpty(message.data))
                {
                    lastElement = new NMElement(message);
                }
                else
                {
                    lastElement = new NMElement(message);
                }
            }

            if (message.functionName == "click")
            {
                if(recording)
                {
                    if (lastElement == null) return;
                    var re = new RecordEvent(); re.Button = Input.MouseButton.Left;
                    var a = new GetElement { DisplayName = lastElement.ToString() };

                    var selector = new NMSelector(lastElement, null, true);
                    a.Selector = selector.ToString();
                    a.Image = lastElement.ImageString();
                    a.MaxResults = 1;

                    re.Selector = selector;
                    re.a = new GetElementResult(a);
                    re.SupportInput = lastElement.SupportInput;
                    re.ClickHandled = true;
                    OnUserAction?.Invoke(this, re);
                    return;
                }
            }
        }
        public void LaunchBySelector(Selector selector, TimeSpan timeout)
        {
            var first = selector[0];
            var second = selector[1];
            var p = first.Properties.Where(x => x.Name == "browser").FirstOrDefault();
            string browser = "";
            if (p != null) { browser = p.Value; }

            p = first.Properties.Where(x => x.Name == "url").FirstOrDefault();
            string url = "";
            if (p != null) { url = p.Value; }

            NMHook.openurl(browser, url);

        }
        public void CloseBySelector(Selector selector, TimeSpan timeout, bool Force)
        {
            var first = selector[0];
            var second = selector[1];
            var p = first.Properties.Where(x => x.Name == "browser").FirstOrDefault();
            string browser = "";
            if (p != null) { browser = p.Value; }

            p = first.Properties.Where(x => x.Name == "url").FirstOrDefault();
            string url = "";
            if (p != null) { url = p.Value; }
            NMHook.reloadtabs();
            var tabs = NMHook.tabs.Where(x => x.browser == browser && x.url == url).ToList();
            if(string.IsNullOrEmpty(url)) tabs = NMHook.tabs.Where(x => x.browser == browser).ToList();
            foreach (var tab in tabs)
            {
                NMHook.CloseTab(tab);
            }
        }
        public bool Match(SelectorItem item, IElement m)
        {
            var el = new NMElement(m.RawElement as NativeMessagingMessage);
            return NMSelectorItem.Match(item, el);
        }
        public bool parseUserAction(ref IRecordEvent e)
        {
            if (lastElement == null) return false;
            if (e.UIElement == null) return false;

            if (e.UIElement.ProcessId < 1) return false;
            var p = System.Diagnostics.Process.GetProcessById(e.UIElement.ProcessId);
            if (p.ProcessName.ToLower() != "chrome" && p.ProcessName.ToLower() != "firefox") return false;

            var selector = new NMSelector(lastElement, null, true);
            var a = new GetElement { DisplayName = lastElement.id + " " + lastElement.type + " " + lastElement.Name };
            a.Selector = selector.ToString();
            a.Image = lastElement.ImageString();
            a.MaxResults = 1;

            e.Element = lastElement;
            e.Selector = selector;
            e.a = new GetElementResult(a);
            e.SupportInput = lastElement.SupportInput;
            e.ClickHandled = true;
            e.OffsetX = e.X - lastElement.Rectangle.X;
            e.OffsetY = e.Y - lastElement.Rectangle.Y;
            lastElement.Click(true, e.Button, e.X, e.Y);
            return true;
        }
        public bool recording { get; set; } = false;
        public void Start()
        {
            recording = true;
        }
        public void Stop()
        {
            recording = false;
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
            var aa = new ActivityAction<NMElement>();
            var da = new DelegateInArgument<NMElement>();
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
        public RecordEvent() { SupportVirtualClick = true; }
        public UIElement UIElement { get; set; }
        public IElement Element { get; set; }
        public Selector Selector { get; set; }
        public IBodyActivity a { get; set; }
        public bool SupportInput { get; set; }
        public bool ClickHandled { get; set; }
        public bool SupportVirtualClick { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }
        public Input.MouseButton Button { get; set; }
    }
}
