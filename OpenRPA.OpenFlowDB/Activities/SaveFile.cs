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

namespace OpenRPA.OpenFlowDB
{
    [System.ComponentModel.Designer(typeof(SaveFileDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.savefile.png")]
    [LocalizedToolboxTooltip("activity_savefile_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_savefile", typeof(Resources.strings))]
    public class SaveFile : AsyncTaskCodeActivity<string>
    {
        [RequiredArgument, LocalizedDisplayName("activity_savefile_filename", typeof(Resources.strings)), LocalizedDescription("activity_savefile_filename_help", typeof(Resources.strings))]
        public InArgument<string> Filename { get; set; }
        [LocalizedDisplayName("activity_savefile_path", typeof(Resources.strings)), LocalizedDescription("activity_savefile_path_help", typeof(Resources.strings))]
        public InArgument<string> Path { get; set; }
        //[LocalizedDisplayName("activity_savefile_result", typeof(Resources.strings)), LocalizedDescription("activity_savefile_result_help", typeof(Resources.strings))]
        //public new OutArgument<string> Result { get; set; }
        protected async override Task<string> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var filename = Filename.Get(context);
            filename = Environment.ExpandEnvironmentVariables(filename);
            var path = Path.Get(context);
            if (!System.IO.File.Exists(filename)) throw new System.IO.FileNotFoundException("File not found " + filename);

            string id = await global.webSocketClient.UploadFile(filename, path, null);
            return id;
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