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
    [System.ComponentModel.Designer(typeof(SetAutoLoginDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.setautologin.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public sealed class SetAutoLogin : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Username { get; set; }
        [RequiredArgument]
        [OverloadGroup("password")]
        public InArgument<System.Security.SecureString> Password { get; set; }
        [RequiredArgument]
        [OverloadGroup("unsecurepassword")]
        public InArgument<string> UnsecurePassword { get; set; }
        public InArgument<int> AutoLogonCount { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            string username = Username.Get(context);
            System.Security.SecureString password = Password.Get(context);
            string unsecurepassword = UnsecurePassword.Get(context);
            int autologoncount = AutoLogonCount.Get(context);
            if (string.IsNullOrEmpty(unsecurepassword))
            {
                unsecurepassword = new System.Net.NetworkCredential(string.Empty, password).Password;
            }
            
            if(!string.IsNullOrEmpty(username))
            {
                NativeMethods.LsaPrivateData.SetAutologin(username, unsecurepassword, autologoncount);
            }
            else
            {
                NativeMethods.LsaPrivateData.RemoveAutologin();
            }

        }
    }
}
