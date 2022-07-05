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
    [System.Drawing.ToolboxBitmap(typeof(Login), "Resources.toolbox.setproperty.png")]
    [LocalizedToolboxTooltip("activity_setproperty_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_setproperty", typeof(Resources.strings))]
    public class SetProperty : CodeActivity
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
        [RequiredArgument, LocalizedDisplayName("activity_setproperty_systemname", typeof(Resources.strings)), LocalizedDescription("activity_setproperty_systemname_help", typeof(Resources.strings))]
        public InArgument<string> SystemName { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_setproperty_path", typeof(Resources.strings)), LocalizedDescription("activity_setproperty_path_help", typeof(Resources.strings))]
        public InArgument<string> Path { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_setproperty_actionname", typeof(Resources.strings)), LocalizedDescription("activity_setproperty_actionname_help", typeof(Resources.strings))]
        public InArgument<string> ActionName { get; set; }
        [LocalizedDisplayName("activity_setproperty_parameters", typeof(Resources.strings)), LocalizedDescription("activity_setproperty_parameters_help", typeof(Resources.strings))]
        public InArgument<string> Parameters { get; set; }
        [Browsable(false)]
        public string Image { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            string systemname = SystemName.Get(context);
            string path = Path.Get(context);
            string actionname = ActionName.Get(context);
            var parameters = Parameters.Get(context);
            // object[] parameters = Parameters.Get(context);
            //var _params = new List<object>();
            //Dictionary<string, object> arguments = (from argument in Arguments
            //                                        where argument.Value.Direction != ArgumentDirection.Out
            //                                        select argument).ToDictionary((KeyValuePair<string, Argument> argument) => argument.Key, (KeyValuePair<string, Argument> argument) => argument.Value.Get(context));
            //foreach (var a in arguments)
            //{
            //    _params.Add(a.Value);
            //}
            // var data = new SAPInvokeMethod(systemname, path, actionname, _params.ToArray());
            object[] _parameters = null;
            if(!string.IsNullOrEmpty(parameters))
            {
                _parameters = JsonConvert.DeserializeObject<object[]>(parameters);
            }
            var data = new SAPInvokeMethod(systemname, path, actionname, _parameters);
            var message = new SAPEvent("setproperty");
            message.Set(data);
            var result = SAPhook.Instance.SendMessage(message, TimeSpan.FromSeconds(PluginConfig.bridge_timeout_seconds));
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