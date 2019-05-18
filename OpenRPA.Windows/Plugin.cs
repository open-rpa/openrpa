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
using FlaUI.Core.AutomationElements.Infrastructure;
using OpenRPA.Input;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;

namespace OpenRPA.Windows
{
    public class Plugin : IPlugin
    {
        public static Interfaces.Selector.treeelement[] _GetRootElements(Selector anchor)
        {
            int CurrentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;

            var result = new List<Interfaces.Selector.treeelement>();
            Task.Run(() =>
            {
                var automation = AutomationUtil.getAutomation();
                var _rootElement = automation.GetDesktop();
                if(anchor!=null)
                {
                    WindowsSelector Windowsselector = anchor as WindowsSelector;
                    if (Windowsselector == null) { Windowsselector = new WindowsSelector(anchor.ToString()); }
                    var elements = WindowsSelector.GetElementsWithuiSelector(Windowsselector, null, 5);
                    if(elements.Count() > 0 )
                    {
                        _rootElement = elements[0].RawElement;
                    }

                }
                var _treeWalker = automation.TreeWalkerFactory.GetControlViewWalker();
                if (_rootElement != null)
                {
                    var elementNode = _treeWalker.GetFirstChild(_rootElement);
                    while (elementNode != null)
                    {
                        if(!elementNode.Properties.ProcessId.IsSupported)
                        {
                            result.Add(new WindowsTreeElement(null, false, automation, elementNode, _treeWalker));
                        } else if(elementNode.Properties.ProcessId.ValueOrDefault != CurrentProcessId)
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
            }).Wait(1000);
            return result.ToArray();
        }
        public Interfaces.Selector.treeelement[] GetRootElements(Selector anchor)
        {
            return Plugin._GetRootElements(anchor);
        }
        public Interfaces.Selector.Selector GetSelector(Selector anchor, Interfaces.Selector.treeelement item)
        {
            var windowsitem = item as WindowsTreeElement;
            WindowsSelector winanchor = anchor as WindowsSelector;
            if (winanchor == null && anchor != null)
            {
                winanchor = new WindowsSelector(anchor.ToString());
            }
            return new WindowsSelector(windowsitem.RawElement, winanchor, true);
        }
        public string Name { get => "Windows"; }
        public event Action<IPlugin, IRecordEvent> OnUserAction;
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
                Log.Debug(string.Format("Windows.Recording::OnMouseUp::begin"));
                var re = new RecordEvent(); re.Button = e.Button;
                var a = new GetElement { DisplayName = e.Element.Id + "-" + e.Element.Name };
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                WindowsSelector sel = null;
                // sel = new WindowsSelector(e.Element.rawElement, null, true);
                sel = new WindowsSelector(e.Element.RawElement, null, false);
                if (sel.Count < 2) return;
                if (sel == null) return;
                a.Selector = sel.ToString();
                a.MaxResults = 1;
                a.Image = e.Element.ImageString();
                re.UIElement = e.Element;
                re.Element = e.Element;
                re.Selector = sel;
                re.X = e.X;
                re.Y = e.Y;

                re.a = new GetElementResult(a);
                re.SupportInput = e.Element.SupportInput;
                Log.Debug(string.Format("Windows.Recording::OnMouseUp::end {0:mm\\:ss\\.fff}", sw.Elapsed));

                OnUserAction?.Invoke(this, re);
            }));
            thread.IsBackground = true;
            thread.Start();
        }
        public bool parseUserAction(ref IRecordEvent e) { return false; }
        public void Initialize()
        {
        }
        public IElement[] GetElementsWithSelector(Selector selector, IElement fromElement = null, int maxresults = 1)
        {
            WindowsSelector winselector = selector as WindowsSelector;
            if(winselector == null)
            {
                winselector = new WindowsSelector(selector.ToString());
            }
            var result = WindowsSelector.GetElementsWithuiSelector(winselector, fromElement, maxresults);
            return result;
        }
        public void LaunchBySelector(Selector selector, TimeSpan timeout)
        {
            IElement[] elements = { };
            var sw = new Stopwatch();
            sw.Start();
            do
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
                        Log.Error(ex, "");
                    }
                    return new UIElement[] { };
                }, TimeSpan.FromMilliseconds(250)).Result;
                if (elements == null)
                {
                    elements = new IElement[] { };
                }
            } while (elements != null && elements.Length == 0 && sw.Elapsed < timeout);

            if (elements.Length > 0)
            {
                return;
            }

            if (selector == null || selector.Count == 0) return;
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

            Process process = null;
            if (isImmersiveProcess)
            {
                process = FlaUI.Core.Tools.WindowsStoreAppLauncher.Launch(applicationUserModelId, arguments);
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("Starting a new instance of " + processname);
                process = Process.Start(new ProcessStartInfo
                {
                    FileName = Environment.ExpandEnvironmentVariables(filename),
                    Arguments = Environment.ExpandEnvironmentVariables(arguments)
                });
            }
            process.WaitForInputIdle();

        }
        public bool Match(SelectorItem item, IElement m)
        {
            return WindowsSelectorItem.Match(item, m.RawElement as AutomationElement);
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
            var aa = new ActivityAction<UIElement>();
            var da = new DelegateInArgument<UIElement>();
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
        public IBodyActivity a { get; set; }
        public Interfaces.Selector.Selector Selector { get; set; }
        public bool SupportInput { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public bool ClickHandled { get; set; }
        public MouseButton Button { get; set; }
    }
}
