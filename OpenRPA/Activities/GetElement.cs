using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(GetElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.gethtmlelement.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class GetElement : NativeActivity, System.Activities.Presentation.IActivityTemplateFactory
    {
        //[RequiredArgument]
        //public InArgument<string> XPath { get; set; }

        [System.ComponentModel.Browsable(false)]
        public ActivityAction<IElement> Body { get; set; }
        public InArgument<int> MaxResults { get; set; }
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }
        public InArgument<IElement> From { get; set; }
        public OutArgument<IElement[]> Elements { get; set; }
        private Variable<IEnumerator<IElement>> _elements = new Variable<IEnumerator<IElement>>("_elements");
        public Activity LoopAction { get; set; }
        public GetElement()
        {
        }
        protected override void Execute(NativeActivityContext context)
        {
            var selectorstring = Selector.Get(context);
            var selector = new Interfaces.Selector.Selector(selectorstring);
            var timeout = TimeSpan.FromSeconds(3);

            var pluginname = selector.First().Selector;
            var Plugin = Plugins.recordPlugins.Where(x => x.Name == pluginname).First();

            IElement[] elements = { };
            var sw = new Stopwatch();
            sw.Start();
            do
            {
                elements = Plugin.GetElementsWithSelector(selector, null);
            } while (elements .Count() == 0 && sw.Elapsed < timeout);
            context.SetValue(Elements, elements);
            IEnumerator<IElement> _enum = elements.ToList().GetEnumerator();
            context.SetValue(_elements, _enum);
            bool more = _enum.MoveNext();
            if (more)
            {
                context.ScheduleAction(Body, _enum.Current, OnBodyComplete);
            }
            else
            {
                throw new Interfaces.ElementNotFoundException("Failed locating item");
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<IElement> _enum = _elements.Get(context);
            bool more = _enum.MoveNext();
            if (more)
            {
                context.ScheduleAction<IElement>(Body, _enum.Current, OnBodyComplete);
            }
            else
            {
                if (LoopAction != null)
                {
                    context.ScheduleActivity(LoopAction, LoopActionComplete);
                }
            }
        }
        private void LoopActionComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            Execute(context);
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            Interfaces.Extensions.AddCacheArgument(metadata, "Selector", Selector);
            Interfaces.Extensions.AddCacheArgument(metadata, "From", From);
            Interfaces.Extensions.AddCacheArgument(metadata, "Elements", Elements);
            Interfaces.Extensions.AddCacheArgument(metadata, "MaxResults", MaxResults);
            metadata.AddImplementationVariable(_elements);
            base.CacheMetadata(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            var fef = new GetElement();
            var aa = new ActivityAction<IElement>();
            var da = new DelegateInArgument<IElement>();
            da.Name = "item";
            fef.Body = aa;
            aa.Argument = da;
            return fef;
        }

    }
}