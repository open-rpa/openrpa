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
    [System.ComponentModel.Designer(typeof(InsertRangeDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.insertrange.png")]
    [LocalizedToolboxTooltip("activity_insertrange_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_insertrange", typeof(Resources.strings))]
    public class InsertRange : ExcelActivity
    {
        [Category("Input")]
        [RequiredArgument, LocalizedDisplayName("activity_insertrange_cell", typeof(Resources.strings)), LocalizedDescription("activity_insertrange_cell_help", typeof(Resources.strings))]
        public InArgument<string> Cell { get; set; }
        [Category("Input")]
        [RequiredArgument, LocalizedDisplayName("activity_insertrange_count", typeof(Resources.strings)), LocalizedDescription("activity_insertrange_count_help", typeof(Resources.strings))]
        public InArgument<int> Count { get; set; }
        [LocalizedDisplayName("activity_insertrange_shiftright", typeof(Resources.strings)), LocalizedDescription("activity_insertrange_shiftright_help", typeof(Resources.strings))]
        public InArgument<bool> ShiftRight { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            base.Execute(context);
            var cell = Cell.Get(context);
            var count = Count.Get(context);
            var shiftright = ShiftRight.Get(context);
            Microsoft.Office.Interop.Excel.Range xlRange = worksheet.get_Range(cell);
            var direction = XlInsertShiftDirection.xlShiftDown;
            if (shiftright) direction = XlInsertShiftDirection.xlShiftToRight;
            for (var i = 0; i < count; i++)
            {
                xlRange.Insert(direction);
            }
            var sheetPassword = SheetPassword.Get(context);
            if (string.IsNullOrEmpty(sheetPassword)) sheetPassword = null;
            if (!string.IsNullOrEmpty(sheetPassword) && worksheet != null)
            {
                worksheet.Protect(sheetPassword);
            }
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