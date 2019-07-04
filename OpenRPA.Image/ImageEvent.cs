using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Image
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.InteropServices;
    class ImageEvent
    {
        private ImageEvent() { }
        //private Image<Bgr, Byte> template;
        private Bitmap template;
        private System.Threading.AutoResetEvent waitHandle;
        private System.Timers.Timer timer;
        private Rectangle result = Rectangle.Empty;
        private string Processname = null;
        private bool CompareGray = false;
        private bool running = false;
        private Double threshold;
        private Rectangle limit;
        public static Rectangle waitFor(Bitmap Image, Double Threshold, String Processname, TimeSpan TimeOut, bool CompareGray, Rectangle Limit)
        {

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var me = new ImageEvent();
            me.limit = Limit;
            me.Processname = Processname;
            //util.RemoveNoise(ref Image);
            //rpaactivities.image.util.showImage(Image);
            //me.template = new Image<Bgr, Byte>(Image);
            // rpaactivities.image.util.saveImage(Image, "waitFor-Image");

            me.template = Image;

            me.CompareGray = CompareGray;
            me.threshold = Threshold;
            me.running = true;
            if (me.findMatch()) return me.result;

            if (TimeOut.TotalMilliseconds < 100) return me.result;
            me.timer = new System.Timers.Timer();
            me.timer.Elapsed += new System.Timers.ElapsedEventHandler(me.onElapsed);
            me.timer.AutoReset = true;
            me.timer.Interval = 100;
            me.timer.Start();

            me.waitHandle = new System.Threading.AutoResetEvent(false);
            me.waitHandle.WaitOne(TimeOut);
            me.running = false;
            me.timer.Stop();
            return me.result;
        }

        //public static List<image.Highlighter> HighlightMatches(Bitmap Image, Double threshold, bool CompareGray, String Processname, Rectangle limit)
        //{
        //    var himatches = new List<image.Highlighter>();
        //    var template = Image;
        //    var _lock = new object();
        //    var ps = System.Diagnostics.Process.GetProcessesByName(Processname);
        //    foreach (var p in ps)
        //    {
        //        var rects = new List<Rectangle>();
        //        var allChildWindows = new WindowHandleInfo(p.MainWindowHandle).GetAllChildHandles();
        //        allChildWindows.Add(p.MainWindowHandle);
        //        foreach (var window in allChildWindows)
        //        {
        //            WindowHandleInfo.RECT rct;
        //            if (!WindowHandleInfo.GetWindowRect(new HandleRef(_lock, window), out rct))
        //            {
        //                continue;
        //            }
        //            var rect = new Rectangle(rct.Left, rct.Top, rct.Right - rct.Left + 1, rct.Bottom - rct.Top + 1);
        //            if (!limit.IsEmpty)
        //            {
        //                rect = new Rectangle(rect.X + limit.X, rect.Y + limit.Y, limit.Width, limit.Height);
        //                himatches.Add(new image.Highlighter(rect, System.Drawing.Color.Blue));
        //            }
        //            if (rect.Width < template.Width && rect.Height < template.Height)
        //            {
        //                continue;
        //            }
        //            rects.Add(rect);
        //        }
        //        foreach (var rect in rects.ToList())
        //        {
        //            foreach (var subrect in rects.ToList())
        //            {
        //                if (rect.Contains(subrect) && !rect.Equals(subrect))
        //                {
        //                    rects.Remove(subrect);
        //                }
        //            }

        //        }
        //        foreach (var rect in rects)
        //        {
        //            var desktop = image.util.screenshot(rect);
        //            try
        //            {
        //                var results = Matches.FindMatches(desktop, template, threshold, 10, CompareGray);
        //                if (results.Count() > 0)
        //                {
        //                    himatches.Add(new image.Highlighter(new Rectangle(rect.X + results[0].X, rect.Y + results[0].Y, results[0].Width, results[0].Height), System.Drawing.Color.Red));
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                System.Diagnostics.Trace.WriteLine(ex.ToString());
        //            }
        //        }
        //    }
        //    return himatches;
        //}

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
                        this.result = results[0];
                        return true;
                    }
                }
                else
                {
                    var ps = System.Diagnostics.Process.GetProcessesByName(Processname);
                    foreach (var p in ps)
                    {
                        var rects = new List<Rectangle>();
                        var allChildWindows = new WindowHandleInfo(p.MainWindowHandle).GetAllChildHandles();
                        allChildWindows.Add(p.MainWindowHandle);
                        foreach (var window in allChildWindows)
                        {
                            WindowHandleInfo.RECT rct;
                            if (!WindowHandleInfo.GetWindowRect(new HandleRef(this, window), out rct))
                            {
                                continue;
                            }
                            var rect = new Rectangle(rct.Left, rct.Top, rct.Right - rct.Left + 1, rct.Bottom - rct.Top + 1);
                            if (!limit.IsEmpty)
                            {
                                rect = new Rectangle(rect.X + limit.X, rect.Y + limit.Y, limit.Width, limit.Height);
                            }
                            if (rect.Width < template.Width && rect.Height < template.Height)
                            {
                                continue;
                            }
                            rects.Add(rect);
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
                            //System.Diagnostics.Trace.WriteLine("**** Match within window at " + rect.ToString());
                            var desktop = Interfaces.Image.Util.Screenshot(rect);
                            try
                            {
                                var results = Matches.FindMatches(desktop, template, threshold, 10, CompareGray);
                                if (results.Count() > 0)
                                {
                                    //this.result = results[0];
                                    this.result = new Rectangle(rect.X + results[0].X, rect.Y + results[0].Y, results[0].Width, results[0].Height);
                                    //if (!limit.IsEmpty)
                                    //{
                                    //    this.result = new Rectangle((int)w.BoundingRectangle.X + limit.X + results[0].X, (int)w.BoundingRectangle.Y + limit.Y + results[0].Y, results[0].Width, results[0].Height);
                                    //}
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
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
            return false;
        }



    }



    public static class MyEnumWindows
    {
        private delegate bool EnumWindowsProc(IntPtr windowHandle, IntPtr lParam);

        [DllImport("user32")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndStart, EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        private static List<string> windowTitles = new List<string>();
        private static List<IntPtr> windows = new List<IntPtr>();

        public static List<string> GetWindowTitles(bool includeChildren)
        {
            windowTitles.Clear();
            EnumWindows(EnumWindowsCallback, includeChildren ? (IntPtr)1 : IntPtr.Zero);
            return windowTitles;
        }
        public static List<IntPtr> GetWindows(bool includeChildren)
        {
            windows.Clear();
            EnumWindows(EnumWindowsCallback, includeChildren ? (IntPtr)1 : IntPtr.Zero);
            return windows;
        }

        private static bool EnumWindowsCallback(IntPtr testWindowHandle, IntPtr includeChildren)
        {
            windows.Add(testWindowHandle);
            string title = GetWindowTitle(testWindowHandle);
            if (TitleMatches(title))
            {
                windowTitles.Add(title);
            }
            if (includeChildren.Equals(IntPtr.Zero) == false)
            {
                EnumChildWindows(testWindowHandle, EnumWindowsCallback, IntPtr.Zero);
            }
            return true;
        }

        private static string GetWindowTitle(IntPtr windowHandle)
        {
            uint SMTO_ABORTIFHUNG = 0x0002;
            uint WM_GETTEXT = 0xD;
            int MAX_STRING_SIZE = 32768;
            IntPtr result;
            string title = string.Empty;
            IntPtr memoryHandle = Marshal.AllocCoTaskMem(MAX_STRING_SIZE);
            Marshal.Copy(title.ToCharArray(), 0, memoryHandle, title.Length);
            SendMessageTimeout(windowHandle, WM_GETTEXT, (IntPtr)MAX_STRING_SIZE, memoryHandle, SMTO_ABORTIFHUNG, (uint)1000, out result);
            title = Marshal.PtrToStringAuto(memoryHandle);
            Marshal.FreeCoTaskMem(memoryHandle);
            return title;
        }

        private static bool TitleMatches(string title)
        {
            bool match = title.Contains("e");
            return match;
        }

    }







    public class WindowHandleInfo
    {
        private delegate bool EnumWindowProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lParam);

        private IntPtr _MainHandle;

        public WindowHandleInfo(IntPtr handle)
        {
            this._MainHandle = handle;
        }

        public List<IntPtr> GetAllChildHandles()
        {
            List<IntPtr> childHandles = new List<IntPtr>();

            GCHandle gcChildhandlesList = GCHandle.Alloc(childHandles);
            IntPtr pointerChildHandlesList = GCHandle.ToIntPtr(gcChildhandlesList);

            try
            {
                EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                EnumChildWindows(this._MainHandle, childProc, pointerChildHandlesList);
            }
            finally
            {
                gcChildhandlesList.Free();
            }

            return childHandles;
        }

        private bool EnumWindow(IntPtr hWnd, IntPtr lParam)
        {
            GCHandle gcChildhandlesList = GCHandle.FromIntPtr(lParam);

            if (gcChildhandlesList == null || gcChildhandlesList.Target == null)
            {
                return false;
            }

            List<IntPtr> childHandles = gcChildhandlesList.Target as List<IntPtr>;
            childHandles.Add(hWnd);

            return true;
        }


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }
    }

}
