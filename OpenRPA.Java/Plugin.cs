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
        public static treeelement[] _GetRootElements()
        {
            var result = new List<treeelement>();
            Javahook.Instance.refreshJvms();
            foreach (var jvm in Javahook.Instance.jvms)
            {
                result.Add(new JavaTreeElement(null, true, new JavaElement(jvm)));
            }
            return result.ToArray();
        }
        public treeelement[] GetRootElements()
        {
            return Plugin._GetRootElements();
        }
        public Interfaces.Selector.Selector GetSelector(Interfaces.Selector.treeelement item)
        {
            var javaitem = item as JavaTreeElement;
            return new JavaSelector(javaitem.JavaElement, null, true);
        }
        public event Action<IPlugin, IRecordEvent> OnUserAction;
        public string Name { get => "Java"; }
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
            Console.WriteLine("JavaShutDown: " + vmID);
        }
        public JavaElement lastElement { get; set; }
        private void Hook_OnMouseClicked(int vmID, WindowsAccessBridgeInterop.AccessibleContextNode ac)
        {
            lastElement = new JavaElement(ac);
            lastElement.SetPath();
            Console.WriteLine("OnMouseClicked: " + lastElement.id + " " + lastElement.role + " " + lastElement.Name);
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
            Console.WriteLine("MouseEntered: " + lastElement.id + " " + lastElement.role + " " + lastElement.Name);
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

            e.a = new GetElementResult(a);
            e.SupportInput = lastElement.SupportInput;
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
