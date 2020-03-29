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
        [RequiredArgument]
        [System.ComponentModel.Category("Input")]
        [OverloadGroup("asworkbook")]
        public InOutArgument<Microsoft.Office.Interop.Excel.Workbook> Workbook { get; set; }
        [RequiredArgument]
        [System.ComponentModel.Category("Input")]
        [OverloadGroup("asfilename")]
        public InArgument<string> Filename { get; set; }
        [RequiredArgument]
        [System.ComponentModel.Category("Input")]
        public InArgument<bool> SaveChanges { get; set; } = true;
        protected override void Execute(CodeActivityContext context)
        {
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
                foreach (Microsoft.Office.Interop.Excel.Workbook w in officewrap.application.Workbooks)
                {
                    if (w.FullName == filename || string.IsNullOrEmpty(filename))
                    {
                        try
                        {
                            workbook = w;
                            w.Close(saveChanges);
                            //worksheet = workbook.ActiveSheet;
                            break;
                        }
                        catch (Exception)
                        {
                            workbook = null;
                        }
                    }
                }
                if(workbookcount== 0 || string.IsNullOrEmpty(filename))
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