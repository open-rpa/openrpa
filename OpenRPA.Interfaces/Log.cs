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
            // System.Diagnostics.Trace.WriteLine(message, "Verbose");
        }
        public static void Activity(string message)
        {
            System.Diagnostics.Trace.WriteLine(message, "Activity");
        }
        public static void Debug(string message)
        {
            System.Diagnostics.Trace.WriteLine(message, "Debug");
        }
        public static void Selector(string message)
        {
            // System.Diagnostics.Trace.WriteLine(message, "Selector");
        }
        public static void Information(string message)
        {
            System.Diagnostics.Trace.WriteLine(message, "Information");
        }
        public static void Output(string message)
        {
            System.Diagnostics.Trace.WriteLine(message, "Output");
        }
        public static void Warning(string message)
        {
            System.Diagnostics.Trace.WriteLine(message, "Warning");
        }
        public static void Error(object obj, string message)
        {
            var _message = obj.ToString();
            if (!string.IsNullOrEmpty(message)) _message = message + "\n" + _message;
            System.Diagnostics.Trace.WriteLine(_message, "Error");
        }
        public static void Error(string message)
        {
            System.Diagnostics.Trace.WriteLine(message, "Error");
        }
    }
}
