//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace OpenRPA.Updater
//{
//    using NuGet.Common;
//    public class OpenRPAPackageManagerLogger : ILogger
//    {
//        public event Action Updated;
//        public string Logs { get; set; }
//        public void _Log(LogLevel level, string message)
//        {
//            // var msg = string.Format(message, args);
//            var msg = message;
//            Logs = (msg + Environment.NewLine) + Logs;
//            if (level == LogLevel.Debug) System.Diagnostics.Debug.WriteLine(msg);
//            if (level == LogLevel.Verbose) System.Diagnostics.Debug.WriteLine(msg);
//            if (level == LogLevel.Information) System.Diagnostics.Debug.WriteLine(msg);
//            if (level == LogLevel.Minimal) System.Diagnostics.Debug.WriteLine(msg);
//            if (level == LogLevel.Warning) System.Diagnostics.Debug.WriteLine(msg);
//            if (level == LogLevel.Error) System.Diagnostics.Debug.WriteLine(msg);
//            Updated?.Invoke();
//        }
//        private static OpenRPAPackageManagerLogger _instance = null;
//        public static OpenRPAPackageManagerLogger Instance
//        {
//            get
//            {
//                if (_instance == null) _instance = new OpenRPAPackageManagerLogger();
//                return _instance;
//            }
//        }
//        public void Log(LogLevel level, string data)
//        {
//            _Log(level, data);
//        }
//        public void Log(ILogMessage message)
//        {
//            _Log(message.Level, message.Message);
//        }
//        public Task LogAsync(LogLevel level, string data)
//        {
//            _Log(level, data);
//            return Task.CompletedTask;
//        }
//        public Task LogAsync(ILogMessage message)
//        {
//            _Log(message.Level, message.Message);
//            return Task.CompletedTask;
//        }
//        public void LogDebug(string data)
//        {
//            _Log(LogLevel.Debug, data);
//        }
//        public void LogError(string data)
//        {
//            _Log(LogLevel.Error, data);
//        }
//        public void LogInformation(string data)
//        {
//            _Log(LogLevel.Information, data);
//        }
//        public void LogInformationSummary(string data)
//        {
//            _Log(LogLevel.Information, data);
//        }
//        public void LogMinimal(string data)
//        {
//            _Log(LogLevel.Minimal, data);
//        }
//        public void LogVerbose(string data)
//        {
//            _Log(LogLevel.Verbose, data);
//        }
//        public void LogWarning(string data)
//        {
//            _Log(LogLevel.Warning, data);
//        }
//    }
//}
