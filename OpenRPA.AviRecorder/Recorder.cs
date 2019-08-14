using System;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
#if FX45
using System.Threading.Tasks;
#endif
using System.Windows;
using NAudio.Wave;
using SharpAvi.Codecs;
using SharpAvi.Output;
using System.Windows.Interop;
using System.Diagnostics;
using SharpAvi;

namespace OpenRPA.AviRecorder
{
    internal class Recorder : IDisposable
    {
        private readonly int screenWidth;
        private readonly int screenHeight;
        private readonly AviWriter writer;
        private readonly IAviVideoStream videoStream;
        private readonly IAviAudioStream audioStream;
        private readonly WaveInEvent audioSource;
        private readonly Thread screenThread;
        private readonly ManualResetEvent stopThread = new ManualResetEvent(false);
        private readonly AutoResetEvent videoFrameWritten = new AutoResetEvent(false);
        private readonly AutoResetEvent audioBlockWritten = new AutoResetEvent(false);

        public Recorder(string fileName,
            FourCC codec, int quality,
            int audioSourceIndex, SupportedWaveFormat audioWaveFormat, bool encodeAudio, int audioBitRate)
        {
            System.Windows.Media.Matrix toDevice;
            using (var source = new HwndSource(new HwndSourceParameters()))
            {
                toDevice = source.CompositionTarget.TransformToDevice;
            }

            screenWidth = (int)Math.Round(SystemParameters.PrimaryScreenWidth * toDevice.M11);
            screenHeight = (int)Math.Round(SystemParameters.PrimaryScreenHeight * toDevice.M22);

            // Create AVI writer and specify FPS
            writer = new AviWriter(fileName)
            {
                FramesPerSecond = 10,
                EmitIndex1 = true,
            };

            // Create video stream
            videoStream = CreateVideoStream(codec, quality);
            // Set only name. Other properties were when creating stream, 
            // either explicitly by arguments or implicitly by the encoder used
            videoStream.Name = "Screencast";

            if (audioSourceIndex >= 0)
            {
                var waveFormat = ToWaveFormat(audioWaveFormat);

                audioStream = CreateAudioStream(waveFormat, encodeAudio, audioBitRate);
                // Set only name. Other properties were when creating stream, 
                // either explicitly by arguments or implicitly by the encoder used
                audioStream.Name = "Voice";

                audioSource = new WaveInEvent
                {
                    DeviceNumber = audioSourceIndex,
                    WaveFormat = waveFormat,
                    // Buffer size to store duration of 1 frame
                    BufferMilliseconds = (int)Math.Ceiling(1000 / writer.FramesPerSecond),
                    NumberOfBuffers = 3,
                };
                audioSource.DataAvailable += audioSource_DataAvailable;
            }

            screenThread = new Thread(RecordScreen)
            {
                Name = typeof(Recorder).Name + ".RecordScreen",
                IsBackground = true
            };

            if (audioSource != null)
            {
                videoFrameWritten.Set();
                audioBlockWritten.Reset();
                audioSource.StartRecording();
            }
            screenThread.Start();
        }

        private IAviVideoStream CreateVideoStream(FourCC codec, int quality)
        {
            // Select encoder type based on FOURCC of codec
            if (codec == KnownFourCCs.Codecs.Uncompressed)
            {
                return writer.AddUncompressedVideoStream(screenWidth, screenHeight);
            }
            else if (codec == KnownFourCCs.Codecs.MotionJpeg)
            {
                return writer.AddMotionJpegVideoStream(screenWidth, screenHeight, quality
                    );
            }
            else
            {
                return writer.AddMpeg4VideoStream(screenWidth, screenHeight, (double)writer.FramesPerSecond,
                    // It seems that all tested MPEG-4 VfW codecs ignore the quality affecting parameters passed through VfW API
                    // They only respect the settings from their own configuration dialogs, and Mpeg4VideoEncoder currently has no support for this
                    quality: quality,
                    codec: codec,
                    // Most of VfW codecs expect single-threaded use, so we wrap this encoder to special wrapper
                    // Thus all calls to the encoder (including its instantiation) will be invoked on a single thread although encoding (and writing) is performed asynchronously
                    forceSingleThreadedAccess: true);
            }
        }

        private IAviAudioStream CreateAudioStream(WaveFormat waveFormat, bool encode, int bitRate)
        {
            // Create encoding or simple stream based on settings
            if (encode)
            {
                // LAME DLL path is set in App.OnStartup()
                return writer.AddMp3AudioStream(waveFormat.Channels, waveFormat.SampleRate, bitRate);
            }
            else
            {
                return writer.AddAudioStream(
                    channelCount: waveFormat.Channels,
                    samplesPerSecond: waveFormat.SampleRate,
                    bitsPerSample: waveFormat.BitsPerSample);
            }
        }

        private static WaveFormat ToWaveFormat(SupportedWaveFormat waveFormat)
        {
            switch (waveFormat)
            {
                case SupportedWaveFormat.WAVE_FORMAT_44M16:
                    return new WaveFormat(44100, 16, 1);
                case SupportedWaveFormat.WAVE_FORMAT_44S16:
                    return new WaveFormat(44100, 16, 2);
                default:
                    throw new NotSupportedException("Wave formats other than '16-bit 44.1kHz' are not currently supported.");
            }
        }

        public void Dispose()
        {
            stopThread.Set();
            screenThread.Join();
            if (audioSource != null)
            {
                audioSource.StopRecording();
                audioSource.DataAvailable -= audioSource_DataAvailable;
            }

            // Close writer: the remaining data is written to a file and file is closed
            writer.Close();

            stopThread.Close();
        }

        private void RecordScreen()
        {
            var stopwatch = new Stopwatch();
            var buffer = new byte[screenWidth * screenHeight * 4];
            System.Threading.Tasks.Task videoWriteTask = null;
            var isFirstFrame = true;
            var shotsTaken = 0;
            var timeTillNextFrame = TimeSpan.Zero;
            stopwatch.Start();

            while (!stopThread.WaitOne(timeTillNextFrame))
            {
                GetScreenshot(buffer);
                shotsTaken++;

                // Wait for the previous frame is written
                if (!isFirstFrame)
                {
                    videoWriteTask.Wait();
                    videoFrameWritten.Set();
                }

                if (audioStream != null)
                {
                    var signalled = WaitHandle.WaitAny(new WaitHandle[] { audioBlockWritten, stopThread });
                    if (signalled == 1)
                        break;
                }

                // Start asynchronous (encoding and) writing of the new frame
                videoWriteTask = videoStream.WriteFrameAsync(true, buffer, 0, buffer.Length);

                timeTillNextFrame = TimeSpan.FromSeconds(shotsTaken / (double)writer.FramesPerSecond - stopwatch.Elapsed.TotalSeconds);
                if (timeTillNextFrame < TimeSpan.Zero)
                    timeTillNextFrame = TimeSpan.Zero;

                isFirstFrame = false;
            }

            stopwatch.Stop();

            // Wait for the last frame is written
            if (!isFirstFrame)
            {
                videoWriteTask.Wait();
            }
        }

        private void GetScreenshot(byte[] buffer)
        {
            using (var bitmap = new Bitmap(screenWidth, screenHeight))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(screenWidth, screenHeight));
                var bits = bitmap.LockBits(new Rectangle(0, 0, screenWidth, screenHeight), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
                Marshal.Copy(bits.Scan0, buffer, 0, buffer.Length);
                bitmap.UnlockBits(bits);

                // Should also capture the mouse cursor here, but skipping for simplicity
                // For those who are interested, look at http://www.codeproject.com/Articles/12850/Capturing-the-Desktop-Screen-with-the-Mouse-Cursor
            }
        }

        private void audioSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            var signalled = WaitHandle.WaitAny(new WaitHandle[] { videoFrameWritten, stopThread });
            if (signalled == 0)
            {
                audioStream.WriteBlock(e.Buffer, 0, e.BytesRecorded);
                audioBlockWritten.Set();
            }
        }
    }
}
