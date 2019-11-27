using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
        [Editor(typeof(SelectNewEmailOptionsEditor), typeof(ExtendedPropertyValueEditor))]
        [RequiredArgument]
        public InArgument<string> Codec { get; set; } = "MotionJpeg";
        public InArgument<string> Folder { get; set; }
        [RequiredArgument]
        public InArgument<int> Quality { get; set; } = 70;
        protected override void Execute(NativeActivityContext context)
        {
            var strcodec = Codec.Get(context);
            var folder = Folder.Get(context);
            var quality = Quality.Get(context);
            if (string.IsNullOrEmpty(folder))
            {
                folder = Interfaces.Extensions.MyVideos;
            }
            if (quality < 10) quality = 10;
            if (quality > 100) quality = 100;
            SharpAvi.FourCC codec;
            if (strcodec == null) strcodec = "motionjpeg";
            switch (strcodec.ToLower())
            {
                case "uncompressed": codec = SharpAvi.KnownFourCCs.Codecs.Uncompressed; break;
                case "motionjpeg": codec = SharpAvi.KnownFourCCs.Codecs.MotionJpeg; break;
                case "microsoftmpeg4v3": codec = SharpAvi.KnownFourCCs.Codecs.MicrosoftMpeg4V3; break;
                case "microsoftmpeg4v2": codec = SharpAvi.KnownFourCCs.Codecs.MicrosoftMpeg4V2; break;
                case "xvid": codec = SharpAvi.KnownFourCCs.Codecs.Xvid; break;
                case "divx": codec = SharpAvi.KnownFourCCs.Codecs.DivX; break;
                case "x264": codec = SharpAvi.KnownFourCCs.Codecs.X264; break;
                default: codec = SharpAvi.KnownFourCCs.Codecs.MotionJpeg; break;
            }
            if (!Record.Instance.IsRecording)
            {
                try
                {
                    Record.Instance.StartRecording(codec, folder, quality);
                }
                catch (Exception)
                {
                    try
                    {
                        System.IO.File.Delete(Record.Instance.lastFileName);
                    }
                    catch (Exception)
                    {
                    }
                    throw;
                }
            }
        }

    }

    class SelectNewEmailOptionsEditor : CustomSelectEditor
    {
        public override DataTable options
        {
            get
            {
                DataTable lst = new DataTable();
                lst.Columns.Add("ID", typeof(string));
                lst.Columns.Add("TEXT", typeof(string));
                lst.Rows.Add("Uncompressed", "Uncompressed");
                lst.Rows.Add("MotionJpeg", "MotionJpeg");
                //lst.Rows.Add("DivX", "DivX");
                //lst.Rows.Add("MicrosoftMpeg4V2", "MicrosoftMpeg4V2");
                //lst.Rows.Add("MicrosoftMpeg4V3", "MicrosoftMpeg4V3");
                lst.Rows.Add("X264", "X264");
                //lst.Rows.Add("Xvid", "Xvid");
                return lst;
            }
        }

    }
}