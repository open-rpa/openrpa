using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
    [System.Drawing.ToolboxBitmap(typeof(Speak), "Resources.toolbox.speak.png")]
    [LocalizedToolboxTooltip("activity_speak_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_speak", typeof(Resources.strings))]
    public class Speak : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Text { get; set; }
        [System.ComponentModel.Category("Misc")]
        [Editor(typeof(VoiceTypeEditor), typeof(ExtendedPropertyValueEditor))]
        public InArgument<string> Voice { get; set; }
        public InArgument<int> Rate { get; set; }
        public InArgument<int> Volume { get; set; }
        public InArgument<bool> Async { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var voice = Voice.Get(context);
            var rate = Rate.Get(context);
            var volume = Volume.Get(context);
            var doasync = Async.Get(context);
            var text = Text.Get(context);
            var task = Task.Run(() =>
            {
                using (var synthesizer = new System.Speech.Synthesis.SpeechSynthesizer())
                {
                    if (!string.IsNullOrEmpty(voice)) synthesizer.SelectVoice(voice);
                    if (rate >= 1 && rate <= 10) synthesizer.Rate = rate;
                    if (volume >= 1 && volume <= 100) synthesizer.Volume = volume;
                    synthesizer.SetOutputToDefaultAudioDevice();
                    synthesizer.Speak(text);
                }
            });
            if (!doasync) task.Wait();
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
        class VoiceTypeEditor : CustomSelectEditor
        {
            public static DataTable dtoptions = null;
            public override DataTable options
            {
                get
                {
                    if (dtoptions == null)
                    {
                        try
                        {
                            using (var synthesizer = new System.Speech.Synthesis.SpeechSynthesizer())
                            {
                                var voices = synthesizer.GetInstalledVoices();
                                dtoptions = new DataTable();
                                dtoptions.Columns.Add("ID", typeof(string));
                                dtoptions.Columns.Add("TEXT", typeof(string));
                                foreach (var voice in voices)
                                {
                                    dtoptions.Rows.Add(voice.VoiceInfo.Name, voice.VoiceInfo.Description);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    }
                    return dtoptions;
                }
            }
        }

    }
}