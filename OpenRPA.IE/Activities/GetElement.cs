using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.IE
{
    [System.ComponentModel.Designer(typeof(GetElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetElement), "Resources.toolbox.gethtmlelement.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class GetElement : NativeActivity, System.Activities.Presentation.IActivityTemplateFactory
    {
        //[RequiredArgument]
        //public InArgument<string> XPath { get; set; }

        [RequiredArgument]
        public InArgument<string> Selector { get; set; }
        public InArgument<IEElement> From { get; set; }
        public OutArgument<IEElement> Element { get; set; }
        [System.ComponentModel.Browsable(false)]
        public ActivityAction<IEElement> Body { get; set; }

        public GetElement()
        {
        }

        protected override void Execute(NativeActivityContext context)
        {
            IEElement result = null;
            var selector = Selector.Get(context);
            var sel = new IESelector(selector);
            var timeout = TimeSpan.FromSeconds(3);
            IEElement[] elements = { };
            var sw = new Stopwatch();
            sw.Start();
            do
            {
                elements = IESelector.GetElementsWithuiSelector(sel, null);
                if (elements.Count() > 0) result = (IEElement)elements[0];
            } while (result == null && sw.Elapsed < timeout);
            context.SetValue(Element, result);
            if (result != null)
            {
                context.ScheduleAction(Body, result, OnBodyComplete);
            }
            else
            {
                throw new Interfaces.ElementNotFoundException("Failed locating item");
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
        }

        public Activity Create(System.Windows.DependencyObject target)
        {
            var fef = new GetElement();
            var aa = new ActivityAction<IEElement>();
            var da = new DelegateInArgument<IEElement>();
            da.Name = "item";
            fef.Body = aa;
            aa.Argument = da;
            return fef;
        }

    }
}