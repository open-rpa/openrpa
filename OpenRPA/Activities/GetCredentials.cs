using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(GetCredentialsDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.entity.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public sealed class GetCredentials : AsyncTaskCodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Name { get; set; }
        [RequiredArgument]
        public OutArgument<string> Username { get; set; }
        [RequiredArgument]
        public OutArgument<System.Security.SecureString> Password { get; set; }
        protected override void AfterExecute(AsyncCodeActivityContext context, object result)
        {
            JObject res = result as JObject;
            Username.Set(context, res["username"].ToString());
            var pass = new System.Net.NetworkCredential("", res["password"].ToString()).SecurePassword;
            Password.Set(context, pass);
        }
        protected async override Task<object> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var name = Name.Get(context);
            var result = await global.webSocketClient.Query<JObject>("openrpa", "{name: \"" + name + "\", _type: \"credential\"}", top:2);
            if (result.Length != 1) throw new Exception("Failed locating credentials " + name);
            return result[0];
        }
    }
}
