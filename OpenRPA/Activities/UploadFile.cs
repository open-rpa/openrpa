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
    [System.ComponentModel.Designer(typeof(UploadFileDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.highlight.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class UploadFile : AsyncTaskCodeActivity<int>
    {
        [RequiredArgument]
        public InArgument<string> Filename { get; set; }
        public InArgument<string> Path { get; set; }
        protected async override Task<int> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var filename = Filename.Get(context);
            filename = Environment.ExpandEnvironmentVariables(filename);
            var path = Path.Get(context);
            if (!System.IO.File.Exists(filename)) throw new System.IO.FileNotFoundException("File not found " + filename);

            await global.webSocketClient.UploadFile(filename, path);

            return 13;
        }
    }
}