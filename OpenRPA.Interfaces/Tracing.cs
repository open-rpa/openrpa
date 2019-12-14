using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public class Tracing : TraceListener, System.ComponentModel.INotifyPropertyChanged //, ILogEventSink
    {
        private int maxLines = 20;
        private string _TraceMessages = "";
        public string TraceMessages
        {
            get
            {
                string result = string.Empty;
                result = _TraceMessages;
                var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                int count = lines.Count();
                if (count > maxLines)
                {
                    var list = lines.ToList();
                    list.RemoveRange((maxLines - 10), (count - maxLines) + 10);
                    _TraceMessages = result;
                }
                return result;
            }
            set {
                _TraceMessages = value;
            }
        }
        private string _OutputMessages = "";
        public string OutputMessages
        {
            get
            {
                string result = "";
                result = _OutputMessages;
                var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                int count = lines.Count();
                if (count > maxLines)
                {
                    var list = lines.ToList();
                    list.RemoveRange((maxLines - 10), (count - maxLines) + 10);
                    _OutputMessages = result;
                }
                return result;
            }
            set {
                _OutputMessages = value;
            }
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
        public override void WriteLine(string message, string category)
        {
            if (string.IsNullOrEmpty(logpath))
            {
                logpath = Interfaces.Extensions.ProjectsDirectory;
            }
            if (category == "Tracing") return;
            DateTime dt = DateTime.Now;
            //lock(Log.loglock)
            //{
            //    System.IO.File.AppendAllText(System.IO.Path.Combine(logpath, "log.txt"), string.Format(@"[{0:HH\:mm\:ss\.fff}][{1}] {2}" + Environment.NewLine, dt, category, message));
            //}
            if (category == "Output")
            {
                _OutputMessages = _OutputMessages.Insert(0, string.Format(@"[{0:HH\:mm\:ss\.fff}][{1}] {2}" + Environment.NewLine, dt, category, message));
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("OutputMessages"));
            }
            _TraceMessages = _TraceMessages.Insert(0, string.Format(@"[{0:HH\:mm\:ss\.fff}][{1}] {2}" + Environment.NewLine, dt, category, message));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Trace"));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("TraceMessages"));
        }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public DateTime lastEvent = DateTime.Now;
        //  private Task pending = null;
        protected virtual void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                Task.Run(() => { PropertyChanged?.Invoke(this, e); });
            }
            //if (DateTime.Now - lastEvent < TimeSpan.FromSeconds(1))
            //{
            //    try
            //    {
            //        if (pending != null)
            //        {
            //            if (pending.IsCompleted) pending = null;
            //        }
            //        if (pending == null)
            //        {
            //            pending = Task.Run(async () =>
            //            {
            //                try
            //                {
            //                    await Task.Delay(500);
            //                    OnPropertyChanged(e);
            //                }
            //                catch (Exception)
            //                {
            //                    pending = null;
            //                }
            //            });
            //        }
            //        return;
            //    }
            //    catch (Exception)
            //    {
            //        pending = null;
            //    }
            //}
            //if (PropertyChanged != null)
            //{
            //    Task.Run(() => { PropertyChanged?.Invoke(this, e); lastEvent = DateTime.Now; });
            //}
        }
        public IFormatProvider formatProvider { get; set; }
    }

    public class ConsoleDecorator : TextWriter
    {
        private TextWriter m_OriginalConsoleStream;
        public override Encoding Encoding => Encoding.Unicode;
        public bool isError = false;
        public ConsoleDecorator(TextWriter consoleTextWriter, bool isError = false)
        {
            this.isError = isError;
            m_OriginalConsoleStream = consoleTextWriter;
        }
        public override void WriteLine(string value)
        {
            var msg = value;
            if (string.IsNullOrEmpty(msg)) return;
            if (msg.EndsWith(Environment.NewLine)) msg = msg.Remove(msg.Length - 2);
            if (msg.StartsWith(Environment.NewLine)) msg = msg.Remove(0, 2);
            msg = msg.Trim(new char[] { '\uFEFF' });
            if (string.IsNullOrEmpty(msg)) return;
            char c = msg[0];
            if (isError) Log.Error(msg);
            if (!isError) Log.Output(msg);
        }
        public static void SetToConsole()
        {
            Console.SetOut(new ConsoleDecorator(Console.Out));
        }
    }
}
