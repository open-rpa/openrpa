using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRPA.Interfaces;
using System.Management.Automation;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using OpenRPA.Interfaces.entity;

namespace OpenRPA.PS
{
    [Cmdlet("Invoke", "OpenRPA")]
    public class InvokeOpenRPA : OpenRPACmdlet 
    {
        [Parameter(ValueFromPipeline = true, Position = 1, Mandatory = false)]
        public PSObject Object { get; set; }
        public string json { get; set; }
        [Parameter(Position = 2, ParameterSetName = "Using ID", Mandatory = true)]
        public string WorkflowId { get; set; }
        [Parameter(Position = 2, ParameterSetName = "Using Filename", Mandatory = true)]
        public string Filename { get; set; }
        public void WriteStatus(string message)
        {
            bool Debug = (bool)GetVariableValue("openflowdebug", false);
            if(!Debug) Debug = (bool)GetVariableValue("openrpadebug", false);
            if (!Debug) return;

            int origRow = Console.CursorTop;
            int origCol = Console.CursorLeft;
            System.Diagnostics.Trace.WriteLine(message + " cord: " + origRow + "," + origCol);
            // Console.SetCursorPosition(1, Console.WindowHeight - 1);
            message += new string(' ', Console.WindowWidth);
            message = "=| " + message.Substring(0, Console.WindowWidth - 3);
            Console.SetCursorPosition(0, 1);
            Console.Write(message);
            Console.SetCursorPosition(origCol, origRow);
        }
        public static void WriteStatus(int diff, string message)
        {
            int origRow = Console.CursorTop;
            int origCol = Console.CursorLeft;
            // System.Diagnostics.Trace.WriteLine(message + " cord: " + origRow + "," + origCol);
            Console.SetCursorPosition(1, (Console.WindowHeight - 1) - diff);
            // Console.SetCursorPosition(1, 1);
            Console.Write(message);
            Console.SetCursorPosition(origCol, origRow);
        }
        // readonly System.Threading.AutoResetEvent workItemsWaiting = new System.Threading.AutoResetEvent(false);
        public class TempClient : IOpenRPAClient
        {
            public List<IWorkflowInstance> WorkflowInstances => throw new NotImplementedException();

            public event StatusEventHandler Status;
            public event SignedinEventHandler Signedin;
            public event ConnectedEventHandler Connected;
            public event DisconnectedEventHandler Disconnected;
            public event ReadyForActionEventHandler ReadyForAction;
            public IWorkflow GetWorkflowByIDOrRelativeFilename(string IDOrRelativeFilename)
            {
                throw new NotImplementedException();
            }
            public IDesigner GetWorkflowDesignerByIDOrRelativeFilename(string IDOrRelativeFilename)
            {
                throw new NotImplementedException();
            }
            public IWorkflowInstance GetWorkflowInstanceByInstanceId(string InstanceId)
            {
                throw new NotImplementedException();
            }
        }
        protected override async Task ProcessRecordAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(WorkflowId) && string.IsNullOrEmpty(Filename))
                {
                    WriteError(new ErrorRecord(new Exception("Missing WorkflowId or Filename"), "", ErrorCategory.NotSpecified, null));
                    return;
                }
                if (Object != null)
                {
                    json = Object.toJson();
                }
                if (string.IsNullOrEmpty(json)) json = "{}";
                JObject tmpObject = JObject.Parse(json);
                correlationId = Guid.NewGuid().ToString().Replace("{", "").Replace("}", "").Replace("-", "");
                IDictionary<string, object> _robotcommand = new System.Dynamic.ExpandoObject();
                _robotcommand["workflowid"] = WorkflowId;
                _robotcommand["command"] = "invoke";
                _robotcommand.Add("data", tmpObject);
                WriteProgress(new ProgressRecord(0, "Invoking", "Invoking " + WorkflowId));

                var client = new TempClient();
                AutomationHelper.syncContext = System.Threading.SynchronizationContext.Current;
                OpenRPA.Interfaces.Plugins.LoadPlugins(client, "c:\\program files\\openrpa", false);
                //System.Activities.Activity activity = null;
                System.Activities.ActivityBuilder ab2;
                var Xaml = System.IO.File.ReadAllText(Filename);
                using (var stream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(Xaml)))
                {
                    ab2 = System.Xaml.XamlServices.Load(
                        System.Activities.XamlIntegration.ActivityXamlServices.CreateBuilderReader(
                        new System.Xaml.XamlXmlReader(stream))) as System.Activities.ActivityBuilder;
                }

                var result = System.Activities.WorkflowInvoker.Invoke(ab2.Implementation);
                // workItemsWaiting.WaitOne();
                WriteProgress(new ProgressRecord(0, "Invoking", "completed") { RecordType = ProgressRecordType.Completed });
                if (result != null)
                {
                    var payload = JObject.FromObject(result);
                    var _result = payload.toPSObject();
                    WriteObject(_result);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "", ErrorCategory.NotSpecified, null));
                WriteProgress(new ProgressRecord(0, "Invoking", "completed") { RecordType = ProgressRecordType.Completed });
            }
        }
        protected override Task EndProcessingAsync()
        {
            // if(global.webSocketClient != null) global.webSocketClient.OnQueueMessage -= WebSocketClient_OnQueueMessage;
            return base.EndProcessingAsync();
        }
        private string correlationId = null;
        // private Interfaces.mq.RobotCommand command = null;
        //private void WebSocketClient_OnQueueMessage(IQueueMessage message, QueueMessageEventArgs e)
        //{
        //    if (correlationId == message.correlationId && message.data != null)
        //    {
        //        command = Newtonsoft.Json.JsonConvert.DeserializeObject<Interfaces.mq.RobotCommand>(message.data.ToString());
        //        if (command.command == "invokefailed" || command.command == "invokeaborted" || command.command == "invokecompleted")
        //        {
        //            workItemsWaiting.Set();
        //        }
        //    }
        //}
    }
}
