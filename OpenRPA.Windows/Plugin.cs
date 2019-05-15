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
        public static Interfaces.Selector.treeelement[] _GetRootElements()
        {
            var result = new List<Interfaces.Selector.treeelement>();
            Task.Run(() =>
            {
                var automation = AutomationUtil.getAutomation();
                var _rootElement = automation.GetDesktop();
                var _treeWalker = automation.TreeWalkerFactory.GetControlViewWalker();
                // Interfaces.Selector.treeelement ui = null;
                //var elementNode = _rootElement;
                //ui = new WindowsTreeElement(null, false, automation, elementNode, _treeWalker);
                if (_rootElement != null)
                {
                    var elementNode = _treeWalker.GetFirstChild(_rootElement);
                    while (elementNode != null)
                    {
                        result.Add(new WindowsTreeElement(null, false, automation, elementNode, _treeWalker));
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
        public Interfaces.Selector.treeelement[] GetRootElements()
        {
            return Plugin._GetRootElements();
        }
        public Interfaces.Selector.Selector GetSelector(Interfaces.Selector.treeelement item)
        {
            var windowsitem = item as WindowsTreeElement;
            return new WindowsSelector(windowsitem.RawElement, null, true);
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
                sel = new WindowsSelector(e.Element.rawElement, null, false);
                if (sel.Count < 2) return;
                if (sel == null) return;
                a.Selector = sel.ToString();
                a.MaxResults = 1;
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
            var result = WindowsSelector.GetElementsWithuiSelector(selector as WindowsSelector, fromElement, maxresults);
            return result;
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
