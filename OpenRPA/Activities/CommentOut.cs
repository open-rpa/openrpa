using FlaUI.Core.AutomationElements.Infrastructure;
using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(CommentOutDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.commentout.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    // [designer.ToolboxTooltip(Text = "Find an HTML element based on css/xpath selector")]
    public class CommentOut : CodeActivity
    {
        [DefaultValue(null)]
        public Activity Body { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            // This is an empty method because this activity is meant to "comment" other activities out,
            // so it intentionally does nothing at execution time.
        }

    }
}