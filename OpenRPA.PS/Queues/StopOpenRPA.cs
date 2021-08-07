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
    [Cmdlet("Stop", "OpenRPA", DefaultParameterSetName = "Using Id")]
    public class StopOpenRPA : Cmdlet
    {
        [Parameter(Position = 0, ParameterSetName = "Using Id", Mandatory = true)]
        public string WorkflowId { get; set; }
        [Parameter(Position = 0, ParameterSetName = "Using Filename", Mandatory = false)]
        public string Filename { get; set; }
        [Parameter()] public SwitchParameter ChildSession { get; set; }
        [Parameter()] public SwitchParameter All { get; set; }
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
                string Id = WorkflowId;
                if (string.IsNullOrEmpty(WorkflowId) && string.IsNullOrEmpty(Filename))
                {
                    if (!All.IsPresent)
                    {
                        WriteError(new ErrorRecord(new Exception("Missing WorkflowId or Filename"), "", ErrorCategory.NotSpecified, null));
                        return;
                    }
                }
                if (string.IsNullOrEmpty(Id)) Id = Filename;
                WriteProgress(new ProgressRecord(0, "Invoking", "Invoking " + WorkflowId));
                Interfaces.IPCService.OpenRPAServiceUtil.GetInstance(ChildSession: ChildSession.IsPresent);
                int result = 0;
                if (string.IsNullOrEmpty(Id))
                {
                    result = Interfaces.IPCService.OpenRPAServiceUtil.RemoteInstance.KillAllWorkflows();
                }
                else
                {
                    result = Interfaces.IPCService.OpenRPAServiceUtil.RemoteInstance.KillWorkflows(Id);
                }
                WriteObject(result);

                WriteProgress(new ProgressRecord(0, "Invoking", "completed") { RecordType = ProgressRecordType.Completed });
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "", ErrorCategory.NotSpecified, null));
                WriteProgress(new ProgressRecord(0, "Invoking", "completed") { RecordType = ProgressRecordType.Completed });
            }
        }
    }
}
