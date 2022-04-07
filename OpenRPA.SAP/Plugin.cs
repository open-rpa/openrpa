using OpenRPA.Input;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
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
            SAPhook.Instance.RefreshConnections();
            var result = new List<treeelement>();
            if (anchor != null)
            {
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
        public int Priority { get => 50; }
        // public string Status => (hook!=null && hook.jvms.Count>0 ? "online":"offline");
        private string _Status = "Offline";
        public string Status { get => _Status; }
        private void SetStatus(string status)
        {
            _Status = status;
            NotifyPropertyChanged("Status");
        }
        private System.Timers.Timer timer;
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
        public void Initialize(IOpenRPAClient client)
        {
            try
            {
                _ = PluginConfig.auto_launch_sap_bridge;
                _ = PluginConfig.record_with_get_element;
                _ = PluginConfig.bridge_timeout_seconds;
                // SAPhook.Instance.OnRecordEvent += OnRecordEvent;
                SAPhook.Instance.Connected += () => { SetStatus("Online"); };
                SAPhook.Instance.Disconnected += () => { SetStatus("Offline"); };
                if (timer == null)
                {
                    timer = new System.Timers.Timer(5000);
                    timer.Elapsed += Timer_Elapsed;
                }
                timer.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
            try
            {
                SAPhook.Instance.RefreshConnections();
                if (SAPhook.Instance.Connections.Length < 1)
                {
                    SetStatus("Online(-1)");
                }
                else
                {
                    SetStatus("Online(" + SAPhook.Instance.Connections.Length + ")");

                }
            }
            catch (Exception ex)
            {
                Log.Error("Error getting sap sessions: " + ex.Message);
            }
            // timer.Interval = TimeSpan.FromMinutes(5).TotalMilliseconds;
            timer.Interval = TimeSpan.FromSeconds(30).TotalMilliseconds;
            timer.Start();
        }
        public static Plugin Instance { get; set; }
        public void Start()
        {
            if (!SAPhook.Instance.isConnected) return;
            Instance = this;
            var e = new SAPToogleRecordingEvent();
            e.overlay = false;
            e.overlay = Config.local.record_overlay;
            e.mousemove = Config.local.record_overlay;
            var msg = new SAPEvent("beginrecord"); msg.Set(e);
            SAPhook.Instance.SendMessage(msg, TimeSpan.FromSeconds(5));
        }
        public void Stop()
        {
            if (!SAPhook.Instance.isConnected) return;
            SAPhook.Instance.SendMessage(new SAPEvent("endrecord"), TimeSpan.FromSeconds(5));
        }
        // public SAPElement LastElement { get; set; }
        private IRecordEvent LastRecorderEvent;
        public bool ParseUserAction(ref IRecordEvent e)
        {
            if (e.UIElement == null) { Log.Output("UIElement is null"); return false; }
            if (e.UIElement.ProcessId > 0)
            {
                using (var p = System.Diagnostics.Process.GetProcessById(e.UIElement.ProcessId))
                    if (p.ProcessName.ToLower() != "saplogon") { Log.Output("p.ProcessName is not saplogon but " + p.ProcessName); return false; }
            }
            else
            {
                Log.Output("e.UIElement.ProcessId is " + (e.UIElement.ProcessId.ToString()));
                return false;
            }
            LastRecorderEvent = e;
            e.ClickHandled = false;

            if (PluginConfig.record_with_get_element)
            {
                var LastElement = SAPhook.Instance.LastElement;
                if (LastElement == null)
                {
                    Log.Output("Skip adding activity, LastElement is null ( wait a little to sap bridge to load ui tree )");
                    e.a = null;
                    return true;
                }
                var selector = new SAPSelector(LastElement, null, true);
                var a = new GetElement { DisplayName = LastElement.Role + " " + LastElement.Name };
                a.Variables.Add(new Variable<int>("Index", 0));
                a.Variables.Add(new Variable<int>("Total", 0));
                a.Selector = selector.ToString();
                a.Image = LastElement.ImageString();
                a.MaxResults = 1;

                e.Element = LastElement;
                e.Selector = selector;
                e.a = new GetElementResult(a);
                e.SupportInput = LastElement.SupportInput;
                // e.SupportSelect = LastElement.tagname.ToLower() == "select";
                e.OffsetX = e.X - LastElement.Rectangle.X;
                e.OffsetY = e.Y - LastElement.Rectangle.Y;
                e.ClickHandled = true;
                LastElement.Click(true, e.Button, e.X, e.Y, false, false);
            }
            else
            {
                Log.Debug("Skip adding activity, record_with_get_element is false");
                e.a = null;
            }
            return true;
        }
        public void RaiseUserAction(RecordEvent r)
        {
            if (!PluginConfig.record_with_get_element)
            {
                OnUserAction?.Invoke(this, r);
            }
        }
        public IElement[] GetElementsWithSelector(Selector selector, IElement fromElement = null, int maxresults = 1)
        {
            var result = SAPSelector.GetElementsWithuiSelector(selector as SAPSelector, fromElement, 0, maxresults, false);
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
        private int lastid = -1;
        public bool ParseMouseMoveAction(ref IRecordEvent e)
        {
            if (SAPhook.Instance.LastElement == null) return false;
            if (e.UIElement == null) return false;
            if (e.UIElement.ProcessId < 1) return false;
            if (e.UIElement.ProcessId != lastid)
            {
                using (var p = System.Diagnostics.Process.GetProcessById(e.UIElement.ProcessId))
                    if (p.ProcessName.ToLower() != "saplogon") return false;
                lastid = e.UIElement.ProcessId;
            }
            e.Element = SAPhook.Instance.LastElement;
            if (e.Element == null) e.UIElement = null;
            return true;
        }
        void IRecordPlugin.StatusTextMouseUp()
        {
            if (!SAPhook.Instance.isConnected)
            {
                SAPhook.EnsureSAPBridge();
            }
            else
            {
                if (SAPhook.Instance != null && SAPhook.Instance.Connections != null && SAPhook.Instance.Connections.Length < 1)
                {
                    System.Diagnostics.Process.Start("saplogon.exe");
                }
            }
        }
    }
    public class GetElementResult : IBodyActivity
    {
        public GetElementResult(GetElement activity)
        {
            Activity = activity;
        }
        public GetElementResult(InvokeMethod activity)
        {
            Activity = activity;
        }
        public GetElementResult(SetProperty activity)
        {
            Activity = activity;
        }
        public Activity Activity { get; set; }
        public void AddActivity(Activity a, string Name)
        {
            if (Activity is GetElement)
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
