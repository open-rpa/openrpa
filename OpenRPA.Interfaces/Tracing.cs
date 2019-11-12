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
        public override void WriteLine(string message, string category)
        {
            if (category == "Tracing") return;
            DateTime dt = DateTime.Now;
            System.IO.File.AppendAllText("log.txt", string.Format(@"[{0:HH\:mm\:ss\.fff}][{1}] {2}" + Environment.NewLine, dt, category, message));
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
        protected virtual void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(PropertyChanged != null)
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
        public override void WriteLine(string value)
        {
            // m_OriginalConsoleStream.WriteLine(value);
            var msg = value;
            if (string.IsNullOrEmpty(msg)) return;
            if (msg.EndsWith(Environment.NewLine)) msg = msg.Remove(msg.Length - 2);
            if (msg.StartsWith(Environment.NewLine)) msg = msg.Remove(0, 2);
            msg = msg.Trim(new char[] { '\uFEFF' });
            if (string.IsNullOrEmpty(msg)) return;
            //if (lastmsg == msg && lastmsg == Environment.NewLine) return;
            //lastmsg = msg;
            char c = msg[0];
            if (isError) Log.Error(msg);
            if (!isError) Log.Output(msg);

            // Fire event here with value
        }
        public static void SetToConsole()
        {
            Console.SetOut(new ConsoleDecorator(Console.Out));
        }
    }
    //public class DebugTextWriter : StreamWriter
    //{
    //    private TextWriter @out;
    //    public DebugTextWriter()
    //        : base(new DebugOutStream(), Encoding.Unicode, 1024)
    //    {
    //        AutoFlush = true;
    //    }

    //    public DebugTextWriter(TextWriter @out) : base(new DebugOutStream(), Encoding.Unicode, 1024)
    //    {
    //        this.@out = @out;
    //        AutoFlush = true;
    //    }

    //    class DebugOutStream : Stream
    //    {
    //        public DebugOutStream() { }
    //        public override void Write(byte[] buffer, int offset, int count)
    //        {
    //            var msg = Encoding.Unicode.GetString(buffer, offset, count);
    //            if (string.IsNullOrEmpty(msg)) return;
    //            if (msg.EndsWith(Environment.NewLine)) msg = msg.Remove(msg.Length - 2);
    //            if (msg.StartsWith(Environment.NewLine)) msg = msg.Remove(0, 2);
    //            msg = msg.Trim(new char[] { '\uFEFF' });
    //            if (string.IsNullOrEmpty(msg)) return;
    //            //if (lastmsg == msg && lastmsg == Environment.NewLine) return;
    //            //lastmsg = msg;
    //            char c = msg[0];
    //            Log.Output(msg);
    //            // Log.Debug(msg, Tracing.Output);
    //            // Debug.Write(Encoding.Unicode.GetString(buffer, offset, count));
    //        }
    //        public override bool CanRead { get { return false; } }
    //        public override bool CanSeek { get { return false; } }
    //        public override bool CanWrite { get { return true; } }
    //        public override void Flush() { System.Diagnostics.Debug.Flush(); }
    //        public override long Length { get { throw new InvalidOperationException(); } }
    //        public override int Read(byte[] buffer, int offset, int count) { throw new InvalidOperationException(); }
    //        public override long Seek(long offset, SeekOrigin origin) { throw new InvalidOperationException(); }
    //        public override void SetLength(long value) { throw new InvalidOperationException(); }
    //        public override long Position
    //        {
    //            get { throw new InvalidOperationException(); }
    //            set { throw new InvalidOperationException(); }
    //        }
    //    };
    //}
}
