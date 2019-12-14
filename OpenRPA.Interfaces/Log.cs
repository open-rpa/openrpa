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
        public static void LogLine(string message, string category)
        {
            if (!Config.local.log_to_file) return;
            if (nlog == null)
            {
                var config = new NLog.Config.LoggingConfiguration();
                var logfile = new NLog.Targets.FileTarget("logfile") { FileName = System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "logfile.txt") };
                var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
                // config.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
                // config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logfile);
                config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, logfile);
                NLog.LogManager.Configuration = config;
                nlog = NLog.LogManager.GetCurrentClassLogger();
            }
            switch(category)
            {
                case "Error": nlog.Error(message); break;
                case "Warning": nlog.Warn(message); break;
                case "Output": nlog.Info(message); break;
                case "Information": nlog.Info(message); break;
                case "Debug": nlog.Debug(message); break;
                case "Verbose": nlog.Trace(message); break;
                case "Activity": nlog.Trace(message); break;
                case "Selector": nlog.Debug(message); break;
                case "SelectorVerbose": nlog.Trace(message); break;
            }
            // nlog.Debug("Starting up");
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
            LogLine(message, "Verbose");
            if (Config.local.log_verbose) { System.Diagnostics.Trace.WriteLine(message, "Verbose"); }
        }
        public static void Activity(string message)
        {
            LogLine(message, "Activity");
            if (Config.local.log_activity) { System.Diagnostics.Trace.WriteLine(message, "Activity"); } 
        }
        public static void Debug(string message)
        {
            LogLine(message, "Debug");
            if (Config.local.log_debug) { System.Diagnostics.Trace.WriteLine(message, "Debug"); } 
        }
        public static void Selector(string message)
        {
            LogLine(message, "Verbose");
            if (Config.local.log_selector) { System.Diagnostics.Trace.WriteLine(message, "Selector"); } 
        }
        public static void SelectorVerbose(string message)
        {
            LogLine(message, "SelectorVerbose");
            if (Config.local.log_selector_verbose) { System.Diagnostics.Trace.WriteLine(message, "SelectorVerbose"); } 
        }
        public static void Information(string message)
        {
            LogLine(message, "Information");
            if (Config.local.log_information) { System.Diagnostics.Trace.WriteLine(message, "Information"); }
        }
        public static void Output(string message)
        {
            LogLine(message, "Output");
            if (Config.local.log_output) { System.Diagnostics.Trace.WriteLine(message, "Output"); } 
        }
        public static void Warning(string message)
        {
            LogLine(message, "Warning");
            if (Config.local.log_warning) { System.Diagnostics.Trace.WriteLine(message, "Warning"); } 
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
