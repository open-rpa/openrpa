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
            object value = range.Value;
            if (range.Value2 is TResult val2)
            {
                context.SetValue(Result, val2);
            }
            else if (range.Value is TResult val)
            {
                context.SetValue(Result, val);
            }
            else
            {
                if(value != null && typeof(TResult) == typeof(bool))
                {
                    if (value is double d) value = (d > 0);
                    if (value is int i) value = (i > 0);
                    if (value is string s) value = (s == "1" || s.ToLower() == "true");
                }
                if (value != null && value.GetType() == typeof(double) && typeof(TResult) == typeof(int))
                {
                    if (value != null) value = int.Parse(value.ToString());
                    if (value == null) value = int.Parse("0");
                }
                if (value != null && value.GetType() == typeof(DateTime) && typeof(TResult) == typeof(string))
                {
                    value = value.ToString();
                }
                if (value != null && value.GetType() == typeof(int) && typeof(TResult) == typeof(string))
                {
                    value = value.ToString();
                }
                if (value != null && value.GetType() == typeof(double) && typeof(TResult) == typeof(string))
                {
                    value = value.ToString();
                }
                if (value != null) context.SetValue(Result, (TResult)value);
                if (value == null) context.SetValue(Result, default(TResult));
            }
            var sheetPassword = SheetPassword.Get(context);
            if (string.IsNullOrEmpty(sheetPassword)) sheetPassword = null;
            if (!string.IsNullOrEmpty(sheetPassword) && worksheet != null)
            {
                worksheet.Protect(sheetPassword);
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