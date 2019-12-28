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
using System.Windows.Threading;
using System.Reflection;

namespace OpenRPA.Windows
{
    [System.ComponentModel.Designer(typeof(GetElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetElement), "Resources.toolbox.getuielement.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class GetElement : NativeActivity, System.Activities.Presentation.IActivityTemplateFactory
    {
        public GetElement()
        {
            MaxResults = 1;
            MinResults = 1;
            Timeout = new InArgument<TimeSpan>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<TimeSpan>("TimeSpan.FromMilliseconds(3000)")
            };
        }
        [Browsable(false)]
        public ActivityAction<UIElement> Body { get; set; }
        public InArgument<TimeSpan> Timeout { get; set; }
        public InArgument<int> MaxResults { get; set; }
        public InArgument<int> MinResults { get; set; }
        public InArgument<string> Selector { get; set; }
        // public InArgument<UIElement> From { get; set; }
        public InArgument<IElement> From { get; set; }
        public OutArgument<UIElement[]> Elements { get; set; }
        [Browsable(false)]
        public string Image { get; set; }
        private Variable<IEnumerator<UIElement>> _elements = new Variable<IEnumerator<UIElement>>("_elements");
        private Variable<Stopwatch> _sw = new Variable<Stopwatch>("_sw");
        [System.ComponentModel.Browsable(false)]
        public Activity LoopAction { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            var sw = new Stopwatch();
            sw.Start();
            Log.Selector(string.Format("Windows.GetElement::begin {0:mm\\:ss\\.fff}", sw.Elapsed));

            UIElement[] elements = null;
            var selector = Selector.Get(context);
            var sel = new WindowsSelector(selector);
            var timeout = Timeout.Get(context);
            var maxresults = MaxResults.Get(context);
            var minresults = MinResults.Get(context);
            if (maxresults < 1) maxresults = 1;
            var from = From.Get(context);

            //            double _timeout = 250;
            double _timeout = 5000;
            if (PluginConfig.search_descendants)
            {
                _timeout = 5000;
            }
            //#if DEBUG
            //            _timeout = _timeout * 8;
            //#endif
            
            do
            {
                if (PluginConfig.get_elements_in_different_thread)
                {
                    elements = OpenRPA.AutomationHelper.RunSTAThread<UIElement[]>(() =>
                    {
                        try
                        {
                            Log.Selector(string.Format("Windows.GetElement::GetElementsWithuiSelector in non UI thread {0:mm\\:ss\\.fff}", sw.Elapsed));
                            return WindowsSelector.GetElementsWithuiSelector(sel, from, maxresults);
                        }
                        catch (System.Threading.ThreadAbortException)
                        {
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "");
                        }
                        return new UIElement[] { };
                    }, TimeSpan.FromMilliseconds(_timeout)).Result;
                }
                else
                {
                    Log.Selector(string.Format("Windows.GetElement::GetElementsWithuiSelector using UI thread {0:mm\\:ss\\.fff}", sw.Elapsed));
                    elements = WindowsSelector.GetElementsWithuiSelector(sel, from, maxresults);
                }
                //elements = WindowsSelector.GetElementsWithuiSelector(sel, from, maxresults);
                if (elements == null)
                {
                    elements = new UIElement[] { };
                }
                if(elements.Length == 0) Log.Selector(string.Format("Windows.GetElement::Found no elements {0:mm\\:ss\\.fff}", sw.Elapsed));
            } while (elements != null && elements.Length == 0 && sw.Elapsed < timeout);
            //if (PluginConfig.get_elements_in_different_thread && elements.Length > 0)
            //{
            //    // Get them again, we need the COM objects to be loaded in the UI thread
            //    elements = WindowsSelector.GetElementsWithuiSelector(sel, from, maxresults);
            //}
            context.SetValue(Elements, elements);
            if(elements.Count() < minresults)
            {
                Log.Selector(string.Format("Windows.GetElement::Failed locating " + minresults + " item(s) {0:mm\\:ss\\.fff}", sw.Elapsed));
                throw new ElementNotFoundException("Failed locating " + minresults + " item(s)");
            }
            IEnumerator<UIElement> _enum = elements.ToList().GetEnumerator();
            bool more = _enum.MoveNext();
            if (more)
            {
                context.SetValue(_elements, _enum);
                context.SetValue(_sw, sw);
                Log.Selector(string.Format("Windows.GetElement::end:: call ScheduleAction: {0:mm\\:ss\\.fff}", sw.Elapsed));
                context.ScheduleAction<UIElement>(Body, _enum.Current, OnBodyComplete);
            } else
            {
                Log.Selector(string.Format("Windows.GetElement:end {0:mm\\:ss\\.fff}", sw.Elapsed));
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<UIElement> _enum = _elements.Get(context);
            Stopwatch sw = _sw.Get(context);
            Log.Selector(string.Format("Windows.GetElement:OnBodyComplete::begin {0:mm\\:ss\\.fff}", sw.Elapsed));
            bool more = _enum.MoveNext();
            if (more)
            {
                Log.Selector(string.Format("Windows.GetElement:ScheduleAction {0:mm\\:ss\\.fff}", sw.Elapsed));
                context.ScheduleAction<UIElement>(Body, _enum.Current, OnBodyComplete);
            }
            else
            {
                if (LoopAction != null)
                {
                    context.ScheduleActivity(LoopAction, LoopActionComplete);
                }
            }
            Log.Selector(string.Format("Windows.GetElement:end {0:mm\\:ss\\.fff}", sw.Elapsed));
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
            metadata.AddImplementationVariable(_sw);
            base.CacheMetadata(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            var da = new DelegateInArgument<UIElement>
            {
                Name = "item"
            };
            Type t = Type.GetType("OpenRPA.Activities.ClickElement, OpenRPA");
            var instance = Activator.CreateInstance(t);
            var fef = new GetElement();
            fef.Body = new ActivityAction<UIElement>
            {
                Argument = da,
                Handler = (Activity)instance
            };
            return fef;
        }
    }
}