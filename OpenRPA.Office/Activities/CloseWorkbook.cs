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
using OpenRPA.Interfaces;

namespace OpenRPA.Office.Activities
{
    [System.ComponentModel.Designer(typeof(CloseWorkbookDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.closeworkbook.png")]
    [LocalizedToolboxTooltip("activity_closeworkbook_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_closeworkbook", typeof(Resources.strings))]
    public class CloseWorkbook : CodeActivity
    {
        [System.ComponentModel.Category("Misc")]
        public virtual InArgument<bool> RemoveReadPassword { get; set; }
        [System.ComponentModel.Category("Misc")]
        public virtual InArgument<string> ReadPassword { get; set; }
        [System.ComponentModel.Category("Misc")]
        public virtual InArgument<bool> RemoveWritePassword { get; set; }
        [System.ComponentModel.Category("Misc")]
        public virtual InArgument<string> WritePassword { get; set; }

        [RequiredArgument]
        [Category("Input")]
        [OverloadGroup("asworkbook")]
        [LocalizedDisplayName("activity_closeworkbook_workbook", typeof(Resources.strings)), LocalizedDescription("activity_closeworkbook_workbook_help", typeof(Resources.strings))]
        public InOutArgument<Microsoft.Office.Interop.Excel.Workbook> Workbook { get; set; }
        [RequiredArgument]
        [Category("Input")]
        [OverloadGroup("asfilename")]
        [LocalizedDisplayName("activity_closeworkbook_filename", typeof(Resources.strings)), LocalizedDescription("activity_closeworkbook_filename_help", typeof(Resources.strings))]
        public InArgument<string> Filename { get; set; }
        [RequiredArgument]
        [Category("Input")]
        [LocalizedDisplayName("activity_closeworkbook_savechanges", typeof(Resources.strings)), LocalizedDescription("activity_closeworkbook_savechanges_help", typeof(Resources.strings))]
        public InArgument<bool> SaveChanges { get; set; } = true;
        protected override void Execute(CodeActivityContext context)
        {
            var readPassword = ReadPassword.Get(context);
            if (string.IsNullOrEmpty(readPassword)) readPassword = null;
            var writePassword = WritePassword.Get(context);
            if (string.IsNullOrEmpty(writePassword)) writePassword = null;
            var removeReadPassword = RemoveReadPassword.Get(context);
            var removeWritePassword = RemoveWritePassword.Get(context);

            var workbook = Workbook.Get(context);
            var filename = Filename.Get(context);
            var saveChanges = SaveChanges.Get(context);
            if (!string.IsNullOrEmpty(filename)) filename = Environment.ExpandEnvironmentVariables(filename);
            if (string.IsNullOrEmpty(filename))
            {
                workbook.Close(saveChanges);
            }
            else
            {
                int workbookcount = 0;
                bool foundit = false;
                foreach (Microsoft.Office.Interop.Excel.Workbook w in officewrap.application.Workbooks)
                {
                    if (w.FullName == filename || string.IsNullOrEmpty(filename))
                    {
                        try
                        {
                            officewrap.application.DisplayAlerts = false;
                            workbook = w;
                            if (!string.IsNullOrEmpty(readPassword)) w.Password = readPassword;
                            if (removeReadPassword) { w.Password = ""; readPassword = ""; }
                            if (!string.IsNullOrEmpty(writePassword)) w.WritePassword = writePassword;
                            if (removeWritePassword) { w.WritePassword = ""; writePassword = ""; }
                            w.SaveAs(Filename: filename, Password: readPassword, WriteResPassword: writePassword);
                            w.Close();
                            officewrap.application.DisplayAlerts = true;
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
                Microsoft.Office.Interop.Excel.Workbook tempworkbook = officewrap.application.ActiveWorkbook;
                if(!foundit && tempworkbook != null)
                {
                    officewrap.application.DisplayAlerts = false;
                    if(saveChanges && !string.IsNullOrEmpty(filename))
                    {
                        tempworkbook.SaveAs(Filename: filename, Password: readPassword, WriteResPassword: writePassword);
                        tempworkbook.Close();
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(readPassword)) tempworkbook.Password = readPassword;
                        if (removeReadPassword) tempworkbook.Password = "";
                        if (!string.IsNullOrEmpty(writePassword)) tempworkbook.WritePassword = writePassword;
                        if (removeWritePassword) tempworkbook.WritePassword = "";
                        tempworkbook.Save();
                        tempworkbook.Close(saveChanges);
                    }
                    officewrap.application.DisplayAlerts = true;
                }
                if (workbookcount== 0 || string.IsNullOrEmpty(filename))
                {
                    officewrap.application.Quit();
                }
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