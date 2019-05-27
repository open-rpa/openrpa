using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace OpenRPA.Office.Activities
{
    [System.ComponentModel.Designer(typeof(ReadCellDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.comment.png")]
    [System.Activities.Presentation.DefaultTypeArgument(typeof(String))]
    public class ReadCell<TResult> : ExcelActivityOf<TResult>
    {
        [RequiredArgument]
        [System.ComponentModel.Category("Input")]
        public InArgument<string> Cell { get; set; }
        [System.ComponentModel.Category("Output")]
        public OutArgument<string> Formula { get; set; }
        [System.ComponentModel.Category("Output")]
        public OutArgument<Microsoft.Office.Interop.Excel.Range> Range { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            base.Execute(context);
            var cell = Cell.Get(context);
            //Microsoft.Office.Interop.Excel.Range range = worksheet.get_Range(cell, cell);
            Microsoft.Office.Interop.Excel.Range range = worksheet.get_Range(cell);
            //Microsoft.Office.Interop.Excel.Range range = (Microsoft.Office.Interop.Excel.Range)excelWorksheet.UsedRange;
            //string sValue = (range.Cells[1, 1] as Microsoft.Office.Interop.Excel.Range).Value2.ToString();
            //string sValue = null;
            //var v = range.Value2;
            //if (v != null)
            //{
            //    sValue = range.Value2.ToString();
            //}
            //Value.Set(context, sValue);
            Formula.Set(context, range.Formula);
            Range.Set(context, range);
            //var range = worksheet.get_Range("A1", "A" + Convert.ToString(worksheet.UsedRange.Rows.Count));
            if(this.ResultType == typeof(string))
            {
                if(range.Value2==null) context.SetValue(Result, null);
                try
                {
                    context.SetValue(Result, range.Value2.ToString());
                }
                catch (Exception)
                {
                    //Trace.WriteLine(ex.ToString(), "Debug");
                    context.SetValue(Result, null);
                }
                
            }
            else
            {
                context.SetValue(Result, range.Value2);
            }
            //cleanup();
        }
    }
}