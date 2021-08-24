using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(StopOpenRPADesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.invokerpaworkflow.png")]
    [LocalizedToolboxTooltip("activity_stopopenrpa_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_stopopenrpa", typeof(Resources.strings))]
    public class StopOpenRPA : NativeActivity
    {
        [LocalizedDisplayName("activity_workflow", typeof(Resources.strings)), LocalizedDescription("activity_workflow_help", typeof(Resources.strings))]
        public InArgument<string> workflow { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_killall", typeof(Resources.strings)), LocalizedDescription("activity_killall_help", typeof(Resources.strings))]
        public InArgument<bool> KillAll { get; set; } = false;
        [RequiredArgument, LocalizedDisplayName("activity_killself", typeof(Resources.strings)), LocalizedDescription("activity_killself_help", typeof(Resources.strings))]
        public InArgument<bool> KillSelf { get; set; } = false;
        public OutArgument<int> Result { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            bool killall = KillAll.Get(context);
            bool killself = KillSelf.Get(context);
            string WorkflowInstanceId = context.WorkflowInstanceId.ToString();
            string workflowid = workflow.Get(context);
            if (killall) workflowid = "";
            if (string.IsNullOrEmpty(workflowid) && !killall) throw new Exception("Kill all not enabled and no workflow selected");
            int result = 0;
            try
            {
                var Instance = WorkflowInstance.Instances.Where(x => x.InstanceId == context.WorkflowInstanceId.ToString()).FirstOrDefault();
                foreach (var i in global.OpenRPAClient.WorkflowInstances.ToList())
                {
                    if (!killself && i.InstanceId == WorkflowInstanceId) continue;
                    if (!i.isCompleted)
                    {
                        if (!string.IsNullOrEmpty(workflowid))
                        {
                            if (i.Workflow._id == workflowid || i.Workflow.RelativeFilename == workflowid)
                            {
                                i.Abort("Killed by StopOpenRPA activity from " + Instance.Workflow.name);
                                result++;
                            }
                        }
                        else
                        {
                            i.Abort("Killed by StopOpenRPA activity from " + Instance.Workflow.name);
                            result++;
                        }
                    }
                }
                Result.Set(context, result);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
        }
        [LocalizedDisplayName("activity_displayname", typeof(Resources.strings)), LocalizedDescription("activity_displayname_help", typeof(Resources.strings))]
        public new string DisplayName
        {
            get
            {
                var displayName = base.DisplayName;
                if (displayName == this.GetType().Name)
                {
                    var displayNameAttribute = this.GetType().GetCustomAttributes(typeof(DisplayNameAttribute), true).FirstOrDefault() as DisplayNameAttribute;
                    if (displayNameAttribute != null) displayName = displayNameAttribute.DisplayName;
                }
                return displayName;
            }
            set
            {
                base.DisplayName = value;
            }
        }
    }
}