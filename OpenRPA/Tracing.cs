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
using Microsoft.Extensions.Logging;
using Microsoft.CodeAnalysis;
using OpenTelemetry.Logs;
using System.Diagnostics.Metrics;

namespace OpenRPA.Interfaces
{
    //public static partial class LoggerExtensions
    //{
    //    [LoggerMessage(
    //    Level = LogLevel.Critical,
    //    Message = "Could not open socket to `{e}`")]
    //    static partial void SocketError(this ILogger<Tracing> logger, LoggerEntry e);

    //    [LoggerMessage(Level = LogLevel.Critical)]
    //    static partial void Critical(this ILogger<Tracing> logger, [LogProperties()] in LoggerEntry e);
    //    [LoggerMessage(Level = LogLevel.Error)]
    //    static partial void Error(this ILogger<Tracing> logger, [LogProperties()] in LoggerEntry e);
    //    [LoggerMessage(Level = LogLevel.Warning)]
    //    static partial void Warning(this ILogger<Tracing> logger, [LogProperties()] in LoggerEntry e);
    //    [LoggerMessage(Level = LogLevel.Debug)]
    //    static partial void Debug(this ILogger<Tracing> logger, [LogProperties()] in LoggerEntry e);
    //    [LoggerMessage(Level = LogLevel.Trace)]
    //    static partial void Trace(this ILogger<Tracing> logger, [LogProperties()] in LoggerEntry e);
    //}
    public class LoggerEntry
    {
        public LoggerEntry(string message)
        {
            this.message = message;
            this.project = null;
            this.workflow = null;
            ofid = Config.local.openflow_uniqueid;
            host = Environment.MachineName.ToLower();
            fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
        }
        public LoggerEntry(string message, IProject project, IWorkflow workflow)
        {
            this.message = message;
            this.project = null;
            if (project != null) this.project = project.name;
            this.workflow = null;
            if (workflow != null) this.workflow = workflow.name;
            ofid = Config.local.openflow_uniqueid;
            host = Environment.MachineName.ToLower();
            fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
        }
        public string message { get; set; }
        public string project{ get; set; }
        public string workflow { get; set; }
        public string ofid { get; set; }
        public string fqdn { get; set; }
        public string host { get; set; }
        public void Critical() { Log(LogLevel.Critical); }
        public void Error() { Log(LogLevel.Error); }
        public void Warning() { Log(LogLevel.Warning); }
        public void Information() { Log(LogLevel.Information); }
        public void Debug() { Log(LogLevel.Debug); }
        public void Trace() { Log(LogLevel.Trace); }
        public void Log(LogLevel logLevel)
        {
            message = message.Replace("{", "(").Replace("}", ")");
            if (!string.IsNullOrEmpty(project) && !string.IsNullOrEmpty(workflow))
            {
                RobotInstance.LocalLogProvider?.Log(logLevel, "[{project}][{workflow}] " + message, project, workflow);
            }
            else
            {
                RobotInstance.LocalLogProvider?.Log(logLevel, message);
            }
        }
    }
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
            try
            {
                if (string.IsNullOrEmpty(logpath))
                {
                    logpath = Extensions.ProjectsDirectory;
                }
                try
                {
                    IProject project = null;
                    IWorkflow workflow = null;
                    if (InstanceId.IsValueCreated)
                    {
                        var i = WorkflowInstance.Instances.Where(x => x.InstanceId == InstanceId.Value).LastOrDefault();
                        // message = "[" + Thread.CurrentThread.ManagedThreadId + "][" + InstanceId.Value + "]" + message;
                        workflow = i?.Workflow;
                        project = i?.Workflow?.Project();
                    }
                    
                    // if (i.console == null) i.console = new List<WorkflowConsoleLog>();
                    int lvl = 7;
                    if (category == "Error")
                    {
                        lvl = 0;
                        (new LoggerEntry(message, project, workflow)).Error();
                    }
                    else if (category == "Warning")
                    {
                        lvl = 1;
                        (new LoggerEntry(message, project, workflow)).Warning();
                    }
                    else if (category == "Output" || category == "Information" || category == "")
                    {
                        lvl = 2;
                        (new LoggerEntry(message, project, workflow)).Information();
                    }
                    else if (category == "Debug")
                    {
                        lvl = 3;
                        (new LoggerEntry(message, project, workflow)).Debug();
                    }
                    else if (category == "Verbose")
                    {
                        lvl = 4;
                        (new LoggerEntry(message, project, workflow)).Trace();
                    }
                    else if (category == "network")
                    {
                        (new LoggerEntry(message, project, workflow)).Trace();
                    }
                    else
                    {
                        (new LoggerEntry(message, project, workflow)).Trace();
                    }

                    if (InstanceId.IsValueCreated)
                    {
                        var i = WorkflowInstance.Instances.Where(x => x.InstanceId == InstanceId.Value).LastOrDefault();
                        // message = "[" + Thread.CurrentThread.ManagedThreadId + "][" + InstanceId.Value + "]" + message;
                        if (i != null && i.Workflow != null && project != null && category != "Network")
                        {
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
                                    global.webSocketClient.QueueMessage(i.queuename, command, null, i.correlationId, 0, true, i.TraceId, i.SpanId);
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
                }
                catch (Exception)
                {
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
            catch (Exception)
            {
            }
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
}
