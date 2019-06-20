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

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(ClickElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.elementclick.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class ClickElement : CodeActivity
    {
        [RequiredArgument]
        public InArgument<bool> AnimateMouse { get; set; } = false;
        [RequiredArgument]
        //[Editor(typeof(SelectButtonEditor), typeof(PropertyValueEditor))]
        public InArgument<int> Button { get; set; } = (int)Input.MouseButton.Left;
        [RequiredArgument]
        public int OffsetX { get; set; } = 0;
        [RequiredArgument]
        public int OffsetY { get; set; } = 0;
        [RequiredArgument]
        public InArgument<IElement> Element { get; set; }
        [RequiredArgument]
        public InArgument<bool> DoubleClick { get; set; } = false;
        public InArgument<bool> VirtualClick { get; set; } = true;
        protected override void Execute(CodeActivityContext context)
        {
            var el = Element.Get(context);
            if (el == null) throw new ArgumentException("element cannot be null");
            var doubleClick = false;
            if (DoubleClick != null) doubleClick = DoubleClick.Get(context);
            TimeSpan timeout = TimeSpan.FromSeconds(0);
            //if (Timeout != null) timeout = Timeout.Get(context);
            var button = Button.Get(context);

            var virtualClick = false;
            if (VirtualClick != null) virtualClick = VirtualClick.Get(context);
            // if (button != (int)Input.MouseButton.Left) { VirtualClick = false; }

            //var flabuttun = (FlaUI.Core.Input.MouseButton)Enum.Parse(typeof(FlaUI.Core.Input.MouseButton), button.ToString(), true);
            //el.Click(AnimateMouse.Get(context), flabuttun, doubleClick, OffsetX, OffsetY, timeout, virtualClick);
            var _button = (Input.MouseButton)button;
            el.Click(virtualClick, _button, OffsetX, OffsetY);

        }
    }
}