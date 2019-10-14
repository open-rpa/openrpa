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

namespace OpenRPA.Input
{
    public sealed class InputEventArgs : EventArgs
    {
        //public bool Handled { get; set; }
        public void SetElement(UIElement Element)
        {
            this.Element = Element;
        }
        public UIElement Element { get; internal set; }
        public InputEventType Type { get; internal set; }

        public int X { get; internal set; }

        public int Y { get; internal set; }

        public MouseButton Button { get; internal set; }

        public KeyboardKey Key { get; internal set; }
        public int KeyValue { get; internal set; }

        public bool AltKey { get; internal set; }

        public bool CtrlKey { get; internal set; }

        public bool ShiftKey { get; internal set; }

        public bool WinKey { get; internal set; }

        public override string ToString()
        {
            return string.Format(
                "[{0} X={1}, Y={2}, Button={3}, Key={4}, AltKey={5}, CtrlKey={6}, ShiftKey={7}, WinKey={8}]",
                Type, X, Y, Button, Key, AltKey, CtrlKey, ShiftKey, WinKey);
        }
    }
}
