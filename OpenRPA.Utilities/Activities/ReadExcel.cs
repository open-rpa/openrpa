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
    [Designer(typeof(ReadExcelDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.readexcel.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class ReadExcel : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Filename { get; set; }
        public InArgument<bool> UseHeaderRow { get; set; } = true;
        [RequiredArgument]
        public OutArgument<System.Data.DataSet> DataSet { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var useHeaderRow = false;
            if (UseHeaderRow != null) useHeaderRow = UseHeaderRow.Get(context);
            System.Data.DataSet result = null;
            var filename = Filename.Get(context);
            filename = Environment.ExpandEnvironmentVariables(filename);
            using (var stream = System.IO.File.Open(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                // Auto-detect format, supports:
                //  - Binary Excel files (2.0-2003 format; *.xls)
                //  - OpenXml Excel files (2007 format; *.xlsx)
                var conf = new ExcelDataReader.ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataReader.ExcelDataTableConfiguration
                    {
                        UseHeaderRow = useHeaderRow
                    }
                };
                using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream))
                {
                    // Choose one of either 1 or 2:
                    // 1. Use the reader methods
                    //do
                    //{
                    //    while (reader.Read())
                    //    {
                    //        // reader.GetDouble(0);
                    //    }
                    //} while (reader.NextResult());
                    // 2. Use the AsDataSet extension method
                    result = reader.AsDataSet(conf);
                }
            }
            if (DataSet != null) context.SetValue(DataSet, result);
        }
        //public static System.Data.DataSet ImportExcel(string filePath)
        //{
        //    var result = new System.Data.DataSet();
        //    // Open the Excel file using ClosedXML.
        //    // Keep in mind the Excel file cannot be open when trying to read it
        //    using (XLWorkbook workBook = new XLWorkbook(filePath))
        //    {
        //        foreach (IXLWorksheet workSheet in workBook.Worksheets)
        //        {
        //            //Create a new DataTable.
        //            System.Data.DataTable dt = new System.Data.DataTable();

        //            //Loop through the Worksheet rows.
        //            bool firstRow = true;
        //            foreach (IXLRow row in workSheet.Rows())
        //            {
        //                //Use the first row to add columns to DataTable.
        //                if (firstRow)
        //                {
        //                    foreach (IXLCell cell in row.Cells())
        //                    {
        //                        dt.Columns.Add(cell.Value.ToString());
        //                    }
        //                    firstRow = false;
        //                }
        //                else
        //                {
        //                    //Add rows to DataTable.
        //                    dt.Rows.Add();
        //                    int i = 0;
        //                    var first = row.FirstCellUsed();
        //                    var last = row.LastCellUsed();
        //                    if (first == null || last == null) continue;
        //                    foreach (IXLCell cell in row.Cells(first.Address.ColumnNumber, last.Address.ColumnNumber))
        //                    {
        //                        dt.Rows[dt.Rows.Count - 1][i] = cell.Value.ToString();
        //                        i++;
        //                    }
        //                }
        //            }

        //            result.Tables.Add(dt);
        //        }
        //    }
        //    return result;
        //}

    }
}