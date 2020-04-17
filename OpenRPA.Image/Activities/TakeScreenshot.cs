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
    [System.ComponentModel.Designer(typeof(TakeScreenshotDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetImage), "Resources.toolbox.camera.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    [LocalizedToolboxTooltip("activity_takescreenshot_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_takescreenshot", typeof(Resources.strings))]
    public class TakeScreenshot : NativeActivity, System.Activities.Presentation.IActivityTemplateFactory
    {
        public TakeScreenshot()
        {
            Element = new InArgument<IElement>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IElement>("item")
            };
        }
        public InArgument<IElement> Element { get; set; }
        public OutArgument<ImageElement> Result { get; set; }
        public InArgument<int> X { get; set; }
        public InArgument<int> Y { get; set; }
        public InArgument<int> Width { get; set; }
        public InArgument<int> Height { get; set; }
        [Browsable(false)]
        public ActivityAction<ImageElement> Body { get; set; }
        private Variable<ImageElement> elements = new Variable<ImageElement>("elements");
        protected override void Execute(NativeActivityContext context)
        {
            var relativelement = Element.Get(context);
            Rectangle match = new Rectangle(0, 0, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
            if(relativelement!=null)
            {
                match = relativelement.Rectangle;
            }
            match.X += X.Get(context);
            match.Y += Y.Get(context);
            var h = Height.Get(context);
            var w = Width.Get(context);
            if (h > 10) match.Height = h;
            if (w > 10) match.Width = w;
            var b = Interfaces.Image.Util.Screenshot(match);
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
            Extensions.AddCacheArgument(metadata, "Element", Element);

            Extensions.AddCacheArgument(metadata, "Result", Result);
            Extensions.AddCacheArgument(metadata, "OffsetX", X);
            Extensions.AddCacheArgument(metadata, "OffsetY", Y);
            Extensions.AddCacheArgument(metadata, "Width", Width);
            Extensions.AddCacheArgument(metadata, "Height", Height);

            metadata.AddImplementationVariable(elements);
            base.CacheMetadata(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            var fef = new TakeScreenshot();
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