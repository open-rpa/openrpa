using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.SAPBridge
{
    [Flags]
    public enum MouseButton
    {
        None,

        Left,

        Right,

        Middle = 4
    }
    public enum InputEventType
    {
        KeyUp = 0x0101,

        KeyDown = 0x0100,

        MouseUp = 0x0202,

        MouseDown = 0x0201,

        MouseMove = 0x0200,
    }
    public sealed class InputEventArgs : EventArgs
    {
        public InputEventType Type { get; internal set; }

        public int X { get; internal set; }

        public int Y { get; internal set; }

        public MouseButton Button { get; internal set; }

        public bool AltKey { get; internal set; }

        public bool CtrlKey { get; internal set; }

        public bool ShiftKey { get; internal set; }

        public bool WinKey { get; internal set; }

        public override string ToString()
        {
            return string.Format(
                "[{0} X={1}, Y={2}, Button={3}, AltKey={4}, CtrlKey={5}, ShiftKey={6}, WinKey={7}]",
                Type, X, Y, Button, AltKey, CtrlKey, ShiftKey, WinKey);
        }
    }
}
