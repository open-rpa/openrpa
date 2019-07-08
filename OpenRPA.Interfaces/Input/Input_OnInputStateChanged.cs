// BSD 2-Clause License

// Copyright(c) 2017, Arvie Delgado
// All rights reserved.

// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:

// * Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.

// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.

// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED.IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using OpenRPA.Interfaces;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
//using System.Windows.Forms;

namespace OpenRPA.Input
{
    public sealed partial class InputDriver : IDisposable
    {
        #region Native

        private const Int32 HC_ACTION = 0;

        private const Int32 WH_KEYBOARD_LL = 13;

        private const Int32 WH_MOUSE_LL = 14;

        private delegate IntPtr LLProc(Int32 nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(Int32 idHook, LLProc lpfn, IntPtr hMod, UInt32 dwThreadId);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, Int32 nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern Boolean UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern Boolean GetPhysicalCursorPos(ref POINT pt);

        [DllImport("user32.dll")]
        private static extern Int16 GetKeyState(Int32 nVirtKey);

        private struct POINT
        {
            public int x;
            public int y;
        }

        private IntPtr keyboardHook;

        private LLProc keyboardProc;

        private IntPtr mouseHook;

        private LLProc mouseProc;

        private const Int32 VK_EXTENDED = 0x100;

        private const Int32 VK_LSHIFT = 0xA0;

        private const Int32 VK_RSHIFT = 0xA1;

        private const Int32 VK_LCONTROL = 0xA2;

        private const Int32 VK_RCONTROL = 0xA3;

        private const Int32 VK_LMENU = 0xA4;

        private const Int32 VK_RMENU = 0xA5;

        private const Int32 VK_LWIN = 0x5B;

        private const Int32 VK_RWIN = 0x5C;

        private const Int32 VK_PACKET = 0xE7;

        private const Int32 WM_KEYUP = 0x0101;

        private const Int32 WM_SYSKEYUP = 0x0105;

        private const Int32 WM_KEYDOWN = 0x0100;

        private const Int32 WM_SYSKEYDOWN = 0x0104;

        private const Int32 WM_LBUTTONUP = 0x0202;

        private const Int32 WM_LBUTTONDOWN = 0x0201;

        private const Int32 WM_MBUTTONUP = 0x0208;

        private const Int32 WM_MBUTTONDOWN = 0x0207;

        private const Int32 WM_MOUSEMOVE = 0x0200;

        private const Int32 WM_RBUTTONUP = 0x0205;

        private const Int32 WM_RBUTTONDOWN = 0x0204;

        private const Int32 LLKHF_EXTENDED = 0x01;

        private struct KBDLLHOOKSTRUCT
        {
#pragma warning disable 649
            public UInt32 vkCode;
            public UInt32 scanCode;
            public LLKHF flags;
            public UInt32 time;
            public UIntPtr dwExtraInfo;
        }

        [Flags]
        private enum LLKHF : UInt32
        {
            LLKHF_EXTENDED = 0x01,
            LLKHF_INJECTED = 0x10,
            LLKHF_ALTDOWN = 0x20,
            LLKHF_UP = 0x80
        }

        private static Int32 LOBYTE(Int32 x) => x & 0xFF;

        private static Int32 HIBYTE(Int32 x) => x >> 8;

        #endregion

        private static InputDriver _Instance = null;
        public static InputDriver Instance
        {
            get
            {
                if (_Instance == null) _Instance = new InputDriver() { CallNext = true, SkipEvent = false };
                return _Instance;
            }
        }
        public bool CallNext { get; set; }
        public bool SkipEvent { get; set; }
        private int currentprocessid = 0;
        private IntPtr LowLevelKeyboardProc(Int32 nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= HC_ACTION)
            {
                var e = new InputEventArgs();
                var pt = new POINT();
                GetPhysicalCursorPos(ref pt);
                e.X = pt.x;
                e.Y = pt.y;
                e.AltKey = HIBYTE(GetKeyState(VK_LMENU) | GetKeyState(VK_RMENU)) != 0;
                e.CtrlKey = HIBYTE(GetKeyState(VK_LCONTROL) | GetKeyState(VK_RCONTROL)) != 0;
                e.ShiftKey = HIBYTE(GetKeyState(VK_LSHIFT) | GetKeyState(VK_RSHIFT)) != 0;
                e.WinKey = HIBYTE(GetKeyState(VK_LWIN) | GetKeyState(VK_RWIN)) != 0;

                var k = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                e.Key = (KeyboardKey)k.vkCode; // virtual key
                e.KeyValue = unchecked((int)k.vkCode);
                if (VK_PACKET == (Int32)e.Key)
                {
                    e.Key = (KeyboardKey)(-k.scanCode); // unicode char
                    e.KeyValue = unchecked((int)-k.scanCode);
                }
                else if (k.flags.HasFlag(LLKHF.LLKHF_EXTENDED))
                {
                    e.Key += VK_EXTENDED; // extended virtual key
                    e.KeyValue += VK_EXTENDED;
                }
                
                switch ((Int32)wParam)
                {
                    case WM_KEYUP:
                    case WM_SYSKEYUP:
                        e.Type = InputEventType.KeyUp;
                        OnKeyUp(e);
                        //OnInput(e);
                        break;
                    case WM_KEYDOWN:
                    case WM_SYSKEYDOWN:
                        e.Type = InputEventType.KeyDown;
                        OnKeyDown(e);
                        //OnInput(e);
                        break;
                }
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private IntPtr LowLevelMouseProc(Int32 nCode, IntPtr wParam, IntPtr lParam)
        {
            if (SkipEvent)
            {
                if(CallNext) return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
                return (IntPtr)1;
            }
            if (currentprocessid == 0) currentprocessid = System.Diagnostics.Process.GetCurrentProcess().Id;
            if (nCode >= HC_ACTION)
            {
                var e = new InputEventArgs();
                var pt = new POINT();
                GetPhysicalCursorPos(ref pt);
                e.X = pt.x;
                e.Y = pt.y;
                e.AltKey = HIBYTE(GetKeyState(VK_LMENU) | GetKeyState(VK_RMENU)) != 0;
                e.CtrlKey = HIBYTE(GetKeyState(VK_LCONTROL) | GetKeyState(VK_RCONTROL)) != 0;
                e.ShiftKey = HIBYTE(GetKeyState(VK_LSHIFT) | GetKeyState(VK_RSHIFT)) != 0;
                e.WinKey = HIBYTE(GetKeyState(VK_LWIN) | GetKeyState(VK_RWIN)) != 0;

                switch ((Int32)wParam)
                {
                    case WM_LBUTTONUP:
                        e.Type = InputEventType.MouseUp;
                        e.Button = MouseButton.Left;
                        RaiseOnMouseUp(e);
                        //OnInput(e);
                        break;
                    case WM_RBUTTONUP:
                        e.Type = InputEventType.MouseUp;
                        e.Button = MouseButton.Right;
                        RaiseOnMouseUp(e);
                        //OnInput(e);
                        break;
                    case WM_MBUTTONUP:
                        e.Type = InputEventType.MouseUp;
                        e.Button = MouseButton.Middle;
                        RaiseOnMouseUp(e);
                        //OnInput(e);
                        break;
                    case WM_LBUTTONDOWN:
                        e.Type = InputEventType.MouseDown;
                        e.Button = MouseButton.Left;
                        RaiseOnMouseDown(e);
                        //OnInput(e);
                        break;
                    case WM_RBUTTONDOWN:
                        e.Type = InputEventType.MouseDown;
                        e.Button = MouseButton.Right;
                        RaiseOnMouseDown(e);
                        //OnInput(e);
                        break;
                    case WM_MBUTTONDOWN:
                        e.Type = InputEventType.MouseDown;
                        e.Button = MouseButton.Middle;
                        RaiseOnMouseDown(e);
                        //OnInput(e);
                        break;
                    case WM_MOUSEMOVE:
                        e.Type = InputEventType.MouseMove;
                        RaiseOnMouseMove(e);
                        //OnInput(e);
                        break;
                }
                if (CallNext || (Int32)wParam == WM_MOUSEMOVE) return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
                try
                {
                    if (e.Element != null && e.Element.ProcessId == currentprocessid) return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
                }
                Log.Debug("Skip CallNextHookEx");
                return (IntPtr)1;
            }
            else
            {
                return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            }
        }
        public UIElement Element = null;
        // private bool mouseDownWaiting = false;
        private void RaiseOnMouseMove(InputEventArgs e)
        {
            if (e.Button == MouseButton.None)
            {
                //try
                //{
                //    e.Element = AutomationHelper.GetFromPoint(e.X, e.Y);
                //}
                //catch (Exception ex)
                //{
                //    Log.Error(ex, "");
                //}
                //Element = e.Element;
            }
            OnMouseMove(e);
        }
        private void RaiseOnMouseDown(InputEventArgs e)
        {
            try
            {
                Element = AutomationHelper.GetFromPoint(e.X, e.Y);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
            }
            e.Element = Element;
            // if (e.Element != null && e.Element.ProcessId == currentprocessid) return;
            OnMouseDown(e);
        }
        private void RaiseOnMouseUp(InputEventArgs e)
        {
            try
            {
                if(Element == null)
                {
                    Element = AutomationHelper.GetFromPoint(e.X, e.Y);
                }
            }
            catch (Exception)
            {
            }

            try
            {
                e.Element = Element;
                if (e.Element != null)
                {
                    e.Element.Refresh();
                }
                // if (e.Element != null && e.Element.ProcessId == currentprocessid) return;
                OnMouseUp(e);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private InputDriver()
        {
            keyboardProc = LowLevelKeyboardProc;
            keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProc, IntPtr.Zero, 0);
            if (keyboardHook == IntPtr.Zero) throw new Win32Exception();
            mouseProc = LowLevelMouseProc;
            mouseHook = SetWindowsHookEx(WH_MOUSE_LL, mouseProc, IntPtr.Zero, 0);
            if (mouseHook == IntPtr.Zero) throw new Win32Exception();
        }
        public void Dispose()
        {
            UnhookWindowsHookEx(keyboardHook);
            UnhookWindowsHookEx(mouseHook);
        }
    }
}
