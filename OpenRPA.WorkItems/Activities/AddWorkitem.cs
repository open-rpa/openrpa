using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRPA.Interfaces.Input;
using OpenRPA.Interfaces.entity;

namespace OpenRPA.WorkItems.Activities
{
    [System.ComponentModel.Designer(typeof(AddWorkitemDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.addworkitem.png")]
    [LocalizedToolboxTooltip("activity_addworkitem_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_addworkitem", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_addworkitem_helpurl", typeof(Resources.strings))]
    public class AddWorkitem : AsyncTaskCodeActivity
    {
        public AddWorkitem()
        {
            var builder = new System.Activities.Presentation.Metadata.AttributeTableBuilder();
            builder.AddCustomAttributes(typeof(AddWorkitem), "Payload", 
                new EditorAttribute(typeof(OpenRPA.Interfaces.Activities.ArgumentCollectionEditor), 
                typeof(System.Activities.Presentation.PropertyEditing.PropertyValueEditor)));
            System.Activities.Presentation.Metadata.MetadataStore.AddAttributeTable(builder.CreateTable());
            Payload = new Dictionary<string, InArgument>();
        }
        [RequiredArgument, LocalizedDisplayName("activity_addworkitem_wiqid", typeof(Resources.strings)), LocalizedDescription("activity_addworkitem_wiqid_help", typeof(Resources.strings)), OverloadGroup("By ID")]
        public InArgument<string> wiqid { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_addworkitem_wiq", typeof(Resources.strings)), LocalizedDescription("activity_addworkitem_wiq_help", typeof(Resources.strings)), OverloadGroup("By Name")]
        public InArgument<string> wiq { get; set; }
        [LocalizedDisplayName("activity_addworkitem_name", typeof(Resources.strings)), LocalizedDescription("activity_addworkitem_name_help", typeof(Resources.strings))]
        public InArgument<string> Name { get; set; }
        [LocalizedDisplayName("activity_addworkitem_files", typeof(Resources.strings)), LocalizedDescription("activity_addworkitem_files_help", typeof(Resources.strings))]
        public InArgument<string[]> Files { get; set; }
        [LocalizedDisplayName("activity_addworkitem_workitem", typeof(Resources.strings)), LocalizedDescription("activity_addworkitem_workitem_help", typeof(Resources.strings))]
        public OutArgument<IWorkitem> Workitem { get; set; }
        [LocalizedDisplayName("activity_addworkitem_payload", typeof(Resources.strings)), LocalizedDescription("activity_addworkitem_payload_help", typeof(Resources.strings)), Browsable(true)]
        public Dictionary<string, InArgument> Payload { get; set; }
        [LocalizedDisplayName("activity_addworkitem_priority", typeof(Resources.strings)), LocalizedDescription("activity_addworkitem_priority_help", typeof(Resources.strings))]
        public InArgument<int> Priority { get; set; }
        [LocalizedDisplayName("activity_addworkitem_nextrun", typeof(Resources.strings)), LocalizedDescription("activity_addworkitem_nextrun_help", typeof(Resources.strings))]
        public InArgument<DateTime?> NextRun { get; set; }
        [LocalizedDisplayName("activity_addworkitem_success_wiq", typeof(Resources.strings)), LocalizedDescription("activity_addworkitem_success_wiq_help", typeof(Resources.strings))]
        public InArgument<string> Success_wiq { get; set; }
        [LocalizedDisplayName("activity_addworkitem_failed_wiq", typeof(Resources.strings)), LocalizedDescription("activity_addworkitem_failed_wiq_help", typeof(Resources.strings))]
        public InArgument<string> Failed_wiq { get; set; }
        protected async override Task<object> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var files = Files.Get<string[]>(context);
            var t = new Workitem();
            t.wiqid = wiqid.Get<string>(context);
            t.wiq = wiq.Get<string>(context);
            t.name = Name.Get<string>(context);
            t.priority = Priority.Get<int>(context);
            t.nextrun = NextRun.Get<DateTime?>(context);
            t.success_wiq = Success_wiq.Get<string>(context);
            t.failed_wiq = Failed_wiq.Get<string>(context);
            if (t.payload == null) t.payload = new Dictionary<string, object>();
            foreach (var item in Payload)
            {
                t.payload.Add(item.Key, item.Value.Get(context));
            }
            Workitem result = null;
            await RobotInstance.instance.WaitForSignedIn(TimeSpan.FromSeconds(10));
            result = await global.webSocketClient.AddWorkitem<Workitem>(t, files);
            return result;
        }
        protected override void AfterExecute(AsyncCodeActivityContext context, object result)
        {
            Workitem.Set(context, result);
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