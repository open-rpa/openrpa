using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.AviRecorder.Activities
{
    [System.ComponentModel.Designer(typeof(PlayRecordingDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.gethtmlelement.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class PlayRecording : NativeActivity
    {
        [RequiredArgument]
        public InArgument<string> Filename { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            var filename = Filename.Get(context);
            GenericTools.RunUI(() =>
            {
                var f = new Playback(filename);
                f.ShowDialog();
            });
        }

    }
}