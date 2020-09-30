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
    public class workflow : apibase
    {
        [Newtonsoft.Json.JsonProperty("projectandname")]
        public string ProjectAndName { get; set; }
    }
    [Cmdlet("Invoke", "OpenRPA")]
    public class InvokeOpenRPA : OpenRPACmdlet, IDynamicParameters
    {
        // [Parameter(ValueFromPipeline = true, Position = 1, Mandatory = false, ParameterSetName = "withObject")]
        [Parameter(ValueFromPipeline = true, Position = 1, Mandatory = false)]
        public PSObject Object { get; set; }
        //[Parameter(ValueFromPipeline = true, Position = 1, Mandatory = false, ParameterSetName = "withJson")]
        public string json { get; set; }
        [Parameter(Position = 2)]
        public string TargetId { get; set; }
        [Parameter(Position = 3)]
        public string WorkflowId { get; set; }
        private static RuntimeDefinedParameterDictionary _staticStorage;
        private static apiuser[] _robots;
        private static workflow[] _workflows;
        private static string lasttargetid = null;
        private static int callcount = 0;
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
        public object GetDynamicParameters()
        {
            try
            {
                callcount++;
                if (_Collections == null)
                {
                    Initialize().Wait();
                }
                RuntimeDefinedParameter targetnameparameter = null;
                RuntimeDefinedParameter workflownameparameter = null;
                if (_robots == null)
                {
                    WriteStatus("Getting possible robots and roles");
                    _robots = global.webSocketClient.Query<apiuser>("users", "{\"$or\":[ {\"_type\": \"user\"}, {\"_type\": \"role\", \"rparole\": true} ]}", top: 2000).Result;
                }
                var TargetName = this.GetUnboundValue<string>("TargetName");
                if (_staticStorage != null)
                {
                    _staticStorage.TryGetValue("TargetName", out targetnameparameter);
                    _staticStorage.TryGetValue("WorkflowName", out workflownameparameter);
                    //WriteStatus(2, "targetname: " + targetnameparameter.Value + " workflowname: " + workflownameparameter.Value + " test: " + TargetName + "    ");
                    //WriteStatus(1, "targetname: " + targetnameparameter.IsSet + " workflowname: " + workflownameparameter.IsSet + "     ");
                }
                else
                {
                    var robotnames = _robots.Select(x => x.name).ToArray();
                    var targetnameattr = new Collection<Attribute>()
                    {
                        new ParameterAttribute() {
                            HelpMessage = "Targer username or group name",
                            Position = 1
                        },
                        new ValidateSetAttribute(robotnames)
                    };
                    targetnameparameter = new RuntimeDefinedParameter("TargetName", typeof(string), targetnameattr);
                    var runtimeDefinedParameterDictionary = new RuntimeDefinedParameterDictionary();
                    runtimeDefinedParameterDictionary.Add("TargetName", targetnameparameter);
                    _staticStorage = runtimeDefinedParameterDictionary;
                }

                apiuser robot = null;
                string targetid = TargetId;

                if (targetnameparameter.Value != null) TargetName = targetnameparameter.Value.ToString();
                if (!string.IsNullOrEmpty(TargetName))
                {
                    robot = _robots.Where(x => x.name == TargetName).FirstOrDefault();
                    if (robot != null) { targetid = robot._id; }
                } 
                else if (!string.IsNullOrEmpty(targetid))
                {
                    robot = _robots.Where(x => x._id == targetid).FirstOrDefault();
                }

                if ((_workflows == null || lasttargetid != targetid) && robot != null)
                {
                    WriteStatus("Getting possible workflows for " + robot.name);
                    _workflows = global.webSocketClient.Query<workflow>("openrpa", "{_type: 'workflow'}", projection: "{\"projectandname\": 1}", queryas: targetid, top: 2000).Result;
                    lasttargetid = targetid;
                }
                int wflen = 0;
                if (_workflows != null) wflen = _workflows.Length;
                if (robot != null)
                {
                    WriteStatus("(" + callcount + ") robots: " + _robots.Length + " workflows: " + wflen + " for " + robot.name);
                }
                else
                {
                    WriteStatus("(" + callcount + ") robots: " + _robots.Length + " workflows: " + wflen);
                }
                
                if (workflownameparameter == null)
                {
                    var workflownameattr = new Collection<Attribute>()
                    {
                        new ParameterAttribute() {
                            HelpMessage = "Workflow name",
                            Position = 2
                        }
                    };
                    workflownameparameter = new RuntimeDefinedParameter("WorkflowName", typeof(string), workflownameattr);
                    _staticStorage.Add("WorkflowName", workflownameparameter);
                }
                if (workflownameparameter != null)
                {
                    ValidateSetAttribute wfname = (ValidateSetAttribute)workflownameparameter.Attributes.Where(x => x.GetType() == typeof(ValidateSetAttribute)).FirstOrDefault();
                    if(wfname != null) workflownameparameter.Attributes.Remove(wfname);
                    if(_workflows != null && _workflows.Length > 0)
                    {
                        var workflownames = _workflows.Select(x => x.ProjectAndName).ToArray();
                        wfname = new ValidateSetAttribute(workflownames);
                        workflownameparameter.Attributes.Add(wfname);
                    }
                }
                return _staticStorage;
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine(ex.Message);
                throw;
            }
        }
        readonly System.Threading.AutoResetEvent workItemsWaiting = new System.Threading.AutoResetEvent(false);
        protected override async Task ProcessRecordAsync()
        {
            try
            {
                RuntimeDefinedParameter targetnameparameter = null;
                RuntimeDefinedParameter workflownameparameter = null;
                if (_staticStorage != null)
                {
                    _staticStorage.TryGetValue("TargetName", out targetnameparameter);
                    _staticStorage.TryGetValue("WorkflowName", out workflownameparameter);
                }
                apiuser robot = null;
                string targetid = TargetId;
                workflow workflow;
                string workflowid = WorkflowId;
                if (targetnameparameter.Value != null) targetid = targetnameparameter.Value.ToString();
                if (!string.IsNullOrEmpty(targetid))
                {
                    robot = _robots.Where(x => x.name == targetid).FirstOrDefault();
                    if (robot != null) { targetid = robot._id; }
                }
                if (_Collections == null)
                {
                    Initialize().Wait();
                }
                if (_workflows == null && robot != null)
                {
                    _workflows = await global.webSocketClient.Query<workflow>("openrpa", "{_type: 'workflow'}", projection: "{\"projectandname\": 1}", queryas: robot._id, top: 2000);
                }
                if (workflownameparameter.Value != null)
                {
                    workflow = _workflows.Where(x => x.ProjectAndName == workflownameparameter.Value.ToString()).FirstOrDefault();
                    if (workflow != null) { workflowid = workflow._id; }
                }
                if (string.IsNullOrEmpty(targetid))
                {
                    WriteError(new ErrorRecord(new Exception("Missing robot name or robot id"), "", ErrorCategory.NotSpecified, null));
                    return;
                }
                robot = _robots.Where(x => x._id == targetid).FirstOrDefault();
                if (string.IsNullOrEmpty(workflowid))
                {
                    WriteError(new ErrorRecord(new Exception("Missing workflow name or workflow id"), "", ErrorCategory.NotSpecified, null));
                    return;
                }
                _staticStorage = null;
                callcount = 0;
                workflow = _workflows.Where(x => x._id == workflowid).FirstOrDefault();
                if (Object != null)
                {
                    json = Object.toJson();
                }
                if (string.IsNullOrEmpty(json)) json = "{}";
                await RegisterQueue();
                JObject tmpObject = JObject.Parse(json);
                correlationId = Guid.NewGuid().ToString().Replace("{", "").Replace("}", "").Replace("-", "");

                global.webSocketClient.OnQueueMessage += WebSocketClient_OnQueueMessage;
                
                IDictionary<string, object> _robotcommand = new System.Dynamic.ExpandoObject();
                _robotcommand["workflowid"] = workflowid;
                _robotcommand["command"] = "invoke";
                _robotcommand.Add("data", tmpObject);
                WriteProgress(new ProgressRecord(0, "Invoking", "Invoking " + workflow.ProjectAndName + " on " + robot.name + "(" + robot.username + ")"));
                var result = await global.webSocketClient.QueueMessage(targetid, _robotcommand, psqueue, correlationId);
                workItemsWaiting.WaitOne();
                WriteProgress(new ProgressRecord(0, "Invoking", "completed") { RecordType = ProgressRecordType.Completed });
                if (command.command == "invokefailed" || command.command == "invokeaborted")
                {
                    var _ex = new Exception("Invoke failed");
                    if(command.data != null && !string.IsNullOrEmpty(command.data.ToString()))
                    {
                        try
                        {
                            _ex = Newtonsoft.Json.JsonConvert.DeserializeObject<Exception>(command.data.ToString());
                        }
                        catch (Exception)
                        {
                        }
                    }
                    WriteError(new ErrorRecord(_ex, "", ErrorCategory.NotSpecified, null));
                    return;
                }
                if (command.data != null && !string.IsNullOrEmpty(command.data.ToString()))
                {
                    var payload = JObject.Parse(command.data.ToString());
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
            global.webSocketClient.OnQueueMessage -= WebSocketClient_OnQueueMessage;
            return base.EndProcessingAsync();
        }
        private string correlationId = null;
        private Interfaces.mq.RobotCommand command = null;
        private string state = null;
        private void WebSocketClient_OnQueueMessage(IQueueMessage message, QueueMessageEventArgs e)
        {
            if (correlationId == message.correlationId && message.data != null)
            {
                command = Newtonsoft.Json.JsonConvert.DeserializeObject<Interfaces.mq.RobotCommand>(message.data.ToString());
                if (command.command == "invokefailed" || command.command == "invokeaborted" || command.command == "invokecompleted")
                {
                    workItemsWaiting.Set();
                }
            }
        }
    }
}
