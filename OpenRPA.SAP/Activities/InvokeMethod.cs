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
    [Designer(typeof(InvokeMethodDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(Login), "Resources.toolbox.SAP_logo_small2.png")]
    [LocalizedToolboxTooltip("activity_invokemethod_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_invokemethod", typeof(Resources.strings))]
    public class InvokeMethod : CodeActivity
    {        
        [RequiredArgument, LocalizedDisplayName("activity_invokemethod_systemname", typeof(Resources.strings)), LocalizedDescription("activity_invokemethod_systemname_help", typeof(Resources.strings))]
        public InArgument<string> SystemName { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_invokemethod_path", typeof(Resources.strings)), LocalizedDescription("activity_invokemethod_path_help", typeof(Resources.strings))]
        public InArgument<string> Path { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_invokemethod_actionname", typeof(Resources.strings)), LocalizedDescription("activity_invokemethod_actionname_help", typeof(Resources.strings))]
        public InArgument<string> ActionName { get; set; }
        [LocalizedDisplayName("activity_invokemethod_parameters", typeof(Resources.strings)), LocalizedDescription("activity_invokemethod_parameters_help", typeof(Resources.strings))]
        public InArgument<string> Parameters { get; set; }
        [LocalizedDisplayName("activity_invokemethod_result", typeof(Resources.strings)), LocalizedDescription("activity_invokemethod_result_help", typeof(Resources.strings))]
        public OutArgument<string> Result { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            string systemname = SystemName.Get(context);
            string path = Path.Get(context);
            string actionname = ActionName.Get(context);
            var parameters = Parameters.Get(context);
            object[] _parameters = null;
            if(!string.IsNullOrEmpty(parameters))
            {
                _parameters = JsonConvert.DeserializeObject<object[]>(parameters);
            }
            var result = SAPhook.Instance.InvokeMethod(systemname, path, actionname, _parameters, TimeSpan.FromSeconds(PluginConfig.bridge_timeout_seconds));
            try
            {
                if(result == null)
                {
                    Result.Set(context, null);
                } 
                else
                {
                    var res = result as string;
                    if(res != null)
                    {
                        Result.Set(context, result);
                    } 
                    else
                    {
                        Result.Set(context, result.ToString());
                    }
                }
            }
            catch (Exception)
            {
                Result.Set(context, result.ToString());
            }
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