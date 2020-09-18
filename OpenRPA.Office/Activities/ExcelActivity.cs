using Microsoft.Office.Interop.Excel;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Office.Activities
{
    public static class officewrap
    {
        public static Microsoft.Office.Interop.Excel.Range range()
        {
            return null;
        }

        private static Microsoft.Office.Interop.Excel.Application _application;

        public static Microsoft.Office.Interop.Excel.Application application
        {
            get
            {
                if (_application != null)
                {
                    try
                    {
                        var v = _application.Visible;
                    }
                    catch (Exception)
                    {
                        _application = null;
                    }
                }
                if (_application == null)
                {
                    _application = StartExcel();
                    _application.Visible = true;
                }
                return _application;
            }
        }
        private static Microsoft.Office.Interop.Excel.Application StartExcel()
        {
            Microsoft.Office.Interop.Excel.Application instance = null;
            OpenRPA.Interfaces.GenericTools.RunUI(() =>
            {
                try
                {
                    instance = (Microsoft.Office.Interop.Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    //instance = new Microsoft.Office.Interop.Excel.Application();
                }
                finally
                {
                    if (instance == null) instance = (Application)Activator.CreateInstance(System.Runtime.InteropServices.Marshal.GetTypeFromCLSID(new Guid("00024500-0000-0000-C000-000000000046")));
                    instance.Visible = true;
                }
            });

            return instance;
        }

        internal static void Quit()
        {
            if (_application != null)
            {
                try
                {
                    _application.Quit();
                }
                catch (Exception)
                {
                }
            }
            _application = null;
            GC.Collect();
        }
    }
    public class ExcelActivity : CodeActivity
    {

        public ExcelActivity()
        {
            Visible = true;
        }
        [System.ComponentModel.Category("Misc")]
        public virtual InArgument<string> ReadPassword { get; set; }
        [System.ComponentModel.Category("Misc")]
        public virtual InArgument<string> WritePassword { get; set; }
        [System.ComponentModel.Category("Misc")]
        public virtual InArgument<string> SheetPassword { get; set; }
        [System.ComponentModel.Category("Input")]
        public virtual InArgument<string> Filename { get; set; }
        [System.ComponentModel.Category("Input")]
        public virtual InArgument<string> Worksheet { get; set; }
        [RequiredArgument]
        [System.ComponentModel.Category("Misc")]
        public virtual InArgument<bool> Visible { get; set; }

        [System.ComponentModel.Browsable(false)]
        public OutArgument<Microsoft.Office.Interop.Excel.Application> Application { get; set; }
        [System.ComponentModel.Category("Session")]
        public InOutArgument<Microsoft.Office.Interop.Excel.Workbook> Workbook { get; set; }

        internal string filename;
        internal Microsoft.Office.Interop.Excel.Workbook workbook;
        internal Microsoft.Office.Interop.Excel.Worksheet worksheet;


        private void doOpen(CodeActivityContext context)
        {
            var readPassword = ReadPassword.Get(context);
            if (string.IsNullOrEmpty(readPassword)) readPassword = null;
            var writePassword = WritePassword.Get(context);
            if (string.IsNullOrEmpty(writePassword)) writePassword = null;
            if(!string.IsNullOrEmpty(filename))
            {
                foreach (Microsoft.Office.Interop.Excel.Workbook w in officewrap.application.Workbooks)
                {
                    if (w.FullName == filename)
                    {
                        try
                        {
                            workbook = w;
                            worksheet = workbook.ActiveSheet as Microsoft.Office.Interop.Excel.Worksheet;
                            break;
                        }
                        catch (Exception)
                        {
                            workbook = null;
                        }
                    }
                }
                if (workbook == null)
                {
                    officewrap.application.DisplayAlerts = false;
                    //application.AutomationSecurity = Microsoft.Office.Core.MsoAutomationSecurity.msoAutomationSecurityLow;
                    if (System.IO.File.Exists(filename))
                    {
                        workbook = officewrap.application.Workbooks.Open(filename, ReadOnly: false,
                            Password: readPassword, WriteResPassword: writePassword);
                    }
                    else
                    {
                        if(!string.IsNullOrEmpty(filename))
                        {
                            workbook = officewrap.application.Workbooks.Add();
                            workbook.Activate();
                            //workbook.SaveCopyAs(filename);
                            workbook.SaveAs(Filename: filename);
                        }
                    }
                    officewrap.application.DisplayAlerts = true;
                }
            }
            if(workbook == null) workbook = officewrap.application.ActiveWorkbook;
        }

        //public void cleanup()
        //{
        //    //workbook = null;
        //    //worksheet = null;
        //    Task.Run(() => { GC.Collect(); });
        //}

        protected override void Execute(CodeActivityContext context)
        {
            filename = Filename.Get(context);
            officewrap.application.Visible = Visible.Get(context);
            if (!string.IsNullOrEmpty(filename)) filename = Environment.ExpandEnvironmentVariables(filename);
            workbook = (Workbook != null ? Workbook.Get(context) : null);
            if (!string.IsNullOrEmpty(filename) && workbook != null)
            {
                if (workbook.FullName.ToLower() != filename.ToLower())
                {
                    try
                    {
                        workbook.Close(true);
                        workbook = null;
                        Task.Run(() => { GC.Collect(); });
                    }
                    catch (Exception)
                    {
                        throw;
                    }

                }
            }
            if (workbook == null)
            {
                doOpen(context);
            }
            string _worksheet = (Worksheet != null ? Worksheet.Get(context) : null);
            try
            {
                worksheet = workbook.ActiveSheet as Microsoft.Office.Interop.Excel.Worksheet;
            }
            catch (Exception)
            {
                worksheet = null;
                workbook = null;
                Task.Run(() => { GC.Collect(); });
            }
            if (worksheet == null)
            {
                officewrap.Quit();
                doOpen(context);
                worksheet = workbook.ActiveSheet as Microsoft.Office.Interop.Excel.Worksheet;
            }
            if (!string.IsNullOrEmpty(_worksheet))
            {
                bool found = false;
                foreach (object obj in workbook.Sheets)
                {
                    Worksheet s = obj as Worksheet;
                    if (s != null && s.Name == _worksheet)
                    {
                        s.Activate();
                        worksheet = s;
                        found = true;
                        break;
                    }
                }
                if(!found)
                {
                    worksheet = workbook.Sheets.Add(Type.Missing, workbook.Sheets[workbook.Sheets.Count], 1, Microsoft.Office.Interop.Excel.XlSheetType.xlWorksheet) as Microsoft.Office.Interop.Excel.Worksheet;
                    worksheet.Name = _worksheet;
                }
            }
            if (Workbook != null) Workbook.Set(context, workbook);
            var sheetPassword = SheetPassword.Get(context);
            if (string.IsNullOrEmpty(sheetPassword)) sheetPassword = null;
            if (!string.IsNullOrEmpty(sheetPassword) && worksheet != null)
            {
                worksheet.Unprotect(sheetPassword);
            }
        }
    }


    public class ExcelActivityOf<TResult> : NativeActivity<TResult>
    {
        public ExcelActivityOf()
        {
            Visible = true;
        }
        [System.ComponentModel.Category("Misc")]
        public virtual InArgument<string> ReadPassword { get; set; }
        [System.ComponentModel.Category("Misc")]
        public virtual InArgument<string> WritePassword { get; set; }
        [System.ComponentModel.Category("Misc")]
        public virtual InArgument<string> SheetPassword { get; set; }
        public InArgument<string> Filename { get; set; }
        //[RequiredArgument]
        [System.ComponentModel.Browsable(false)]
        public InArgument<bool> Visible { get; set; }
        public InArgument<string> Worksheet { get; set; }
        public InOutArgument<Microsoft.Office.Interop.Excel.Workbook> Workbook { get; set; }

        [System.ComponentModel.Browsable(false)]
        public InOutArgument<Microsoft.Office.Interop.Excel.Application> Application { get; set; }

        internal string filename;
        internal Microsoft.Office.Interop.Excel.Workbook workbook;
        internal Microsoft.Office.Interop.Excel.Worksheet worksheet;
        protected override void Execute(NativeActivityContext context)
        {
            var readPassword = ReadPassword.Get(context);
            if (string.IsNullOrEmpty(readPassword)) readPassword = null;
            var writePassword = WritePassword.Get(context);
            if (string.IsNullOrEmpty(writePassword)) writePassword = null;
            filename = Filename.Get(context);
            officewrap.application.Visible = true;
            // officewrap.application.Visible = Visible.Get(context);
            if (!string.IsNullOrEmpty(filename)) filename = Environment.ExpandEnvironmentVariables(filename);
            workbook = (Workbook != null ? Workbook.Get(context) : null);
            if (!string.IsNullOrEmpty(filename) && workbook != null)
            {
                if (workbook.FullName.ToLower() != filename.ToLower())
                {
                    try
                    {
                        workbook.Close(true);
                        workbook = null;
                        Task.Run(() => { GC.Collect(); });
                    }
                    catch (Exception)
                    {
                        throw;
                    }

                }
            }
            if (!string.IsNullOrEmpty(filename) && workbook == null)
            {
                foreach (Microsoft.Office.Interop.Excel.Workbook w in officewrap.application.Workbooks)
                {
                    if (w.FullName == filename)
                    {
                        workbook = w;
                        break;
                    }
                }
                if (workbook == null)
                {
                    officewrap.application.DisplayAlerts = false;
                    //application.AutomationSecurity = Microsoft.Office.Core.MsoAutomationSecurity.msoAutomationSecurityLow;
                    if (System.IO.File.Exists(filename))
                    {
                        //workbook = officewrap.application.Workbooks.Open(filename, ReadOnly: false);
                        workbook = officewrap.application.Workbooks.Open(filename, ReadOnly: false,
                                Password: readPassword, WriteResPassword: writePassword);
                    }
                    else
                    {
                        workbook = officewrap.application.Workbooks.Add();
                        workbook.Activate();
                        //workbook.SaveCopyAs(filename);
                        workbook.SaveAs(Filename: filename);
                    }
                    officewrap.application.DisplayAlerts = true;

                }
            }
            if (workbook == null) workbook = officewrap.application.ActiveWorkbook;
            if(workbook == null)
            {
            }
            var _worksheet = (Worksheet != null ? Worksheet.Get(context) : null);
            worksheet = workbook.ActiveSheet as Microsoft.Office.Interop.Excel.Worksheet;
            if (!string.IsNullOrEmpty(_worksheet))
            {
                foreach (object obj in workbook.Sheets)
                {
                    Worksheet s = obj as Worksheet;
                    if (s != null && s.Name == _worksheet)
                    {
                        s.Activate();
                        worksheet = s;
                    }
                }
            }
            var sheetPassword = SheetPassword.Get(context);
            if (string.IsNullOrEmpty(sheetPassword)) sheetPassword = null;
            if (!string.IsNullOrEmpty(sheetPassword) && worksheet != null)
            {
                worksheet.Unprotect(sheetPassword);
            }
            //Application.Set(context, application);
            Workbook.Set(context, workbook);
        }
        //public void cleanup()
        //{
        //    //workbook = null;
        //    //worksheet = null;
        //    Task.Run(() => { GC.Collect(); });
        //}

    }
}
