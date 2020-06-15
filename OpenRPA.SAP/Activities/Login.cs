using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup.Localizer;
using Microsoft.VisualBasic;

namespace OpenRPA.SAP
{
    [Designer(typeof(LoginDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(Login), "Resources.toolbox.SAP_logo_small2.png")]
    [LocalizedToolboxTooltip("activity_login_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_login", typeof(Resources.strings))]
    public class Login : CodeActivity
    {
        [RequiredArgument, LocalizedDisplayName("activity_login_host", typeof(Resources.strings)), LocalizedDescription("activity_login_host_help", typeof(Resources.strings))]
        public InArgument<string> Host { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_login_username", typeof(Resources.strings)), LocalizedDescription("activity_login_username_help", typeof(Resources.strings))]
        public InArgument<string> Username { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_login_password", typeof(Resources.strings)), LocalizedDescription("activity_login_password_help", typeof(Resources.strings))]
        public InArgument<string> Password { get; set; }
        [LocalizedDisplayName("activity_login_client", typeof(Resources.strings)), LocalizedDescription("activity_login_client_help", typeof(Resources.strings))]
        public InArgument<string> Client { get; set; }
        [LocalizedDisplayName("activity_login_language", typeof(Resources.strings)), LocalizedDescription("activity_login_language_help", typeof(Resources.strings))]
        public InArgument<string> Language { get; set; }
        [LocalizedDisplayName("activity_login_systemname", typeof(Resources.strings)), LocalizedDescription("activity_login_systemname_help", typeof(Resources.strings))]
        public InArgument<string> SystemName { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            string host = Host.Get(context);
            string username = Username.Get(context);
            string password = Password.Get(context);
            string client = Client.Get(context);
            string language = Language.Get(context);
            string systemname = SystemName.Get(context);
            SAPhook.Instance.RefreshConnections();
            bool dologin = true;
            SAPSession _session = null;
            if (SAPhook.Instance.Sessions != null)
                foreach(var session in SAPhook.Instance.Sessions)
                {
                    if (session.Info.SystemName.ToLower() == systemname.ToLower()) { _session = session; dologin = false; break; }
                }
            if (dologin)
            {
                var data = new SAPLoginEvent(host, username, password, client, language, systemname);
                var message = new SAPEvent("login");
                message.Set(data);
                _ = SAPhook.Instance.SendMessage(message, TimeSpan.FromMinutes(10));
            }
            if(_session==null)
            {
                SAPhook.Instance.RefreshConnections();
                if (SAPhook.Instance.Sessions != null)
                    foreach (var session in SAPhook.Instance.Sessions)
                    {
                        if (session.Info.SystemName.ToLower() == systemname.ToLower()) { _session = session; break; }
                    }
            }
            // SAPhook.Instance.InvokeMethod(systemname, "wnd[0]", "resizeWorkingPane", new object[] { 300,200,false}, TimeSpan.FromSeconds(PluginConfig.bridge_timeout_seconds));
            Interaction.AppActivate(_session.ActiveWindow.Text);

            //SAPhook.Instance.InvokeMethod(systemname, "wnd[0]", "iconify", null, TimeSpan.FromSeconds(PluginConfig.bridge_timeout_seconds));
            //SAPhook.Instance.InvokeMethod(systemname, "wnd[0]", "iconify", null, TimeSpan.FromSeconds(PluginConfig.bridge_timeout_seconds));
            // SAPhook.Instance.InvokeMethod(systemname, "wnd[0]", "maximize", null, TimeSpan.FromSeconds(PluginConfig.bridge_timeout_seconds));
            //session.findById("wnd[0]").iconify
            //session.findById("wnd[0]").maximize
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