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
    public class Plugin : IPlugin
    {
        public static treeelement[] _GetRootElements(Selector anchor)
        {
            var result = new List<treeelement>();
            Javahook.Instance.refreshJvms();
            if (anchor != null)
            {
                JavaSelector Javaselector = anchor as JavaSelector;
                if (Javaselector == null) { Javaselector = new JavaSelector(anchor.ToString()); }
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
                    result.Add(new JavaTreeElement(null, true, new JavaElement(jvm)));
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
        public event Action<IPlugin, IRecordEvent> OnUserAction;
        public string Name { get => "Java"; }
        public string Status => (hook!=null && hook.jvms.Count>0 ? "online":"offline");
        public Javahook hook { get; set; } = new Javahook();
        public void Start()
        {
            hook.OnMouseClicked += Hook_OnMouseClicked;
        }
        public void Stop()
        {
            hook.OnMouseClicked -= Hook_OnMouseClicked;
        }
        private void Hook_OnJavaShutDown(int vmID)
        {
            Log.Information("JavaShutDown: " + vmID);
        }
        public JavaElement lastElement { get; set; }
        private void Hook_OnMouseClicked(int vmID, WindowsAccessBridgeInterop.AccessibleContextNode ac)
        {
            lastElement = new JavaElement(ac);
            lastElement.SetPath();
            Log.Debug("OnMouseClicked: " + lastElement.id + " " + lastElement.role + " " + lastElement.Name);
            if (lastElement == null) return;

            var re = new RecordEvent(); re.Button = MouseButton.Left;
            var a = new GetElement { DisplayName = lastElement.title };
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            JavaSelector sel = null;
            // sel = new JavaSelector(e.Element.rawElement, null, true);
            sel = new JavaSelector(lastElement, null, true);
            if (sel == null) return;
            if (sel.Count < 2) return;
            a.Selector = sel.ToString();
            a.Image = lastElement.ImageString();
            a.MaxResults = 1;
            re.Element = lastElement;
            re.Selector = sel;
            re.X = lastElement.X;
            re.Y = lastElement.Y;
            re.a = new GetElementResult(a);
            re.SupportInput = lastElement.SupportInput;

            Log.Debug(string.Format("Java.Recording::OnMouseClicked::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            OnUserAction?.Invoke(this, re);
        }
        private void Hook_OnMouseEntered(int vmID, WindowsAccessBridgeInterop.AccessibleContextNode ac)
        {
            lastElement = new JavaElement(ac);
            lastElement.SetPath();
            Log.Verbose("MouseEntered: " + lastElement.id + " " + lastElement.role + " " + lastElement.Name);
        }
        public bool parseUserAction(ref IRecordEvent e)
        {
            if (lastElement == null) return false;
            if (e.UIElement == null) return false;

            if(e.UIElement.ClassName == null || !e.UIElement.ClassName.StartsWith("SunAwt"))
            {
                if (e.UIElement.ProcessId < 1) return false;
                var p = System.Diagnostics.Process.GetProcessById(e.UIElement.ProcessId);
                if (p.ProcessName.ToLower() != "java") return false;
            }
            var selector = new JavaSelector(lastElement, null, true);
            var a = new GetElement { DisplayName = lastElement.id + " " + lastElement.role + " " + lastElement.Name };
            a.Selector = selector.ToString();
            a.Image = lastElement.ImageString();
            a.MaxResults = 1;

            e.a = new GetElementResult(a);
            e.SupportInput = lastElement.SupportInput;
            e.ClickHandled = true;
            lastElement.Click();
            return true;
        }
        public void Initialize()
        {
            Javahook.Instance.init();
            try
            {
                hook.init();
                hook.OnJavaShutDown += Hook_OnJavaShutDown;
                hook.OnMouseEntered += Hook_OnMouseEntered;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
            }

        }
        public IElement[] GetElementsWithSelector(Selector selector, IElement fromElement = null, int maxresults = 1)
        {
            var result = JavaSelector.GetElementsWithuiSelector(selector as JavaSelector, fromElement, maxresults );
            return result;
        }
        public void LaunchBySelector(Selector selector, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        public bool Match(SelectorItem item, IElement m)
        {
            var el = new JavaElement(m.RawElement as WindowsAccessBridgeInterop.AccessibleNode);
            return JavaSelectorItem.Match(item, el);
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
            var aa = new ActivityAction<JavaElement>();
            var da = new DelegateInArgument<JavaElement>();
            da.Name = Name;
            aa.Handler = a;
            ((GetElement)Activity).Body = aa;
            aa.Argument = da;
        }
    }
    public class RecordEvent : IRecordEvent
    {
        public UIElement UIElement { get; set; }
        public IElement Element { get; set; }
        public Selector Selector { get; set; }
        public IBodyActivity a { get; set; }
        public bool SupportInput { get; set; }
        public bool ClickHandled { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public MouseButton Button { get; set; }
    }

}
