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

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(GetFileDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.downloadfile.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class GetFile : AsyncTaskCodeActivity<int>
    {
        [RequiredArgument,OverloadGroupAttribute("Filename")]
        public InArgument<string> Filename { get; set; }
        [RequiredArgument,OverloadGroupAttribute("Id")]
        public InArgument<string> _id { get; set; }
        [RequiredArgument]
        public InArgument<string> LocalPath { get; set; }
        [RequiredArgument]
        public InArgument<bool> IgnorePath { get; set; } = false;
        protected async override Task<int> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var filename = Filename.Get(context);
            var id = _id.Get(context);
            var filepath = LocalPath.Get(context);
            var ignorepath = IgnorePath.Get(context);
            await global.webSocketClient.DownloadFileAndSave(filename, id, filepath, ignorepath);
            return 13;
        }
    }
}