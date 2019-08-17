using Accord.Video.FFMPEG;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.AviRecorder
{
    public class ScreenRecorder
    {
        private System.Drawing.Rectangle area;
        private int FramesPerSecond = 0;
        private System.Timers.Timer timer1;
        private Accord.Video.FFMPEG.VideoFileWriter vf;
        private DateTime? _firstFrameTime;
        public string filename { get; set; }

        public static ScreenRecorder Instance { get; private set; }
        public static ScreenRecorder Create(string filename, Accord.Video.FFMPEG.VideoCodec codec, int fps, System.Drawing.Rectangle area)
        {
            if (Instance != null) return Instance;
            Instance = new ScreenRecorder();
            Instance.area = area;
            Instance.vf = new Accord.Video.FFMPEG.VideoFileWriter();
            if (area == System.Drawing.Rectangle.Empty)
            {
                System.Windows.Media.Matrix toDevice;
                using (var source = new System.Windows.Interop.HwndSource(new System.Windows.Interop.HwndSourceParameters()))
                {
                    toDevice = source.CompositionTarget.TransformToDevice;
                }
                int screenWidth = (int)Math.Round(System.Windows.SystemParameters.PrimaryScreenWidth * toDevice.M11);
                int screenHeight = (int)Math.Round(System.Windows.SystemParameters.PrimaryScreenHeight * toDevice.M22);
                Instance.area = new System.Drawing.Rectangle(0, 0, screenWidth, screenHeight);
            }
            Instance.FramesPerSecond = fps;
            Instance.timer1 = new System.Timers.Timer();
            Instance.timer1.Interval = 20;
            Instance.timer1.Elapsed += Instance.Timer1_Elapsed;
            Instance.timer1.Interval = 1000 / Instance.FramesPerSecond;
            if (string.IsNullOrEmpty(filename))
            {
                var exePath = new Uri(System.Reflection.Assembly.GetEntryAssembly().Location).LocalPath;
                var outputFolder = System.IO.Path.GetDirectoryName(exePath);
                filename = System.IO.Path.Combine(outputFolder, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".avi");
            }
            Instance.filename = filename;
            Instance.vf.Open(filename, Instance.area.Width, Instance.area.Height, Instance.FramesPerSecond, codec, 1000000);
            Instance.timer1.Start();
            return Instance;
        }
        public void Stop()
        {
            if (vf == null) return;
            timer1.Stop();
            lock (vf)
            {
                vf.Close();
            }
            vf.Dispose();
        }
        private void Timer1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (vf == null) return;
            using (var bp = new System.Drawing.Bitmap(area.Width, area.Height))
            using (var gr = System.Drawing.Graphics.FromImage(bp))
            {
                gr.CopyFromScreen(area.X, area.Y, 0, 0, new System.Drawing.Size(area.Width, area.Height));
                try
                {
                    lock (vf)
                    {
                        if (_firstFrameTime != null)
                        {
                            vf.WriteVideoFrame(bp, DateTime.Now - _firstFrameTime.Value);
                        }
                        else
                        {
                            vf.WriteVideoFrame(bp);
                            _firstFrameTime = DateTime.Now;
                        }
                    }
                }
                catch (Exception)
                {
                    Stop();
                    throw;
                }
            }
        }
    }
}
