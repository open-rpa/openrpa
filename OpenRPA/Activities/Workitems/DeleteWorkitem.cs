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
    [System.ComponentModel.Designer(typeof(DeleteWorkitemDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.deleteworkitem.png")]
    [LocalizedToolboxTooltip("activity_deleteworkitem_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_deleteworkitem", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_deleteworkitem_helpurl", typeof(Resources.strings))]
    public class DeleteWorkitem : AsyncTaskCodeActivity
    {
        [LocalizedDisplayName("activity_deleteworkitem_workitem", typeof(Resources.strings)), LocalizedDescription("activity_deleteworkitem_workitem_help", typeof(Resources.strings))]
        public InArgument<IWorkitem> Workitem { get; set; }
        protected async override Task<object> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var status = new string[] { "failed", "successful", "abandoned", "retry", "processing" };
            var t = Workitem.Get(context);
            if (t == null) throw new Exception("Missing Workitem");
            t.state = t.state.ToLower();
            if (!status.Contains(t.state)) throw new Exception("Illegal state on Workitem, must be failed, successful, abandoned or retry");
            await global.webSocketClient.DeleteWorkitem(t._id);
            return null;
        }
        protected override void AfterExecute(AsyncCodeActivityContext context, object result)
        {
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