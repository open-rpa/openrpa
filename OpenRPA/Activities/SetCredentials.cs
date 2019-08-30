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
    [System.ComponentModel.Designer(typeof(SetCredentialsDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.entity.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public sealed class SetCredentials : AsyncTaskCodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Name { get; set; }
        [RequiredArgument]
        public InArgument<string> Username { get; set; }
        [RequiredArgument]
        public InArgument<string> Password { get; set; }
        protected async override Task<object> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var name = Name.Get(context);
            var username = Username.Get(context);
            var password = Password.Get(context);
            var obj = JObject.Parse("{name: \"" + name + "\", _type: \"credential\", _encrypt: [\"username\", \"password\"], username: \"" + username + "\", password: \"" + password + "\" }");
            await global.webSocketClient.InsertOrUpdateOne("openrpa", 1, true, "name", obj);
            return null;
        }
        protected override void AfterExecute(AsyncCodeActivityContext context, object result)
        {
        }
    }
}
