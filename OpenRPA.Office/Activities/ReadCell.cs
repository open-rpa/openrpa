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
    [System.ComponentModel.Designer(typeof(ReadCellDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.readexcel.png")]
    [System.Activities.Presentation.DefaultTypeArgument(typeof(String))]
    [LocalizedToolboxTooltip("activity_readcell_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_readcell", typeof(Resources.strings))]
    public class ReadCell<TResult> : ExcelActivityOf<TResult>
    {
        [RequiredArgument]
        [System.ComponentModel.Category("Input")]
        public InArgument<string> Cell { get; set; }
        [System.ComponentModel.Category("Output")]
        public OutArgument<string> Formula { get; set; }
        [System.ComponentModel.Category("Output")]
        public OutArgument<Microsoft.Office.Interop.Excel.Range> Range { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            base.Execute(context);
            var cell = Cell.Get(context);
            Microsoft.Office.Interop.Excel.Range range = worksheet.get_Range(cell);
            Formula.Set(context, range.Formula);
            Range.Set(context, range);
            if(this.ResultType == typeof(string))
            {
                if(range.Value2==null) context.SetValue(Result, null);
                try
                {
                    if(range.Value2 != null)
                    {
                        context.SetValue(Result, range.Value2.ToString());
                    } else
                    {
                        context.SetValue(Result, null);
                    }
                }
                catch (Exception)
                {
                    context.SetValue(Result, null);
                }
            }
            else
            {
                context.SetValue(Result, (TResult)range.Value2);
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