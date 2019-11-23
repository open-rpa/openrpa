using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public class Log
    {
        public static void Verbose(string message)
        {
            if(Config.local.log_verbose) System.Diagnostics.Trace.WriteLine(message, "Verbose");
        }
        public static void Activity(string message)
        {
            if (Config.local.log_activity) System.Diagnostics.Trace.WriteLine(message, "Activity");
        }
        public static void Debug(string message)
        {
            if (Config.local.log_debug) System.Diagnostics.Trace.WriteLine(message, "Debug");
        }
        public static void Selector(string message)
        {
            if (Config.local.log_selector) System.Diagnostics.Trace.WriteLine(message, "Selector");
        }
        public static void SelectorVerbose(string message)
        {
            if (Config.local.log_selector_verbose) System.Diagnostics.Trace.WriteLine(message, "SelectorVerbose");
        }
        public static void Information(string message)
        {
            if (Config.local.log_information) System.Diagnostics.Trace.WriteLine(message, "Information");
        }
        public static void Output(string message)
        {
            if (Config.local.log_output) System.Diagnostics.Trace.WriteLine(message, "Output");
        }
        public static void Warning(string message)
        {
            if (Config.local.log_warning) System.Diagnostics.Trace.WriteLine(message, "Warning");
        }
        public static void Error(object obj, string message)
        {
            if (!Config.local.log_error) return;
            var _message = obj.ToString();
            if (!string.IsNullOrEmpty(message)) _message = message + "\n" + _message;
            System.Diagnostics.Trace.WriteLine(_message, "Error");
        }
        public static void Error(string message)
        {
            if (Config.local.log_error) System.Diagnostics.Trace.WriteLine(message, "Error");
        }
    }
}
