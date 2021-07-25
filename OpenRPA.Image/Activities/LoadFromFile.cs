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
    [System.ComponentModel.Designer(typeof(LoadFromFileDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(LoadFromFile), "Resources.toolbox.getimage.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    [LocalizedToolboxTooltip("activity_loadfromfile_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_loadfromfile", typeof(Resources.strings))]
    public class LoadFromFile : BreakableLoop, System.Activities.Presentation.IActivityTemplateFactory
    {
        [RequiredArgument]
        public InArgument<string> Filename { get; set; }
        public OutArgument<ImageElement> Result { get; set; }
        [Browsable(false)]
        public ActivityAction<ImageElement> Body { get; set; }
        private Variable<ImageElement> elements = new Variable<ImageElement>("elements");
        protected override void StartLoop(NativeActivityContext context)
        {
            var filename = Filename.Get(context);
            filename = Environment.ExpandEnvironmentVariables(filename);

            var b = new Bitmap(filename);
            var v = new ImageElement(Rectangle.Empty, b);
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
            Interfaces.Extensions.AddCacheArgument(metadata, "Filename", Filename);
            metadata.AddImplementationVariable(elements);
            base.CacheMetadata(metadata);
            AddIndexTotal(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            var fef = new LoadFromFile();
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