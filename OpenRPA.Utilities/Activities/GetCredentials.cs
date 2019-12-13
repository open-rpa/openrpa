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

namespace OpenRPA.Utilities
{
    [System.ComponentModel.Designer(typeof(GetCredentialsDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.entity.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public sealed class GetCredentials : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Name { get; set; }
        [RequiredArgument]
        public OutArgument<string> Username { get; set; }
        [RequiredArgument]
        [OverloadGroup("password")]
        public OutArgument<System.Security.SecureString> Password { get; set; }
        [RequiredArgument]
        [OverloadGroup("unsecurepassword")]
        public OutArgument<string> UnsecurePassword { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            string name = Name.Get(context);
            var cred = CredentialManager.ReadCredential(name);
            if(cred!=null)
            {
                var pass = new System.Net.NetworkCredential("", cred.Password).SecurePassword;
                Password.Set(context, pass);
                UnsecurePassword.Set(context, cred.Password);
                Username.Set(context, cred.UserName);
            }
        }
    }
}
