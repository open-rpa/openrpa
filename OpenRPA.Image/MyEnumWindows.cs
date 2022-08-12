using System;
using System.Collections.Generic;
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
            windows.Clear();
            EnumWindows(EnumWindowsCallback, includeChildren ? (IntPtr)1 : IntPtr.Zero);
            return windows;
        }
        public static List<IntPtr> GetWindows(bool includeChildren, int processId)
        {
            windows.Clear();
            EnumWindows(EnumWindowsCallback, includeChildren ? (IntPtr)1 : IntPtr.Zero);
            foreach (var h in windows.ToList())
            {
                uint _processId = 0;
                GetWindowThreadProcessId(h, out _processId);
                if (_processId != processId) windows.Remove(h);
            }
            return windows;
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
