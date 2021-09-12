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
    [System.Drawing.ToolboxBitmap(typeof(Login), "Resources.toolbox.SAP_logo_small2.png")]
    [LocalizedToolboxTooltip("activity_executetransaction_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_executetransaction", typeof(Resources.strings))]
    public class ExecuteTransaction : CodeActivity
    {
        public void loadImageAsync(string Id, string SystemName,string statusBarText)
        {
            Task.Run(() =>
            {
                StatusBarText = statusBarText;
                var msg = new SAPEvent("getitem");
                msg.Set(new SAPEventElement() { Id = Id, SystemName = SystemName, GetAllProperties = false });
                msg = SAPhook.Instance.SendMessage(msg, TimeSpan.FromSeconds(PluginConfig.bridge_timeout_seconds));
                if (msg != null)
                {
                    var ele = msg.Get<SAPEventElement>();
                    if (!string.IsNullOrEmpty(ele.Id))
                    {
                        var _element = new SAPElement(null, ele);
                        Image = _element.ImageString();
                        var message = _element.Properties.Where(x => x.Key == "Tooltip").FirstOrDefault();
                        if (message.Value != null && !string.IsNullOrEmpty(message.Value.Value)) this.message = message.Value.Value;
                        var tooltip = _element.Properties.Where(x => x.Key == "DefaultTooltip").FirstOrDefault();
                        if (tooltip.Value != null && !string.IsNullOrEmpty(tooltip.Value.Value)) this.tooltip = tooltip.Value.Value;
                        DisplayName = "Set " + Id.Substring(Id.LastIndexOf("/") + 1);
                    } else
                    {
                        Log.Debug("loadImageAsync: Fail locating " + Id + " returned null id");
                    }

                } else
                {
                    Log.Debug("loadImageAsync: Fail locating " + Id);
                }
            });
        }
        [Browsable(false)]
        public string message { get; set; } = "";
        [Browsable(false)]
        public string tooltip { get; set; } = "";
        [Browsable(false)]
        public string StatusBarText { get; set; } = "";
        [RequiredArgument, LocalizedDisplayName("activity_executetransaction_systemname", typeof(Resources.strings)), LocalizedDescription("activity_executetransaction_systemname_help", typeof(Resources.strings))]
        public InArgument<string> SystemName { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_executetransaction_path", typeof(Resources.strings)), LocalizedDescription("activity_executetransaction_path_help", typeof(Resources.strings))]
        public InArgument<string> Path { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_executetransaction_transactioncode", typeof(Resources.strings)), LocalizedDescription("activity_executetransaction_transactioncode_help", typeof(Resources.strings))]
        public InArgument<string> TransactionCode { get; set; }
        [Browsable(false)]
        public string Image { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            string systemname = SystemName.Get(context);
            string path = Path.Get(context);
            string transactionCode = TransactionCode.Get(context);
            object[] _parameters = new object[1];

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