using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(GetWorkflowInstanceDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.getworkflowinstance.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    [LocalizedToolboxTooltip("activity_getworkflowinstance_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_getworkflowinstance", typeof(Resources.strings))]
    public class GetWorkflowInstance : NativeActivity
    {
        public InArgument<string> Browser { get; set; }
        [RequiredArgument, OverloadGroup("Result")]
        public OutArgument<WorkflowInstance> Result { get; set; }
        [RequiredArgument, OverloadGroup("Results")]
        public OutArgument<WorkflowInstance[]> Results { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            string WorkflowInstanceId = context.WorkflowInstanceId.ToString();
            if (Result != null && !Result.GetIsEmpty())
            {
                var i = WorkflowInstance.Instances.Where(x => x.InstanceId == WorkflowInstanceId).FirstOrDefault();
                Result.Set(context, i);
            } else
            {
                Results.Set(context, WorkflowInstance.Instances.ToArray());
            }
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
        }
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