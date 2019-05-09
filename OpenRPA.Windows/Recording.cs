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
using Serilog;

namespace OpenRPA.Windows
{
    public class Recording : IRecording
    {
        public static Interfaces.Selector.treeelement[] GetRootElements()
        {
            var result = new List<Interfaces.Selector.treeelement>();
            var automation = AutomationUtil.getAutomation();
            var _rootElement = automation.GetDesktop();
            var _treeWalker = automation.TreeWalkerFactory.GetControlViewWalker();
            //result.Add(new WindowsTreeElement(null, true, automation, _rootElement, _treeWalker));

            var elementNode = _treeWalker.GetFirstChild(_rootElement);
            while (elementNode != null)
            {
                result.Add(new WindowsTreeElement(null, false, automation, elementNode, _treeWalker));
                try
                {
                    Console.WriteLine("Adding " + elementNode.ToString());
                    elementNode = _treeWalker.GetNextSibling(elementNode);
                }
                catch (Exception)
                {
                    elementNode = null;
                }
            }
            return result.ToArray();
        }
        public Interfaces.Selector.treeelement[] GetRootEelements()
        {
            return Recording.GetRootElements();
        }
        public string Name { get => "Windows"; }
        public event Action<IRecording, IRecordEvent> OnUserAction;
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
                re.UIElement = e.Element;
                re.Element = e.Element;
                re.Selector = sel;
                re.X = e.X;
                re.Y = e.Y;

                Log.Debug(e.Element.SupportInput + " / " + e.Element.ControlType);
                re.a = new GetElementResult(a);
                re.SupportInput = e.Element.SupportInput;
                Log.Debug(string.Format("Windows.Recording::OnMouseUp::end {0:mm\\:ss\\.fff}", sw.Elapsed));
                OnUserAction?.Invoke(this, re);
            }));
            thread.IsBackground = true;
            thread.Start();
        }

        public bool parseUserAction(ref IRecordEvent e) { return true; }

        public void Initialize()
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
        public MouseButton Button { get; set; }
    }
}
