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

namespace OpenRPA.WorkItems
{
    [System.ComponentModel.Designer(typeof(AddWorkitemDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(AddWorkitem), "Resources.toolbox.addworkitem.png")]
    [LocalizedToolboxTooltip("activity_addworkitem_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_addworkitem", typeof(Resources.strings))]
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
        [LocalizedDisplayName("activity_addworkitem_result", typeof(Resources.strings)), LocalizedDescription("activity_addworkitem_result_help", typeof(Resources.strings))]
        public OutArgument<IWorkitem> Result { get; set; }
        [LocalizedDisplayName("activity_addworkitem_payload", typeof(Resources.strings)), LocalizedDescription("activity_addworkitem_payload_help", typeof(Resources.strings)), Browsable(true)]
        public Dictionary<string, InArgument> Payload { get; set; }
        [LocalizedDisplayName("activity_addworkitem_priority", typeof(Resources.strings)), LocalizedDescription("activity_addworkitem_priority_help", typeof(Resources.strings))]
        public InArgument<int> Priority { get; set; }
        [LocalizedDisplayName("activity_addworkitem_nextrun", typeof(Resources.strings)), LocalizedDescription("activity_addworkitem_nextrun_help", typeof(Resources.strings))]
        public InArgument<DateTime> NextRun { get; set; }
        protected async override Task<object> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var files = Files.Get<string[]>(context);
            var t = new Workitem();
            t.wiqid = wiqid.Get<string>(context);
            t.wiq = wiq.Get<string>(context);
            t.name = Name.Get<string>(context);
            t.priority = Priority.Get<int>(context);
            t.nextrun = NextRun.Get<DateTime>(context);
            if (t.payload == null) t.payload = new Dictionary<string, object>();
            foreach (var item in Payload)
            {
                t.payload.Add(item.Key, item.Value.Get(context));
            }
            var result = await global.webSocketClient.AddWorkitem<Workitem>(t, files);
            return result;        
        }
        protected override void AfterExecute(AsyncCodeActivityContext context, object result)
        {
            Result.Set(context, result);
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
    public class Workitem : IWorkitem
    {
        public Workitem()
        {
            _type = "workitem";
            payload = new Dictionary<string, object>();
        }
        public string wiqid { get; set; }
        public string wiq { get; set; }
        public string state { get; set; }
        public Dictionary<string, object> payload { get; set; }
        public int retries { get; set; }
        public int priority { get; set; }
        public DateTime? lastrun { get; set; }
        public DateTime? nextrun { get; set; }
        public WorkitemFile[] files { get; set; }
        public string username { get; set; }
        public string userid { get; set; }
        public string _id { get; set; }
        public string _type { get; set; }
        public string name { get; set; }
        public DateTime _modified { get; set; }
        public string _modifiedby { get; set; }
        public string _modifiedbyid { get; set; }
        public DateTime _created { get; set; }
        public string _createdby { get; set; }
        public string _createdbyid { get; set; }
        public ace[] _acl { get; set; }
        public string[] _encrypt { get; set; }
        public long _version { get; set; }
        public void AddRight(TokenUser user, ace_right[] rights)
        {
            throw new NotImplementedException();
        }
        public void AddRight(string _id, string name, ace_right[] rights)
        {
            throw new NotImplementedException();
        }
        public bool hasRight(apiuser user, ace_right bit)
        {
            throw new NotImplementedException();
        }
        public bool hasRight(TokenUser user, ace_right bit)
        {
            throw new NotImplementedException();
        }
    }
}