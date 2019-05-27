//using ClosedXML.Excel;
//using ExcelDataReader;
using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Office.Interop.Excel;
//using System.Windows.Forms;

namespace OpenRPA.Office.Activities
{
    [System.ComponentModel.Designer(typeof(WriteCellDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.readexcel.png")]
    //[designer.ToolboxTooltip(Text = "Read CSV, xls or xlsx file and loads it into a DataSet")]
    public class WriteCell<TResult> : ExcelActivityOf<TResult>
    {

        [RequiredArgument]
        [System.ComponentModel.Category("Input")]
        public InArgument<string> Cell { get; set; }
        [System.ComponentModel.Category("Input")]
        public InArgument<TResult> Value { get; set; }
        [System.ComponentModel.Category("Input")]
        public InArgument<string> Formula { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            base.Execute(context);
            var cell = Cell.Get(context);
            var value = Value.Get(context);
            var formula = Formula.Get(context);
            Microsoft.Office.Interop.Excel.Range range = worksheet.get_Range(cell);
            if(!string.IsNullOrEmpty(formula))
            {
                range.Formula = formula;
            }
            else
            {
                range.Value2 = value;
            }
            //cleanup();
        }



    }
}