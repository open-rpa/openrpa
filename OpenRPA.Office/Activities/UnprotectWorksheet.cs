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
    [System.ComponentModel.Designer(typeof(UnprotectWorksheetDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.unprotectworksheet.png")]
    [LocalizedToolboxTooltip("activity_unprotectworksheet_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_unprotectworksheet", typeof(Resources.strings))]
    public class UnprotectWorksheet : ExcelActivity
    {
        protected override void Execute(CodeActivityContext context)
        {
            base.Execute(context);
            var sheetPassword = SheetPassword.Get(context);
            if (string.IsNullOrEmpty(sheetPassword)) sheetPassword = null;
            if (worksheet != null)
            {
                worksheet.Unprotect(sheetPassword);
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