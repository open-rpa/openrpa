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
        private static NLog.Logger nlog = null;
        public static int Indent = 0;
        public static System.Collections.Concurrent.ConcurrentStack<string> functions = new System.Collections.Concurrent.ConcurrentStack<string>();
        public static void FunctionIndent(string cls, string func, string message = "")
        {
            Indent++;
            // functions.Push(cls + "." + func);
            functions.Push(func);
            if (!Config.local.log_to_file) return;
            if (string.IsNullOrEmpty(message))
            {
                LogLine(string.Join("::", functions.Reverse()) + "::BEGIN", "Func");
                //LogLine(string.Format("[{0}][{1}][{2}]::BEGIN", cls, func, Indent), "Func");
            }
            else
            {
                LogLine(string.Join("::", functions.Reverse()) + " " + message + "::BEGIN", "Func");
                //LogLine(string.Format("[{0}][{1}][{2}]::BEGIN:{3}", cls, func, Indent, message), "Func");
            }
        }
        public static void FunctionOutdent(string cls, string func, string message = "")
        {
            if (Config.local.log_to_file)
            {
                if (string.IsNullOrEmpty(message))
                {
                    LogLine(string.Join("::", functions.Reverse()) + "::END", "Func");
                    //LogLine(string.Format("[{0}][{1}][{2}]::END", cls, func, Indent, message), "Func");
                }
                else
                {
                    LogLine(string.Join("::", functions.Reverse()) + " " + message + "::END", "Func");
                    //LogLine(string.Format("[{0}][{1}][{2}]::END:{3}", cls, func, Indent, message), "Func");
                }
            }
            Indent--;
            string dummy = "";
            functions.TryPop(out dummy);
        }
        public static void Function(string cls, string func, string message = "")
        {
            if (!Config.local.log_to_file) return;
            if (string.IsNullOrEmpty(message))
            {
                LogLine(string.Join("::", functions.Reverse()), "Func");
                // LogLine(string.Format("[{0}][{1}][{2}]", cls, func, Indent), "Func");
            }
            else
            {
                LogLine(string.Join("::", functions.Reverse()) + " " + message, "Func");
                // LogLine(string.Format("[{0}][{1}][{2}]::{3}", cls, func, Indent, message), "Func");
            }
        }
        public static void ResetLogPath(string folder)
        {
            Extensions.ProjectsDirectory = folder;
            Config.local.log_to_file = true;
            nlog = null;
        }
        public static void LogLine(string message, string category)
        {
            if (!Config.local.log_to_file) return;
            if (nlog == null)
            {
                var config = new NLog.Config.LoggingConfiguration();
                var logfile = new NLog.Targets.FileTarget("logfile") { FileName = System.IO.Path.Combine(Extensions.ProjectsDirectory, "logfile.txt") };
                logfile.Layout = "${time}|${message}";
                // var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
                // config.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
                // config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logfile);
                // config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, logfile);
                var min = NLog.LogLevel.FromOrdinal(Config.local.log_file_level_minimum);
                var max = NLog.LogLevel.FromOrdinal(Config.local.log_file_level_maximum);
                config.AddRule(min, max, logfile);
                //public static readonly NLog.LogLevel Trace = new NLog.LogLevel("Trace", 0);
                //public static readonly NLog.LogLevel Debug = new NLog.LogLevel("Debug", 1);
                //public static readonly NLog.LogLevel Info = new NLog.LogLevel("Info", 2);
                //public static readonly NLog.LogLevel Warn = new NLog.LogLevel("Warn", 3);
                //public static readonly NLog.LogLevel Error = new NLog.LogLevel("Error", 4);
                //public static readonly NLog.LogLevel Fatal = new NLog.LogLevel("Fatal", 5);
                //public static readonly NLog.LogLevel Off = new NLog.LogLevel("Off", 6);

                NLog.LogManager.Configuration = config;
                nlog = NLog.LogManager.GetCurrentClassLogger();
            }
            Task.Run(() =>
            {
                System.Threading.Thread.CurrentThread.Name = "NLogging";
                switch (category)
                {
                    case "Error": nlog.Error(message); break;
                    case "Warning": nlog.Warn(message); break;
                    case "Output": nlog.Info(message); break;
                    case "Information": nlog.Info(message); break;
                    case "Debug": nlog.Debug(message); break;
                    case "Verbose": nlog.Trace(message); break;
                    case "Func": nlog.Info(message); break;
                    case "Activity": nlog.Trace(message); break;
                    case "Selector": nlog.Debug(message); break;
                    case "SelectorVerbose": nlog.Trace(message); break;
                }
            });
        }
        public static void Verbose(string message)
        {
            if (Config.local.log_verbose) { System.Diagnostics.Trace.WriteLine(message, "Verbose"); }
            LogLine(message, "Verbose");
        }
        public static void Network(string message)
        {
            if (Config.local.log_network) { System.Diagnostics.Trace.WriteLine(message, "Network"); }
            LogLine(message, "Network");
        }
        public static void Activity(string message)
        {
            if (Config.local.log_activity) { System.Diagnostics.Trace.WriteLine(message, "Activity"); }
            LogLine(message, "Activity");
        }
        public static void Debug(string message)
        {
            if (Config.local.log_debug) { System.Diagnostics.Trace.WriteLine(message, "Debug"); }
            LogLine(message, "Debug");
        }
        public static void Selector(string message)
        {
            if (Config.local.log_selector) { System.Diagnostics.Trace.WriteLine(message, "Selector"); }
            LogLine(message, "Verbose");
        }
        public static void SelectorVerbose(string message)
        {
            if (Config.local.log_selector_verbose) { System.Diagnostics.Trace.WriteLine(message, "SelectorVerbose"); }
            LogLine(message, "SelectorVerbose");
        }
        public static void Information(string message)
        {
            if (Config.local.log_information) { System.Diagnostics.Trace.WriteLine(message, "Information"); }
            LogLine(message, "Information");
        }
        public static void Output(string message)
        {
            if (Config.local.log_output) { System.Diagnostics.Trace.WriteLine(message, "Output"); }
            LogLine(message, "Output");
        }
        public static void Warning(string message)
        {
            if (Config.local.log_warning) { System.Diagnostics.Trace.WriteLine(message, "Warning"); }
            LogLine(message, "Warning");
        }
        public static void Error(object obj, string message)
        {
            var _message = obj.ToString();
            if (!string.IsNullOrEmpty(message)) _message = message + "\n" + _message;
            System.Diagnostics.Trace.WriteLine(_message, "Error");
            LogLine(_message, "Error");
        }
        public static void Error(string message)
        {
            if (Config.local.log_error) { System.Diagnostics.Trace.WriteLine(message, "Error"); }
            LogLine(message, "Error");
        }
    }
}
