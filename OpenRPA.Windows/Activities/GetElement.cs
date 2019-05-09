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
using Serilog;

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
        public InArgument<string> Selector { get; set; }
        public InArgument<UIElement> From { get; set; }
        public OutArgument<UIElement> Element { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            UIElement result = null;

            var selector = Selector.Get(context);
            var sel = new WindowsSelector(selector);
            var timeout = TimeSpan.FromSeconds(5);


            UIElement[] elements = { };
            var sw = new Stopwatch();
            sw.Start();
            do
            {
                elements = OpenRPA.AutomationHelper.RunSTAThread<UIElement[]>(() =>
                {
                    try
                    {
                        return WindowsSelector.GetElementsWithuiSelector(sel, null);
                    }
                    catch (System.Threading.ThreadAbortException)
                    {
                        Log.Debug("Timeout getting element");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "");
                    }
                    return new UIElement[] { };
                }, TimeSpan.FromMilliseconds(250)).Result;
                if(elements==null) 
                {
                    elements = new UIElement[] { };
                }
                if (elements.Count() > 0) result = (UIElement)elements[0];
            } while (result == null && sw.Elapsed < timeout);
            if (elements.Count() > 0) result = (UIElement)elements[0];
            context.SetValue(Element, result);
            if (result != null) {
                context.ScheduleAction(Body, result, OnBodyComplete);
            } else
            {
                throw new ElementNotFoundException("Failed locating item");
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            Interfaces.Extensions.AddCacheArgument(metadata, "Selector", Selector);
            Interfaces.Extensions.AddCacheArgument(metadata, "From", From);
            Interfaces.Extensions.AddCacheArgument(metadata, "Element", Element);
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