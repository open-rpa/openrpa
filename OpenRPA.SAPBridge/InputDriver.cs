using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.SAPBridge
{
    public delegate void InputEventHandler(InputEventArgs e);
    public struct POINT
    {
        public int x;
        public int y;
    }
    public class InputDriver
    {
        private InputDriver()
        {
            Initialize();
        }
        private static InputDriver _Instance = null;
        public static InputDriver Instance
        {
            get
            {
                if (_Instance == null) _Instance = new InputDriver();
                return _Instance;
            }
        }
        public event InputEventHandler OnMouseMove;
        public event InputEventHandler OnMouseDown;
        public event InputEventHandler OnMouseUp;
        public const Int32 HC_ACTION = 0;
        public const Int32 WH_KEYBOARD_LL = 13;
        public const Int32 WH_MOUSE_LL = 14;
        public const Int32 WM_MOUSEMOVE = 0x0200;
        public const Int32 WM_LBUTTONUP = 0x0202;
        public const Int32 WM_LBUTTONDOWN = 0x0201;
        public const Int32 WM_RBUTTONUP = 0x0205;
        public const Int32 WM_RBUTTONDOWN = 0x0204;
        public delegate IntPtr LLProc(Int32 nCode, IntPtr wParam, IntPtr lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern Boolean GetPhysicalCursorPos(ref POINT pt);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, Int32 nCode, IntPtr wParam, IntPtr lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(Int32 idHook, LLProc lpfn, IntPtr hMod, UInt32 dwThreadId);

        private IntPtr mouseHook;
        private LLProc mouseProc;

        private bool isInitialized = false;
        public void Initialize()
        {
            if (isInitialized) return;
            mouseProc = LowLevelMouseProc;
            mouseHook = SetWindowsHookEx(WH_MOUSE_LL, mouseProc, IntPtr.Zero, 0);
            if (mouseHook == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
            isInitialized = true;
        }
        private IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if((int)wParam== WM_MOUSEMOVE)
            {
                var e = new InputEventArgs();
                var pt = new POINT();
                GetPhysicalCursorPos(ref pt);
                e.X = pt.x;
                e.Y = pt.y;
                Task.Run(()=>OnMouseMove?.Invoke(e));
            }
            else if ((int)wParam == WM_LBUTTONDOWN || (int)wParam == WM_RBUTTONDOWN)
            {
                var e = new InputEventArgs();
                var pt = new POINT();
                GetPhysicalCursorPos(ref pt);
                e.X = pt.x;
                e.Y = pt.y;
                Task.Run(() => OnMouseDown?.Invoke(e));
            }
            else if ((int)wParam == WM_LBUTTONUP || (int)wParam == WM_RBUTTONUP)
            {
                var e = new InputEventArgs();
                var pt = new POINT();
                GetPhysicalCursorPos(ref pt);
                e.X = pt.x;
                e.Y = pt.y;
                Task.Run(() => OnMouseUp?.Invoke(e));
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }
    }
}
