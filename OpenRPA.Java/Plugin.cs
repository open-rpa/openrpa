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

namespace OpenRPA.Java
{
    public class Plugin : ObservableObject, IRecordPlugin
    {

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public static JavaElement[] EnumRoots(WindowsAccessBridgeInterop.AccessibleJvm jvm)
        {
            var results = new List<JavaElement>();
            var children = jvm.GetChildren();
            if (children != null && children.Count() > 0)
            {
                var firstac = children.First() as WindowsAccessBridgeInterop.AccessibleContextNode;
                var hwnd = jvm.AccessBridge.Functions.GetHWNDFromAccessibleContext(jvm.JvmId, firstac.AccessibleContextHandle);
                RECT rect = new RECT();
                GetWindowRect(hwnd, ref rect);

                int x = rect.Left + ((rect.Right - rect.Left) / 2);
                int y = rect.Top + ((rect.Bottom - rect.Top) / 2);
                var res = firstac.GetNodePathAtUsingAccessBridge(new System.Drawing.Point(x, y));
                if (res != null)
                {
                    var Root = new JavaElement(res.Root);
                    var Parent = Root;
                    while (Parent.Parent != null) Parent = Parent.Parent;
                    if (!results.Contains(Parent)) results.Add(Parent);
                }


                //for(var x= rect.Left; x < rect.Right; x += 10)
                //{
                //    for (var y = rect.Top; y < rect.Bottom; y += 10)
                //    {
                //        var res = firstac.GetNodePathAtUsingAccessBridge(new System.Drawing.Point(x, y));
                //        if (res != null)
                //        {
                //            var Root = new JavaElement(res.Root);
                //            var Parent = Root;
                //            while (Parent.Parent != null) Parent = Parent.Parent;
                //            if(!results.Contains(Parent)) results.Add(Parent);
                //        }
                //    }
                //}
            }


            return results.ToArray();
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        public static treeelement[] _GetRootElements(Selector anchor)
        {
            var result = new List<treeelement>();
            Javahook.Instance.refreshJvms();
            if (anchor != null)
            {
                if (!(anchor is JavaSelector Javaselector)) { Javaselector = new JavaSelector(anchor.ToString()); }
                var elements = JavaSelector.GetElementsWithuiSelector(Javaselector, null, 1);
                foreach (var _ele in elements)
                {
                    var e = new JavaTreeElement(null, true, _ele);
                    result.Add(e);

                }
                return result.ToArray();
            }
            else
            {
                foreach (var jvm in Javahook.Instance.jvms)
                {
                    var item = new JavaTreeElement(null, true, new JavaElement(jvm));
                    result.Add(item);
                    foreach (var e in Plugin.EnumRoots(jvm))
                    {
                        item.Children.Add(new JavaTreeElement(item, true, e));
                        // result.Add(new JavaTreeElement(item, true, e));
                    }
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
            var javaitem = item as JavaTreeElement;
            JavaSelector javaanchor = anchor as JavaSelector;
            if (javaanchor == null && anchor != null)
            {
                javaanchor = new JavaSelector(anchor.ToString());
            }
            return new JavaSelector(javaitem.JavaElement, javaanchor, true);
        }
        public event Action<IRecordPlugin, IRecordEvent> OnUserAction;
        public event Action<IRecordPlugin, IRecordEvent> OnMouseMove
        {
            add { }
            remove { }
        }
        public string Name { get => "Java"; }
        // public string Status => (hook!=null && hook.jvms.Count>0 ? "online":"offline");
        private string _Status = "";
        public string Status { get => _Status; }
        public int Priority { get => 60; }
        private void SetStatus(string status)
        {
            _Status = status;
            NotifyPropertyChanged("Status");
        }
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
            Javahook.Instance.init();
            Javahook.Instance.Connected += () => { SetStatus("Online"); };
            Javahook.Instance.Disconnected += () => { SetStatus("Offline"); };
            InputDriver.Instance.OnMouseUp += OnMouseUp;
        }
        public void Stop()
        {
            InputDriver.Instance.OnMouseUp -= OnMouseUp;
        }
        private void OnMouseUp(InputEventArgs e)
        {
            JavaElement foundElement = null;
            foreach (var jvm in Javahook.Instance.jvms)
            {
                var _children = jvm.GetChildren();
                if (_children.Count() > 0)
                {
                    var firstac = _children.First() as WindowsAccessBridgeInterop.AccessibleContextNode;
                    var res = firstac.GetNodePathAtUsingAccessBridge(new System.Drawing.Point(e.X, e.Y));
                    if (res != null)
                    {
                        var Root = new JavaElement(res.Root);
                        var Parent = Root;
                        while (Parent.Parent != null) Parent = Parent.Parent;
                        if (res.Count > 0)
                        {
                            foundElement = new JavaElement(res.Last());
                        }
                    }
                }
            }
            if (foundElement == null) return;


            foundElement.SetPath();
            Log.Debug("OnMouseClicked: " + foundElement.id + " " + foundElement.role + " " + foundElement.Name);
            if (foundElement == null) return;

            var re = new RecordEvent
            {
                Button = MouseButton.Left
            }; var a = new GetElement { DisplayName = foundElement.title };
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            // sel = new JavaSelector(e.Element.rawElement, null, true);
            JavaSelector sel = new JavaSelector(foundElement, null, true);
            if (sel == null) return;
            if (sel.Count < 2) return;
            a.Selector = sel.ToString();
            a.Image = foundElement.ImageString();
            a.MaxResults = 1;
            re.Element = foundElement;
            re.Selector = sel;
            re.X = foundElement.X;
            re.Y = foundElement.Y;
            re.a = new GetElementResult(a);
            re.SupportInput = foundElement.SupportInput;
            re.SupportSelect = false;

            Log.Debug(string.Format("Java.Recording::OnMouseClicked::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            OnUserAction?.Invoke(this, re);
        }
        private void Hook_OnJavaShutDown(int vmID)
        {
            Log.Information("JavaShutDown: " + vmID);
            NotifyPropertyChanged("Status");
        }
        // public JavaElement LastElement { get; set; }
        public bool ParseUserAction(ref IRecordEvent e)
        {
            if (e.UIElement == null) return false;

            JavaElement foundElement = null;
            foreach (var jvm in Javahook.Instance.jvms)
            {
                var _children = jvm.GetChildren();
                if (_children.Count() > 0)
                {
                    var firstac = _children.First() as WindowsAccessBridgeInterop.AccessibleContextNode;
                    var res = firstac.GetNodePathAtUsingAccessBridge(new System.Drawing.Point(e.X, e.Y));
                    if (res != null)
                    {
                        var Root = new JavaElement(res.Root);
                        var Parent = Root;
                        while (Parent.Parent != null) Parent = Parent.Parent;
                        if (res.Count > 0)
                        {
                            foundElement = new JavaElement(res.Last());
                        }
                    }
                }
            }
            if (foundElement == null) return false;

            var selector = new JavaSelector(foundElement, null, true);
            var a = new GetElement { DisplayName = foundElement.id + " " + foundElement.role + " " + foundElement.Name };
            a.Selector = selector.ToString();
            a.Image = foundElement.ImageString();
            a.MaxResults = 1;

            e.a = new GetElementResult(a);
            e.SupportInput = foundElement.SupportInput;
            e.ClickHandled = true;
            e.Selector = selector;
            e.Element = foundElement;
            foundElement.Click(true, e.Button, 0, 0, false, false);
            return true;
        }
        public void Initialize(IOpenRPAClient client)
        {
            try
            {
                Javahook.Instance.OnInitilized += Hook_OnInitilized;
                Javahook.Instance.OnJavaShutDown += Hook_OnJavaShutDown;
                Javahook.Instance.OnNewjvm += Hook_OnNewjvm;
                Javahook.Instance.init();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
            }
        }
        private void Hook_OnNewjvm(WindowsAccessBridgeInterop.AccessBridge accessBridge, WindowsAccessBridgeInterop.AccessibleJvm[] newjvms)
        {
            _Status = "Online(" + Javahook.Instance.jvms.Count + ")";
            NotifyPropertyChanged("Status");
        }
        private void Hook_OnInitilized(WindowsAccessBridgeInterop.AccessBridge accessBridge)
        {
            _Status = "Online(" + Javahook.Instance.jvms.Count + ")";
            NotifyPropertyChanged("Status");
        }
        public IElement[] GetElementsWithSelector(Selector selector, IElement fromElement = null, int maxresults = 1)
        {
            if (!(selector is JavaSelector javaselector))
            {
                javaselector = new JavaSelector(selector.ToString());
            }
            var result = JavaSelector.GetElementsWithuiSelector(javaselector, fromElement, maxresults);
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
            var el = new JavaElement(m.RawElement as WindowsAccessBridgeInterop.AccessibleNode);
            return JavaSelectorItem.Match(item, el);
        }
        public bool ParseMouseMoveAction(ref IRecordEvent e)
        {
            if (e.UIElement == null) return false;

            JavaElement foundElement = null;
            foreach (var jvm in Javahook.Instance.jvms)
            {
                var _children = jvm.GetChildren();
                if (_children.Count() > 0)
                {
                    var firstac = _children.First() as WindowsAccessBridgeInterop.AccessibleContextNode;
                    var res = firstac.GetNodePathAtUsingAccessBridge(new System.Drawing.Point(e.X, e.Y));
                    if (res != null)
                    {
                        var Root = new JavaElement(res.Root);
                        var Parent = Root;
                        while (Parent.Parent != null) Parent = Parent.Parent;
                        if (res.Count > 0)
                        {
                            foundElement = new JavaElement(res.Last());
                        }
                    }
                }
            }
            if (foundElement == null) return false;
            e.Element = foundElement;
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
            var aa = new ActivityAction<JavaElement>();
            var da = new DelegateInArgument<JavaElement>
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
