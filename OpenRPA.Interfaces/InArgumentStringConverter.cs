using System;
using System.Activities;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OpenRPA.Interfaces
{
    public class InArgumentStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is InArgument<string>)
            {
                Activity<string> expression = ((InArgument<string>)value).Expression;
                if (expression is Literal<string>)
                {
                    return ((Literal<string>)expression).Value;
                }
            }
            var test = value as System.Activities.Presentation.Model.ModelItem;
            if (test != null)
            {
                if (test.ItemType == typeof(InArgument<string>))
                {
                    var val = test.Properties["Expression"].Value;
                    if (val != null)
                    {
                        var valstring = val.ToString();
                        return valstring;
                    }
                    return val;
                }
            }
            return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                return new InArgument<string>(new Literal<string>((string)value));
            }
            else
            {
                return null;
            }
        }
    }
}
