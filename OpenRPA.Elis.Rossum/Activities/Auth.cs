using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Elis.Rossum
{
    [System.ComponentModel.Designer(typeof(AuthDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(Auth), "Resources.toolbox.auth.png")]
    [LocalizedToolboxTooltip("activity_auth_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_auth", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_auth_helpurl", typeof(Resources.strings))]
    public class Auth : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Username { get; set; }
        [RequiredArgument]
        public InArgument<string> Password { get; set; }
        [RequiredArgument]
        public OutArgument<string> Result { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var username = Username.Get(context);
            var password = Password.Get(context);
            var json = "{\"username\":\"" + username + "\", \"password\":\"" + password + "\"}";
            var url = "https://api.elis.rossum.ai/v1" + "/auth/login";
            var res = SimpleRequests.POST(url, json);
            var o = JObject.Parse(res);
            var key = o["key"].ToString();
            context.SetValue(Result, key);
        }
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