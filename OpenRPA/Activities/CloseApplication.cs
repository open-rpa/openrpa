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
    [Designer(typeof(CloseApplicationDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.killapp.png")]
    [LocalizedToolboxTooltip("activity_closeapplication_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_closeapplication", typeof(Resources.strings))]
    public class CloseApplication : CodeActivity
    {
        public CloseApplication()
        {
            Timeout = new InArgument<TimeSpan>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<TimeSpan>("TimeSpan.FromMilliseconds(1000)")
            };
            Force = true;
        }
        [RequiredArgument, LocalizedDisplayName("activity_selector", typeof(Resources.strings)), LocalizedDescription("activity_selector_help", typeof(Resources.strings))]
        public InArgument<string> Selector { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_timeout", typeof(Resources.strings)), LocalizedDescription("activity_timeout_help", typeof(Resources.strings))]
        public InArgument<TimeSpan> Timeout { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_force", typeof(Resources.strings)), LocalizedDescription("activity_force_help", typeof(Resources.strings))]
        public InArgument<bool> Force { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var selectorstring = Selector.Get(context);
            selectorstring = OpenRPA.Interfaces.Selector.Selector.ReplaceVariables(selectorstring, context.DataContext);
            var selector = new Interfaces.Selector.Selector(selectorstring);
            var pluginname = selector.First().Selector;
            var Plugin = Plugins.recordPlugins.Where(x => x.Name == pluginname).First();
            var timeout = Timeout.Get(context);
            var force = Force.Get(context);
            Plugin.CloseBySelector(selector, timeout, force);

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