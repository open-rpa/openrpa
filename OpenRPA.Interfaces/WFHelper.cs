using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public static class WFHelper
    {
        public static System.Activities.ActivityWithResult TryCreateLiteral(Type type, string expressionText)
        {
            var ActivityDesignerAsm = typeof(System.Activities.Presentation.ActivityDesigner).Assembly;
            var types = ActivityDesignerAsm.GetTypes();
            var ExpressionHelper = types.Where(x => x.Name == "ExpressionHelper").FirstOrDefault();
            var ParserContext = types.Where(x => x.Name == "ParserContext").FirstOrDefault();

            object context = Activator.CreateInstance(ParserContext);

            var TryCreateLiteralMethod = ExpressionHelper.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(x => x.Name == "TryCreateLiteral").FirstOrDefault();
            var o = TryCreateLiteralMethod.Invoke(ExpressionHelper, new object[] { type, expressionText, context });
            return o as System.Activities.ActivityWithResult;
        }
    }
}
