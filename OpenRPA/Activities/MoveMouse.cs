using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace OpenRPA.Activities
{
    [Designer(typeof(MoveMouseDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.mouse.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class MoveMouse : CodeActivity
    {
        public MoveMouse()
        {
            Element = new InArgument<IElement>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IElement>("item")
            };
            AnimateMouse = Config.local.use_animate_mouse;
            PostWait = Config.local.use_postwait;
        }
        [RequiredArgument]
        public InArgument<bool> AnimateMouse { get; set; } = false;
        [RequiredArgument]
        public int OffsetX { get; set; } = 5;
        [RequiredArgument]
        public int OffsetY { get; set; } = 5;
        public InArgument<IElement> Element { get; set; }
        public InArgument<TimeSpan> PostWait { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var el = Element.Get(context);
            var animatemouse = false;
            if (AnimateMouse != null) animatemouse = AnimateMouse.Get(context);
            var x = OffsetX;
            var y = OffsetY;
            if(el != null)
            {
                x += el.Rectangle.X;
                y += el.Rectangle.Y;
            }
            if (animatemouse)
            {
                FlaUI.Core.Input.Mouse.MoveTo(new System.Drawing.Point(x, y));
            }
            else
            {
                NativeMethods.SetCursorPos(x, y);
            }
            TimeSpan postwait = TimeSpan.Zero;
            if (PostWait!=null) { postwait = PostWait.Get(context); }
            if(postwait != TimeSpan.Zero)
            {
                System.Threading.Thread.Sleep(postwait);
            }
        }
    }
}