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
    public class ExecuteNonQuery : NativeActivity
    {
        [RequiredArgument, LocalizedDisplayName("activity_executenonquery_query", typeof(Resources.strings)), LocalizedDescription("activity_executenonquery_query_help", typeof(Resources.strings))]
        public InArgument<string> Query { get; set; }
        //[RequiredArgument, Browsable(false)]
        //public InArgument<Connection> Connection { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            var query = Query.Get(context);
            var connection = context.Properties.Find("Connection");
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            Interfaces.Extensions.AddCacheArgument(metadata, "Query", Query);
            base.CacheMetadata(metadata);
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