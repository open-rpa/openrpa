using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;

namespace OpenRPA.Database
{
    [Designer(typeof(ExecuteScalarDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ExecuteNonQuery), "Resources.toolbox.database.png")]
    [LocalizedToolboxTooltip("activity_executescalar_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_executescalar", typeof(Resources.strings))]
    public class ExecuteScalar<T> : CodeActivity<T>
    {
        [RequiredArgument, Category("Input"), LocalizedDisplayName("activity_executescalar_query", typeof(Resources.strings)), LocalizedDescription("activity_executescalar_query_help", typeof(Resources.strings))]
        public InArgument<string> Query { get; set; }
        [Editor(typeof(CommandTypeEditor), typeof(ExtendedPropertyValueEditor)), Category("Misc")]
        public InArgument<string> CommandType { get; set; }
        // public Type TargetType { get; set; }
        protected override T Execute(CodeActivityContext context)
        {
            var vars = context.DataContext.GetProperties();
            Connection connection = null;
            foreach (dynamic v in vars) { if (v.DisplayName == "conn") connection = v.GetValue(context.DataContext); }
            var query = Query.Get(context);
            var strcommandtype = CommandType.Get(context);
            System.Data.CommandType commandtype = System.Data.CommandType.Text;
            if (!string.IsNullOrEmpty(strcommandtype) && strcommandtype.ToLower() == "storedprocedure") commandtype = System.Data.CommandType.StoredProcedure;
            if (!string.IsNullOrEmpty(strcommandtype) && strcommandtype.ToLower() == "tabledirect") commandtype = System.Data.CommandType.TableDirect;
            var result = connection.ExecuteScalar(query, commandtype);
            return (T)result;
        }
        [LocalizedDisplayName("activity_displayname", typeof(Resources.strings)), LocalizedDescription("activity_displayname_help", typeof(Resources.strings))]
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