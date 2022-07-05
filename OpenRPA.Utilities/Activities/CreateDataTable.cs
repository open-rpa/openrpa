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
    [Designer(typeof(CreateDataTableDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.createdatatable.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class CreateDataTable : CodeActivity
    {
        [RequiredArgument]
        public OutArgument<DataTable> DataTable { get; set; }
        [RequiredArgument]
        public InArgument<string[]> ColumnNames { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var dt = new DataTable();
            foreach(var column in ColumnNames.Get(context))
            {
                dt.Columns.Add(column);
            }
            DataTable.Set(context, dt);
        }

    }


}