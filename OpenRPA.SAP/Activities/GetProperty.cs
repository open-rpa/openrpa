using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OpenRPA.SAP
{
    [Designer(typeof(GetPropertyDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(Login), "Resources.toolbox.getproperty.png")]
    [LocalizedToolboxTooltip("activity_getproperty_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_getproperty", typeof(Resources.strings))]
    public class GetProperty : CodeActivity
    {
        [RequiredArgument, LocalizedDisplayName("activity_getproperty_systemname", typeof(Resources.strings)), LocalizedDescription("activity_getproperty_systemname_help", typeof(Resources.strings))]
        public InArgument<string> SystemName { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_getproperty_path", typeof(Resources.strings)), LocalizedDescription("activity_getproperty_path_help", typeof(Resources.strings))]
        public InArgument<string> Path { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_getproperty_actionname", typeof(Resources.strings)), LocalizedDescription("activity_getproperty_actionname_help", typeof(Resources.strings))]
        public InArgument<string> ActionName { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_getproperty_result", typeof(Resources.strings)), LocalizedDescription("activity_getproperty_result_help", typeof(Resources.strings))]
        public OutArgument<string> Result { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            string systemname = SystemName.Get(context);
            string path = Path.Get(context);
            string actionname = ActionName.Get(context);
            object[] _parameters = null;
            var data = new SAPInvokeMethod(systemname, path, actionname, _parameters);
            var message = new SAPEvent("getproperty");
            message.Set(data);
            var reply = SAPhook.Instance.SendMessage(message, TimeSpan.FromSeconds(PluginConfig.bridge_timeout_seconds));
            data = reply.Get<SAPInvokeMethod>();
            Result.Set(context, data.Result);            
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