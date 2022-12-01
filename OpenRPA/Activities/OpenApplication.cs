using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(OpenApplicationDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.openapplication.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    [LocalizedToolboxTooltip("activity_openapplication_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_openapplication", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_openapplication_helpurl", typeof(Resources.strings))]
    public class OpenApplication : BreakableLoop, System.Activities.Presentation.IActivityTemplateFactory
    {
        [RequiredArgument, LocalizedDisplayName("activity_selector", typeof(Resources.strings)), LocalizedDescription("activity_selector_help", typeof(Resources.strings))]
        public InArgument<string> Selector { get; set; }
        [LocalizedDisplayName("activity_timeout", typeof(Resources.strings)), LocalizedDescription("activity_timeout_help", typeof(Resources.strings))]
        public InArgument<TimeSpan> Timeout { get; set; }
        [LocalizedDisplayName("activity_checkrunning", typeof(Resources.strings)), LocalizedDescription("activity_checkrunning_help", typeof(Resources.strings))]
        public InArgument<bool> CheckRunning { get; set; } = true;

        [LocalizedDisplayName("activity_screen", typeof(Resources.strings)), LocalizedDescription("activity_screen_help", typeof(Resources.strings))]
        public InArgument<int> Screen { get; set; }
        [LocalizedDisplayName("activity_x", typeof(Resources.strings)), LocalizedDescription("activity_x_help", typeof(Resources.strings))]
        public InArgument<int> X { get; set; }
        [LocalizedDisplayName("activity_y", typeof(Resources.strings)), LocalizedDescription("activity_y_help", typeof(Resources.strings))]
        public InArgument<int> Y { get; set; }
        [LocalizedDisplayName("activity_width", typeof(Resources.strings)), LocalizedDescription("activity_width_help", typeof(Resources.strings))]
        public InArgument<int> Width { get; set; }
        [LocalizedDisplayName("activity_height", typeof(Resources.strings)), LocalizedDescription("activity_height_help", typeof(Resources.strings))]
        public InArgument<int> Height { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_animatemove", typeof(Resources.strings)), LocalizedDescription("activity_animatemove_help", typeof(Resources.strings))]
        public InArgument<bool> AnimateMove { get; set; } = false;
        [LocalizedDisplayName("activity_result", typeof(Resources.strings)), LocalizedDescription("activity_result_help", typeof(Resources.strings))]
        public OutArgument<IElement> Result { get; set; }
        private Variable<IElement> _element = new Variable<IElement>("_element");
        [Browsable(false)]
        public ActivityAction<IElement> Body { get; set; }
        protected override void StartLoop(NativeActivityContext context)
        {
            var selectorstring = Selector.Get(context);
            selectorstring = OpenRPA.Interfaces.Selector.Selector.ReplaceVariables(selectorstring, context.DataContext);
            var selector = new Interfaces.Selector.Selector(selectorstring);
            var checkrunning = CheckRunning.Get(context);
            checkrunning = true;
            var pluginname = selector.First().Selector;
            var Plugin = Plugins.recordPlugins.Where(x => x.Name == pluginname).First();
            var timeout = Timeout.Get(context);
            if (Timeout == null || Timeout.Expression == null) timeout = TimeSpan.FromSeconds(3);
            var element = Plugin.LaunchBySelector(selector, checkrunning, timeout);
            Result.Set(context, element);
            _element.Set(context, element);
            if (element != null && element is UIElement ui)
            {
                //var window = ((UIElement)element).GetWindow();
                var screen = Screen.Get(context);
                var x = X.Get(context);
                var y = Y.Get(context);
                var width = Width.Get(context);
                var height = Height.Get(context);
                var animatemove = AnimateMove.Get(context);
                // if ((width == 0 && height == 0) || (x == 0 && y == 0))
                if (width <= 30 || height <= 10)
                {
                }
                else
                {
                    var allScreens = System.Windows.Forms.Screen.AllScreens.ToList();
                    if (screen < 0)
                    {
                        Log.Warning("screen cannot be below 0, using screen 0");
                        screen = 0;
                    }
                    if (screen >= allScreens.Count)
                    {
                        Log.Warning("screen " + screen + " does not exists, using " + (allScreens.Count - 1) + " instead");
                        screen = allScreens.Count - 1;
                    }
                    x += allScreens[screen].WorkingArea.X;
                    y += allScreens[screen].WorkingArea.Y;
                    if (animatemove) ui.MoveWindowTo(x, y, width, height);
                    if (!animatemove)
                    {
                        ui.SetWindowSize(width, height);
                        ui.SetWindowPosition(x, y);
                    }
                }
            }
            if (element != null && Body != null)
            {
                IncIndex(context);
                SetTotal(context, 1);
                context.ScheduleAction(Body, element, OnBodyComplete);
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            if (breakRequested) return;
            IElement element = _element.Get(context);
            if (element != null && element is UIElement ui)
            {
                var x = X.Get(context);
                var y = Y.Get(context);
                var width = Width.Get(context);
                var height = Height.Get(context);
                var animatemove = AnimateMove.Get(context);
                if (width <= 30 || height <= 10)
                {
                    if (animatemove) ui.MoveWindowTo(x, y, width, height);
                    if (!animatemove)
                    {
                        ui.SetWindowSize(width, height);
                        ui.SetWindowPosition(x, y);
                    }
                }
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
        public System.Activities.Activity Create(System.Windows.DependencyObject target)
        {
            var da = new DelegateInArgument<IElement>
            {
                Name = "item"
            };
            // Type t = Type.GetType("OpenRPA.Activities.ClickElement, OpenRPA");
            // var instance = Activator.CreateInstance(t);
            var fef = new OpenApplication();
            fef.Variables.Add(new Variable<int>("Index", 0));
            fef.Variables.Add(new Variable<int>("Total", 0));
            fef.Body = new ActivityAction<IElement>
            {
                Argument = da
                // , Handler = (Activity)instance
            };
            return fef;
        }
        [LocalizedDisplayName("activity_displayname", typeof(Resources.strings)), LocalizedDescription("activity_displayname_help", typeof(Resources.strings))]
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