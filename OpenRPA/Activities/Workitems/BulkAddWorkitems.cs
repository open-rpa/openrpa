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
using OpenTelemetry.Trace;

namespace OpenRPA.WorkItems
{
    [System.ComponentModel.Designer(typeof(BulkAddWorkitemsDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.bulkaddworkitems.png")]
    [LocalizedToolboxTooltip("activity_bulkaddworkitems_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_bulkaddworkitems", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_bulkaddworkitems_helpurl", typeof(Resources.strings))]
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
        [LocalizedDisplayName("activity_bulkaddworkitems_bulksize", typeof(Resources.strings)), LocalizedDescription("activity_bulkaddworkitems_bulksize_help", typeof(Resources.strings))]
        public InArgument<int> BulkSize { get; set; }
        [LocalizedDisplayName("activity_addworkitem_success_wiq", typeof(Resources.strings)), LocalizedDescription("activity_addworkitem_success_wiq_help", typeof(Resources.strings))]
        public InArgument<string> Success_wiq { get; set; }
        [LocalizedDisplayName("activity_addworkitem_failed_wiq", typeof(Resources.strings)), LocalizedDescription("activity_addworkitem_failed_wiq_help", typeof(Resources.strings))]
        public InArgument<string> Failed_wiq { get; set; }
        protected async override Task<object> ExecuteAsync(AsyncCodeActivityContext context)
        {
            string WorkflowInstanceId = context.WorkflowInstanceId.ToString();
            var instance = global.OpenRPAClient.GetWorkflowInstanceByInstanceId(WorkflowInstanceId);
            string traceId = instance?.TraceId; string spanId = instance?.SpanId;
            var _wiqid = wiqid.Get(context);
            var _wiq = wiq.Get(context);
            var dt = DataTable.Get(context);
            var priority = Priority.Get<int>(context);
            var bulksize = BulkSize.Get<int>(context);
            if (bulksize < 1) bulksize = 50;
            var nextrun = NextRun.Get<DateTime?>(context);
            var items = new List<OpenRPA.Interfaces.AddWorkitem>();
            var filefields = Filefields.Get<string[]>(context);
            if (filefields == null) filefields = new string[] { };
            for (var i = 0; i < filefields.Length; i++) filefields[i] = filefields[i].ToLower();
            var counter = 0;
            var bulkcounter = 0;
            if (dt == null)
            {
                Log.Warning("BulkAddWorkitems: Datatable is null");
                return null;
            }
            if (dt.Rows == null)
            {
                Log.Warning("BulkAddWorkitems: Datatable contains no rows");
                return null;
            }
            foreach (DataRow row in dt.Rows)
            {
                counter++;
                bulkcounter++;
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
                    if (filefields.Contains(columnname))
                    {
                        if (field.DataType == typeof(string))
                        {
                            _files.Add(new MessageWorkitemFile() { filename = row[field.ColumnName].ToString() });
                        }
                    }
                }
                wi.files = _files.ToArray();
                items.Add(wi);
                if (bulkcounter >= bulksize)
                {
                    await global.webSocketClient.AddWorkitems(_wiqid, _wiq, items.ToArray(), traceId, spanId);
                    items.Clear();
                }
            }
            if (items.Count > 0) await global.webSocketClient.AddWorkitems(_wiqid, _wiq, items.ToArray(),
                Success_wiq.Get<string>(context), null, Failed_wiq.Get<string>(context), null, traceId, spanId);
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