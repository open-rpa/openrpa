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
    [LocalizedToolboxTooltip("activity_getimage_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_getimage", typeof(Resources.strings))]
    public class GetImage : BreakableLoop, System.Activities.Presentation.IActivityTemplateFactory
    {
        // I want this !!!!
        // https://stackoverflow.com/questions/50669794/alternative-to-taking-rapid-screenshots-of-a-window
        public GetImage()
        {
            OffsetX = 0;
            OffsetY = 0;
            Width = 10;
            Height = 10;
            Element = new InArgument<IElement>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IElement>("item")
            };
        }
        [Browsable(false)]
        public InArgument<bool> FailOnNotFound { get; set; }
        [RequiredArgument]
        public InArgument<IElement> Element { get; set; }
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
        protected override void StartLoop(NativeActivityContext context)
        {
            var relativelement = Element.Get(context);
            var match = relativelement.Rectangle;
            match.X += OffsetX.Get(context);
            match.Y += OffsetY.Get(context);
            match.Width = Width.Get(context);
            match.Height = Height.Get(context);
            var imageelement = relativelement as ImageElement;
            if (imageelement != null)
            {
                var processname = imageelement.Processname;
                if (!string.IsNullOrEmpty(processname))
                {
                    var _element = AutomationHelper.GetFromPoint(match.X, match.Y);
                    if (_element.ProcessId < 1) throw new ElementNotFoundException("Failed locating Image, expected " + processname + " but found nothing");
                    using (var p = System.Diagnostics.Process.GetProcessById(_element.ProcessId))
                    {
                        if (p.ProcessName != processname)
                        {
                            throw new ElementNotFoundException("Failed locating Image, expected " + processname + " but found " + p.ProcessName);
                        }
                    }
                }
            }
            var b = Interfaces.Image.Util.Screenshot(match);
            //Interfaces.Image.Util.SaveImageStamped(b, "c:\\temp", "GetImage-result");
            var v = new ImageElement(match, b);
            context.SetValue(Result, v);
            IncIndex(context);
            SetTotal(context, 1);
            context.ScheduleAction(Body, v, OnBodyComplete);
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            Extensions.AddCacheArgument(metadata, "Element", Element);

            Extensions.AddCacheArgument(metadata, "Result", Result);
            Extensions.AddCacheArgument(metadata, "OffsetX", OffsetX);
            Extensions.AddCacheArgument(metadata, "OffsetY", OffsetY);
            Extensions.AddCacheArgument(metadata, "Width", Width);
            Extensions.AddCacheArgument(metadata, "Height", Height);

            metadata.AddImplementationVariable(elements);
            base.CacheMetadata(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            Type t = typeof(GetElement);
            var wfdesigner = Plugin.client.Window.LastDesigner;
            WFHelper.DynamicAssemblyMonitor(wfdesigner.WorkflowDesigner, t.Assembly.GetName().Name, t.Assembly, true);
            var fef = new GetImage();
            fef.Variables.Add(new Variable<int>("Index", 0));
            fef.Variables.Add(new Variable<int>("Total", 0));
            var aa = new ActivityAction<ImageElement>();
            var da = new DelegateInArgument<ImageElement>();
            da.Name = "item";
            fef.Body = aa;
            aa.Argument = da;
            return fef;
        }
        public new string DisplayName
        {
            get
            {
                var displayName = base.DisplayName;
                if (displayName == this.GetType().Name)
                {
                    var displayNameAttribute = this.GetType().GetCustomAttributes(typeof(DisplayNameAttribute), true).FirstOrDefault() as DisplayNameAttribute;
                    if (displayNameAttribute != null) displayName = displayNameAttribute.DisplayName;
                }
                return displayName;
            }
            set
            {
                base.DisplayName = value;
            }
        }
    }
}