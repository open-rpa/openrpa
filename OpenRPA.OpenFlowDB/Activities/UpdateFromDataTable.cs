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
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Data;
using Newtonsoft.Json;

namespace OpenRPA.OpenFlowDB
{
    [System.ComponentModel.Designer(typeof(UpdateFromDataTableDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.entity.png")]
    [LocalizedToolboxTooltip("activity_updatefromdataTable_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_updatefromdataTable", typeof(Resources.strings))]
    public class UpdateFromDataTable : AsyncTaskCodeActivity<JArray>
    {
        public InArgument<string> Uniqueness { get; set; }
        public InArgument<string> Type { get; set; }
        [RequiredArgument]
        public InArgument<string> Collection { get; set; } = "entities";
        [RequiredArgument]
        public InArgument<DataTable> DataTable { get; set; }
        public InArgument<string> EncryptFields { get; set; }
        private List<JObject> results = new List<JObject>();
        protected async override Task<JArray> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var encrypt = EncryptFields.Get(context);
            if (encrypt == null) encrypt = "";
            var collection = Collection.Get(context);
            if (string.IsNullOrEmpty(collection)) collection = "entities";
            var type = Type.Get(context);
            var uniqueness = Uniqueness.Get(context);

            var dt = DataTable.Get(context);

            foreach (DataRow row in dt.Rows)
            {
                if(row.RowState == DataRowState.Deleted)
                {

                    var _id = row["_id", DataRowVersion.Original].ToString();
                    await global.webSocketClient.DeleteOne(collection, _id);
                } 
                else if(row.RowState == DataRowState.Added || row.RowState == DataRowState.Modified)
                {
                    var settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        Converters = { new DataRowConverter() },
                    };
                    var rowJson = JsonConvert.SerializeObject(row, Formatting.Indented, settings);
                    var result = JObject.Parse(rowJson);
                    if (!string.IsNullOrEmpty(encrypt))
                    {
                        result["_encrypt"] = encrypt;
                    }
                    var name = result.GetValue("name", StringComparison.OrdinalIgnoreCase)?.Value<string>();
                    result["name"] = name;
                    if (!string.IsNullOrEmpty(type))
                    {
                        result["_type"] = type;
                    }
                    var _result = await global.webSocketClient.InsertOrUpdateOne(collection, 1, false, uniqueness, result);
                    results.Add(_result);
                }
            }
            dt.AcceptChanges();
            System.Windows.Forms.Application.DoEvents();
            return new JArray(results);
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
    }
}
