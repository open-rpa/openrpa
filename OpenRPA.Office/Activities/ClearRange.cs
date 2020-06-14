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
    using Microsoft.Office.Interop;
    [System.ComponentModel.Designer(typeof(ClearRangeDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.readexcel.png")]
    [LocalizedToolboxTooltip("activity_clearrange_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_clearrange", typeof(Resources.strings))]
    public class ClearRange : ExcelActivity
    {
        [Category("Input")]
        [LocalizedDisplayName("activity_clearrange_cells", typeof(Resources.strings)), LocalizedDescription("activity_clearrange_cells_help", typeof(Resources.strings))]
        public InArgument<string> Cells { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            base.Execute(context);
            var cells = Cells.Get(context);
            Microsoft.Office.Interop.Excel.Range xlRange = null;
            if (string.IsNullOrEmpty(cells))
            {
                xlRange = base.worksheet.UsedRange;
                // Find the last real row
                if(xlRange.Count > 1)
                {
                    try
                    {
                        var nInLastRow = base.worksheet.Cells.Find("*", System.Reflection.Missing.Value,
    System.Reflection.Missing.Value, System.Reflection.Missing.Value, XlSearchOrder.xlByRows, XlSearchDirection.xlPrevious, false, System.Reflection.Missing.Value, System.Reflection.Missing.Value).Row;

                        //// Find the last real column
                        var nInLastCol = base.worksheet.Cells.Find("*", System.Reflection.Missing.Value, System.Reflection.Missing.Value, System.Reflection.Missing.Value, XlSearchOrder.xlByColumns, XlSearchDirection.xlPrevious, false, System.Reflection.Missing.Value, System.Reflection.Missing.Value).Column;

                        // var o = base.worksheet.Cells[nInLastRow, nInLastCol];
                        var o = base.worksheet.Cells[nInLastRow + 1, 1];
                        xlRange = o as Range;

                        // Range last = base.worksheet.Cells.SpecialCells(XlCellType.xlCellTypeLastCell, Type.Missing);
                        // xlRange = base.worksheet.get_Range("A1", last);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            else
            {
                xlRange = base.worksheet.get_Range(cells);
            }
            xlRange.Clear();
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