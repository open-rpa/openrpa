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

namespace OpenRPA.Utilities
{
    [Designer(typeof(ReadJSONDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.readjson.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class ReadJSON : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Filename { get; set; }
        [RequiredArgument]
        public OutArgument<System.Data.DataTable> DataTable { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var filename = Filename.Get(context);
            filename = Environment.ExpandEnvironmentVariables(filename);
            string json = System.IO.File.ReadAllText(filename);
            System.Data.DataTable dt = (System.Data.DataTable)JsonConvert.DeserializeObject(json, (typeof(System.Data.DataTable)));
            DataTable.Set(context, dt);

        }
    }
}