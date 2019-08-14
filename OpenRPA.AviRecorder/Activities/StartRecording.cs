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
    [System.ComponentModel.Designer(typeof(StartRecordingDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.gethtmlelement.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class StartRecording : NativeActivity
    {
        //[RequiredArgument]
        //public InArgument<string> XPath { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            if (!Record.Instance.IsRecording) Record.Instance.StartRecording();
        }

    }
}