//using ClosedXML.Excel;
//using ExcelDataReader;
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
//using System.Windows.Forms;

namespace OpenRPA.Office.Activities
{
    [System.ComponentModel.Designer(typeof(CloseWorkbookDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(CloseWorkbook), "Resources.toolbox.readexcel.png")]
    //[designer.ToolboxTooltip(Text = "Read CSV, xls or xlsx file and loads it into a DataSet")]
    public class CloseWorkbook : CodeActivity
    {
        public CloseWorkbook()
        {
        }

        [RequiredArgument]
        [System.ComponentModel.Category("Input")]
        [OverloadGroup("asworkbook")]
        public InOutArgument<Microsoft.Office.Interop.Excel.Workbook> Workbook { get; set; }

        [RequiredArgument]
        [System.ComponentModel.Category("Input")]
        [OverloadGroup("asfilename")]
        public InArgument<string> Filename { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var workbook = Workbook.Get(context);
            var filename = Filename.Get(context);
            if (!string.IsNullOrEmpty(filename)) filename = Environment.ExpandEnvironmentVariables(filename);
            if (string.IsNullOrEmpty(filename))
            {
                workbook.Save();
            }
            else
            {
                foreach (Microsoft.Office.Interop.Excel.Workbook w in officewrap.application.Workbooks)
                {
                    if (w.FullName == filename)
                    {
                        try
                        {
                            workbook = w;
                            w.Close(false);
                            //worksheet = workbook.ActiveSheet;
                            break;
                        }
                        catch (Exception)
                        {
                            workbook = null;
                        }
                    }
                }
            }
            //    workbook.Application.DisplayAlerts = false;
            //    if(workbook.FullName != filename)
            //    {
            //        foreach (Workbook w in workbook.Application.Workbooks)
            //        {
            //            if (w.FullName == filename)
            //            {
            //                w.Close(false);
            //                break;
            //            }
            //        }
            //    }
            //}

        }
    }
}