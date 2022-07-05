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
using ExcelDataReader;

namespace OpenRPA.Utilities
{
    [Designer(typeof(WriteCSVDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.writecsv.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class WriteCSV : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Filename { get; set; }
        [RequiredArgument]
        public InArgument<System.Data.DataTable> DataTable { get; set; }
        [RequiredArgument]
        public InArgument<bool> IncludeHeader { get; set; } = true;
        public InArgument<string> Delimeter { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var includeHeader = false;
            if (IncludeHeader != null) includeHeader = IncludeHeader.Get(context);

            var filename = Filename.Get(context);
            filename = Environment.ExpandEnvironmentVariables(filename);

            var delimeter = Delimeter.Get(context);
            if (string.IsNullOrEmpty(delimeter)) delimeter = ";";

            var dt = DataTable.Get(context);
            dt.ToCSV(filename, delimeter, includeHeader);
        }
    }
}