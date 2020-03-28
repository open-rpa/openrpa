using System;
using System.Activities.Presentation.Toolbox;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using OpenRPA.Interfaces;

namespace OpenRPA.Views
{
    [ValueConversion(typeof(ToolboxItemWrapper), typeof(string))]
    public class ToolTipConvertor : IValueConverter
    {
        public static Dictionary<ToolboxItemWrapper, string> ToolTipDic = new Dictionary<ToolboxItemWrapper, string>();
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ToolboxItemWrapper itemWrapper = (ToolboxItemWrapper)value;
            // var attr = itemWrapper.Type.GetCustomAttribute<Interfaces.ToolboxTooltipAttribute>(false);
            var attr = OpenRPA.Interfaces.Extensions.GetMyCustomAttributes<ToolboxTooltipAttribute>(itemWrapper.Type, false).FirstOrDefault();
                //itemWrapper.Type. .GetMyCustomAttributes<Interfaces.ToolboxTooltipAttribute>(false);
            if (attr != null)
            {
                return attr.Text;
            }
            if (ToolTipDic.ContainsKey(itemWrapper))
            {
                return ToolTipDic[itemWrapper];
            }
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
