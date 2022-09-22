using Newtonsoft.Json.Linq;
using OpenRPA.Views;
using System;
using System.Activities.Hosting;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public class Tracing : TraceListener, System.ComponentModel.INotifyPropertyChanged //, ILogEventSink
    {
        // private int maxLines = 20;
        private string _TraceMessages = "";
        public string TraceMessages
        {
            get
            {
                string result = string.Empty;
                result = _TraceMessages;
                var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                int count = lines.Count();
                if (count > Config.local.max_trace_lines)
                {
                    var list = lines.ToList();
                    list.RemoveRange((Config.local.max_trace_lines - 10), (count - Config.local.max_trace_lines) + 10);
                    _TraceMessages = string.Join(Environment.NewLine, list);
                }
                return _TraceMessages;
            }
            set
            {
                _TraceMessages = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Trace"));
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("TraceMessages"));
            }
        }
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            base.TraceEvent(eventCache, source, eventType, id);
        }
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            base.TraceEvent(eventCache, source, eventType, id, format, args);
        }
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            base.TraceEvent(eventCache, source, eventType, id, message);
        }
        public IWorkflowInstance workflowInstance { get; set; }
        private string _OutputMessages = "";
        public string OutputMessages
        {
            get
            {
                string result = "";
                result = _OutputMessages;
                var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                int count = lines.Count();
                if (count > Config.local.max_output_lines)
                {
                    var list = lines.ToList();
                    list.RemoveRange((Config.local.max_output_lines - 10), (count - Config.local.max_output_lines) + 10);
                    _OutputMessages = string.Join(Environment.NewLine, list);
                }
                return _OutputMessages;
            }
            set
            {
                _OutputMessages = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("OutputMessages"));
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
        public static ThreadLocal<string> InstanceId = new ThreadLocal<string>();
        public override void WriteLine(string message, string category)
        {
            if (string.IsNullOrEmpty(logpath))
            {
                logpath = Extensions.ProjectsDirectory;
            }
            if (InstanceId.IsValueCreated)
            {
                var i = WorkflowInstance.Instances.Where(x => x.InstanceId == InstanceId.Value).LastOrDefault();
                // message = "[" + Thread.CurrentThread.ManagedThreadId + "][" + InstanceId.Value + "]" + message;
                IProject project = i?.Workflow?.Project();
                if (i != null && i.Workflow != null && project != null && category != "Network")
                {
                    // if (i.console == null) i.console = new List<WorkflowConsoleLog>();
                    int lvl = 7;
                    if (category == "Error") lvl = 0;
                    if (category == "Warning") lvl = 1;
                    if (category == "Output" || category == "Information" || category == "") lvl = 2;
                    if (category == "Debug") lvl = 3;
                    if (category == "Verbose") lvl = 4;
                    if ((i.Workflow.save_output || project.save_output) && !Config.local.skip_online_state)
                    {
                        var msg = new WorkflowConsoleLog() { msg = message, lvl = lvl };
                        if (Monitor.TryEnter(i, 1000))
                        {
                            try
                            {
                                if (i.console == null) i.console = new List<WorkflowConsoleLog>();
                                i.console.Insert(0, msg);
                                i.isDirty = true;
                            }
                            finally
                            {
                                Monitor.Exit(i);
                            }
                        }
                    }
                    if ((i.Workflow.send_output || project.send_output) && !string.IsNullOrEmpty(i.queuename) && !string.IsNullOrEmpty(i.correlationId))
                    {
                        try
                        {
                            mq.RobotOutputCommand command = new mq.RobotOutputCommand();
                            command.command = "output";
                            command.level = lvl;
                            command.workflowid = i.WorkflowId;
                            command.data = message;
                            global.webSocketClient.QueueMessage(i.queuename, command, null, i.correlationId, 0);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    //message = "[" + i.queuename + "]" + message;
                    //message = "[" + Thread.CurrentThread.ManagedThreadId + "][" + InstanceId.Value + "]" + message;
                }
                else
                {
                    //message = "[" + Thread.CurrentThread.ManagedThreadId + "][" + InstanceId.Value + "]" + message;
                }
            } 
            else
            {
                //message = "[" + Thread.CurrentThread.ManagedThreadId + "][null]" + message;
            }

            if (category == "Tracing") return;
            DateTime dt = DateTime.Now;
            if (category == "Output")
            {
                _OutputMessages = _OutputMessages.Insert(0, string.Format(@"[{0:HH\:mm\:ss\.fff}][{1}] {2}" + Environment.NewLine, dt, category, message));
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("OutputMessages"));
            }
            if (category == "Output" && !Config.local.log_output) return;
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
