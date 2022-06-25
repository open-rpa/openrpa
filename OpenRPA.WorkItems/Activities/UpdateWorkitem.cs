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
using System.Data;

namespace OpenRPA.WorkItems.Activities
{
    [System.ComponentModel.Designer(typeof(UpdateWorkitemDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.updateworkitem.png")]
    [LocalizedToolboxTooltip("activity_updateworkitem_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_updateworkitem", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_updateworkitem_helpurl", typeof(Resources.strings))]
    public class UpdateWorkitem : AsyncTaskCodeActivity
    {
        [LocalizedDisplayName("activity_updateworkitem_workitem", typeof(Resources.strings)), LocalizedDescription("activity_updateworkitem_workitem_help", typeof(Resources.strings))]
        public InArgument<IWorkitem> Workitem { get; set; }
        [LocalizedDisplayName("activity_updateworkitem_state", typeof(Resources.strings)), LocalizedDescription("activity_updateworkitem_state_help", typeof(Resources.strings))]
        public InArgument<string> State { get; set; }
        [LocalizedDisplayName("activity_updateworkitem_exception", typeof(Resources.strings)), LocalizedDescription("activity_updateworkitem_exception_help", typeof(Resources.strings))]
        public InArgument<Exception> Exception { get; set; }
        [LocalizedDisplayName("activity_updateworkitem_files", typeof(Resources.strings)), LocalizedDescription("activity_updateworkitem_files_help", typeof(Resources.strings))]
        public InArgument<string[]> Files { get; set; }
        [LocalizedDisplayName("activity_updateworkitem_ignoremaxretries", typeof(Resources.strings)), LocalizedDescription("activity_updateworkitem_ignoremaxretries_help", typeof(Resources.strings))]
        public InArgument<bool> IgnoreMaxretries { get; set; }
        [LocalizedDisplayName("activity_addworkitem_success_wiq", typeof(Resources.strings)), LocalizedDescription("activity_addworkitem_success_wiq_help", typeof(Resources.strings))]
        public InArgument<string> Success_wiq { get; set; }
        [LocalizedDisplayName("activity_addworkitem_failed_wiq", typeof(Resources.strings)), LocalizedDescription("activity_addworkitem_failed_wiq_help", typeof(Resources.strings))]
        public InArgument<string> Failed_wiq { get; set; }
        protected async override Task<object> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var status = new string[] { "failed", "successful", "abandoned", "retry", "processing" };
            var files = Files.Get<string[]>(context);
            var t = Workitem.Get(context);
            var ex = Exception.Get(context);
            var ignoremaxretries = IgnoreMaxretries.Get(context);
            if (t == null) throw new Exception("Missing Workitem");
            t.success_wiq = Success_wiq.Get<string>(context);
            t.failed_wiq = Failed_wiq.Get<string>(context);
            if (State != null && State.Expression != null)
            {
                var state = State.Get(context);
                if (!string.IsNullOrEmpty(state)) t.state = state;
            }
            if(ex != null)
            {
                while (ex.InnerException != null) ex = ex.InnerException;
                t.errormessage = ex.Message; t.errortype = "application";
                t.errorsource = ex.Source;
                if (ex is BusinessRuleException)
                {
                    t.errortype = "business";
                }
            }
            t.state = t.state.ToLower();
            if (!status.Contains(t.state)) throw new Exception("Illegal state on Workitem, must be failed, successful, abandoned or retry");
            await RobotInstance.instance.WaitForSignedIn(TimeSpan.FromSeconds(10));
            var result = await global.webSocketClient.UpdateWorkitem<Workitem>(t, files, ignoremaxretries);
            return result;
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