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

using System;
using System.Runtime.InteropServices;

namespace OpenRPA.Input
{
    public sealed partial class InputDriver
    {
        #region Native

        [DllImport("user32.dll")]
        private static extern UInt32 SendInput(UInt32 numberOfInputs, INPUT[] inputs, Int32 sizeOfInputStructure);

        [DllImport("user32.dll")]
        private static extern UInt16 VkKeyScanEx(Char ch, IntPtr dwhkl);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(UInt32 idThread);

        [DllImport("user32.dll")]
        private static extern Boolean SetPhysicalCursorPos(Int32 X, Int32 Y);

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUT
        {
            public const UInt32 INPUT_MOUSE = 0;

            public const UInt32 INPUT_KEYBOARD = 1;

            [FieldOffset(0)]
            public UInt32 type;

            [FieldOffset(4)]
            public MOUSEINPUT mi;

            [FieldOffset(4)]
            public KEYBDINPUT ki;
        }

        private struct MOUSEINPUT
        {
            public Int32 dx;
            public Int32 dy;
            public UInt32 mouseData;
            public MOUSEEVENTF dwFlags;
            public UInt32 time;
            public UIntPtr dwExtraInfo;
        }

        private struct KEYBDINPUT
        {
            public UInt16 wVk;
            public UInt16 wScan;
            public KEYEVENTF dwFlags;
            public UInt32 time;
            public UIntPtr dwExtraInfo;
        }

        [Flags]
        private enum KEYEVENTF : UInt32
        {
            KEYEVENTF_EXTENDEDKEY = 0x0001,
            KEYEVENTF_KEYUP = 0x0002,
            KEYEVENTF_UNICODE = 0x0004,
            KEYEVENTF_SCANCODE = 0x0008,
        }

        [Flags]
        private enum MOUSEEVENTF : UInt32
        {
            MOUSEEVENTF_ABSOLUTE = 0x8000,
            MOUSEEVENTF_HWHEEL = 0x01000,
            MOUSEEVENTF_MOVE = 0x0001,
            MOUSEEVENTF_MOVE_NOCOALESCE = 0x2000,
            MOUSEEVENTF_LEFTDOWN = 0x0002,
            MOUSEEVENTF_LEFTUP = 0x0004,
            MOUSEEVENTF_RIGHTDOWN = 0x0008,
            MOUSEEVENTF_RIGHTUP = 0x0010,
            MOUSEEVENTF_MIDDLEDOWN = 0x0020,
            MOUSEEVENTF_MIDDLEUP = 0x0040,
            MOUSEEVENTF_VIRTUALDESK = 0x4000,
            MOUSEEVENTF_WHEEL = 0x0800,
            MOUSEEVENTF_XDOWN = 0x0080,
            MOUSEEVENTF_XUP = 0x0100
        }

        #endregion

        private void SetInputState(InputEventArgs e)
        {
            var input = new INPUT();
            switch (e.Type)
            {
                case InputEventType.KeyUp:
                case InputEventType.KeyDown:
                    input.type = INPUT.INPUT_KEYBOARD;
                    input.ki.time = default;
                    input.ki.dwExtraInfo = default;
                    if (e.Key < 0)
                    {
                        input.ki.wScan = (UInt16)(-(Int16)(e.Key));
                        input.ki.dwFlags = KEYEVENTF.KEYEVENTF_UNICODE;
                    //  input.ki.wVk = (UInt16)(VkKeyScanEx((Char)input.ki.wScan, GetKeyboardLayout(0)) & 0xFF);
                    }
                    else
                    {
                        input.ki.wVk = (UInt16)LOBYTE((Int16)e.Key);
                        if (HIBYTE((Int16)(e.Key)) > 0)
                        {
                            input.ki.dwFlags |= KEYEVENTF.KEYEVENTF_EXTENDEDKEY;
                        }
                    }
                    if (e.Type == InputEventType.KeyUp)
                    {
                        input.ki.dwFlags |= KEYEVENTF.KEYEVENTF_KEYUP;
                    }
                    SendInput(1, new[] { input }, Marshal.SizeOf(input));
                    break;

                case InputEventType.MouseUp:
                case InputEventType.MouseDown:
                    input.type = INPUT.INPUT_MOUSE;
                    input.mi.dx = default;
                    input.mi.dy = default;
                    input.mi.mouseData = default;
                    input.mi.time = default;
                    input.mi.dwExtraInfo = default;
                    if (e.Button.HasFlag(MouseButton.Left))
                    {
                        input.mi.dwFlags |= e.Type == InputEventType.MouseUp ? MOUSEEVENTF.MOUSEEVENTF_LEFTUP : MOUSEEVENTF.MOUSEEVENTF_LEFTDOWN;
                    }
                    if (e.Button.HasFlag(MouseButton.Right))
                    {
                        input.mi.dwFlags |= e.Type == InputEventType.MouseUp ? MOUSEEVENTF.MOUSEEVENTF_RIGHTUP : MOUSEEVENTF.MOUSEEVENTF_RIGHTDOWN;
                    }
                    if (e.Button.HasFlag(MouseButton.Middle))
                    {
                        input.mi.dwFlags |= e.Type == InputEventType.MouseUp ? MOUSEEVENTF.MOUSEEVENTF_MIDDLEUP : MOUSEEVENTF.MOUSEEVENTF_MIDDLEDOWN;
                    }
                    SendInput(1, new[] { input }, Marshal.SizeOf(input));
                    break;

                case InputEventType.MouseMove:
                    SetPhysicalCursorPos(e.X, e.Y);
                    break;
            }
        }
    }
}
