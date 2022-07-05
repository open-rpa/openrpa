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
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.playrecording.png")]
    [LocalizedToolboxTooltip("activity_playrecording_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_playrecording", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_playrecording_helpurl", typeof(Resources.strings))]
    public class PlayRecording : NativeActivity
    {
        [RequiredArgument]
        public InArgument<string> Filename { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            var filename = Filename.Get(context);
            GenericTools.RunUI(() =>
            {
                try
                {
                    var f = new Playback(filename);
                    f.ShowDialog();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });
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