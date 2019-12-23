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
        public OutArgument<IElement> Result { get; set; }
        private Variable<IEnumerator<IElement>> _elements = new Variable<IEnumerator<IElement>>("_elements");
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
            if(element!=null && Body != null)
            {
                context.ScheduleAction(Body, element, OnBodyComplete);
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            Interfaces.Extensions.AddCacheArgument(metadata, "Selector", Selector);
            Interfaces.Extensions.AddCacheArgument(metadata, "Timeout", Timeout);
            Interfaces.Extensions.AddCacheArgument(metadata, "Result", Result);

            metadata.AddImplementationVariable(_elements);
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