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
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.clearrange.png")]
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
            }
            else
            {
                xlRange = base.worksheet.get_Range(cells);
            }
            string srange = xlRange.Address[false, false, Microsoft.Office.Interop.Excel.XlReferenceStyle.xlA1, false, null];
            worksheet.Range[srange].Clear();
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