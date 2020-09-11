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
    [System.ComponentModel.Designer(typeof(ProtectWorksheetDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.protect.png")]
    [LocalizedToolboxTooltip("activity_protectworksheet_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_protectworksheet", typeof(Resources.strings))]
    public class ProtectWorksheet : ExcelActivity
    {
        [System.ComponentModel.Category("Misc")] public InArgument<bool> DrawingObjects { get; set; }
        [System.ComponentModel.Category("Misc")] public InArgument<bool> Contents { get; set; } = true;
        [System.ComponentModel.Category("Misc")] public InArgument<bool> Scenarios { get; set; } = true;
        [System.ComponentModel.Category("Misc")] public InArgument<bool> UserInterfaceOnly { get; set; }
        [System.ComponentModel.Category("Misc")] public InArgument<bool> AllowFormattingCells { get; set; }
        [System.ComponentModel.Category("Misc")] public InArgument<bool> AllowFormattingColumns { get; set; }
        [System.ComponentModel.Category("Misc")] public InArgument<bool> AllowFormattingRows { get; set; }
        [System.ComponentModel.Category("Misc")] public InArgument<bool> AllowInsertingColumns { get; set; }
        [System.ComponentModel.Category("Misc")] public InArgument<bool> AllowInsertingRows { get; set; }
        [System.ComponentModel.Category("Misc")] public InArgument<bool> AllowInsertingHyperlinks { get; set; }
        [System.ComponentModel.Category("Misc")] public InArgument<bool> AllowDeletingColumns { get; set; }
        [System.ComponentModel.Category("Misc")] public InArgument<bool> AllowDeletingRows { get; set; }
        [System.ComponentModel.Category("Misc")] public InArgument<bool> AllowSorting { get; set; }
        [System.ComponentModel.Category("Misc")] public InArgument<bool> AllowFiltering { get; set; }
        [System.ComponentModel.Category("Misc")] public InArgument<bool> AllowUsingPivotTables { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            base.Execute(context);
            var sheetPassword = SheetPassword.Get(context);
            if (string.IsNullOrEmpty(sheetPassword)) sheetPassword = null;
            if (!string.IsNullOrEmpty(sheetPassword) && worksheet != null)
            {
                worksheet.Protect(sheetPassword, DrawingObjects.Get(context), Contents.Get(context), Scenarios.Get(context),
                    UserInterfaceOnly.Get(context), AllowFormattingCells.Get(context), AllowFormattingColumns.Get(context), AllowFormattingRows.Get(context),
                    AllowInsertingColumns.Get(context), AllowInsertingRows.Get(context), AllowInsertingHyperlinks.Get(context), AllowDeletingColumns.Get(context), 
                    AllowDeletingRows.Get(context), AllowSorting.Get(context), AllowFiltering.Get(context), AllowUsingPivotTables.Get(context));
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