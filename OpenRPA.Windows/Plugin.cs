using System;
using System.Activities;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.AutomationElements.Infrastructure;
using OpenRPA.Input;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;

namespace OpenRPA.Windows
{
    public class Plugin : ObservableObject, IRecordPlugin
    {
        private static int CurrentProcessId = 0;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        public static treeelement[] _GetRootElements(Selector anchor)
        {
            if (CurrentProcessId == 0) CurrentProcessId = Process.GetCurrentProcess().Id;

            var result = new List<treeelement>();
            Task.Run(() =>
            {
                var automation = AutomationUtil.getAutomation();
                var _rootElement = automation.GetDesktop();
                var _treeWalker = automation.TreeWalkerFactory.GetControlViewWalker();
                if (anchor != null)
                {
                    if (!(anchor is WindowsSelector Windowsselector)) { Windowsselector = new WindowsSelector(anchor.ToString()); }
                    var elements = WindowsSelector.GetElementsWithuiSelector(Windowsselector, null, 5, null);
                    if (elements.Count() > 0)
                    {
                        foreach (var elementNode in elements)
                        {
                            result.Add(new WindowsTreeElement(null, false, automation, elementNode.RawElement, _treeWalker));
                        }
                        //_rootElement = elements[0].RawElement;
                        return;
                    }

                }
                if (_rootElement != null)
                {
                    var elementNode = _treeWalker.GetFirstChild(_rootElement);
                    while (elementNode != null)
                    {
                        if (!elementNode.Properties.ProcessId.IsSupported)
                        {
                            result.Add(new WindowsTreeElement(null, false, automation, elementNode, _treeWalker));
                        }
                        else if (elementNode.Properties.ProcessId.ValueOrDefault != CurrentProcessId)
                        {
                            result.Add(new WindowsTreeElement(null, false, automation, elementNode, _treeWalker));
                        }

                        try
                        {
                            elementNode = _treeWalker.GetNextSibling(elementNode);
                        }
                        catch (Exception)
                        {
                            elementNode = null;
                        }
                    }
                }
            }).Wait(5000);
            return result.ToArray();
        }
        public treeelement[] GetRootElements(Selector anchor)
        {
            return Plugin._GetRootElements(anchor);
        }
        public Selector GetSelector(Selector anchor, treeelement item)
        {
            var windowsitem = item as WindowsTreeElement;
            WindowsSelector winanchor = anchor as WindowsSelector;
            if (winanchor == null && anchor != null)
            {
                winanchor = new WindowsSelector(anchor.ToString());
            }
            return new WindowsSelector(windowsitem.RawElement, winanchor, PluginConfig.enum_selector_properties);
        }
        public string Name { get => "Windows"; }
        public int Priority { get => 10; }
        public string Status => _status;
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
        private string _status = "";
        public event Action<IRecordPlugin, IRecordEvent> OnUserAction;
        public event Action<IRecordPlugin, IRecordEvent> OnMouseMove;
        public void Start()
        {
            InputDriver.Instance.OnMouseUp += OnMouseUp;
            InputDriver.Instance.OnMouseDown += OnMouseDown;
            InputDriver.Instance.OnMouseMove += _OnMouseMove;
        }
        public void Stop()
        {
            InputDriver.Instance.OnMouseUp -= OnMouseUp;
            InputDriver.Instance.OnMouseDown -= OnMouseDown;
            InputDriver.Instance.OnMouseMove -= _OnMouseMove;
        }
        private static readonly object _lock = new object();
        private static bool _processing = false;
        private void OnMouseDown(InputEventArgs e)
        {
            isMouseDown = true;
            var re = new RecordEvent
            {
                Button = e.Button
            }; OnMouseMove?.Invoke(this, re);
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        private void _OnMouseMove(InputEventArgs e)
        {
            if (isMouseDown) return;
            if (CurrentProcessId == 0) CurrentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            var thread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    if (System.Threading.Monitor.TryEnter(_lock, Config.local.thread_lock_timeout_seconds * 1000))
                    {
                        try
                        {
                            if (_processing) return;
                            _processing = true;
                        }
                        finally
                        {
                            System.Threading.Monitor.Exit(_lock);
                        }
                    }
                    try
                    {
                        if (e.Element == null)
                        {
                            var Element = AutomationHelper.GetFromPoint(e.X, e.Y);
                            if (Element != null) e.SetElement(Element);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                    if (System.Threading.Monitor.TryEnter(_lock, Config.local.thread_lock_timeout_seconds * 1000))
                    {
                        try
                        {
                            _processing = false;
                        }
                        finally
                        {
                            System.Threading.Monitor.Exit(_lock);
                        }
                    }
                    if (e.Element == null) return;

                    if (e.Element.RawElement.Properties.ProcessId.IsSupported && e.Element.RawElement.Properties.ProcessId.ValueOrDefault == CurrentProcessId)
                    {
                        return;
                    }
                    var re = new RecordEvent
                    {
                        Button = e.Button,
                        OffsetX = e.X - e.Element.Rectangle.X,
                        OffsetY = e.Y - e.Element.Rectangle.Y,
                        Element = e.Element,
                        UIElement = e.Element,
                        X = e.X,
                        Y = e.Y
                    }; OnMouseMove?.Invoke(this, re);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }));
            thread.IsBackground = true;
            thread.Start();
        }
        private bool isMouseDown = false;
        private void OnMouseUp(InputEventArgs e)
        {
            isMouseDown = false;
            var thread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    Log.Debug("Windows.Recording::OnMouseUp::begin");
                    var re = new RecordEvent
                    {
                        Button = e.Button
                    }; var a = new GetElement { DisplayName = e.Element.Name };
                    a.Variables.Add(new Variable<int>("Index", 0));
                    a.Variables.Add(new Variable<int>("Total", 0));
                    var sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    WindowsSelector sel = null;
                    // sel = new WindowsSelector(e.Element.rawElement, null, true);
                    sel = new WindowsSelector(e.Element.RawElement, null, PluginConfig.enum_selector_properties);
                    if (sel.Count < 2) return;
                    if (sel == null) return;
                    a.Selector = sel.ToString();
                    a.MaxResults = 1;
                    a.Image = e.Element.ImageString();
                    re.OffsetX = e.X - e.Element.Rectangle.X;
                    re.OffsetY = e.Y - e.Element.Rectangle.Y;
                    re.UIElement = e.Element;
                    re.Element = e.Element;
                    re.Selector = sel;
                    re.X = e.X;
                    re.Y = e.Y;
                    if (sel.Count > 3)
                    {
                        var p1 = sel[1].Properties.Where(x => x.Name == "ClassName").FirstOrDefault();
                        var p2 = sel[2].Properties.Where(x => x.Name == "AutomationId").FirstOrDefault();
                        if (p1 != null && p2 != null)
                        {
                            if (p1.Value.StartsWith("Windows.UI") && p2.Value == "SplitViewFrameXAMLWindow") re.SupportVirtualClick = false;
                        }
                    }
                    re.a = new GetElementResult(a);
                    re.SupportInput = e.Element.SupportInput;
                    re.SupportSelect = e.Element.SupportSelect;
                    Log.Debug(string.Format("Windows.Recording::OnMouseUp::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                    OnUserAction?.Invoke(this, re);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }));
            thread.IsBackground = true;
            thread.Start();
        }
        public bool ParseUserAction(ref IRecordEvent e) { return false; }
        public static IOpenRPAClient client { get; set; }
        public void Initialize(IOpenRPAClient client)
        {
            Plugin.client = client;
            // _ = PluginConfig.allow_child_searching;
            _ = PluginConfig.allow_attach;
            _ = PluginConfig.cache_timeout;
            _ = PluginConfig.enable_cache;
            _ = PluginConfig.allow_multiple_hits_mid_selector;
            _ = PluginConfig.enum_selector_properties;
            _ = PluginConfig.get_elements_in_different_thread;
            _ = PluginConfig.search_timeout;
            _ = PluginConfig.traverse_selector_both_ways;
            _ = PluginConfig.enable_windows_detector;
        }
        public IElement[] GetElementsWithSelector(Selector selector, IElement fromElement = null, int maxresults = 1)
        {
            if (!(selector is WindowsSelector winselector))
            {
                winselector = new WindowsSelector(selector.ToString());
            }
            var result = WindowsSelector.GetElementsWithuiSelector(winselector, fromElement, maxresults, null);
            return result;
        }
        public IElement LaunchBySelector(Selector selector, bool CheckRunning, TimeSpan timeout)
        {
            if (selector == null || selector.Count == 0) return null;
            Process process = null;
            var sw = new Stopwatch();
            IElement[] elements = { };
            if (CheckRunning)
            {
                if (PluginConfig.get_elements_in_different_thread)
                {
                    elements = AutomationHelper.RunSTAThread<IElement[]>(() =>
                    {
                        try
                        {
                            Log.Selector("LaunchBySelector in non UI thread");
                            return GetElementsWithSelector(selector, null, 1);
                        }
                        catch (System.Threading.ThreadAbortException ex)
                        {
                            Log.Error(ex.ToString());
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                        return new UIElement[] { };
                    }, TimeSpan.FromMilliseconds(5000)).Result;

                }
                else
                {
                    Log.Selector("LaunchBySelector using UI thread");
                    elements = GetElementsWithSelector(selector, null, 1);
                }
                if (elements == null)
                {
                    elements = new IElement[] { };
                }
                // elements = GetElementsWithSelector(selector, null, 1);
                if (elements.Length > 0)
                {
                    elements[0].Focus();
                    var _window = ((UIElement)elements[0]);
                    return new UIElement(_window.GetWindow());
                }
            }
            var f = selector.First();
            SelectorItemProperty p;
            bool isImmersiveProcess = false;
            string applicationUserModelId = null;
            string filename = null;
            string processname = null;
            string arguments = null;
            p = f.Properties.Where(x => x.Name == "isImmersiveProcess").FirstOrDefault();
            if (p != null) isImmersiveProcess = bool.Parse(p.Value);
            p = f.Properties.Where(x => x.Name == "applicationUserModelId").FirstOrDefault();
            if (p != null) applicationUserModelId = p.Value;
            p = f.Properties.Where(x => x.Name == "filename").FirstOrDefault();
            if (p != null) filename = p.Value;
            p = f.Properties.Where(x => x.Name == "processname").FirstOrDefault();
            if (p != null) processname = p.Value;
            p = f.Properties.Where(x => x.Name == "arguments").FirstOrDefault();
            if (p != null) arguments = p.Value;
            if (isImmersiveProcess)
            {
                process = FlaUI.Core.Tools.WindowsStoreAppLauncher.Launch(applicationUserModelId, arguments);
            }
            else
            {
                Log.Debug("Starting a new instance of " + processname);
                process = Process.Start(new ProcessStartInfo
                {
                    FileName = Environment.ExpandEnvironmentVariables(filename),
                    Arguments = Environment.ExpandEnvironmentVariables(arguments)
                });
            }
            try
            {
                GenericTools.Restore(process.MainWindowHandle);
            }
            catch (Exception ex)
            {
                Log.Error("restore window: " + ex.ToString());
            }
            try
            {
                // process.WaitForInputIdle();
            }
            catch (Exception ex)
            {
                Log.Error("WaitForInputIdle window: " + ex.ToString());
            }


            sw = new Stopwatch();
            sw.Start();
            if (timeout < TimeSpan.FromSeconds(10))
            {
                timeout = TimeSpan.FromSeconds(10);
            }
            do
            {
                if (PluginConfig.get_elements_in_different_thread)
                {
                    elements = AutomationHelper.RunSTAThread<IElement[]>(() =>
                    {
                        try
                        {
                            Log.Selector("LaunchBySelector in non UI thread");
                            return GetElementsWithSelector(selector, null, 1);
                        }
                        catch (System.Threading.ThreadAbortException ex)
                        {
                            Log.Error(ex.ToString());
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                        return new UIElement[] { };
                    }, TimeSpan.FromMilliseconds(5000)).Result;

                }
                else
                {
                    Log.Selector("LaunchBySelector using UI thread");
                    elements = GetElementsWithSelector(selector, null, 1);
                }
                if (elements == null)
                {
                    elements = new IElement[] { };
                }
            } while (elements != null && elements.Length == 0 && sw.Elapsed < timeout);
            WindowsSelectorItem.ClearCache();
            if (elements.Length > 0)
            {
                var window = ((UIElement)elements[0]);
                return new UIElement(window.GetWindow());
            }
            else
            {
                return null;
            }

        }
        public bool Match(SelectorItem item, IElement m)
        {
            return WindowsSelectorItem.Match(item, m.RawElement as AutomationElement);
        }
        public void CloseBySelector(Selector selector, TimeSpan timeout, bool Force)
        {
            IElement[] elements = { };
            if (PluginConfig.get_elements_in_different_thread)
            {
                elements = OpenRPA.AutomationHelper.RunSTAThread<IElement[]>(() =>
                {
                    try
                    {
                        return GetElementsWithSelector(selector, null, 1);
                    }
                    catch (System.Threading.ThreadAbortException)
                    {
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                    return new UIElement[] { };
                }, TimeSpan.FromMilliseconds(5000)).Result;
            }
            else
            {
                elements = GetElementsWithSelector(selector, null, 1);
            }
            if (elements == null)
            {
                elements = new IElement[] { };
            }
            // elements = GetElementsWithSelector(selector, null, 1);
            if (elements.Length > 0)
            {
                //using (var automation = Interfaces.AutomationUtil.getAutomation())
                //{
                //    foreach (var _ele in elements)
                //    {
                //        var element = _ele.RawElement as AutomationElement;
                //        using (var app = new FlaUI.Core.Application(element.Properties.ProcessId.Value, false))
                //        {
                //            app.Close();
                //        }

                //    }
                //}
                foreach (var _ele in elements)
                {
                    var element = _ele.RawElement as AutomationElement;
                    if (element.Properties.ProcessId.IsSupported)
                    {
                        var processid = element.Properties.ProcessId.Value;
                        using (var Process = System.Diagnostics.Process.GetProcessById(processid))
                            Process.Kill();
                    }
                }
            }
        }
        public bool ParseMouseMoveAction(ref IRecordEvent e)
        {
            return true;
        }
        void IRecordPlugin.StatusTextMouseUp()
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
        public void AddActivity(Activity a, string Name)
        {
            var aa = new ActivityAction<UIElement>();
            var da = new DelegateInArgument<UIElement>
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
                if (Config.local.use_sendkeys)
                {
                    AddActivity(new System.Activities.Statements.Assign<string>
                    {
                        To = new Microsoft.VisualBasic.Activities.VisualBasicReference<string>("item.SendKeys"),
                        Value = value
                    }, "item");
                    (element as UIElement).SendKeys = value;

                }
                else
                {
                    AddActivity(new System.Activities.Statements.Assign<string>
                    {
                        To = new Microsoft.VisualBasic.Activities.VisualBasicReference<string>("item.Value"),
                        Value = value
                    }, "item");
                    element.Value = value;

                }
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
        public IBodyActivity a { get; set; }
        public Interfaces.Selector.Selector Selector { get; set; }
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
