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
    [Designer(typeof(SetPropertyDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(Login), "Resources.toolbox.executetransaction.png")]
    [LocalizedToolboxTooltip("activity_executetransaction_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_executetransaction", typeof(Resources.strings))]
    public class ExecuteTransaction : CodeActivity
    {
        [RequiredArgument, LocalizedDisplayName("activity_executetransaction_systemname", typeof(Resources.strings)), LocalizedDescription("activity_executetransaction_systemname_help", typeof(Resources.strings))]
        public InArgument<string> SystemName { get; set; }
        [LocalizedDisplayName("activity_executetransaction_path", typeof(Resources.strings)), LocalizedDescription("activity_executetransaction_path_help", typeof(Resources.strings))]
        public InArgument<string> Path { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_executetransaction_transactioncode", typeof(Resources.strings)), LocalizedDescription("activity_executetransaction_transactioncode_help", typeof(Resources.strings))]
        public InArgument<string> TransactionCode { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            string systemname = SystemName.Get(context);
            string path = Path.Get(context);
            string transactionCode = TransactionCode.Get(context);
            object[] _parameters = new object[1];
            if (string.IsNullOrEmpty(path)) path = "/app/con[0]/ses[0]";

            _parameters[0] = transactionCode;

            SAPhook.Instance.InvokeMethod(systemname, path, "StartTransaction", _parameters, TimeSpan.FromSeconds(PluginConfig.bridge_timeout_seconds));
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