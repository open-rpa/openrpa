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
using System.Threading;

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(HighlightElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.highlight.png")]
    [LocalizedToolboxTooltip("activity_highlightelement_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_highlightelement", typeof(Resources.strings))]
    public class HighlightElement : AsyncTaskCodeActivity<int>
    {
        public HighlightElement()
        {
            Element = new InArgument<IElement>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IElement>("item")
            };
            Blocking = false;
            //Duration = TimeSpan.FromMilliseconds(250);
            Duration = new InArgument<TimeSpan>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<TimeSpan>("TimeSpan.FromMilliseconds(2000)")
            };
        }
        [RequiredArgument, LocalizedDisplayName("activity_element", typeof(Resources.strings)), LocalizedDescription("activity_element_help", typeof(Resources.strings))]
        public InArgument<IElement> Element { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_blocking", typeof(Resources.strings)), LocalizedDescription("activity_blocking_help", typeof(Resources.strings))]
        public InArgument<bool> Blocking { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_duration", typeof(Resources.strings)), LocalizedDescription("activity_duration_help", typeof(Resources.strings))]
        public InArgument<TimeSpan> Duration { get; set; }
        protected async override Task<int> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var el = Element.Get(context);
            var blocking = Blocking.Get(context);
            var duration = Duration.Get(context);
            await el.Highlight(blocking, System.Drawing.Color.Red, duration);
            return 13;
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