using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    //public static class OpenRPALoggerConfigurationExtensions
    //{
    //    //public static LoggerConfiguration OpenRPATracing(this LoggerSinkConfiguration sinkConfiguration, LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose, string outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}", IFormatProvider formatProvider = null, LoggingLevelSwitch levelSwitch = null, LogEventLevel? standardErrorFromLevel = null)
    //    //{
    //    //    var result = new LoggerConfiguration();
    //    //    result.

    //    //    return result;
    //    //}

    //    public static LoggerConfiguration OpenRPATracing(
    //          this LoggerSinkConfiguration loggerConfiguration,
    //          Tracing tracing,
    //          IFormatProvider formatProvider = null)
    //    {

    //        tracing.formatProvider = formatProvider;
    //        return loggerConfiguration.Sink(tracing);
    //        //return loggerConfiguration.Sink(new MySink(formatProvider));
    //    }
    //}
    public class Tracing : TraceListener, System.ComponentModel.INotifyPropertyChanged //, ILogEventSink
    {
        //public static string Error = "Error";
        //public static string Selector = "Selector";
        //public static string Output = "Output";
        private int maxLines = 20;
        //private StringBuilder Tracebuilder = new StringBuilder();
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
                    //Tracebuilder.Clear();
                    //Tracebuilder.Insert(0, result);
                    //var arr = lines.ToList();
                    //arr.RemoveRange(0, (count - maxLines));
                    //result = String.Join(Environment.NewLine, arr);
                    //Tracebuilder.Clear();
                    //Tracebuilder.Insert(0, result);
                }
                return result;
            }
            //get { return String.Join(Environment.NewLine, _result.ToArray()); }
        }
        //private StringBuilder Outputbuilder = new StringBuilder();
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
                    //Outputbuilder.Clear();
                    //Outputbuilder.Insert(0, result);
                    //arr.RemoveRange(maxLines, (count - maxLines) );

                    //var arr = lines.ToList();
                    //arr.RemoveRange(0, (count - maxLines));
                    //result = String.Join(Environment.NewLine, arr);
                    //Outputbuilder.Clear();
                    //Outputbuilder.Insert(0, result);
                }
                return result;
            }
            //get { return String.Join(Environment.NewLine, _result.ToArray()); }
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
            //builder.Insert(0, message + Environment.NewLine);
            DateTime dt = DateTime.Now;
            if (category == "Output")
            {
                //Outputbuilder.Append(string.Format(@"[{0:HH\:mm\:ss\.fff}][{1}] {2}" + Environment.NewLine, dt, category, message));
                //Outputbuilder.Insert(0, string.Format(@"[{0:HH\:mm\:ss\.fff}][{1}] {2}" + Environment.NewLine, dt, category, message));
                _OutputMessages = _OutputMessages.Insert(0, string.Format(@"[{0:HH\:mm\:ss\.fff}][{1}] {2}" + Environment.NewLine, dt, category, message));
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("OutputMessages"));
            }
            //Tracebuilder.Append(string.Format(@"[{0:HH\:mm\:ss\.fff}][{1}] {2}" + Environment.NewLine, dt, category, message));
            // Tracebuilder.Insert(0, string.Format(@"[{0:HH\:mm\:ss\.fff}][{1}] {2}" + Environment.NewLine, dt, category, message));
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
        //public void Emit(LogEvent logEvent)
        //{
        //    DateTime dt = DateTime.Now;
        //    var message = logEvent.RenderMessage(formatProvider);
        //    if(logEvent.Level == LogEventLevel.Information)
        //    {
        //        //Outputbuilder.Append(string.Format(@"[{0:HH\:mm\:ss\.fff}][{1}] {2}" + Environment.NewLine, dt, logEvent.Level.ToString(), message));
        //        Outputbuilder.Insert(0, string.Format(@"[{0:HH\:mm\:ss\.fff}][{1}] {2}" + Environment.NewLine, dt, logEvent.Level.ToString(), message));
        //        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("OutputMessages"));
        //    }
        //    //Tracebuilder.Append(string.Format(@"[{0:HH\:mm\:ss\.fff}][{1}] {2}" + Environment.NewLine, dt, logEvent.Level.ToString(), message));
        //    Tracebuilder.Insert(0, string.Format(@"[{0:HH\:mm\:ss\.fff}][{1}] {2}" + Environment.NewLine, dt, logEvent.Level.ToString(), message));
        //    System.Diagnostics.Trace.Write Line(string.Format(@"[{0:HH\:mm\:ss\.fff}][{1}] {2}", dt, logEvent.Level.ToString(), message), "Tracing");
        //    OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Trace"));
        //    OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("TraceMessages"));

        //}

    }

    public class DebugTextWriter : StreamWriter
    {
        public DebugTextWriter()
            : base(new DebugOutStream(), Encoding.Unicode, 1024)
        {
            this.AutoFlush = true;
        }

        class DebugOutStream : Stream
        {
            //private string lastmsg = null;
            public override void Write(byte[] buffer, int offset, int count)
            {
                var msg = Encoding.Unicode.GetString(buffer, offset, count);
                if (string.IsNullOrEmpty(msg)) return;
                if (msg.EndsWith(Environment.NewLine)) msg = msg.Remove(msg.Length - 2);
                if (msg.StartsWith(Environment.NewLine)) msg = msg.Remove(0, 2);
                msg = msg.Trim(new char[] { '\uFEFF' });
                if (string.IsNullOrEmpty(msg)) return;
                //if (lastmsg == msg && lastmsg == Environment.NewLine) return;
                //lastmsg = msg;
                char c = msg[0];
                Log.Output(msg);

                // Log.Debug(msg, Tracing.Output);
                // Debug.Write(Encoding.Unicode.GetString(buffer, offset, count));
            }
            public override bool CanRead { get { return false; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return true; } }
            public override void Flush() { System.Diagnostics.Debug.Flush(); }
            public override long Length { get { throw new InvalidOperationException(); } }
            public override int Read(byte[] buffer, int offset, int count) { throw new InvalidOperationException(); }
            public override long Seek(long offset, SeekOrigin origin) { throw new InvalidOperationException(); }
            public override void SetLength(long value) { throw new InvalidOperationException(); }
            public override long Position
            {
                get { throw new InvalidOperationException(); }
                set { throw new InvalidOperationException(); }
            }
        };
    }
}
