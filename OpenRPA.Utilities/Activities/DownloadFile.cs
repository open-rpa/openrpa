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
using System.Runtime.InteropServices;

namespace OpenRPA.Utilities
{
    [System.ComponentModel.Designer(typeof(DownloadFileDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.downloadfile.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class DownloadFile : AsyncTaskCodeActivity
    {
        [RequiredArgument]
        public InArgument<string> URL { get; set; }
        [RequiredArgument]
        public InArgument<string> LocalPath { get; set; }
        public InArgument<bool> Overwrite { get; set; } = false;
        public InArgument<bool> IgnoreSecurity { get; set; } = false;
        public InArgument<string> Username { get; set; }
        public InArgument<string> Password { get; set; }
        protected override void AfterExecute(AsyncCodeActivityContext context, object result)
        {
        }
        //static string GetFileNameFromUrl(string url)
        //{
        //    Uri uri;
        //    if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
        //        uri = new Uri(url);
        //    return System.IO.Path.GetFileName(uri.LocalPath);
        //}
        protected async override Task<object> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var username = Username.Get(context);
            var password = Password.Get(context);
            var url = URL.Get(context);
            var filepath = LocalPath.Get(context);
            var overwrite = Overwrite.Get(context);
            var ignoresecurity = IgnoreSecurity.Get(context);
            filepath = Environment.ExpandEnvironmentVariables(filepath);
            // if(string.IsNullOrEmpty(filename)) filename = "temp."
            if(System.IO.File.Exists(filepath) && ! overwrite ) return 42;
            using (var client = new System.Net.WebClient())
            {
                var Expect100Continue = System.Net.ServicePointManager.Expect100Continue;
                var SecurityProtocol = System.Net.ServicePointManager.SecurityProtocol;
                if (ignoresecurity)
                {
                    System.Net.ServicePointManager.Expect100Continue = true;
                    System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Ssl3;
                }
                if(!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
                {
                    client.Credentials = new System.Net.NetworkCredential(username, password);
                }
                    var dir = System.IO.Path.GetDirectoryName(filepath);
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                await client.DownloadFileTaskAsync(new Uri(url), filepath);
                if (ignoresecurity)
                {
                    System.Net.ServicePointManager.Expect100Continue = Expect100Continue;
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocol;

                }
            }
            return 42;
        }
    }
}