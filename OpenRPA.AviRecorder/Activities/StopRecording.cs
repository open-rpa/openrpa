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
    [System.ComponentModel.Designer(typeof(StopRecordingDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.gethtmlelement.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class StopRecording : NativeActivity
    {
        //[RequiredArgument]
        //public InArgument<string> XPath { get; set; }
        public OutArgument<string> Filename { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            if (Record.Instance.IsRecording) Record.Instance.StopRecording();
            Filename.Set(context, Record.Instance.lastFileName);
        }

    }
}