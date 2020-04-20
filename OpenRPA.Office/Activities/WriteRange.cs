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
using OpenRPA.Interfaces;

namespace OpenRPA.Office.Activities
{
    [System.ComponentModel.Designer(typeof(WriteRangeDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.readexcel.png")]
    [LocalizedToolboxTooltip("activity_writerange_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_writerange", typeof(Resources.strings))]
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

            object[,] arr = new object[dt.Rows.Count, dt.Columns.Count];
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                System.Data.DataRow dr = dt.Rows[r];
                for (int c = 0; c < dt.Columns.Count; c++)
                {
                    arr[r, c] = dr[c];
                }
            }
            // Microsoft.Office.Interop.Excel.Range c1 = (Microsoft.Office.Interop.Excel.Range)worksheet.Cells[topRow, 1];
            Microsoft.Office.Interop.Excel.Range c2 = (Microsoft.Office.Interop.Excel.Range)worksheet.Cells[xlRange.Row + dt.Rows.Count - 1, xlRange.Column + dt.Columns.Count];
            Microsoft.Office.Interop.Excel.Range range = worksheet.get_Range(xlRange, c2);
            //range.Value = arr;
            range.set_Value(Microsoft.Office.Interop.Excel.XlRangeValueDataType.xlRangeValueDefault, arr);

            //for (var i = 0; i < dt.Rows.Count; i++)
            //{
            //    for (var j = 0; j < dt.Columns.Count; j++)
            //    {
            //        xlRange.Cells[i + idx, j + 1] = dt.Rows[i][j];
            //    }
            //}
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
}