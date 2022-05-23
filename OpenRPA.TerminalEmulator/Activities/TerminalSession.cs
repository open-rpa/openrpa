using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.TerminalEmulator
{
    [System.ComponentModel.Designer(typeof(TerminalSessionDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(TerminalSession), "Resources.toolbox.terminalsession.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    [LocalizedToolboxTooltip("activity_Breakabledowhile_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_Breakabledowhile", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_Breakabledowhile_helpurl", typeof(Resources.strings))]
    public class TerminalSession : BreakableLoop // , System.Activities.Presentation.IActivityTemplateFactory
    {
        public Activity Body { get; set; }
        private Variable<IEnumerator<System.Data.DataRowView>> _elements = new Variable<IEnumerator<System.Data.DataRowView>>("_elements");
        public InArgument<string> Hostname { get; set; }
        public InArgument<string> TermType { get; set; }        
        public InArgument<int> Port { get; set; }
        public InArgument<bool> UseSSL { get; set; }
        protected override void StartLoop(NativeActivityContext context)
        {
            if (Body != null)
            {
                if (!breakRequested && !context.IsCancellationRequested)
                {
                    IncIndex(context);
                    context.ScheduleActivity(Body, OnBodyComplete);
                }
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            StartLoop(context);
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddChild(Body);
            Interfaces.Extensions.AddCacheArgument(metadata, "Hostname", Hostname);
            Interfaces.Extensions.AddCacheArgument(metadata, "TermType", TermType);
            Interfaces.Extensions.AddCacheArgument(metadata, "Port", Port);
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