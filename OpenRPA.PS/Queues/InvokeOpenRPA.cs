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
    public class InvokeOpenRPA : Cmdlet 
    {
        [Parameter(ValueFromPipeline = true, Position = 1, Mandatory = false)]
        public PSObject Object { get; set; }
        public string json { get; set; }
        [Parameter(Position = 2, ParameterSetName = "Using ID", Mandatory = true)]
        public string WorkflowId { get; set; }
        [Parameter(Position = 2, ParameterSetName = "Using Filename", Mandatory = true)]
        public string Filename { get; set; }
        [Parameter()] public SwitchParameter ChildSession { get; set; }
        public void WriteStatus(string message)
        {
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
        protected override void ProcessRecord()
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

                var param = tmpObject.ToObject<Dictionary<string, object>>();
                WriteProgress(new ProgressRecord(0, "Invoking", "Invoking " + WorkflowId));
                //try
                //{
                //    Interfaces.IPCService.OpenRPAServiceUtil.GetInstance();
                //}
                //catch (Exception)
                //{
                //    try
                //    {
                //        System.Diagnostics.Process.Start("OpenRPA.exe");
                //        System.Threading.Thread.Sleep(1000);
                //    }
                //    catch (Exception)
                //    {
                //    }
                //}
                Interfaces.IPCService.OpenRPAServiceUtil.GetInstance(ChildSession: ChildSession.IsPresent);
                var result = Interfaces.IPCService.OpenRPAServiceUtil.RemoteInstance.RunWorkflowByIDOrRelativeFilename(Filename, true, param);
                // Interfaces.IPCService.OpenRPAServiceUtil.RemoteInstance.ParseCommandLineArgs(new string[] { "workflowid", Filename });

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
        // private string correlationId = null;
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
