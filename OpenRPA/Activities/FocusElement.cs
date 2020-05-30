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
    [Designer(typeof(FocusElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.focuselement.png")]
    [LocalizedToolboxTooltip("activity_focuselement_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_focuselement", typeof(Resources.strings))]
    public class FocusElement : CodeActivity
    {
        public FocusElement()
        {
            Element = new InArgument<IElement>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IElement>("item")
            };
        }
        [RequiredArgument, LocalizedDisplayName("activity_element", typeof(Resources.strings)), LocalizedDescription("activity_element_help", typeof(Resources.strings))]
        public InArgument<IElement> Element { get; set; }
        [LocalizedDisplayName("activity_postwait", typeof(Resources.strings)), LocalizedDescription("activity_postwait_help", typeof(Resources.strings))]
        public InArgument<TimeSpan> PostWait { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var el = Element.Get(context);
            if (el == null) throw new ArgumentException("element cannot be null");

            el.Focus();
            TimeSpan postwait = TimeSpan.Zero;
            if (PostWait != null) { postwait = PostWait.Get(context); }
            if (postwait != TimeSpan.Zero)
            {
                System.Threading.Thread.Sleep(postwait);
                // FlaUI.Core.Input.Wait.UntilInputIsProcessed();
            }
        }
        [LocalizedDisplayName("activity_displayname", typeof(Resources.strings)), LocalizedDescription("activity_displayname_help", typeof(Resources.strings))]
        public new string DisplayName
        {
            get
            {
                var displayName = base.DisplayName;
                if (displayName == this.GetType().Name)
                {
                    var displayNameAttribute = this.GetType().GetCustomAttributes(typeof(DisplayNameAttribute), true).FirstOrDefault() as DisplayNameAttribute;
                    if (displayNameAttribute != null) displayName = displayNameAttribute.DisplayName;
                }
                return displayName;
            }
            set
            {
                base.DisplayName = value;
            }
        }
    }
}