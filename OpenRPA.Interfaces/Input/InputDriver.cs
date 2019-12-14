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
using System.Collections.Generic;
using System.ComponentModel;

namespace OpenRPA.Input
{
    public sealed partial class InputDriver : IDisposable
    {
        private IntPtr keyboardHook;

        private NativeMethods.LLProc keyboardProc;

        private IntPtr mouseHook;

        private NativeMethods.LLProc mouseProc;
        public event InputEventHandler OnKeyUp = delegate { };

        public event InputEventHandler OnKeyDown = delegate { };

        public event InputEventHandler OnMouseUp = delegate { };

        public event InputEventHandler OnMouseDown = delegate { };

        public event InputEventHandler OnMouseMove = delegate { };

        public event CancelEventHandler onCancel = delegate { };

        public delegate void CancelEventHandler();

        public void KeyUp(KeyboardKey key) => SetInputState(new InputEventArgs() { Type = InputEventType.KeyUp, Key = key });

        public void KeyDown(KeyboardKey key) => SetInputState(new InputEventArgs() { Type = InputEventType.KeyDown, Key = key });

        public void MouseUp(MouseButton button) => SetInputState(new InputEventArgs() { Type = InputEventType.MouseUp, Button = button });

        public void MouseDown(MouseButton button) => SetInputState(new InputEventArgs() { Type = InputEventType.MouseDown, Button = button });

        public void MouseMove(int x, int y) => SetInputState(new InputEventArgs() { Type = InputEventType.MouseMove, X = x, Y = y });


        public static void DoMouseClick()
        {
            InputDriver.Instance.Element = null;
            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTDOWN | NativeMethods.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }
        public static void Click(MouseButton button)
        {
            InputDriver.Instance.AllowOneClick = true;
            InputDriver.Instance.Element = null;
            if (button == MouseButton.Left)
            {
                NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTDOWN | NativeMethods.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            }
            else if (button == MouseButton.Right)
            {
                NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_RIGHTDOWN | NativeMethods.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
            }
        }
        public void Press(params KeyboardKey[] keys)
        {
            var modKeys = new Dictionary<KeyboardKey, bool>
            {
                { KeyboardKey.LeftAlt, false },
                { KeyboardKey.LeftCtrl, false },
                { KeyboardKey.LeftShift, false },
                { KeyboardKey.LeftWin, false },
                { KeyboardKey.RightAlt, false },
                { KeyboardKey.RightCtrl, false },
                { KeyboardKey.RightShift, false },
                { KeyboardKey.RightWin, false }
            };
            foreach (KeyboardKey key in keys)
            {
                KeyDown(key);
                if (modKeys.ContainsKey(key))
                {
                    modKeys[key] = true;
                }
                else
                {
                    KeyUp(key);
                }
            }
            foreach (KeyboardKey key in modKeys.Keys)
            {
                if (modKeys[key] == true)
                {
                    KeyUp(key);
                }
            }
        }
        public void Write(string text)
        {
            foreach (char c in text ?? string.Empty)
            {
                KeyDown((KeyboardKey)(-c));
                KeyUp((KeyboardKey)(-c));
            }
        }

        public List<Interfaces.Input.vKey> cancelKeys = new List<Interfaces.Input.vKey>();
        private static InputDriver _Instance = null;
        private KeyboardDetectorPlugin cancelDetector;
        public void initCancelKey(string keys)
        {
            if (_Instance.cancelDetector == null)
            {
                _Instance.cancelDetector = new KeyboardDetectorPlugin();
                _Instance.cancelDetector.Entity = new Interfaces.entity.Detector();
                _Instance.cancelDetector.OnDetector += (IDetectorPlugin plugin, IDetectorEvent detector, EventArgs e) =>
                {
                    try
                    {
                        onCancel?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                };
            }
            _Instance.cancelDetector.Stop();
            _Instance.cancelDetector.Keys = keys;
            cancelKeys = Interfaces.Input.vKey.parseText(keys);
            _Instance.cancelDetector.Start();

        }
        public static InputDriver Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new InputDriver() { CallNext = true, AllowOneClick = false };
                }
                return _Instance;
            }
        }
        public bool CallNext { get; set; }
        public bool AllowOneClick { get; set; }
        // public bool SkipEvent { get; set; }
        private int currentprocessid = 0;
        // public var test = Activities.TypeText.parseText(cancelkey.Text);

        private IntPtr LowLevelKeyboardProc(Int32 nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= NativeMethods.HC_ACTION)
            {
                var e = new InputEventArgs();
                var pt = new NativeMethods.POINT();
                NativeMethods.GetPhysicalCursorPos(ref pt);
                e.X = pt.x;
                e.Y = pt.y;
                e.AltKey = NativeMethods.HIBYTE(NativeMethods.GetKeyState(NativeMethods.VK_LMENU) | NativeMethods.GetKeyState(NativeMethods.VK_RMENU)) != 0;
                e.CtrlKey = NativeMethods.HIBYTE(NativeMethods.GetKeyState(NativeMethods.VK_LCONTROL) | NativeMethods.GetKeyState(NativeMethods.VK_RCONTROL)) != 0;
                e.ShiftKey = NativeMethods.HIBYTE(NativeMethods.GetKeyState(NativeMethods.VK_LSHIFT) | NativeMethods.GetKeyState(NativeMethods.VK_RSHIFT)) != 0;
                e.WinKey = NativeMethods.HIBYTE(NativeMethods.GetKeyState(NativeMethods.VK_LWIN) | NativeMethods.GetKeyState(NativeMethods.VK_RWIN)) != 0;

                var k = (NativeMethods.KBDLLHOOKSTRUCT)System.Runtime.InteropServices.Marshal.PtrToStructure(lParam, typeof(NativeMethods.KBDLLHOOKSTRUCT));
                e.Key = (KeyboardKey)k.vkCode; // virtual key
                e.KeyValue = unchecked((int)k.vkCode);
                if (NativeMethods.VK_PACKET == (Int32)e.Key)
                {
                    e.Key = (KeyboardKey)(-k.scanCode); // unicode char
                    e.KeyValue = unchecked((int)-k.scanCode);
                }
                else if (k.flags.HasFlag(NativeMethods.LLKHF.LLKHF_EXTENDED))
                {
                    e.Key += NativeMethods.VK_EXTENDED; // extended virtual key
                    e.KeyValue += NativeMethods.VK_EXTENDED;
                }

                switch ((Int32)wParam)
                {
                    case NativeMethods.WM_KEYUP:
                    case NativeMethods.WM_SYSKEYUP:
                        e.Type = InputEventType.KeyUp;
                        OnKeyUp(e);
                        //OnInput(e);
                        break;
                    case NativeMethods.WM_KEYDOWN:
                    case NativeMethods.WM_SYSKEYDOWN:
                        e.Type = InputEventType.KeyDown;
                        OnKeyDown(e);
                        //OnInput(e);
                        break;
                }
            }
            return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }
        private string tostring(int wParam)
        {
            switch (wParam)
            {
                case NativeMethods.WM_LBUTTONUP:
                    return "MouseUp Left";
                case NativeMethods.WM_RBUTTONUP:
                    return "MouseUp Right";
                case NativeMethods.WM_MBUTTONUP:
                    return "MouseUp Middle";
                case NativeMethods.WM_LBUTTONDOWN:
                    return "MouseDown Left";
                case NativeMethods.WM_RBUTTONDOWN:
                    return "MouseDown Right";
                case NativeMethods.WM_MBUTTONDOWN:
                    return "MouseDown Middle";
                case NativeMethods.WM_MOUSEMOVE:
                    return "MouseMove";
                case NativeMethods.WM_MouseWheel:
                    return "WM_MouseWheel";
            }
            return "Unknown (" + wParam + ")";
        }
        private IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if ((int)wParam != NativeMethods.WM_MOUSEMOVE || AllowOneClick)
            {
                // Console.WriteLine(tostring((int)wParam) + $" CallNext: {CallNext} AllowOneClick: {AllowOneClick}");
            }
            if (AllowOneClick)
            {
                if ((int)wParam == NativeMethods.WM_LBUTTONDOWN || (int)wParam == NativeMethods.WM_RBUTTONDOWN || (int)wParam == NativeMethods.WM_MBUTTONDOWN)
                {
                    return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
                }
                if ((int)wParam == NativeMethods.WM_LBUTTONUP || (int)wParam == NativeMethods.WM_RBUTTONUP || (int)wParam == NativeMethods.WM_MBUTTONUP)
                {
                    AllowOneClick = false;
                    return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
                }
            }
            if (currentprocessid == 0) currentprocessid = System.Diagnostics.Process.GetCurrentProcess().Id;
            if (nCode >= NativeMethods.HC_ACTION)
            {
                var e = new InputEventArgs();
                var pt = new NativeMethods.POINT();
                NativeMethods.GetPhysicalCursorPos(ref pt);
                e.X = pt.x;
                e.Y = pt.y;
                e.AltKey = NativeMethods.HIBYTE(NativeMethods.GetKeyState(NativeMethods.VK_LMENU) | NativeMethods.GetKeyState(NativeMethods.VK_RMENU)) != 0;
                e.CtrlKey = NativeMethods.HIBYTE(NativeMethods.GetKeyState(NativeMethods.VK_LCONTROL) | NativeMethods.GetKeyState(NativeMethods.VK_RCONTROL)) != 0;
                e.ShiftKey = NativeMethods.HIBYTE(NativeMethods.GetKeyState(NativeMethods.VK_LSHIFT) | NativeMethods.GetKeyState(NativeMethods.VK_RSHIFT)) != 0;
                e.WinKey = NativeMethods.HIBYTE(NativeMethods.GetKeyState(NativeMethods.VK_LWIN) | NativeMethods.GetKeyState(NativeMethods.VK_RWIN)) != 0;

                switch ((int)wParam)
                {
                    case NativeMethods.WM_LBUTTONUP:
                        e.Type = InputEventType.MouseUp;
                        e.Button = MouseButton.Left;
                        if (!AllowOneClick) RaiseOnMouseUp(e);
                        //OnInput(e);
                        break;
                    case NativeMethods.WM_RBUTTONUP:
                        e.Type = InputEventType.MouseUp;
                        e.Button = MouseButton.Right;
                        if (!AllowOneClick) RaiseOnMouseUp(e);
                        //OnInput(e);
                        break;
                    case NativeMethods.WM_MBUTTONUP:
                        e.Type = InputEventType.MouseUp;
                        e.Button = MouseButton.Middle;
                        if (!AllowOneClick) RaiseOnMouseUp(e);
                        //OnInput(e);
                        break;
                    case NativeMethods.WM_LBUTTONDOWN:
                        e.Type = InputEventType.MouseDown;
                        e.Button = MouseButton.Left;
                        if (!AllowOneClick) RaiseOnMouseDown(e);
                        //OnInput(e);
                        break;
                    case NativeMethods.WM_RBUTTONDOWN:
                        e.Type = InputEventType.MouseDown;
                        e.Button = MouseButton.Right;
                        if (!AllowOneClick) RaiseOnMouseDown(e);
                        //OnInput(e);
                        break;
                    case NativeMethods.WM_MBUTTONDOWN:
                        e.Type = InputEventType.MouseDown;
                        e.Button = MouseButton.Middle;
                        if (!AllowOneClick) RaiseOnMouseDown(e);
                        //OnInput(e);
                        break;
                    case NativeMethods.WM_MOUSEMOVE:
                        e.Type = InputEventType.MouseMove;
                        if (!AllowOneClick) RaiseOnMouseMove(e);
                        //OnInput(e);
                        break;
                }
                if (CallNext || (int)wParam == NativeMethods.WM_MOUSEMOVE || (int)wParam == NativeMethods.WM_MouseWheel)
                {
                    // if((int)wParam != WM_MOUSEMOVE) Log.Debug("CallNextHookEx: " + CallNext);
                    return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
                }
                try
                {
                    if (e.Element != null && e.Element.ProcessId == currentprocessid)
                    {
                        // if ((int)wParam != WM_MOUSEMOVE) Log.Debug("CallNextHookEx: " + CallNext);
                        return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
                }
                // if ((int)wParam != WM_MOUSEMOVE) Log.Debug("Skip CallNextHookEx: " + CallNext);
                return (IntPtr)1;
            }
            else
            {
                return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
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
                if (Element == null)
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
        }
        private bool isInitialized = false;
        public void Initialize()
        {
            if (isInitialized) return;
            keyboardProc = LowLevelKeyboardProc;
            keyboardHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, keyboardProc, IntPtr.Zero, 0);
            if (keyboardHook == IntPtr.Zero) throw new Win32Exception();
            mouseProc = LowLevelMouseProc;
            mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, mouseProc, IntPtr.Zero, 0);
            if (mouseHook == IntPtr.Zero) throw new Win32Exception();
            isInitialized = true;
        }
        public void Dispose()
        {
            NativeMethods.UnhookWindowsHookEx(keyboardHook);
            NativeMethods.UnhookWindowsHookEx(mouseHook);
        }

        private void SetInputState(InputEventArgs e)
        {
            var input = new NativeMethods.INPUT();
            switch (e.Type)
            {
                case InputEventType.KeyUp:
                case InputEventType.KeyDown:
                    input.type = NativeMethods.INPUT.INPUT_KEYBOARD;
                    input.ki.time = default;
                    input.ki.dwExtraInfo = default;
                    if (e.Key < 0)
                    {
                        input.ki.wScan = (UInt16)(-(Int16)(e.Key));
                        input.ki.dwFlags = NativeMethods.KEYEVENTF.KEYEVENTF_UNICODE;
                        //  input.ki.wVk = (UInt16)(VkKeyScanEx((Char)input.ki.wScan, GetKeyboardLayout(0)) & 0xFF);
                    }
                    else
                    {
                        input.ki.wVk = (UInt16)NativeMethods.LOBYTE((Int16)e.Key);
                        if (NativeMethods.HIBYTE((Int16)(e.Key)) > 0)
                        {
                            input.ki.dwFlags |= NativeMethods.KEYEVENTF.KEYEVENTF_EXTENDEDKEY;
                        }
                    }
                    if (e.Type == InputEventType.KeyUp)
                    {
                        input.ki.dwFlags |= NativeMethods.KEYEVENTF.KEYEVENTF_KEYUP;
                    }
                    NativeMethods.SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf(input));
                    break;

                case InputEventType.MouseUp:
                case InputEventType.MouseDown:
                    input.type = NativeMethods.INPUT.INPUT_MOUSE;
                    input.mi.dx = default;
                    input.mi.dy = default;
                    input.mi.mouseData = default;
                    input.mi.time = default;
                    input.mi.dwExtraInfo = default;
                    if (e.Button.HasFlag(MouseButton.Left))
                    {
                        input.mi.dwFlags |= e.Type == InputEventType.MouseUp ? NativeMethods.MOUSEEVENTF.MOUSEEVENTF_LEFTUP : NativeMethods.MOUSEEVENTF.MOUSEEVENTF_LEFTDOWN;
                    }
                    if (e.Button.HasFlag(MouseButton.Right))
                    {
                        input.mi.dwFlags |= e.Type == InputEventType.MouseUp ? NativeMethods.MOUSEEVENTF.MOUSEEVENTF_RIGHTUP : NativeMethods.MOUSEEVENTF.MOUSEEVENTF_RIGHTDOWN;
                    }
                    if (e.Button.HasFlag(MouseButton.Middle))
                    {
                        input.mi.dwFlags |= e.Type == InputEventType.MouseUp ? NativeMethods.MOUSEEVENTF.MOUSEEVENTF_MIDDLEUP : NativeMethods.MOUSEEVENTF.MOUSEEVENTF_MIDDLEDOWN;
                    }
                    NativeMethods.SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf(input));
                    break;

                case InputEventType.MouseMove:
                    NativeMethods.SetPhysicalCursorPos(e.X, e.Y);
                    break;
            }
        }


    }

}
