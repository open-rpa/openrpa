using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.RDService
{
    public class Tracing : TraceListener
    {
        private TextWriter m_OriginalConsoleStream;
        public Tracing(TextWriter consoleTextWriter)
        {
            m_OriginalConsoleStream = consoleTextWriter;
        }
        public override void Write(object o)
        {
            if (o != null) Write(o.ToString());
        }
        public override void Write(object o, string category)
        {
            if (o != null) Write(o.ToString(), category);
        }
        public override void WriteLine(object o)
        {
            if (o != null) WriteLine(o.ToString());
        }
        public override void WriteLine(object o, string category)
        {
            if (o != null) WriteLine(o.ToString(), category);
        }

        public override void Write(string message)
        {
            Write(message, "Output");
        }
        public override void WriteLine(string message)
        {
            WriteLine(message, "Output");
        }
        public override void Write(string message, string category)
        {
            WriteLine(message);
        }

        private static string logpath = "";
        private static bool HadError = false;
        public override void WriteLine(string message, string category)
        {
            if(string.IsNullOrEmpty(logpath))
            {
                // logpath = Interfaces.Extensions.ProjectsDirectory;
                var asm = System.Reflection.Assembly.GetEntryAssembly();
                var filepath = asm.CodeBase.Replace("file:///", "");
                logpath = System.IO.Path.GetDirectoryName(filepath);
                HadError = false;
            }

            if (category == "Tracing") return;
            DateTime dt = DateTime.Now;
            var _msg = string.Format(@"[{0:HH\:mm\:ss\.fff}][{1}] {2}" , dt, category, message);
            try
            {
                if(!HadError) System.IO.File.AppendAllText(System.IO.Path.Combine(logpath, "log_rdservice.txt"), _msg + Environment.NewLine);
            }
            catch (Exception)
            {
                HadError = true;
            }
            m_OriginalConsoleStream.WriteLine(_msg);
            // Console.WriteLine(_msg);
        }

    }

}
