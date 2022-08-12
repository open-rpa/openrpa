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

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var me = new ImageEvent();
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
            me.timer.Elapsed += new System.Timers.ElapsedEventHandler(me.onElapsed);
            me.timer.AutoReset = true;
            me.timer.Interval = 100;
            me.timer.Start();

            me.waitHandle = new System.Threading.AutoResetEvent(false);
            me.waitHandle.WaitOne(TimeOut);
            me.running = false;
            me.timer.Stop();
            return me.results;
        }
        private void onElapsed(object sender, EventArgs e)
        {
            timer.Stop();
            if (!running) return;
            try
            {
                var with = template.Width;
            }
            catch (Exception)
            {
                running = false;
                waitHandle.Set();
            }
            if (findMatch())
            {
                running = false;
                waitHandle.Set();
            }
            timer.Interval = 100;
            if (running) timer.Start();
        }
        private bool findMatch()
        {
            try
            {
                //var results = new List<Rectangle>();
                if (!running) return false;
                try
                {
                    var with = template.Width;
                }
                catch (Exception)
                {
                    running = false;
                    //waitHandle.Set();
                }
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
                        var rects = new List<Rectangle>();
                        var allChildWindows = MyEnumWindows.GetWindows(true, p.Id);
                        // var allChildWindows = new WindowHandleInfo(p.MainWindowHandle).GetAllChildHandles();
                        allChildWindows.Add(p.MainWindowHandle);
                        var temparr = allChildWindows.ToArray();
                        foreach (var window in temparr)
                        {
                            Interfaces.win32.WindowHandleInfo.RECT rct;
                            if (!Interfaces.win32.WindowHandleInfo.GetWindowRect(new HandleRef(this, window), out rct))
                            {
                                continue;
                            }
                            var rect = new Rectangle(rct.Left, rct.Top, rct.Right - rct.Left + 1, rct.Bottom - rct.Top + 1);
                            if (!limit.IsEmpty)
                            {
                                rect = new Rectangle(rect.X + limit.X, rect.Y + limit.Y, limit.Width, limit.Height);
                            }
                            if (rect.Width < template.Width || rect.Height < template.Height)
                            {
                                continue;
                            }
                            if((rect.X > 0 || (rect.X + rect.Width) > 0) &&
                                    (rect.Y > 0 || (rect.Y + rect.Height) > 0)) {
                                rects.Add(rect);
                            }
                        }
                        foreach (var rect in rects.ToList())
                        {
                            foreach (var subrect in rects.ToList())
                            {
                                if (rect.Contains(subrect) && !rect.Equals(subrect))
                                {
                                    rects.Remove(subrect);
                                }
                            }
                        }
                        foreach (var rect in rects)
                        {
                            var desktop = Interfaces.Image.Util.Screenshot(rect);
                            try
                            {
                                var results = Matches.FindMatches(desktop, template, threshold, 10, CompareGray);
                                if (results.Count() > 0)
                                {
                                    var finalresult = new List<Rectangle>();
                                    for(var i = 0; i < results.Length; i ++)
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
                Interfaces.Log.Error(ex.ToString());
            }
            return false;
        }
    }
}
