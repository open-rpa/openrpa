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
    [System.ComponentModel.Designer(typeof(HighlightElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.highlight.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class HighlightElement : CodeActivity
    {
        public HighlightElement()
        {
            Element = new InArgument<IElement>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IElement>("item")
            };
        }
        [RequiredArgument]
        public InArgument<IElement> Element { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            GenericTools.RunUI(() =>
            {
                var el = Element.Get(context);
                if (el == null) throw new ArgumentException("element cannot be null");
                el.Highlight(true, System.Drawing.Color.Red, TimeSpan.FromSeconds(1));
            });
        }
    }
}