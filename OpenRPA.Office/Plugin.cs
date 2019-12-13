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
    public class Plugin : ObservableObject, IRecordPlugin
    {
        public string Name => "Office";
        public string Status => "";
        public event Action<IRecordPlugin, IRecordEvent> OnUserAction;
        public event Action<IRecordPlugin, IRecordEvent> OnMouseMove;
        public System.Windows.Controls.UserControl editor => null;
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
        public void Initialize(IOpenRPAClient client)
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
            if (e.UIElement.ControlType != "DataItem") return false;
            try
            {
                var app = Activities.officewrap.application;
                var workbook = app.ActiveWorkbook;
                if (workbook == null) return false;

                var a = new Activities.ReadCell<string> { DisplayName = e.UIElement.Name.Replace("\"", "").Replace(" ", "") };
                a.Cell = e.UIElement.Name.Replace("\"", "").Replace(" ", "");
                a.Filename = workbook.FullName.replaceEnvironmentVariable();
                e.a = new GetElementResult(a);
                e.SupportInput = true;
                e.SupportSelect = false;
                e.ClickHandled = true;
                return true;

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        public void Start()
        {
        }
        public void Stop()
        {
        }
        public bool parseMouseMoveAction(ref IRecordEvent e)
        {
            if (e.UIElement == null) return false;
            if (e.UIElement.ProcessId < 1) return false;
            var p = System.Diagnostics.Process.GetProcessById(e.UIElement.ProcessId);
            if (p.ProcessName.ToLower() != "excel") return false;
            if (e.UIElement.ControlType != "DataItem") return false;
            e.UIElement = null;
            e.Element = null;
            return true;
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
                try
                {
                    var old = Activity as Activities.ReadCell<string>;

                    double d;
                    var isDouble = double.TryParse(value, out d);
                    int i;
                    var isInt = int.TryParse(value, out i);
                    if (isInt)
                    {
                        var a = new Activities.WriteCell<int> { DisplayName = old.DisplayName };
                        a.Cell = old.Cell;
                        a.Filename = old.Filename;
                        a.Value = i;
                        Activity = a;
                    }
                    else if (isDouble)
                    {
                        var a = new Activities.WriteCell<double> { DisplayName = old.DisplayName };
                        a.Cell = old.Cell;
                        a.Filename = old.Filename;
                        a.Value = d;
                        Activity = a;
                    }
                    else
                    {
                        var a = new Activities.WriteCell<string> { DisplayName = old.DisplayName };
                        a.Cell = old.Cell;
                        a.Filename = old.Filename;
                        a.Value = value;
                        Activity = a;
                    }

                    var app = Activities.officewrap.application;
                    var workbook = app.ActiveWorkbook;
                    // if (workbook == null) workbook = app.ThisWorkbook;
                    if (workbook == null) return;
                    var worksheet = workbook.ActiveSheet as Microsoft.Office.Interop.Excel.Worksheet;
                    var c = old.Cell.Expression.ToString();
                    Microsoft.Office.Interop.Excel.Range range = worksheet.get_Range(c);
                    range.Value2 = value;

                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }

    }
}
