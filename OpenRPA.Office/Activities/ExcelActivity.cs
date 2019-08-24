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
                if (_application == null)
                {
                    _application = StartExcel();
                }
                return _application;
            }
        }
        private static Microsoft.Office.Interop.Excel.Application StartExcel()
        {
            Microsoft.Office.Interop.Excel.Application instance = null;
            try
            {
                instance = (Microsoft.Office.Interop.Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                instance = new Microsoft.Office.Interop.Excel.Application();
            }

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
        [RequiredArgument]
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
                workbook = officewrap.application.Workbooks.Open(filename, ReadOnly: false,
                    Password: readPassword, WriteResPassword: writePassword);
                officewrap.application.DisplayAlerts = true;
            }
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
                if (!string.IsNullOrEmpty(filename))
                {
                    filename = Environment.ExpandEnvironmentVariables(filename);
                    doOpen(context);
                }
                else
                {
                    workbook = officewrap.application.Workbooks.Add();
                }
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
                foreach (Microsoft.Office.Interop.Excel.Worksheet s in workbook.Sheets)
                {
                    if (s.Name == _worksheet)
                    {
                        s.Activate();
                        worksheet = s;
                        break;
                    }
                }
            }
            if (Workbook != null) Workbook.Set(context, workbook);
        }
    }


    public class ExcelActivityOf<TResult> : NativeActivity<TResult>
    {
        public ExcelActivityOf()
        {
            Visible = true;
        }
        [RequiredArgument]
        public InArgument<string> Filename { get; set; }
        [RequiredArgument]
        public InArgument<bool> Visible { get; set; }
        public InArgument<string> Worksheet { get; set; }
        public InOutArgument<Microsoft.Office.Interop.Excel.Workbook> Workbook { get; set; }

        [System.ComponentModel.Browsable(false)]
        public InOutArgument<Microsoft.Office.Interop.Excel.Application> Application { get; set; }

        internal string filename;
        internal Microsoft.Office.Interop.Excel.Workbook workbook;
        internal Microsoft.Office.Interop.Excel.Worksheet worksheet;
        public Microsoft.Office.Interop.Excel.Application StartExcel()
        {
            Microsoft.Office.Interop.Excel.Application instance = null;
            try
            {
                instance = (Microsoft.Office.Interop.Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                instance = new Microsoft.Office.Interop.Excel.Application();
            }

            return instance;
        }
        protected override void Execute(NativeActivityContext context)
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
                if (!string.IsNullOrEmpty(filename))
                {
                    filename = Environment.ExpandEnvironmentVariables(filename);
                    foreach (Microsoft.Office.Interop.Excel.Workbook w in officewrap.application.Workbooks)
                    {
                        if (w.FullName == filename)
                        {
                            workbook = w;
                            break;
                        }
                    }
                    if (workbook == null) workbook = officewrap.application.Workbooks.Open(filename);

                }
                else
                {
                    workbook = officewrap.application.Workbooks.Add();
                }
            }

            var _worksheet = (Worksheet != null ? Worksheet.Get(context) : null);
            worksheet = workbook.ActiveSheet as Microsoft.Office.Interop.Excel.Worksheet;
            if (!string.IsNullOrEmpty(_worksheet))
            {
                foreach (Microsoft.Office.Interop.Excel.Worksheet s in workbook.Sheets)
                {
                    if (s.Name == _worksheet)
                    {
                        s.Activate();
                        worksheet = s;
                    }
                }
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
