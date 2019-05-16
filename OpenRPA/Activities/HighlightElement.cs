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
        }
        [RequiredArgument]
        public InArgument<IElement> Element { get; set; }
        [RequiredArgument]
        public InArgument<bool> Blocking { get; set; }
        //private AutoResetEvent syncEvent = new AutoResetEvent(false);
        private bool blocking = false;
        private IElement el;

        protected async override Task<int> ExecuteAsync(AsyncCodeActivityContext context)
        {
            el = Element.Get(context);
            blocking = Blocking.Get(context);
            //GenericTools.RunUI(() =>
            //{
            //    if (el == null) throw new ArgumentException("element cannot be null");
            //    el.Highlight(true, System.Drawing.Color.Red, TimeSpan.FromSeconds(1));
            //});
            await el.Highlight(blocking, System.Drawing.Color.Red, TimeSpan.FromSeconds(1));
            //await Task.Delay(TimeSpan.FromSeconds(1));
            return 13;
        }
    }
}