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
        protected virtual void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                Task.Run(() => { PropertyChanged?.Invoke(this, e); });
            }
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
        public override void Write(bool value)
        {
            Write(value.ToString());
        }
        public override void Write(char value)
        {
            Write(value.ToString());
        }
        public override void Write(decimal value)
        {
            Write(value.ToString());
        }
        public override void Write(double value)
        {
            Write(value.ToString());
        }
        public override void Write(float value)
        {
            Write(value.ToString());
        }
        public override void Write(int value)
        {
            Write(value.ToString());
        }
        public override void Write(long value)
        {
            Write(value.ToString());
        }
        public override void Write(object value)
        {
            if (value == null) Write("");
            Write(value.ToString());
        }
        public override void WriteLine(bool value)
        {
            base.WriteLine(value);
        }
        public override void WriteLine(char value)
        {
            WriteLine(value.ToString());
        }
        public override void WriteLine(decimal value)
        {
            WriteLine(value.ToString());
        }
        public override void WriteLine(double value)
        {
            WriteLine(value.ToString());
        }
        public override void WriteLine(float value)
        {
            WriteLine(value.ToString());
        }
        public override void WriteLine(int value)
        {
            WriteLine(value.ToString());
        }
        public override void WriteLine(long value)
        {
            WriteLine(value.ToString());
        }
        public override void WriteLine(uint value)
        {
            WriteLine(value.ToString());
        }
        public override void WriteLine(ulong value)
        {
            WriteLine(value.ToString());
        }
        public override void WriteLine(string format, object arg0)
        {
            WriteLine(string.Format(format, arg0));
        }
        public override void WriteLine(string format, object arg0, object arg1)
        {
            WriteLine(string.Format(format, arg0, arg1));
        }
        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            WriteLine(string.Format(format, arg0, arg1, arg2));
        }
        public override void WriteLine(string format, params object[] arg)
        {
            WriteLine(string.Format(format, arg));
        }
        public override void WriteLine(object value)
        {
            if (value == null) WriteLine("");
            WriteLine(value.ToString());
        }
        public override void WriteLine(string value)
        {
            var msg = cache + value;
            cache = "";
            if (string.IsNullOrEmpty(msg)) return;
            if (msg.EndsWith(Environment.NewLine)) msg = msg.Remove(msg.Length - 2);
            if (msg.StartsWith(Environment.NewLine)) msg = msg.Remove(0, 2);
            msg = msg.Trim(new char[] { '\uFEFF' });
            if (string.IsNullOrEmpty(msg)) return;
            if (isError) Log.Error(msg);
            if (!isError) Log.Output(msg);
        }
        private string cache = "";
        public override void Write(string value)
        {
            var msg = value;
            if (string.IsNullOrEmpty(msg)) return;
            if (msg.EndsWith(Environment.NewLine)) msg = msg.Remove(msg.Length - 2);
            if (msg.StartsWith(Environment.NewLine)) msg = msg.Remove(0, 2);
            msg = msg.Trim(new char[] { '\uFEFF' });
            if (string.IsNullOrEmpty(msg)) return;
            cache += msg;
        }
        public static void SetToConsole()
        {
            Console.SetOut(new ConsoleDecorator(Console.Out));
        }
    }
}
