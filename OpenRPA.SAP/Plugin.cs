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
                // SAPhook.Instance.OnRecordEvent += OnRecordEvent;
                SAPhook.Instance.Connected += () => { SetStatus("Online"); };
                SAPhook.Instance.Disconnected += () => { SetStatus("Offline"); };
                timer = new System.Timers.Timer(5000);
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
            }
        }
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
            try
            {
                SAPhook.Instance.RefreshConnections();
                if(SAPhook.Instance.isSapRunning)
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
            timer.Interval = TimeSpan.FromMinutes(5).TotalMilliseconds;
            timer.Start();
        }

        public static Plugin Instance { get; set; }
        public void Start()
        {
            Console.WriteLine("Send beginrecord");
            Instance = this;
            var e = new SAPToogleRecordingEvent(); e.overlay = Config.local.record_overlay;
            var msg = new SAPEvent("beginrecord"); msg.Set(e);
            SAPhook.Instance.SendMessage(msg, TimeSpan.FromSeconds(5));
            Console.WriteLine("End beginrecord");
        }
        public void Stop()
        {
            SAPhook.Instance.SendMessage(new SAPEvent("endrecord"), TimeSpan.FromSeconds(5));
        }
        // public SAPElement LastElement { get; set; }
        private IRecordEvent LastRecorderEvent;
        public bool ParseUserAction(ref IRecordEvent e)
        {
            if (e.UIElement == null) return false;
            if (e.UIElement.ProcessId > 0)
            {
                var p = System.Diagnostics.Process.GetProcessById(e.UIElement.ProcessId);
                if (p.ProcessName.ToLower() != "saplogon") return false;
            } else { return false; }
            LastRecorderEvent = e;
            e.a = null;
            e.ClickHandled = false;
            return true;
        }
        public void RaiseUserAction(RecordEvent r)
        {
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
        private int lastid = -1;
        public bool ParseMouseMoveAction(ref IRecordEvent e)
        {
            if (e.UIElement == null) return false;
            if (e.UIElement.ProcessId < 1) return false;
            if(e.UIElement.ProcessId != lastid)
            {
                Console.WriteLine("Get Process");
                var p = System.Diagnostics.Process.GetProcessById(e.UIElement.ProcessId);
                if (p.ProcessName.ToLower() != "saplogon") return false;
                lastid = e.UIElement.ProcessId;
            }            
            e.Element = null;
            e.UIElement = null;
            return true;
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
            if(Activity is GetElement)
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
