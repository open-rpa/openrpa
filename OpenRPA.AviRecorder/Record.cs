using NAudio.Wave;
using OpenRPA.Interfaces;
using SharpAvi;
using SharpAvi.Codecs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.AviRecorder
{
    public class Record
    {
        private string outputFolder;
        private FourCC encoder;
        private int encodingQuality;
        private int audioSourceIndex;
        private SupportedWaveFormat audioWaveFormat;
        private bool encodeAudio;
        private int audioQuality;

        private Record()
        {
            InitDefaultSettings();
        }
        private static Record _Instance = null;
        public static Record Instance
        {
            get
            {
                if (_Instance == null) _Instance = new Record();
                return _Instance;
            }
        }

        private void InitDefaultSettings()
        {
            var exePath = new Uri(System.Reflection.Assembly.GetEntryAssembly().Location).LocalPath;
            outputFolder = System.IO.Path.GetDirectoryName(exePath);

            encoder = KnownFourCCs.Codecs.MotionJpeg;
            encodingQuality = 70;

            audioSourceIndex = -1;
            audioWaveFormat = SupportedWaveFormat.WAVE_FORMAT_44M16;
            encodeAudio = true;
            audioQuality = (Mp3AudioEncoderLame.SupportedBitRates.Length + 1) / 2;
        }

        public bool IsRecording { get; set; }
        public string lastFileName { get; set; }
        private Recorder recorder;
        public void StartRecording(FourCC encoder, string outputFolder, int encodingQuality)
        {
            if (IsRecording)
                throw new InvalidOperationException("Already recording.");

            this.encoder = encoder;
            this.outputFolder = outputFolder;
            this.encodingQuality = encodingQuality;
            string extension = ".avi";
            if (encoder != KnownFourCCs.Codecs.Uncompressed && encoder != KnownFourCCs.Codecs.MotionJpeg) extension = ".mpeg";

            GenericTools.RunUI(() =>
            {
                lastFileName = System.IO.Path.Combine(outputFolder, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + extension);
                var bitRate = Mp3AudioEncoderLame.SupportedBitRates.OrderBy(br => br).ElementAt(audioQuality);
                recorder = new Recorder(lastFileName,
                    encoder, encodingQuality,
                    audioSourceIndex, audioWaveFormat, encodeAudio, bitRate);
                IsRecording = true;
            });
        }

        public void StopRecording()
        {
            if (!IsRecording)
                throw new InvalidOperationException("Not recording.");
            GenericTools.RunUI(() =>
            {
                try
                {
                    if (recorder != null)
                    {
                        recorder.Dispose();
                        recorder = null;
                    }
                }
                finally
                {
                    IsRecording = false;
                }
            });
        }

    }
}
