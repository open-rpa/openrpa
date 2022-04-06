using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(BreakableWhileDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.foreach.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    [LocalizedToolboxTooltip("activity_breakablewhile_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_breakablewhile", typeof(Resources.strings))]
    public class BreakableWhile : BreakableLoop // , System.Activities.Presentation.IActivityTemplateFactory
    {
        [RequiredArgument, LocalizedDisplayName("activity_condition", typeof(Resources.strings)), LocalizedDescription("activity_condition_help", typeof(Resources.strings))]
        public Activity<bool> Condition { get; set; }
        [Browsable(false)]
        public Activity Body { get; set; }
        private Variable<IEnumerator<System.Data.DataRowView>> _elements = new Variable<IEnumerator<System.Data.DataRowView>>("_elements");
        protected override void StartLoop(NativeActivityContext context)
        {
            if (Body != null)
            {
                if (!breakRequested && !context.IsCancellationRequested)
                {
                    IncIndex(context);
                    context.ScheduleActivity(Condition, OnConditionComplete, null);
                }
            }
        }
        private void OnConditionComplete(NativeActivityContext context, ActivityInstance completedInstance, bool result)
        {
            if (result) context.ScheduleActivity(Body, OnBodyComplete);
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            StartLoop(context);
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddChild(Body);
            metadata.AddChild(Condition);
            metadata.AddImplementationVariable(_elements);
            base.CacheMetadata(metadata);
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