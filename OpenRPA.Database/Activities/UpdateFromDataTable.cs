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
using System.Data;

namespace OpenRPA.Database
{
    [Designer(typeof(UpdateFromDataTableDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ExecuteNonQuery), "Resources.toolbox.updatefromdatatable.png")]
    [LocalizedToolboxTooltip("activity_updatefromtable_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_updatefromtable", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_updatefromtable_helpurl", typeof(Resources.strings))]
    public class UpdateFromDataTable : CodeActivity
    {
        [RequiredArgument, Category("Input"), LocalizedDisplayName("activity_updatefromtable_tablename", typeof(Resources.strings)), LocalizedDescription("activity_updatefromtable_tablename_help", typeof(Resources.strings))]
        public InArgument<string> TableName { get; set; }
        [RequiredArgument, Category("Input"), LocalizedDisplayName("activity_updatefromtable_datatable", typeof(Resources.strings)), LocalizedDescription("activity_updatefromtable_datatable_help", typeof(Resources.strings))]
        public InArgument<DataTable> DataTable { get; set; }
        [Category("Output"), LocalizedDisplayName("activity_updatefromtable_result", typeof(Resources.strings)), LocalizedDescription("activity_updatefromtable_result_help", typeof(Resources.strings))]
        public OutArgument<int> Result { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var vars = context.DataContext.GetProperties();
            Connection connection = null;
            foreach (dynamic v in vars) { if (v.DisplayName == "conn") connection = v.GetValue(context.DataContext); }
            var tablename = TableName.Get(context);
            var datatable = DataTable.Get(context);

            var result = connection.UpdateDataTable(tablename, datatable);
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