using Microsoft.VisualBasic.Activities;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Office
{
    public class Plugin : ObservableObject, IPlugin
    {
        public string Name => "Office";
        public string Status => "";
        public event Action<IPlugin, IRecordEvent> OnUserAction;
        public IElement[] GetElementsWithSelector(Selector selector, IElement fromElement = null, int maxresults = 1)
        {
            return new IElement[] { };
        }
        public static treeelement[] _GetRootElements(Selector anchor)
        {
            return new treeelement[] { };
        }
        public treeelement[] GetRootElements(Selector anchor)
        {
            return Plugin._GetRootElements(anchor);
        }
        public Selector GetSelector(Selector anchor, treeelement item)
        {
            return null;
        }
        public void Initialize()
        {
        }
        public void LaunchBySelector(Selector selector, TimeSpan timeout)
        {
        }
        public void CloseBySelector(Selector selector, TimeSpan timeout, bool Force)
        {
        }
        public bool Match(SelectorItem item, IElement m)
        {
            return false;
        }
        public bool parseUserAction(ref IRecordEvent e)
        {
            if (e.UIElement == null) return false;
            if (e.UIElement.ProcessId < 1) return false;
            var p = System.Diagnostics.Process.GetProcessById(e.UIElement.ProcessId);
            if (p.ProcessName.ToLower() != "excel") return false;
            if(e.UIElement.ControlType != "DataItem") return false;

            var app = Activities.officewrap.application;
            var workbook = app.ActiveWorkbook;

            // var a = new Activities.WriteCell<string>{ DisplayName = e.UIElement.Name.Replace("\"", "").Replace(" ", "") };
            var a = new Activities.ReadCell<string> { DisplayName = e.UIElement.Name.Replace("\"", "").Replace(" ", "") };
            a.Cell = e.UIElement.Name.Replace("\"", "").Replace(" ", "");
            a.Filename = workbook.FullName.replaceEnvironmentVariable();
            e.a = new GetElementResult(a);
            e.SupportInput = true;
            e.ClickHandled = true;
            return true;
        }
        public void Start()
        {
        }
        public void Stop()
        {
        }
        public class GetElementResult : IBodyActivity
        {
            public GetElementResult(Activity activity)
            {
                Activity = activity;
            }
            public Activity Activity { get; set; }
            public void AddActivity(Activity a, string Name)
            {
            }
            public void AddInput(string value, IElement element)
            {
                var old = Activity as Activities.ReadCell<string>;
                var a = new Activities.WriteCell<string> { DisplayName = old.DisplayName };
                a.Cell = old.Cell;
                a.Filename = old.Filename;
                a.Value = value;
                Activity = a;
            }
        }

    }
}
