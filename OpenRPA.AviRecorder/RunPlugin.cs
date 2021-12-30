using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OpenRPA.AviRecorder
{
    class RunPlugin : ObservableObject, IRunPlugin
    {
        private Views.RunPluginView view;
        public UserControl editor
        {
            get
            {
                if (view == null)
                {
                    view = new Views.RunPluginView();
                    view.PropertyChanged += (s, e) =>
                    {
                        NotifyPropertyChanged("Entity");
                        NotifyPropertyChanged("Name");
                    };
                }
                return view;
            }
        }
        public static string PluginName => "AviRecorder";
        public string Name => PluginName;
        private Dictionary<string, Record> Records = new Dictionary<string, Record>();
        public IOpenRPAClient client;
        public void Initialize(IOpenRPAClient client)
        {
            this.client = client;
        }
        public void onWorkflowAborted(ref IWorkflowInstance e)
        {
            stopRecording(e);
        }
        public void onWorkflowCompleted(ref IWorkflowInstance e)
        {
            if (!PluginConfig.enabled) return;
            stopRecording(e);
            if (!PluginConfig.keepsuccessful)
            {
                if (!e.hasError && e.isCompleted)
                {
                    Record r;
                    if (!Records.ContainsKey(e._id)) { r = new Record(e._id); Records.Add(e._id, r); } else { r = Records[e._id]; }

                    try
                    {
                        if (r != null)
                        {
                            System.IO.File.Delete(r.lastFileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
            }
        }
        public void onWorkflowIdle(ref IWorkflowInstance e)
        {
            if (!PluginConfig.enabled) return;
            if (PluginConfig.stoponidle) stopRecording(e);
        }
        public bool onWorkflowResumeBookmark(ref IWorkflowInstance e, string bookmarkName, object value)
        {
            if (!PluginConfig.enabled) return true;
            startRecording(e, null);
            return true;
        }
        public bool onWorkflowStarting(ref IWorkflowInstance e, bool resumed)
        {
            if (!PluginConfig.enabled) return true;
            startRecording(e, null);
            return true;
        }
        public Record startRecording(IWorkflowInstance e, string folder)
        {
            var strcodec = PluginConfig.codec;
            if (string.IsNullOrEmpty(folder)) folder = PluginConfig.folder;
            if (string.IsNullOrEmpty(folder)) folder = Interfaces.Extensions.MyVideos;
            var quality = PluginConfig.quality;

            if (string.IsNullOrEmpty(folder))
            {
                folder = Interfaces.Extensions.MyVideos;
            }
            if (!string.IsNullOrEmpty(folder)) folder = Environment.ExpandEnvironmentVariables(folder);
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
            Record r;
            if (!Records.ContainsKey(e._id)) { r = new Record(e._id); Records.Add(e._id, r); } else { r = Records[e._id]; }
            if (!r.IsRecording) r.StartRecording(codec, folder, quality, e.Workflow.name + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
            return r;

        }
        public Record stopRecording(IWorkflowInstance e)
        {
            Record r = null;
            if (!Records.ContainsKey(e._id)) return r;
            r = Records[e._id];
            if (r.IsRecording) r.StopRecording();
            return r;
        }
    }
}
