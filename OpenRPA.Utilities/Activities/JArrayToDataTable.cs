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
        protected override void Execute(CodeActivityContext context)
        {
            var jarray = JArray.Get(context);
            // System.Data.DataTable dt = (System.Data.DataTable)JsonConvert.DeserializeObject(jarray.ToString(), (typeof(System.Data.DataTable)));
            DataTable dt = jarray.ToDataTable();
            DataTable.Set(context, dt);

        }
    }
}