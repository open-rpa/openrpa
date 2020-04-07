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
using System.Data;
using Microsoft.VisualBasic.FileIO;
using System.Text.RegularExpressions;
using System.Windows;

namespace OpenRPA.Utilities
{
    [Designer(typeof(AddDataRowDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.database.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class AddDataRow  : CodeActivity
    {
        [RequiredArgument]
        public InArgument<DataTable> DataTable { get; set; }
        [RequiredArgument]
        public InArgument<object[]> RowData { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var dt = DataTable.Get(context);
            var data = RowData.Get(context);
            dt.Rows.Add(data);
            // dt.Columns.Add(data);
        }

    }


}