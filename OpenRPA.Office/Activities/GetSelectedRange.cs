using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using OpenRPA.Interfaces;

namespace OpenRPA.Office.Activities
{
    [System.ComponentModel.Designer(typeof(GetSelectedRangeDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.readexcel.png")]
    [System.Activities.Presentation.DefaultTypeArgument(typeof(String))]
    [LocalizedToolboxTooltip("activity_getselectedrange_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_getselectedrange", typeof(Resources.strings))]
    public class GetSelectedRange : ExcelActivity
    {
        [Category("Input")]
        public InArgument<bool> RowAbsolute { get; set; }
        [Category("Input")]
        public InArgument<bool> ColumnAbsolute { get; set; }
        [Category("Input")]
        public InArgument<bool> External { get; set; }
        [Category("Output")]
        public OutArgument<string> Range { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            base.Execute(context);
            Microsoft.Office.Interop.Excel.Range range = officewrap.application.Selection as Microsoft.Office.Interop.Excel.Range;
            var rowabsolute = RowAbsolute.Get(context);
            var columnabsolute = ColumnAbsolute.Get(context);
            var external = External.Get(context);
            if (range!=null)
            {
                Range.Set(context, range.Address[rowabsolute, columnabsolute, Microsoft.Office.Interop.Excel.XlReferenceStyle.xlA1, external, null]);
            } else
            {
                Range.Set(context, "");
            }
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