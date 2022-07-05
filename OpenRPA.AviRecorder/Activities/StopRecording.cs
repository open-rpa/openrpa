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
    [System.ComponentModel.Designer(typeof(StopRecordingDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.stoprecording.png")]
    [LocalizedToolboxTooltip("activity_stoprecording_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_stoprecording", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_stoprecording_helpurl", typeof(Resources.strings))]
    public class StopRecording : NativeActivity
    {
        //[RequiredArgument]
        //public InArgument<string> XPath { get; set; }
        public OutArgument<string> Filename { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            //if (Record.Instance.IsRecording) Record.Instance.StopRecording();
            //Filename.Set(context, Record.Instance.lastFileName);
            var p = Plugins.runPlugins.Where(x => x.Name == RunPlugin.PluginName).FirstOrDefault() as RunPlugin;
            if (p == null) return;
            var instance = p.client.GetWorkflowInstanceByInstanceId(context.WorkflowInstanceId.ToString());
            if (instance == null) return;
            var r = p.stopRecording(instance);
            Filename.Set(context, r.lastFileName);
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