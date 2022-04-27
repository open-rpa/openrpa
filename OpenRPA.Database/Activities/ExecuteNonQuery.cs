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
    [Designer(typeof(ExecuteNonQueryDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ExecuteNonQuery), "Resources.toolbox.database.png")]
    [LocalizedToolboxTooltip("activity_executenonquery_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_executenonquery", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_executenonquery_helpurl", typeof(Resources.strings))]
    public class ExecuteNonQuery : CodeActivity
    {
        [RequiredArgument, Category("Input"), LocalizedDisplayName("activity_executenonquery_query", typeof(Resources.strings)), LocalizedDescription("activity_executenonquery_query_help", typeof(Resources.strings))]
        public InArgument<string> Query { get; set; }
        [Category("Output")]
        public OutArgument<int> Result { get; set; }
        [Editor(typeof(CommandTypeEditor), typeof(ExtendedPropertyValueEditor)), Category("Misc")]
        public InArgument<string> CommandType { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var vars = context.DataContext.GetProperties();
            Connection connection = null;
            foreach (dynamic v in vars) { if (v.DisplayName == "conn") connection = v.GetValue(context.DataContext); }
            var query = Query.Get(context);
            var strcommandtype = CommandType.Get(context);
            System.Data.CommandType commandtype = System.Data.CommandType.Text;
            if (!string.IsNullOrEmpty(strcommandtype) && strcommandtype.ToLower() == "storedprocedure") commandtype = System.Data.CommandType.StoredProcedure;
            if (!string.IsNullOrEmpty(strcommandtype) && strcommandtype.ToLower() == "tabledirect") commandtype = System.Data.CommandType.TableDirect;
            var result = connection.ExecuteNonQuery(query, commandtype);
            Result.Set(context, result);
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