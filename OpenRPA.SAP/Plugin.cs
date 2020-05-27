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

namespace OpenRPA.SAP
{
    public class Plugin : ObservableObject, IRecordPlugin
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        public static treeelement[] _GetRootElements(Selector anchor)
        {
            var result = new List<treeelement>();
            SAPhook.Instance.RefreshSessions();
            if (anchor != null)
            {
                if (!(anchor is SAPSelector SAPselector)) { SAPselector = new SAPSelector(anchor.ToString()); }
                var elements = SAPSelector.GetElementsWithuiSelector(SAPselector, null, 1);
                foreach (var _ele in elements)
                {
                    var e = new SAPTreeElement(null, true, _ele);
                    result.Add(e);

                }
                return result.ToArray();
            }
            else
            {
                foreach (var session in SAPhook.Instance.Sessions)
                {
                    result.Add(new SAPTreeElement(null, true, new SAPElement(session)));
                }
            }
            return result.ToArray();
        }
        public treeelement[] GetRootElements(Selector anchor)
        {
            return Plugin._GetRootElements(anchor);
        }
        public Interfaces.Selector.Selector GetSelector(Selector anchor, Interfaces.Selector.treeelement item)
        {
            var SAPitem = item as SAPTreeElement;
            SAPSelector SAPanchor = anchor as SAPSelector;
            if (SAPanchor == null && anchor != null)
            {
                SAPanchor = new SAPSelector(anchor.ToString());
            }
            return new SAPSelector(SAPitem.SAPElement, SAPanchor, true);
        }
        public event Action<IRecordPlugin, IRecordEvent> OnUserAction;
        public event Action<IRecordPlugin, IRecordEvent> OnMouseMove
        {
            add { }
            remove { }
        }
        public string Name { get => "SAP"; }
        // public string Status => (hook!=null && hook.jvms.Count>0 ? "online":"offline");
        private string _Status = "";
        public string Status { get => _Status; }
        // public SAPhook hook { get; set; } = new SAPhook();
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
            SAPhook.Instance.BeginRecord();
            //SAPhook.Instance.OnMouseClicked += Hook_OnMouseClicked;
        }
        public void Stop()
        {
            // SAPhook.Instance.OnMouseClicked -= Hook_OnMouseClicked;
        }
        // public SAPElement LastElement { get; set; }
        private IRecordEvent LastRecorderEvent;
        public bool ParseUserAction(ref IRecordEvent e)
        {
            if (e.UIElement == null) return false;

            string Processname = "";
            if (e.UIElement.ProcessId > 0)
            {
                var p = System.Diagnostics.Process.GetProcessById(e.UIElement.ProcessId);
                if (p.ProcessName.ToLower() != "saplogon") return false;
                Processname = p.ProcessName;
            } else { return false; }
            LastRecorderEvent = e;

            return true;
        }
        public void Initialize(IOpenRPAClient client)
        {
            try
            {
                SAPhook.Instance.OnRecordEvent += OnRecordEvent;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
            }

        }
        public void OnRecordEvent(SAPElement Element)
        {
            var r = new RecordEvent();
            MouseButton button = MouseButton.Left;

            var selector = new SAPSelector(Element, null, true);
            var a = new GetElement { DisplayName = Element.id + " " + Element.Role + " " + Element.Name };
            a.Selector = selector.ToString();
            a.Image = Element.ImageString();
            a.MaxResults = 1;

            r.a = new GetElementResult(a);
            r.SupportInput = Element.SupportInput;
            r.ClickHandled = true;
            r.Selector = selector;
            r.Element = Element;
            if (LastRecorderEvent != null) button = LastRecorderEvent.Button;

            // Element.Click(true,  e.Button, 0, 0, false, false);
            OnUserAction?.Invoke(this, r);
        }
        public IElement[] GetElementsWithSelector(Selector selector, IElement fromElement = null, int maxresults = 1)
        {
            var result = SAPSelector.GetElementsWithuiSelector(selector as SAPSelector, fromElement, maxresults);
            return result;
        }
        public IElement LaunchBySelector(Selector selector, bool CheckRunning, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        public void CloseBySelector(Selector selector, TimeSpan timeout, bool Force)
        {
            throw new NotImplementedException();
        }
        public bool Match(SelectorItem item, IElement m)
        {
            //var el = new SAPElement(m.RawElement as WindowsAccessBridgeInterop.AccessibleNode);
            //return SAPSelectorItem.Match(item, el);
            return false;
        }
        public bool ParseMouseMoveAction(ref IRecordEvent e)
        {
            if (e.UIElement == null) return false;

            if (e.UIElement.ClassName == null || !e.UIElement.ClassName.StartsWith("SunAwt"))
            {
                if (e.UIElement.ProcessId < 1) return false;
                var p = System.Diagnostics.Process.GetProcessById(e.UIElement.ProcessId);
                if (p.ProcessName.ToLower() != "SAP") return false;
            }
            e.a = null;
            e.ClickHandled = false;
            return true;
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
            var aa = new ActivityAction<SAPElement>();
            var da = new DelegateInArgument<SAPElement>
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
        public UIElement UIElement { get; set; }
        public IElement Element { get; set; }
        public Selector Selector { get; set; }
        public IBodyActivity a { get; set; }
        public bool SupportInput { get; set; }
        public bool SupportSelect { get; set; }
        public bool ClickHandled { get; set; }
        public bool SupportVirtualClick { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }
        public MouseButton Button { get; set; }
    }

}
