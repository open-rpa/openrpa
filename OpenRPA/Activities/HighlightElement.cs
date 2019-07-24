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
using System.Threading;

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(HighlightElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.highlight.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class HighlightElement : AsyncTaskCodeActivity<int>
    {
        public HighlightElement()
        {
            Element = new InArgument<IElement>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IElement>("item")
            };
            Blocking = false;
            //Duration = TimeSpan.FromMilliseconds(250);
            Duration = new InArgument<TimeSpan>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<TimeSpan>("TimeSpan.FromMilliseconds(2000)")
            };
        }
        [RequiredArgument]
        public InArgument<IElement> Element { get; set; }
        [RequiredArgument]
        public InArgument<bool> Blocking { get; set; }
        [RequiredArgument]
        public InArgument<TimeSpan> Duration { get; set; }
        protected async override Task<int> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var el = Element.Get(context);
            var blocking = Blocking.Get(context);
            var duration = Duration.Get(context);
            await el.Highlight(blocking, System.Drawing.Color.Red, duration);
            return 13;
        }
    }
}