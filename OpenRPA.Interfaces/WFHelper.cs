using System;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
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
        public static void AddVBNamespaceSettings(System.Activities.ActivityBuilder rootObject, params Type[] types)
        {
            var vbsettings = Microsoft.VisualBasic.Activities.VisualBasic.GetSettings(rootObject);
            if (vbsettings == null)
            {
                vbsettings = new Microsoft.VisualBasic.Activities.VisualBasicSettings();
            }
            foreach (Type t in types)
            {
                vbsettings.ImportReferences.Add(
                    new Microsoft.VisualBasic.Activities.VisualBasicImportReference
                    {
                        Assembly = t.Assembly.GetName().Name,
                        Import = t.Namespace
                    });
            }
            Microsoft.VisualBasic.Activities.VisualBasic.SetSettings(rootObject, vbsettings);
        }
        public static void AddNamespaceSettings(object rootObject, params Type[] types)
        {
            Microsoft.VisualBasic.Activities.VisualBasicSettings vbsettings = Microsoft.VisualBasic.Activities.VisualBasic.GetSettings(rootObject);
            if (vbsettings == null)
            {
                vbsettings = new Microsoft.VisualBasic.Activities.VisualBasicSettings();
            }
            foreach (Type t in types)
            {
                vbsettings.ImportReferences.Add(
                    new Microsoft.VisualBasic.Activities.VisualBasicImportReference
                    {
                        Assembly = t.Assembly.GetName().Name,
                        Import = t.Namespace
                    });
            }
            Microsoft.VisualBasic.Activities.VisualBasic.SetSettings(rootObject, vbsettings);
        }

    }

}
