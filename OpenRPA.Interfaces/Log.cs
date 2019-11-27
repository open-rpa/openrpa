using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public class Log
    {
        // private static string logpath = "";
        // public static object loglock = new object();
        public static void LogLine(string message, string category)
        {
           // Task.Run(() =>
           //{
           //    if (string.IsNullOrEmpty(logpath))
           //    {
           //        logpath = Interfaces.Extensions.ProjectsDirectory;
           //    }
           //    DateTime dt = DateTime.Now;
           //    lock (loglock)
           //    {
           //        System.IO.File.AppendAllText(System.IO.Path.Combine(logpath, "log.txt"), string.Format(@"[{0:HH\:mm\:ss\.fff}][{1}] {2}" + Environment.NewLine, dt, category, message));
           //    }
           //});
        }
        public static void Verbose(string message)
        {
            if (Config.local.log_verbose) { System.Diagnostics.Trace.WriteLine(message, "Verbose"); } else { LogLine(message, "Verbose"); }
        }
        public static void Activity(string message)
        {
            if (Config.local.log_activity) { System.Diagnostics.Trace.WriteLine(message, "Activity"); } else { LogLine(message, "Activity"); }
        }
        public static void Debug(string message)
        {
            if (Config.local.log_debug) { System.Diagnostics.Trace.WriteLine(message, "Debug"); } else { LogLine(message, "Debug"); }
        }
        public static void Selector(string message)
        {
            if (Config.local.log_selector) { System.Diagnostics.Trace.WriteLine(message, "Selector"); } else { LogLine(message, "Verbose"); }
        }
        public static void SelectorVerbose(string message)
        {
            if (Config.local.log_selector_verbose) { System.Diagnostics.Trace.WriteLine(message, "SelectorVerbose"); } else { LogLine(message, "SelectorVerbose"); }
        }
        public static void Information(string message)
        {
            if (Config.local.log_information) { System.Diagnostics.Trace.WriteLine(message, "Information"); } else { LogLine(message, "Information"); }
        }
        public static void Output(string message)
        {
            if (Config.local.log_output) { System.Diagnostics.Trace.WriteLine(message, "Output"); } else { LogLine(message, "Output"); }
        }
        public static void Warning(string message)
        {
            if (Config.local.log_warning) { System.Diagnostics.Trace.WriteLine(message, "Warning"); } else { LogLine(message, "Warning"); }
        }
        public static void Error(object obj, string message)
        {
            var _message = obj.ToString();
            if (!string.IsNullOrEmpty(message)) _message = message + "\n" + _message;
            if (!Config.local.log_error) { LogLine(_message, "Error"); return; }
            System.Diagnostics.Trace.WriteLine(_message, "Error");
        }
        public static void Error(string message)
        {
            if (Config.local.log_error) { System.Diagnostics.Trace.WriteLine(message, "Error"); } else { LogLine(message, "Error"); return; }
        }
    }
}
