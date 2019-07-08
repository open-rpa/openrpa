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

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenRPA.Input
{
    public sealed partial class InputDriver
    {
        public event InputEventHandler OnKeyUp = delegate { };

        public event InputEventHandler OnKeyDown = delegate { };

        public event InputEventHandler OnMouseUp = delegate { };

        public event InputEventHandler OnMouseDown = delegate { };

        public event InputEventHandler OnMouseMove = delegate { };

        //public event InputEventHandler OnInput = delegate { };

        public void KeyUp(KeyboardKey key) => SetInputState(new InputEventArgs() { Type = InputEventType.KeyUp, Key = key });

        public void KeyDown(KeyboardKey key) => SetInputState(new InputEventArgs() { Type = InputEventType.KeyDown, Key = key });

        public void MouseUp(MouseButton button) => SetInputState(new InputEventArgs() { Type = InputEventType.MouseUp, Button = button });

        public void MouseDown(MouseButton button) => SetInputState(new InputEventArgs() { Type = InputEventType.MouseDown, Button = button });

        public void MouseMove(int x, int y) => SetInputState(new InputEventArgs() { Type = InputEventType.MouseMove, X = x, Y = y });

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        //public static extern void mouse_event(long dwFlags, long dx, long dy, long cButtons, long dwExtraInfo);
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int x, int y);

        public static void DoMouseClick()
        {
            InputDriver.Instance.Element = null;
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }
        public static void Click(MouseButton button)
        {
            InputDriver.Instance.SkipEvent = true;
            InputDriver.Instance.Element = null;
            if (button == MouseButton.Left)
            {
                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            }
            else if (button == MouseButton.Right)
            {
                mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
            }
            InputDriver.Instance.SkipEvent = false;
        }

        //public void Click(MouseButton button)
        //{
        //    MouseDown(button);
        //    System.Threading.Thread.Sleep(100);
        //    MouseUp(button);
        //    System.Threading.Thread.Sleep(100);
        //}

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
    }
}
