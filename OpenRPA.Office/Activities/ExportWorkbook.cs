using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
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
using OpenRPA.Interfaces;

namespace OpenRPA.Office.Activities
{
    [System.ComponentModel.Designer(typeof(ExportWorkbookDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.ExportWorkbook.png")]
    [LocalizedToolboxTooltip("activity_ExportWorkbook_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_ExportWorkbook", typeof(Resources.strings))]
    public class ExportWorkbook : CodeActivity
    {
        [System.ComponentModel.Category("Misc")]
        public virtual InArgument<bool> RemoveReadPassword { get; set; }
        [System.ComponentModel.Category("Misc")]
        public virtual InArgument<string> ReadPassword { get; set; }
        [System.ComponentModel.Category("Misc")]
        public virtual InArgument<bool> RemoveWritePassword { get; set; }
        [System.ComponentModel.Category("Misc")]
        public virtual InArgument<string> WritePassword { get; set; }

        // [RequiredArgument]
        [Category("Input")]
        [OverloadGroup("asworkbook")]
        [LocalizedDisplayName("activity_ExportWorkbook_workbook", typeof(Resources.strings)), LocalizedDescription("activity_ExportWorkbook_workbook_help", typeof(Resources.strings))]
        public InOutArgument<Microsoft.Office.Interop.Excel.Workbook> Workbook { get; set; }
        // [RequiredArgument]
        [Category("Input")]
        [OverloadGroup("asfilename")]
        [LocalizedDisplayName("activity_ExportWorkbook_filename", typeof(Resources.strings)), LocalizedDescription("activity_ExportWorkbook_filename_help", typeof(Resources.strings))]
        public InArgument<string> Filename { get; set; }
        [RequiredArgument]
        [Category("Input")]
        [LocalizedDisplayName("activity_ExportWorkbook_savechanges", typeof(Resources.strings)), LocalizedDescription("activity_ExportWorkbook_savechanges_help", typeof(Resources.strings))]
        public InArgument<bool> SaveChanges { get; set; } = true;
        [RequiredArgument]
        [Category("Misc")]
        [Editor(typeof(XlFixedFormatTypeEditor), typeof(ExtendedPropertyValueEditor))]
        public InArgument<string> Type { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var readPassword = ReadPassword.Get(context);
            if (string.IsNullOrEmpty(readPassword)) readPassword = null;
            var writePassword = WritePassword.Get(context);
            if (string.IsNullOrEmpty(writePassword)) writePassword = null;
            var removeReadPassword = RemoveReadPassword.Get(context);
            var removeWritePassword = RemoveWritePassword.Get(context);

            var formattype = Type.Get(context);
            var workbook = Workbook.Get(context);
            var filename = Filename.Get(context);
            var saveChanges = SaveChanges.Get(context);
            if (!string.IsNullOrEmpty(filename)) filename = Environment.ExpandEnvironmentVariables(filename);
            if (!string.IsNullOrEmpty(filename))
            {
                bool foundit = false;
                foreach (Microsoft.Office.Interop.Excel.Workbook w in officewrap.application.Workbooks)
                {
                    if (w.FullName == filename || string.IsNullOrEmpty(filename))
                    {
                        try
                        {
                            workbook = w;
                            foundit = true;
                            //worksheet = workbook.ActiveSheet;
                            break;
                        }
                        catch (Exception)
                        {
                            workbook = null;
                        }
                    }
                }
                if (!foundit)
                {
                    Workbook tempworkbook = officewrap.application.ActiveWorkbook;
                    if (saveChanges && tempworkbook != null && (System.IO.Path.GetExtension(filename) != ".pdf" && System.IO.Path.GetExtension(filename) != ".xps"))
                    {
                        tempworkbook.SaveAs(Filename: filename, Password: readPassword, WriteResPassword: writePassword);
                        workbook = tempworkbook;
                    }
                }
            }
            if (workbook == null) workbook = officewrap.application.ActiveWorkbook;
            if (workbook != null)
            {
                if (string.IsNullOrEmpty(filename)) filename = workbook.FullName;
                officewrap.application.DisplayAlerts = false;
                if (!string.IsNullOrEmpty(readPassword)) { workbook.Password = readPassword; saveChanges = true; }
                if (removeReadPassword) { workbook.Password = ""; saveChanges = true; }
                if (!string.IsNullOrEmpty(writePassword)) { workbook.WritePassword = writePassword; saveChanges = true; }
                if (removeWritePassword) { workbook.WritePassword = ""; saveChanges = true; }
                var ext = System.IO.Path.GetExtension(filename);
                if (ext.ToLower() != ".xps" && formattype == "1")
                {
                    filename = System.IO.Path.ChangeExtension(filename, "xps");
                }
                else if (ext.ToLower() != ".pdf" && formattype == "0")
                {
                    filename = System.IO.Path.ChangeExtension(filename, "pdf");
                }
                if (System.IO.File.Exists(filename))
                {
                    System.IO.File.Delete(filename);
                }
                if (formattype == "1")
                {
                    workbook.ExportAsFixedFormat(XlFixedFormatType.xlTypeXPS, filename);
                }
                else
                {
                    workbook.ExportAsFixedFormat(XlFixedFormatType.xlTypePDF, filename);
                }
                workbook.Close(saveChanges);
                officewrap.application.DisplayAlerts = true;
            }

            if (officewrap.application.Workbooks.Count == 0)
            {
                officewrap.application.Quit();
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
    class XlFixedFormatTypeEditor : CustomSelectEditor
    {
        public override System.Data.DataTable options
        {
            get
            {
                var lst = new System.Data.DataTable();
                lst.Columns.Add("ID", typeof(string));
                lst.Columns.Add("TEXT", typeof(string));
                lst.Rows.Add("0", "PDF format (.pdf)");
                lst.Rows.Add("1", "XPS format (.xps)");
                return lst;
            }
        }
    }
}