using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Reflection;
using FlaUI.Core.Input;
using System.Runtime.InteropServices;

namespace OpenRPA.Windows
{
    [System.ComponentModel.Designer(typeof(GetWindowsDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetWindows), "Resources.toolbox.getuielement.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    [LocalizedToolboxTooltip("activity_getwindows_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_getwindows", typeof(Resources.strings))]
    public class GetWindows : BreakableLoop, System.Activities.Presentation.IActivityTemplateFactory
    {
        public GetWindows()
        {
        }
        [Browsable(false)]
        public ActivityAction<UIElement> Body { get; set; }
        public OutArgument<UIElement[]> Elements { get; set; }
        private Variable<IEnumerator<UIElement>> _elements = new Variable<IEnumerator<UIElement>>("_elements");
        private Variable<UIElement[]> _lastelements = new Variable<UIElement[]>("_lastelements");
        private Variable<Stopwatch> _sw = new Variable<Stopwatch>("_sw");
        [System.ComponentModel.Browsable(false)]
        public Activity LoopAction { get; set; }
        protected override void StartLoop(NativeActivityContext context)
        {

            var result = new List<UIElement>();
            var windows = RuningWindows.GetOpenedWindows();
            using (var automation = AutomationUtil.getAutomation())
            {
                foreach (var window in windows)
                {
                    // Console.WriteLine(window.Value.Title + " " + window.Value.File);
                    var _window = automation.FromHandle(window.Key);
                    result.Add(new UIElement(_window));
                }
            }
            //string path = RuningWindows.GetProcessPath(windowActive.Handle);
            //if (string.IsNullOrEmpty(path)) return;
            //windowActive.File = new System.IO.FileInfo(path);
            //int length = GenericTools.GetWindowTextLength(windowActive.Handle);
            //if (length == 0) return;
            //StringBuilder builder = new StringBuilder(length);
            //GenericTools.GetWindowText(windowActive.Handle, builder, length + 1);
            //windowActive.Title = builder.ToString();




            WindowsCacheExtension ext = context.GetExtension<WindowsCacheExtension>();
            var sw = new Stopwatch();
            sw.Start();
            Log.Selector(string.Format("Windows.GetWindows::begin {0:mm\\:ss\\.fff}", sw.Elapsed));

            // UIElement[] elements = new UIElement[] { };
            UIElement[] elements = result.ToArray();
            context.SetValue(Elements, elements);

            var lastelements = context.GetValue(_lastelements);
            if (lastelements == null) lastelements = new UIElement[] { };
            context.SetValue(_lastelements, elements);
            IEnumerator<UIElement> _enum = elements.ToList().GetEnumerator();
            bool more = _enum.MoveNext();
            if (lastelements.Length == elements.Length && lastelements.Length > 0)
            {
                more = !System.Collections.StructuralComparisons.StructuralEqualityComparer.Equals(lastelements, elements);
            }
            if (more)
            {
                context.SetValue(_elements, _enum);
                context.SetValue(_sw, sw);
                Log.Selector(string.Format("Windows.GetWindows::end:: call ScheduleAction: {0:mm\\:ss\\.fff}", sw.Elapsed));
                IncIndex(context);
                SetTotal(context, elements.Length);
                context.ScheduleAction<UIElement>(Body, _enum.Current, OnBodyComplete);
            }
            else
            {
                Log.Selector(string.Format("Windows.GetWindows:end {0:mm\\:ss\\.fff}", sw.Elapsed));
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<UIElement> _enum = _elements.Get(context);
            Stopwatch sw = _sw.Get(context);
            Log.Selector(string.Format("Windows.GetWindows:OnBodyComplete::begin {0:mm\\:ss\\.fff}", sw.Elapsed));
            bool more = _enum.MoveNext();
            if (more && !breakRequested)
            {
                IncIndex(context);
                Log.Selector(string.Format("Windows.GetWindows:ScheduleAction {0:mm\\:ss\\.fff}", sw.Elapsed));
                context.ScheduleAction<UIElement>(Body, _enum.Current, OnBodyComplete);
            }
            else
            {
                if (LoopAction != null && !breakRequested)
                {
                    context.ScheduleActivity(LoopAction, LoopActionComplete);
                }
            }
            Log.Selector(string.Format("Windows.GetWindows:end {0:mm\\:ss\\.fff}", sw.Elapsed));
        }
        private void LoopActionComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            if (!breakRequested) StartLoop(context);
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            Interfaces.Extensions.AddCacheArgument(metadata, "Elements", Elements);

            metadata.AddImplementationVariable(_elements);
            metadata.AddImplementationVariable(_lastelements);
            metadata.AddImplementationVariable(_sw);
            base.CacheMetadata(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            var da = new DelegateInArgument<UIElement>
            {
                Name = "item"
            };
            Type t = Type.GetType("OpenRPA.Activities.ClickElement, OpenRPA");
            var instance = Activator.CreateInstance(t);
            var fef = new GetWindows();
            fef.Body = new ActivityAction<UIElement>
            {
                Argument = da,
                Handler = (Activity)instance
            };
            return fef;
        }
        public new string DisplayName
        {
            get
            {
                var displayName = base.DisplayName;
                if (displayName == this.GetType().Name)
                {
                    var displayNameAttribute = this.GetType().GetCustomAttributes(typeof(DisplayNameAttribute), true).FirstOrDefault() as DisplayNameAttribute;
                    if (displayNameAttribute != null) displayName = displayNameAttribute.DisplayName;
                }
                return displayName;
            }
            set
            {
                base.DisplayName = value;
            }
        }
    }




    public class InfoWindow
    {
        public IntPtr Handle = IntPtr.Zero;
        public System.IO.FileInfo File = new System.IO.FileInfo(System.Windows.Forms.Application.ExecutablePath);
        public string Title = System.Windows.Forms.Application.ProductName;
        public override string ToString()
        {
            return File.Name + "\t>\t" + Title;
        }
    }//CLASS

    /// <summary>Contains functionality to get info on the open windows.</summary>
    public static class RuningWindows
    {
        /// <summary>Returns a dictionary that contains the handle and title of all the open windows.</summary>
        /// <returns>A dictionary that contains the handle and title of all the open windows.</returns>
        public static IDictionary<IntPtr, InfoWindow> GetOpenedWindows()
        {
            IntPtr shellWindow = GetShellWindow();
            Dictionary<IntPtr, InfoWindow> windows = new Dictionary<IntPtr, InfoWindow>();

            EnumWindows(new EnumWindowsProc(delegate (IntPtr hWnd, int lParam) {
                if (hWnd == shellWindow) return true;
                if (!IsWindowVisible(hWnd)) return true;
                int length = GenericTools.GetWindowTextLength(hWnd);
                if (length == 0) return true;
                StringBuilder builder = new StringBuilder(length);
                GenericTools.GetWindowText(hWnd, builder, length + 1);
                var info = new InfoWindow();
                info.Handle = hWnd;
                info.File = new System.IO.FileInfo(GetProcessPath(hWnd));
                info.Title = builder.ToString();
                windows[hWnd] = info;
                return true;
            }), 0);
            return windows;
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

        public static string GetProcessPath(IntPtr hwnd)
        {
            uint pid = 0;
            GetWindowThreadProcessId(hwnd, out pid);
            if (hwnd != IntPtr.Zero)
            {
                if (pid != 0)
                {
                    using(var process = Process.GetProcessById((int)pid))
                    {
                        if (process != null)
                        {
                            return process.MainModule.FileName.ToString();
                        }
                    }
                }
            }
            return "";
        }

        [DllImport("USER32.DLL")]
        private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

        [DllImport("USER32.DLL")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("USER32.DLL")]
        private static extern IntPtr GetShellWindow();

        //WARN: Only for "Any CPU":
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out uint processId);

    }
}