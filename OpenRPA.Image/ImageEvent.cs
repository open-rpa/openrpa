using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Image
{
    using OpenRPA.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.InteropServices;
    class ImageEvent
    {
        private ImageEvent() { }
        private Bitmap template;
        private System.Threading.AutoResetEvent waitHandle;
        private System.Timers.Timer timer;
        private Rectangle[] results = new Rectangle[] { };
        private string Processname = null;
        private bool CompareGray = false;
        private bool running = false;
        private Double threshold;
        private Rectangle limit;
        public static Rectangle[] waitFor(Bitmap Image, Double Threshold, String Processname, TimeSpan TimeOut, bool CompareGray, Rectangle Limit)
        {
            var me = new ImageEvent();
            try
            {
                me.limit = Limit;
                me.Processname = Processname;
                // rpaactivities.image.util.saveImage(Image, "waitFor-Image");
                me.template = Image;
                try
                {
                    var W = me.template.Width;
                    var H = me.template.Height;
                }
                catch (Exception)
                {
                    return me.results;
                }

                me.CompareGray = CompareGray;
                me.threshold = Threshold;
                me.running = true;
                if (me.findMatch()) return me.results;

                if (TimeOut.TotalMilliseconds < 100) return me.results;
                me.timer = new System.Timers.Timer();
                me.timer.Elapsed += me.onElapsed;
                me.timer.AutoReset = false;
                me.timer.Interval = 100;
                me.timer.Start();

                me.waitHandle = new System.Threading.AutoResetEvent(false);
                me.waitHandle.WaitOne(TimeOut);
                me.template = null;
                me.running = false;
                me.timer.Stop();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if(me.timer != null)
                {
                    me.timer.Elapsed -= me.onElapsed;
                    me.timer.Dispose();
                    me.timer = null;
                }
            }
            return me.results;
        }
        private void onElapsed(object sender, EventArgs e)
        {
            timer.Stop();
            if (!running) return;
            if (findMatch())
            {
                running = false;
                waitHandle.Set();
            }
            if (timer != null)
            {
                if (running) timer.Start();
            }
        }
        private bool findMatch()
        {
            try
            {
                if (!running) return false;
                if (this.template == null) return false;
                if (string.IsNullOrEmpty(Processname))
                {
                    var desktop = Interfaces.Image.Util.Screenshot();
                    GC.KeepAlive(template);
                    var results = Matches.FindMatches(desktop, template, threshold, 10, CompareGray);
                    if (results.Count() > 0)
                    {
                        this.results = results;
                        return true;
                    }
                }
                else
                {
                    var ps = System.Diagnostics.Process.GetProcessesByName(Processname);
                    foreach (var p in ps)
                    {
                        var rects2 = new List<Rectangle>();
                        if (!running) return false;
                        if (this.template == null) return false;
                        var rects = MyEnumWindows.WindowRects(this, true, p, limit, template.Width, template.Height);
                        // Log.Information("searcing " + rects.Length + " windows for image");
                        if (!running) return false;
                        if (this.template == null) return false;
                        foreach (var rect in rects)
                        {
                            var desktop = Interfaces.Image.Util.Screenshot(rect);
                            try
                            {
                                var results = Matches.FindMatches(desktop, template, threshold, 10, CompareGray);
                                if (results.Count() > 0)
                                {
                                    var finalresult = new List<Rectangle>();
                                    for (var i = 0; i < results.Length; i++)
                                    {
                                        results[i].X += rect.X;
                                        results[i].Y += rect.Y;
                                    }
                                    this.results = results;
                                    return true;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.WriteLine(ex.ToString());
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return false;
        }
    }
}
