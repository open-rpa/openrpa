using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.MSSpeech
{
    [System.ComponentModel.Designer(typeof(SpeakDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(Speak), "Resources.toolbox.getimage.png")]
    [LocalizedToolboxTooltip("activity_speak_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_speak", typeof(Resources.strings))]
    public class Speak : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Text { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            using (var synthesizer = new System.Speech.Synthesis.SpeechSynthesizer())
            {
                var text = Text.Get(context);
                synthesizer.Speak(text);
            }
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