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
    [Designer(typeof(SetAllRowsAddedDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.setallrowsstate.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class SetAllRowsState : CodeActivity
    {
        [RequiredArgument]
        public InArgument<DataTable> DataTable { get; set; }
        [Editor(typeof(RowStateOptionsEditor), typeof(ExtendedPropertyValueEditor))]
        public InArgument<string> RowState { get; set; } = "NotModified";

        protected override void Execute(CodeActivityContext context)
        {
            var dt = DataTable.Get(context);
            var rowstate = RowState.Get(context);
            if (rowstate == "Added") foreach (DataRow row in dt.Rows) row.SetAdded();
            if (rowstate == "Modified") foreach (DataRow row in dt.Rows) row.SetModified();
            if (rowstate == "NotModified") dt.AcceptChanges();
        }
    }
    class RowStateOptionsEditor : CustomSelectEditor
    {
        public override DataTable options
        {
            get
            {
                DataTable lst = new DataTable();
                lst.Columns.Add("ID", typeof(string));
                lst.Columns.Add("TEXT", typeof(string));
                lst.Rows.Add("Added", "Added");
                lst.Rows.Add("Modified", "Modified");
                lst.Rows.Add("NotModified", "Not Modified");
                return lst;
            }
        }
    }


}