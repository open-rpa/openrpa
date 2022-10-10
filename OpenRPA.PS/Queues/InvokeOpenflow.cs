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
    [Cmdlet("Invoke", "Openflow")]
    public class InvokeOpenflow : OpenRPACmdlet, IDynamicParameters
    {
        [Parameter(ValueFromPipeline = true, Position = 1, Mandatory = true, ParameterSetName = "withObject")]
        public PSObject Object { get; set; }
        [Parameter(ValueFromPipeline = true, Position = 1, Mandatory = false, ParameterSetName = "withJson")]
        public string TargetQueue { get; set; }
        public string json { get; set; }
        [Parameter(Mandatory = false)]
        public int Expiration { get; set; }

        private static RuntimeDefinedParameterDictionary _staticStorage;
        private static openflowworkflow[] _workflows;
        public object GetDynamicParameters()
        {
            if (_Collections == null)
            {
                Initialize().Wait(5000);
            }
            if (global.webSocketClient == null || !global.webSocketClient.isConnected || global.webSocketClient.user == null) return new RuntimeDefinedParameterDictionary();
            if (_workflows == null)
            {
                _workflows = global.webSocketClient.Query<openflowworkflow>("workflow", "{_type: 'workflow', rpa: true}").Result;
            }
            var workflows = _workflows.Select(x => x.name).ToArray();
            if (_staticStorage == null)
            {
                var runtimeDefinedParameterDictionary = new RuntimeDefinedParameterDictionary();
                var attrib = new Collection<Attribute>()
                {
                    new ParameterAttribute() {
                        HelpMessage = "What NodeRed \"workflow in\" node to send too",
                        Position = 1
                    },
                    new ValidateSetAttribute(workflows)
                };
                var parameter = new RuntimeDefinedParameter("Workflow", typeof(string), attrib);
                runtimeDefinedParameterDictionary.Add("Workflow", parameter);
                _staticStorage = runtimeDefinedParameterDictionary;
            }
            return _staticStorage;
        }
        readonly System.Threading.AutoResetEvent workItemsWaiting = new System.Threading.AutoResetEvent(false);
        protected override async Task ProcessRecordAsync()
        {
            try
            {
                var WorkflowRuntime = new RuntimeDefinedParameter();
                string WorkflowName = "";
                if (_staticStorage != null)
                {
                    _staticStorage.TryGetValue("Workflow", out WorkflowRuntime);
                    if (WorkflowRuntime.Value != null && !string.IsNullOrEmpty(WorkflowRuntime.Value.ToString())) WorkflowName = WorkflowRuntime.Value.ToString();
                }
                var workflow = (_workflows == null ? null : _workflows.Where(x => x.name == WorkflowName).FirstOrDefault());
                if((workflow == null || string.IsNullOrEmpty(WorkflowName)) && string.IsNullOrEmpty(TargetQueue))
                {
                    WriteError(new ErrorRecord(new Exception("Missing workflow name or workflow not found"), "", ErrorCategory.NotSpecified, null));
                    return;
                }
                if (workflow != null) TargetQueue = workflow.queue;
                if (Object != null)
                {
                    json = Object.toJson();
                }
                if (string.IsNullOrEmpty(json)) json = "{}";
                await RegisterQueue();
                JObject tmpObject = JObject.Parse(json);
                correlationId = Guid.NewGuid().ToString().Replace("{", "").Replace("}", "").Replace("-", "");

                if(global.webSocketClient!=null) global.webSocketClient.OnQueueMessage += WebSocketClient_OnQueueMessage;

                if (workflow != null) WriteProgress(new ProgressRecord(0, "Invoking", "Invoking " + workflow.name));
                if (workflow == null) WriteProgress(new ProgressRecord(0, "Invoking", "Invoking " + TargetQueue));
                var result = await global.webSocketClient.QueueMessage(TargetQueue, tmpObject, psqueue, correlationId, Expiration, true);
                workItemsWaiting.WaitOne();
                WriteProgress(new ProgressRecord(0, "Invoking", "completed") { RecordType = ProgressRecordType.Completed });

                JObject payload = msg;
                if (msg.ContainsKey("payload")) payload = msg.Value<JObject>("payload");
                if (state == "failed")
                {
                    var message = "Invoke OpenFlow Workflow failed";
                    if (msg.ContainsKey("error")) message = msg["error"].ToString();
                    if (msg.ContainsKey("_error")) message = msg["_error"].ToString();
                    if (payload.ContainsKey("error")) message = payload["error"].ToString();
                    if (payload.ContainsKey("_error")) message = payload["_error"].ToString();
                    if (string.IsNullOrEmpty(message)) message = "Invoke OpenFlow Workflow failed";
                    WriteError(new ErrorRecord(new Exception(message), "", ErrorCategory.NotSpecified, null));
                    return;
                }
                if (payload != null)
                {
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
            if(global.webSocketClient!=null) global.webSocketClient.OnQueueMessage -= WebSocketClient_OnQueueMessage;
            return base.EndProcessingAsync();
        }
        private string correlationId = null;
        private JObject msg = null;
        private string state = null;
        private void WebSocketClient_OnQueueMessage(IQueueMessage message, QueueMessageEventArgs e)
        {
            if(correlationId == message.correlationId && message.data != null)
            {
                msg = JObject.Parse(message.data.ToString());
                state = msg["state"].ToString();
                if (!string.IsNullOrEmpty(state))
                {
                    if(state == "failed" || state == "completed")
                    {
                        workItemsWaiting.Set();
                    }
                }
            }
        }
    }
}
