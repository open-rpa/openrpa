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
    [Designer(typeof(DeleteAllRowsDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.deleteallrows.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class DeleteAllRows : CodeActivity
    {
        [RequiredArgument]
        public InArgument<DataTable> DataTable { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var dt = DataTable.Get(context);
            int count = dt.Rows.Count;
            for(var i= count-1; i >= count; i--) dt.Rows[i].Delete();
            //foreach(DataRow row in dt.Rows.)
            //{
            //    row.Delete();
            //}
        }

    }


}