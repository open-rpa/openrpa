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
            var url = URL.Get(context);
            var filepath = LocalPath.Get(context);
            var overwrite = Overwrite.Get(context);
            filepath = Environment.ExpandEnvironmentVariables(filepath);
            // if(string.IsNullOrEmpty(filename)) filename = "temp."
            if(System.IO.File.Exists(filepath) && ! overwrite ) return 42;
            using (var client = new System.Net.WebClient())
            {
                await client.DownloadFileTaskAsync(new Uri(url), filepath);
            }
            return 42;
        }
    }
}