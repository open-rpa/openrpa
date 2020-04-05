using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using OpenRPA.Interfaces;

namespace OpenRPA.Office.Activities
{
    [System.ComponentModel.Designer(typeof(RunExcelMacroDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.readexcel.png")]
    [LocalizedToolboxTooltip("activity_runexcelmacro_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_runexcelmacro", typeof(Resources.strings))]
    public class RunExcelMacro<TResult> : ExcelActivityOf<TResult>
    {
        [System.ComponentModel.Category("Input")]
        public InArgument<string> Name { get; set; }
        [System.ComponentModel.Category("Input")]
        public InArgument<IEnumerable<object>> Parameters { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            base.Execute(context);
            var name = Name.Get(context);
            var parameters = Parameters.Get(context);
            object result = null;
            List<object> _params;
            if (parameters != null) _params = parameters.ToList(); else _params = new List<object>();

            var filename = System.IO.Path.GetFileName(workbook.FullName);
            _params.Insert(0, filename + "!" + name);
            result = (TResult)RunMacro(officewrap.application, _params.ToArray());
            if(result!=null)
            {
                context.SetValue(Result, (TResult)result);
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
        private object RunMacro(Application oApp, object[] oRunArgs)
        {
            return oApp.GetType().InvokeMember("Run", System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.InvokeMethod, null, oApp, oRunArgs);
        }
    }
}
