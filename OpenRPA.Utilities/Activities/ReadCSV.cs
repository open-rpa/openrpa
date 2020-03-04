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

namespace OpenRPA.Utilities
{
    [Designer(typeof(ReadCSVDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.readexcel.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class ReadCSV : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Filename { get; set; }
        public InArgument<string> Delimeter { get; set; }
        public InArgument<bool> UseHeaderRow { get; set; } = true;
        [RequiredArgument]
        public OutArgument<System.Data.DataTable> DataTable { get; set; }
        private static DataTable GetDataTabletFromCSVFile(string csv_file_path, bool useHeaderRow, string[] Delimiters)
        {
            DataTable csvData = new DataTable();
            using (TextFieldParser csvReader = new TextFieldParser(csv_file_path))
            {
                csvReader.SetDelimiters(Delimiters);
                csvReader.HasFieldsEnclosedInQuotes = true;
                if (useHeaderRow)
                {
                    string[] colFields = csvReader.ReadFields();
                    foreach (string column in colFields)
                    {
                        DataColumn datecolumn = new DataColumn(column);
                        datecolumn.AllowDBNull = true;
                        csvData.Columns.Add(datecolumn);
                    }
                }
                bool firstrow = true;
                while (!csvReader.EndOfData)
                {
                    string[] fieldData = csvReader.ReadFields();
                    if (firstrow && !useHeaderRow)
                    {
                        foreach (string column in fieldData)
                        {
                            DataColumn datecolumn = new DataColumn();
                            datecolumn.AllowDBNull = true;
                            csvData.Columns.Add(datecolumn);
                        }
                    }

                    //Making empty value as null
                    for (int i = 0; i < fieldData.Length; i++)
                    {
                        if (fieldData[i] == "")
                        {
                            fieldData[i] = null;
                        }
                    }
                    csvData.Rows.Add(fieldData);
                    firstrow = false;
                }
            }
            return csvData;
        }
        protected override void Execute(CodeActivityContext context)
        {
            var useHeaderRow = false;
            if (UseHeaderRow != null) useHeaderRow = UseHeaderRow.Get(context);
            string[] delimeters = null;
            if (Delimeter != null)
            {
                var d= Delimeter.Get(context);
                if(!string.IsNullOrEmpty(d))
                {
                    delimeters = new string[] { d };
                }
                
            }
            if (delimeters == null || delimeters.Length == 0) delimeters = new string[] { ",", ";" };
            System.Data.DataTable result = null;
            var filename = Filename.Get(context);
            filename = Environment.ExpandEnvironmentVariables(filename);
            result = GetDataTabletFromCSVFile(filename, useHeaderRow, delimeters);
            if (DataTable != null) context.SetValue(DataTable, result);
        }

    }
}