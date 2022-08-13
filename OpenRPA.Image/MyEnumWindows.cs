using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Image
{
    public static class MyEnumWindows
    {
        private delegate bool EnumWindowsProc(IntPtr windowHandle, IntPtr lParam);

        [DllImport("user32")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndStart, EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        private static List<IntPtr> windows = new List<IntPtr>();
        [DllImport("user32.dll", SetLastError = true)]
        //public static extern int GetWindowThreadProcessId(HandleRef handle, out int processId);
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        public static List<IntPtr> GetWindows(bool includeChildren)
        {
            lock (windows)
            {
                windows.Clear();
                EnumWindows(EnumWindowsCallback, includeChildren ? (IntPtr)1 : IntPtr.Zero);
                return windows;
            }
        }
        public static IntPtr[] GetWindows(bool includeChildren, System.Diagnostics.Process p)
        {
            lock (windows)
            {
                windows.Clear();
                EnumWindows(EnumWindowsCallback, includeChildren ? (IntPtr)1 : IntPtr.Zero);
                foreach (var h in windows.ToList())
                {
                    uint _processId = 0;
                    GetWindowThreadProcessId(h, out _processId);
                    if (_processId != p.Id) windows.Remove(h);
                }
            }
            return windows.ToArray();
        }
        private static bool EnumWindowsCallback(IntPtr testWindowHandle, IntPtr includeChildren)
        {
            windows.Add(testWindowHandle);
            if (includeChildren.Equals(IntPtr.Zero) == false)
            {
                EnumChildWindows(testWindowHandle, EnumWindowsCallback, IntPtr.Zero);
            }
            return true;
        }
        //private static List<Rectangle> Rects = new List<Rectangle>();
        public static object reference = new object();
        public static Rectangle[] WindowRects(object sender, bool includeChildren, System.Diagnostics.Process p, Rectangle limit, int minWidth = 0, int minHeight = 0)
        {
            var Rects = new List<Rectangle>();
            // Rects.Clear();
            var allChildWindows = MyEnumWindows.GetWindows(true, p).ToList();
            foreach (var window in allChildWindows)
            {
                Interfaces.win32.WindowHandleInfo.RECT rct;
                if (!Interfaces.win32.WindowHandleInfo.GetWindowRect(new HandleRef(reference, window), out rct))
                {
                    continue;
                }
                var rect = new Rectangle(rct.Left, rct.Top, rct.Right - rct.Left + 1, rct.Bottom - rct.Top + 1);
                if (!limit.IsEmpty)
                {
                    rect = new Rectangle(rect.X + limit.X, rect.Y + limit.Y, limit.Width, limit.Height);
                }
                if (rect.Width < minWidth || rect.Height < minHeight)
                {
                    continue;
                }
                if ((rect.X > 0 || (rect.X + rect.Width) > 0) &&
                        (rect.Y > 0 || (rect.Y + rect.Height) > 0))
                {
                    Rects.Add(rect);
                }
            }
            foreach (var _rect in Rects.ToList())
            {
                foreach (var subrect in Rects.ToList())
                {
                    if (!_rect.Equals(subrect))
                    {
                        if (_rect.Contains(subrect) || subrect.Contains(_rect))
                        {
                            Rects.Remove(subrect);
                        }
                        else if (subrect.X == _rect.X && subrect.Y == _rect.Y && subrect.Height == _rect.Height && subrect.Width == _rect.Width)
                        {
                            Rects.Remove(subrect);
                        }
                        else if (subrect.IntersectsWith(_rect))
                        {
                            var X = subrect.X < _rect.X ? subrect.X : _rect.X;
                            var Y = subrect.Y < _rect.Y ? subrect.Y : _rect.Y;
                            var Width = subrect.Width > _rect.Width ? subrect.Width : _rect.Width;
                            var Height = subrect.Height > _rect.Height ? subrect.Height : _rect.Height;
                            Rects.Remove(subrect);
                            Rects.Add(new Rectangle(X, Y, Width, Height));
                        }
                    }
                }
            }
            return Rects.Distinct().ToArray();
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
}
