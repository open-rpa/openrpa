//using NuGet;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace OpenRPA
//{
//    public class Logger : ILogger
//    {
//        public event Action<string> Updated;
//        public string Logs { get; set; }
//        public void Log(MessageLevel level, string message, params object[] args)
//        {
//            var msg = string.Format(message, args);
//            Logs = (msg + Environment.NewLine) + Logs;
//            if (level == MessageLevel.Debug) System.Diagnostics.Debug.WriteLine(msg);
//            if (level == MessageLevel.Info) System.Diagnostics.Debug.WriteLine(msg);
//            if (level == MessageLevel.Warning) System.Diagnostics.Debug.WriteLine(msg);
//            if (level == MessageLevel.Error) System.Diagnostics.Debug.WriteLine(msg);
//            Updated?.Invoke(message);
//        }
//        void ILogger.Log(MessageLevel level, string message, params object[] args)
//        {
//            Log(level, message, args);
//        }

//        FileConflictResolution IFileConflictResolver.ResolveFileConflict(string message)
//        {
//            throw new NotImplementedException();
//        }

//    }
//}
