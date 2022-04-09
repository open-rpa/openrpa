using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRPA.Interfaces.Input;
using OpenRPA.Interfaces.entity;
using System.Data;

namespace OpenRPA.WorkItems
{
    [System.ComponentModel.Designer(typeof(BulkAddWorkitemsDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.bulkaddworkitems.png")]
    [LocalizedToolboxTooltip("activity_bulkaddworkitems_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_bulkaddworkitems", typeof(Resources.strings))]
    public class BulkAddWorkitems : AsyncTaskCodeActivity
    {
        [RequiredArgument, LocalizedDisplayName("activity_bulkaddworkitems_wiqid", typeof(Resources.strings)), LocalizedDescription("activity_bulkaddworkitems_wiqid_help", typeof(Resources.strings)), OverloadGroup("By ID")]
        public InArgument<string> wiqid { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_bulkaddworkitems_wiq", typeof(Resources.strings)), LocalizedDescription("activity_bulkaddworkitems_wiq_help", typeof(Resources.strings)), OverloadGroup("By Name")]
        public InArgument<string> wiq { get; set; }
        [RequiredArgument, LocalizedDisplayName("activity_bulkaddworkitems_datatable", typeof(Resources.strings)), LocalizedDescription("activity_bulkaddworkitems_datatable_help", typeof(Resources.strings))]
        public InArgument<System.Data.DataTable> DataTable { get; set; }
        [LocalizedDisplayName("activity_bulkaddworkitems_priority", typeof(Resources.strings)), LocalizedDescription("activity_bulkaddworkitems_priority_help", typeof(Resources.strings))]
        public InArgument<int> Priority { get; set; }
        [LocalizedDisplayName("activity_bulkaddworkitems_nextrun", typeof(Resources.strings)), LocalizedDescription("activity_bulkaddworkitems_nextrun_help", typeof(Resources.strings))]
        public InArgument<DateTime?> NextRun { get; set; }
        [LocalizedDisplayName("activity_bulkaddworkitems_filefields", typeof(Resources.strings)), LocalizedDescription("activity_bulkaddworkitems_filefields_help", typeof(Resources.strings))]
        public InArgument<string[]> Filefields { get; set; }
        protected async override Task<object> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var _wiqid = wiqid.Get(context);
            var _wiq = wiq.Get(context);
            var dt = DataTable.Get(context);
            var priority = Priority.Get<int>(context);
            var nextrun = NextRun.Get<DateTime?>(context);
            var items = new List<OpenRPA.Interfaces.AddWorkitem>();
            var filefields = Filefields.Get<string[]>(context);
            if (filefields == null) filefields = new string[] { };
            for (var i = 0; i < filefields.Length; i++) filefields[i] = filefields[i].ToLower();
            var counter = 0;
            foreach (DataRow row in dt.Rows)
            {
                counter++;
                var wi = new Interfaces.AddWorkitem();
                wi.name = "Bulk added item " + counter.ToString();
                wi.priority = priority;
                wi.nextrun = nextrun;
                wi.payload = new Dictionary<string, object>();
                var _files = new List<MessageWorkitemFile>();
                foreach (DataColumn field in dt.Columns)
                {
                    if (string.IsNullOrEmpty(field.ColumnName)) continue;
                    var columnname = field.ColumnName.ToLower();
                    wi.payload.Add(columnname, row[field.ColumnName]);
                    if (columnname == "name") wi.name = row[field.ColumnName].ToString();
                    if(filefields.Contains(columnname))
                    {
                        if(field.DataType == typeof(string))
                        {
                            _files.Add(new MessageWorkitemFile() { filename = row[field.ColumnName].ToString() });
                        }
                    }
                }
                wi.files = _files.ToArray();
                items.Add(wi);
            }
            //if (t.payload == null) t.payload = new Dictionary<string, object>();
            //foreach (var item in Payload)
            //{
            //    t.payload.Add(item.Key, item.Value.Get(context));
            //}
            await global.webSocketClient.AddWorkitems(_wiqid, _wiq, items.ToArray());
            return null;
        }
        protected override void AfterExecute(AsyncCodeActivityContext context, object result)
        {
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