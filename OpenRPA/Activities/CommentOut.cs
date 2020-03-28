using OpenRPA.Interfaces;
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
    [Designer(typeof(CommentOutDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.commentout.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    [ToolboxTooltip("Ignore everything inside this Acitvity, handy for quickly remarking something out")]
    public class CommentOut : CodeActivity
    {
        [DefaultValue(null)]
        public Activity Body { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            // This is an empty method because this activity is meant to "comment" other activities out,
            // so it intentionally does nothing at execution time.
        }
        [LocalizedDisplayName("activity_displayname", typeof(Resources.strings)), LocalizedDescription("activity_displayname_help", typeof(Resources.strings))]
        public new string DisplayName
        {
            get
            {
                return base.DisplayName;
            }
            set
            {
                base.DisplayName = value;
            }
        }
    }
}