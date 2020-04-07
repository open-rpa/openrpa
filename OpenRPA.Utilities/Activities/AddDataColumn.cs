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
    [Designer(typeof(AddDataColumnDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.database.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class AddDataColumn : CodeActivity
    {
        [RequiredArgument]
        public InArgument<DataTable> DataTable { get; set; }
        [RequiredArgument]
        public InArgument<string> ColumnName { get; set; }
        public Type TargetType { get; set; }
        public bool AllowDBNull { get; set; } = true;
        public bool AutoIncrement { get; set; }
        public object DefaultValue { get; set; }
        public int MaxLength { get; set; }
        public bool Unique { get; set; }

        // https://www.neovolve.com/2010/09/30/creating-updatable-generic-windows-workflow-activities/
        protected override void Execute(CodeActivityContext context)
        {
            var dt = DataTable.Get(context);
            var name = ColumnName.Get(context);
            var col = new DataColumn(name, TargetType);
            col.AllowDBNull = AllowDBNull;
            col.AutoIncrement = AutoIncrement;
            col.DefaultValue = DefaultValue;
            try
            {
                col.MaxLength = MaxLength;
            }
            catch (Exception)
            {
            }
            col.Unique = Unique;
            dt.Columns.Add(col);
        }

    }


}