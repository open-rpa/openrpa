using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.NM
{
    [System.ComponentModel.Designer(typeof(GetElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetElement), "Resources.toolbox.gethtmlelement.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class GetElement : NativeActivity, System.Activities.Presentation.IActivityTemplateFactory
    {
        //[RequiredArgument]
        //public InArgument<string> XPath { get; set; }

        [System.ComponentModel.Browsable(false)]
        public ActivityAction<NMElement> Body { get; set; }
        public InArgument<int> MaxResults { get; set; }
        public InArgument<int> MinResults { get; set; }
        public InArgument<TimeSpan> Timeout { get; set; }
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }
        public InArgument<NMElement> From { get; set; }
        public OutArgument<NMElement[]> Elements { get; set; }
        [Browsable(false)]
        public String Image { get; set; }
        private Variable<IEnumerator<NMElement>> _elements = new Variable<IEnumerator<NMElement>>("_elements");
        public Activity LoopAction { get; set; }
        public GetElement()
        {
            MaxResults = 1;
            MinResults = 1;
            Timeout = new InArgument<TimeSpan>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<TimeSpan>("TimeSpan.FromSeconds(3)")
            };
        }
        protected override void Execute(NativeActivityContext context)
        {
            var selector = Selector.Get(context);
            var sel = new NMSelector(selector);
            var timeout = Timeout.Get(context);
            var from = From.Get(context);
            var maxresults = MaxResults.Get(context);
            var minresults = MinResults.Get(context); 
            if (maxresults < 1) maxresults = 1;
            NMElement[] elements = { };
            var sw = new Stopwatch();
            sw.Start();
            do
            {
                elements = NMSelector.GetElementsWithuiSelector(sel, from, maxresults);
            } while (elements .Count() == 0 && sw.Elapsed < timeout);
            if (elements.Count() > maxresults) elements = elements.Take(maxresults).ToArray();
            context.SetValue(Elements, elements);
            IEnumerator<NMElement> _enum = elements.ToList().GetEnumerator();
            bool more = _enum.MoveNext();
            if (more)
            {
                if(elements.Count() > 1) context.SetValue(_elements, _enum);
                context.ScheduleAction(Body, _enum.Current, OnBodyComplete);
            }
            else if (elements.Length < minresults)
            {
                throw new Interfaces.ElementNotFoundException("Failed locating " + minresults + " item");
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<NMElement> _enum = _elements.Get(context);
            if (_enum == null) return;
            bool more = _enum.MoveNext();
            if (more)
            {
                context.ScheduleAction<NMElement>(Body, _enum.Current, OnBodyComplete);
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
            var aa = new ActivityAction<NMElement>();
            var da = new DelegateInArgument<NMElement>();
            da.Name = "item";
            fef.Body = aa;
            aa.Argument = da;
            return fef;
        }

    }
}