using System;
using System.Runtime.InteropServices;

namespace OpenRPA.Image.PrintCapture
{
    public sealed class Win32Funcs
    {
        [DllImport("User32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out Win32Types.Rect lpRect);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out Win32Types.Rect lpRect);

        [DllImport("user32.dll", EntryPoint = "GetWindowDC")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hDc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hDc);

        [DllImport("user32.dll")]
        public static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateDIBSection(IntPtr hdc, ref Win32Types.BitmapInfo bmi,
            uint usage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hDc, IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern bool BitBlt(
            IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hObjectSource, int nXSrc, int nYSrc, uint dwRop);

        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);
    }
}
