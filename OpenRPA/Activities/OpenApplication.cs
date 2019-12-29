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
    [System.ComponentModel.Designer(typeof(OpenApplicationDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.getapp.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    [System.Windows.Markup.ContentProperty("Body")]
    public class OpenApplication : NativeActivity, System.Activities.Presentation.IActivityTemplateFactory
    {
        public OpenApplication()
        {
            Timeout = new InArgument<TimeSpan>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<TimeSpan>("TimeSpan.FromMilliseconds(1000)")
            };
        }
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }
        [RequiredArgument]
        public InArgument<TimeSpan> Timeout { get; set; }
        public InArgument<bool> CheckRunning { get; set; } = true;
        public InArgument<int> X { get; set; }
        public InArgument<int> Y { get; set; }
        public InArgument<int> Width { get; set; }
        public InArgument<int> Height { get; set; }
        public InArgument<bool> AnimateMove { get; set; } = false;
        public OutArgument<IElement> Result { get; set; }
        private Variable<IElement> _element = new Variable<IElement>("_element");
        [Browsable(false)]
        public ActivityAction<IElement> Body { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            var selectorstring = Selector.Get(context);
            var selector = new Interfaces.Selector.Selector(selectorstring);
            var checkrunning = CheckRunning.Get(context);
            checkrunning = true;
            var pluginname = selector.First().Selector;
            var Plugin = Plugins.recordPlugins.Where(x => x.Name == pluginname).First();
            var timeout = Timeout.Get(context);
            var element = Plugin.LaunchBySelector(selector, checkrunning, timeout);
            Result.Set(context, element);
            _element.Set(context, element);
            if (element!=null && element is UIElement ui && Body == null)
            {
                //var window = ((UIElement)element).GetWindow();
                var x = X.Get(context);
                var y = Y.Get(context);
                var width = Width.Get(context);
                var height = Height.Get(context);
                var animatemove = AnimateMove.Get(context);
                if((width == 0 && height == 0) || (x == 0 && y == 0))
                {
                }
                else
                {
                    if (animatemove) ui.MoveWindowTo(x, y, width, height);
                    if (!animatemove)
                    {
                        ui.SetWindowSize(width, height);
                        ui.SetWindowPosition(x, y);
                    }
                }
            }
            if(element!=null && Body != null)
            {
                context.ScheduleAction(Body, element, OnBodyComplete);
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IElement element = _element.Get(context);
            if (element != null && element is UIElement ui)
            {
                //var window = ((UIElement)element).GetWindow();
                var x = X.Get(context);
                var y = Y.Get(context);
                var width = Width.Get(context);
                var height = Height.Get(context);
                var animatemove = AnimateMove.Get(context);
                //if(width > 0 && height > 0)
                //{
                //    ui.SetWindowSize(width, height);
                //}
                //if (x>0 && y>0)
                //{
                // if (animatemove) ui.MoveWindowTo(x, y);
                if (animatemove) ui.MoveWindowTo(x, y, width, height);
                if (!animatemove)
                {
                    ui.SetWindowSize(width, height);
                    ui.SetWindowPosition(x, y);
                }
                //}
            }
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            Interfaces.Extensions.AddCacheArgument(metadata, "Selector", Selector);
            Interfaces.Extensions.AddCacheArgument(metadata, "Timeout", Timeout);
            Interfaces.Extensions.AddCacheArgument(metadata, "Result", Result);

            metadata.AddImplementationVariable(_element);
            base.CacheMetadata(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            var da = new DelegateInArgument<IElement>
            {
                Name = "item"
            };
            // Type t = Type.GetType("OpenRPA.Activities.ClickElement, OpenRPA");
            // var instance = Activator.CreateInstance(t);
            var fef = new OpenApplication();
            fef.Body = new ActivityAction<IElement>
            {
                Argument = da
                // , Handler = (Activity)instance
            };
            return fef;
        }

    }
}