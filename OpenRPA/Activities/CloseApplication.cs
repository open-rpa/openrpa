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
    [Designer(typeof(OpenApplicationDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.killapp.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
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
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }
        [RequiredArgument]
        public InArgument<TimeSpan> Timeout { get; set; }
        [RequiredArgument]
        public InArgument<bool> Force { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var selectorstring = Selector.Get(context);
            var selector = new Interfaces.Selector.Selector(selectorstring);
            var pluginname = selector.First().Selector;
            var Plugin = Plugins.recordPlugins.Where(x => x.Name == pluginname).First();
            var timeout = Timeout.Get(context);
            var force = Force.Get(context);
            Plugin.CloseBySelector(selector, timeout, force);

        }
    }
}