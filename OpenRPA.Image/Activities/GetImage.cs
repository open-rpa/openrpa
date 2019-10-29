using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Image
{
    [System.ComponentModel.Designer(typeof(GetImageDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetImage), "Resources.toolbox.getimage.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class GetImage : NativeActivity, System.Activities.Presentation.IActivityTemplateFactory
    {
        // I want this !!!!
        // https://stackoverflow.com/questions/50669794/alternative-to-taking-rapid-screenshots-of-a-window
        public GetImage()
        {
            FailOnNotFound = true;
            OffsetX = 0;
            OffsetY = 0;
            Width = 10;
            Height = 10;
            Element = new InArgument<ImageElement>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<ImageElement>("item")
            };
        }
        [RequiredArgument]
        public InArgument<bool> FailOnNotFound { get; set; }
        [RequiredArgument]
        public InArgument<ImageElement> Element { get; set; }
        public OutArgument<ImageElement> Result { get; set; }
        [RequiredArgument]
        public InArgument<int> OffsetX { get; set; }
        [RequiredArgument]
        public InArgument<int> OffsetY { get; set; }
        [RequiredArgument]
        public InArgument<int> Width { get; set; }
        [RequiredArgument]
        public InArgument<int> Height { get; set; }
        [Browsable(false)]
        public ActivityAction<ImageElement> Body { get; set; }
        private Variable<ImageElement> elements = new Variable<ImageElement>("elements");
        protected override void Execute(NativeActivityContext context)
        {
            var relativelement = Element.Get(context);
            var match = relativelement.Rectangle;
            match.X += OffsetX.Get(context);
            match.Y += OffsetY.Get(context);
            match.Width = Width.Get(context);
            match.Height = Height.Get(context);
            var processname = relativelement.Processname;
            if (!string.IsNullOrEmpty(processname))
            {
                var _element = AutomationHelper.GetFromPoint(match.X, match.Y);
                if (_element.ProcessId < 1) throw new ElementNotFoundException("Failed locating Image, expected " + processname + " but found nothing");
                var p = System.Diagnostics.Process.GetProcessById(_element.ProcessId);
                if (p.ProcessName != processname)
                {
                    throw new ElementNotFoundException("Failed locating Image, expected " + processname + " but found " + p.ProcessName);
                }
            }
            var b = Interfaces.Image.Util.Screenshot(match);
            //Interfaces.Image.Util.SaveImageStamped(b, "c:\\temp", "GetImage-result");
            var v = new ImageElement(match, b);
            context.SetValue(Result, v);

            context.ScheduleAction(Body, v, OnBodyComplete);

        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
        }
        private void LoopActionComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            Execute(context);
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);

            Interfaces.Extensions.AddCacheArgument(metadata, "FailOnNotFound", FailOnNotFound);
            Interfaces.Extensions.AddCacheArgument(metadata, "Element", Element);

            Interfaces.Extensions.AddCacheArgument(metadata, "Result", Result);
            Interfaces.Extensions.AddCacheArgument(metadata, "OffsetX", OffsetX);
            Interfaces.Extensions.AddCacheArgument(metadata, "OffsetY", OffsetY);
            Interfaces.Extensions.AddCacheArgument(metadata, "Width", Width);
            Interfaces.Extensions.AddCacheArgument(metadata, "Height", Height);

            metadata.AddImplementationVariable(elements);
            base.CacheMetadata(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            var fef = new GetImage();
            var aa = new ActivityAction<ImageElement>();
            var da = new DelegateInArgument<ImageElement>();
            da.Name = "item";
            fef.Body = aa;
            aa.Argument = da;
            return fef;
        }
    }
}