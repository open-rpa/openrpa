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

namespace OpenRPA.Windows
{
    [System.ComponentModel.Designer(typeof(GetElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetElement), "Resources.toolbox.getuielement.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class GetElement : NativeActivity, System.Activities.Presentation.IActivityTemplateFactory
    {
        [Browsable(false)]
        public ActivityAction<UIElement> Body { get; set; }
        public InArgument<int> MaxResults { get; set; }
        public InArgument<string> Selector { get; set; }
        public InArgument<UIElement> From { get; set; }
        public OutArgument<UIElement[]> Elements { get; set; }

        private Variable<IEnumerator<UIElement>> _elements = new Variable<IEnumerator<UIElement>>("_elements");
        [System.ComponentModel.Browsable(false)]
        public Activity LoopAction { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            UIElement[] elements = null;
            var selector = Selector.Get(context);
            var sel = new WindowsSelector(selector);
            var timeout = TimeSpan.FromSeconds(5);
            var maxresults = MaxResults.Get(context);
            var sw = new Stopwatch();
            sw.Start();
            do
            {
                elements = OpenRPA.AutomationHelper.RunSTAThread<UIElement[]>(() =>
                {
                    try
                    {
                        return WindowsSelector.GetElementsWithuiSelector(sel, null, maxresults);
                    }
                    catch (System.Threading.ThreadAbortException)
                    {
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "");
                    }
                    return new UIElement[] { };
                }, TimeSpan.FromMilliseconds(250)).Result;
                if (elements == null)
                {
                    elements = new UIElement[] { };
                }
            } while (elements != null && elements.Length == 0 && sw.Elapsed < timeout);

            context.SetValue(Elements, elements);
            IEnumerator<UIElement> _enum = elements.ToList().GetEnumerator();
            context.SetValue(_elements, _enum);
            bool more = _enum.MoveNext();
            if (more)
            {
                context.ScheduleAction<UIElement>(Body, _enum.Current, OnBodyComplete);
            }
            else
            {
                throw new ElementNotFoundException("Failed locating item");
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<UIElement> _enum = _elements.Get(context);
            bool more = _enum.MoveNext();
            if (more)
            {
                context.ScheduleAction<UIElement>(Body, _enum.Current, OnBodyComplete);
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
            var aa = new ActivityAction<UIElement>();
            var da = new DelegateInArgument<UIElement>();
            da.Name = "item";
            fef.Body = aa;
            aa.Argument = da;
            return fef;
        }
    }
}