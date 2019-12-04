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
    [Designer(typeof(WriteExcelDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.writeexcel.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class WriteExcel : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Filename { get; set; }
        [RequiredArgument, OverloadGroup("DataSet")]
        public InArgument<System.Data.DataSet> DataSet { get; set; }
        [RequiredArgument, OverloadGroup("DataTable")]
        public InArgument<System.Data.DataTable> DataTable { get; set; }
        [RequiredArgument]
        public InArgument<bool> includeHeader { get; set; } = true;
        [RequiredArgument]
        [Editor(typeof(SelectNewEmailOptionsEditor), typeof(System.Activities.Presentation.PropertyEditing.ExtendedPropertyValueEditor))]
        public InArgument<string> Theme { get; set; } = "None";
        protected override void Execute(CodeActivityContext context)
        {
            ClosedXML.Excel.XLWorkbook wb = new ClosedXML.Excel.XLWorkbook();
            var ds = DataSet.Get(context);
            var dt = DataTable.Get(context);
            if(dt != null)
            {
                System.Data.DataTable table = dt;
                var name = table.TableName;
                if (string.IsNullOrEmpty(name)) name = "Sheet1";
                if (!includeHeader.Get(context))
                {
                    System.Data.DataTable t = new System.Data.DataTable() { TableName = name };
                    var sheet = wb.Worksheets.Add(t, name);
                    sheet.FirstRow().FirstCell().InsertData(table.Rows);
                    var table2 = sheet.Tables.FirstOrDefault();
                    if (table2 != null)
                    {
                        table2.ShowAutoFilter = false;
                        table2.Theme = ClosedXML.Excel.XLTableTheme.FromName(Theme.Get(context));
                    }

                }
                else
                {
                    var sheet = wb.Worksheets.Add(table, name);
                    var table2 = sheet.Tables.First();
                    table2.ShowAutoFilter = false;
                    table2.Theme = ClosedXML.Excel.XLTableTheme.FromName(Theme.Get(context));
                }
            } 
            else
            {
                int idx = 0;
                foreach (System.Data.DataTable table in ds.Tables)
                {
                    ++idx;
                    var name = table.TableName;
                    if (string.IsNullOrEmpty(name)) name = "Sheet" + idx.ToString();

                    if (!includeHeader.Get(context))
                    {
                        System.Data.DataTable t = new System.Data.DataTable() { TableName = name };
                        var sheet = wb.Worksheets.Add(t, name);
                        sheet.FirstRow().FirstCell().InsertData(table.Rows);
                        var table2 = sheet.Tables.FirstOrDefault();
                        if (table2 != null)
                        {
                            table2.ShowAutoFilter = false;
                            table2.Theme = ClosedXML.Excel.XLTableTheme.FromName(Theme.Get(context));
                        }

                    }
                    else
                    {
                        var sheet = wb.Worksheets.Add(table, name);
                        var table2 = sheet.Tables.First();
                        table2.ShowAutoFilter = false;
                        table2.Theme = ClosedXML.Excel.XLTableTheme.FromName(Theme.Get(context));
                    }
                }

            }
            var filename = Filename.Get(context);
            filename = Environment.ExpandEnvironmentVariables(filename);
            wb.SaveAs(filename);
        }
        class SelectNewEmailOptionsEditor : CustomSelectEditor
        {
            public override System.Data.DataTable options
            {
                get
                {
                    System.Data.DataTable lst = new System.Data.DataTable();
                    lst.Columns.Add("ID", typeof(string));
                    lst.Columns.Add("TEXT", typeof(string));
                    foreach (var t in ClosedXML.Excel.XLTableTheme.GetAllThemes())
                    {
                        lst.Rows.Add(t.Name, t.Name);
                    }
                    return lst;
                }
            }

        }
    }
}