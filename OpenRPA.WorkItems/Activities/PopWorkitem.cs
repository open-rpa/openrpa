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
    [System.ComponentModel.Designer(typeof(PopWorkitemDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.popworkitem.png")]
    [LocalizedToolboxTooltip("activity_popworkitem_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_popworkitem", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_popworkitem_helpurl", typeof(Resources.strings))]
    public class PopWorkitem : AsyncTaskCodeActivity
    {
        [RequiredArgument, LocalizedDisplayName("activity_popworkitem_wiqid", typeof(Resources.strings)), LocalizedDescription("activity_popworkitem_wiqid_help", typeof(Resources.strings)), OverloadGroup("By ID")]
        public InArgument<string> wiqid { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_popworkitem_wiq", typeof(Resources.strings)), LocalizedDescription("activity_popworkitem_wiq_help", typeof(Resources.strings)), OverloadGroup("By Name")]
        public InArgument<string> wiq { get; set; }
        [LocalizedDisplayName("activity_popworkitem_workitem", typeof(Resources.strings)), LocalizedDescription("activity_popworkitem_workitem_help", typeof(Resources.strings))]
        public OutArgument<IWorkitem> Workitem { get; set; }
        [LocalizedDisplayName("activity_popworkitem_folder", typeof(Resources.strings)), LocalizedDescription("activity_popworkitem_folder_help", typeof(Resources.strings))]
        public InArgument<string> Folder { get; set; }
        protected async override Task<object> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var folder = Folder.Get(context);
            if (!string.IsNullOrEmpty(folder)) folder = Environment.ExpandEnvironmentVariables(folder);
            if (string.IsNullOrEmpty(folder)) folder = Interfaces.Extensions.ProjectsDirectory;
            var _wiq = wiq.Get(context);
            var _wiqid = wiqid.Get(context);
            await RobotInstance.instance.WaitForSignedIn(TimeSpan.FromSeconds(10));
            var result = await global.webSocketClient.PopWorkitem<Workitem>(_wiq, _wiqid);
            if(result != null)
            {
                foreach (var file in result.files)
                {
                    await global.webSocketClient.DownloadFileAndSave(null, file._id, folder, false, false);
                }
            }
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