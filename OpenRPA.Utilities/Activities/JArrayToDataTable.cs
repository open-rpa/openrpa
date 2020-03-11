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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;

namespace OpenRPA.Utilities
{
    [Designer(typeof(JArrayToDataTableDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.readjson.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class JArrayToDataTable : CodeActivity
    {
        [RequiredArgument]
        public InArgument<JArray> JArray { get; set; }
        [RequiredArgument]
        public OutArgument<System.Data.DataTable> DataTable { get; set; }
        private static DataTable toDataTable(string json)
        {
            var result = new DataTable();
            var jArray = Newtonsoft.Json.Linq.JArray.Parse(json);
            //Initialize the columns, If you know the row type, replace this   
            foreach (var row in jArray)
            {
                foreach (var jToken in row)
                {
                    var jproperty = jToken as JProperty;
                    if (jproperty == null) continue;
                    if (result.Columns[jproperty.Name] == null)
                        result.Columns.Add(jproperty.Name, typeof(string));
                }
            }
            foreach (var row in jArray)
            {
                var datarow = result.NewRow();
                foreach (var jToken in row)
                {
                    var jProperty = jToken as JProperty;
                    if (jProperty == null) continue;
                    datarow[jProperty.Name] = jProperty.Value.ToString();
                }
                result.Rows.Add(datarow);
            }

            return result;
        }
        protected override void Execute(CodeActivityContext context)
        {
            var jarray = JArray.Get(context);
            // System.Data.DataTable dt = (System.Data.DataTable)JsonConvert.DeserializeObject(jarray.ToString(), (typeof(System.Data.DataTable)));
            DataTable dt = toDataTable(jarray.ToString());
            DataTable.Set(context, dt);

        }
    }
}