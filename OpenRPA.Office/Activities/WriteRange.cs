//using ClosedXML.Excel;
//using ExcelDataReader;
using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Office.Interop;
using Microsoft.Office.Interop.Excel;
//using System.Windows.Forms;

namespace OpenRPA.Office.Activities
{
    [System.ComponentModel.Designer(typeof(WriteRangeDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.readexcel.png")]
    //[designer.ToolboxTooltip(Text = "Read CSV, xls or xlsx file and loads it into a DataSet")]
    public class WriteRange : ExcelActivity
    {
        public WriteRange()
        {
            UseHeaderRow = true;
        }
        [RequiredArgument]
        [System.ComponentModel.Category("Misc")]
        public InArgument<bool> UseHeaderRow { get; set; }

        [System.ComponentModel.Category("Input")]
        public InArgument<string> Cells { get; set; }
        [System.ComponentModel.Category("Input")]
        [RequiredArgument]
        public InArgument<System.Data.DataTable> DataTable { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var useHeaderRow = (UseHeaderRow != null ? UseHeaderRow.Get(context) : false);
            base.Execute(context);
            var dt = DataTable.Get(context);
            var cells = Cells.Get(context);
            Microsoft.Office.Interop.Excel.Range xlRange = null;
            if (string.IsNullOrEmpty(cells))
            {
                xlRange = base.worksheet.UsedRange;
            }
            else
            {
                xlRange = base.worksheet.get_Range(cells);
            }
            var idx = 1;
            //Header
            if(useHeaderRow)
            {
                for (var i = 0; i < dt.Columns.Count; i++)
                {
                    xlRange.Cells[1, i + 1] = dt.Columns[i].ColumnName;

                }
                idx = 2;
            }
            //Datas
            for (var i = 0; i < dt.Rows.Count; i++)
            {
                for (var j = 0; j < dt.Columns.Count; j++)
                {
                    xlRange.Cells[i + idx, j + 1] = dt.Rows[i][j];
                }
            }
        }
    }
}