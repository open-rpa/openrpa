//using ClosedXML.Excel;
//using ExcelDataReader;
using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Office.Interop;
using Microsoft.Office.Interop.Excel;
//using System.Windows.Forms;

namespace OpenRPA.Office.Activities
{
    [System.ComponentModel.Designer(typeof(ReadRangeDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ReadRange), "Resources.toolbox.readexcel.png")]
    //[designer.ToolboxTooltip(Text = "Read CSV, xls or xlsx file and loads it into a DataSet")]
    public class ReadRange : ExcelActivity
    {
        public ReadRange()
        {
            UseHeaderRow = false;
            ClearFormats = false;
        }
        [RequiredArgument]
        [System.ComponentModel.Category("Misc")]
        public InArgument<bool> UseHeaderRow { get; set; }
        [RequiredArgument]
        [System.ComponentModel.Category("Misc")]
        public InArgument<bool> ClearFormats { get; set; }

        //[RequiredArgument]
        [System.ComponentModel.Category("Input")]
        public InArgument<string> Cells { get; set; }
        [System.ComponentModel.Category("Output")]
        public OutArgument<System.Data.DataTable> DataTable { get; set; }
        [System.ComponentModel.Category("Output")]
        public OutArgument<int> lastUsedRow { get; set; }
        [System.ComponentModel.Category("Output")]
        public OutArgument<string> lastUsedColumn { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            //Range xlActiveRange = base.worksheet.UsedRange;

            var useHeaderRow = (UseHeaderRow != null? UseHeaderRow.Get(context)  : false);
            base.Execute(context);
            var cells = Cells.Get(context);
            Microsoft.Office.Interop.Excel.Range range = null;
            if (string.IsNullOrEmpty(cells))
            {
                range = base.worksheet.UsedRange;

                //Range last = base.worksheet.Cells.SpecialCells(XlCellType.xlCellTypeLastCell, Type.Missing);
                //Range range = base.worksheet.get_Range("A1", last);

                //int lastUsedRow = range.Row;
                //int lastUsedColumn = range.Column;
            }
            else
            {
                range = base.worksheet.get_Range(cells);
            }
            //object[,] valueArray = (object[,])range.Value;
            object[,] valueArray = (object[,])range.get_Value(Microsoft.Office.Interop.Excel.XlRangeValueDataType.xlRangeValueDefault);

                
            var o = ProcessObjects(useHeaderRow, valueArray);

            System.Data.DataTable dt = o as System.Data.DataTable;
            dt.TableName = base.worksheet.Name;
            if (string.IsNullOrEmpty(dt.TableName)) { dt.TableName = "Unknown";  }
            DataTable.Set(context, dt);

            //dt.AsEnumerable();

            //string json = Newtonsoft.Json.JsonConvert.SerializeObject(dt, Newtonsoft.Json.Formatting.Indented);
            ////context.SetValue(Json, JObject.Parse(json));
            //context.SetValue(Json, JArray.Parse(json));

            if (ClearFormats.Get(context))
            {
                worksheet.Columns.ClearFormats();
                worksheet.Rows.ClearFormats();
            }

            if (lastUsedColumn!=null || lastUsedRow!=null)
            {

                // Unhide All Cells and clear formats

                // Detect Last used Row - Ignore cells that contains formulas that result in blank values
                //int lastRowIgnoreFormulas = worksheet.Cells.Find(
                //                "*",
                //                System.Reflection.Missing.Value,
                //                XlFindLookIn.xlValues,
                //                XlLookAt.xlWhole,
                //                XlSearchOrder.xlByRows,
                //                XlSearchDirection.xlPrevious,
                //                false,
                //                System.Reflection.Missing.Value,
                //                System.Reflection.Missing.Value).Row;
                // Detect Last Used Column  - Ignore cells that contains formulas that result in blank values
                //int lastColIgnoreFormulas = worksheet.Cells.Find(
                //                "*",
                //System.Reflection.Missing.Value,
                //                System.Reflection.Missing.Value,
                //                System.Reflection.Missing.Value,
                //                XlSearchOrder.xlByColumns,
                //                XlSearchDirection.xlPrevious,
                //                false,
                //                System.Reflection.Missing.Value,
                //                System.Reflection.Missing.Value).Column;

                // Detect Last used Row / Column - Including cells that contains formulas that result in blank values
                //int lastColIncludeFormulas = worksheet.UsedRange.Columns.Count;
                //int lastColIncludeFormulas = worksheet.UsedRange.Rows.Count;



                //range = base.worksheet.UsedRange;
                int _lastUsedColumn = worksheet.UsedRange.Columns.Count;
                int _lastUsedRow = worksheet.UsedRange.Rows.Count;
                if (lastUsedColumn != null) context.SetValue(lastUsedColumn, ColumnIndexToColumnLetter(_lastUsedColumn));
                if (lastUsedRow != null) context.SetValue(lastUsedRow, _lastUsedRow);
            }

        }

        static string ColumnIndexToColumnLetter(int colIndex)
        {
            int div = colIndex;
            string colLetter = String.Empty;
            int mod = 0;

            while (div > 0)
            {
                mod = (div - 1) % 26;
                colLetter = (char)(65 + mod) + colLetter;
                div = (int)((div - mod) / 26);
            }
            return colLetter;
        }


        private System.Data.DataTable ProcessObjects(bool useHeaderRow, object[,] valueArray)
        {
            System.Data.DataTable dt = new System.Data.DataTable();

            #region Get the COLUMN names
            if(useHeaderRow)
            {
                for (int k = 1; k <= valueArray.GetLength(1); k++)
                {
                    dt.Columns.Add((string)valueArray[1, k]);  //add columns to the data table.
                }
            }
            else
            {
                for (int k = 1; k <= valueArray.GetLength(1); k++)
                {
                    dt.Columns.Add(k.ToString());  //add columns to the data table.
                }

            }
            #endregion

            #region Load Excel SHEET DATA into data table

            object[] singleDValue = new object[valueArray.GetLength(1)];
            //value array first row contains column names. so loop starts from 2 instead of 1
            for (int i = 2; i <= valueArray.GetLength(0); i++)
            {
                for (int j = 0; j < valueArray.GetLength(1); j++)
                {
                    if (valueArray[i, j + 1] != null)
                    {
                        singleDValue[j] = valueArray[i, j + 1].ToString();
                    }
                    else
                    {
                        singleDValue[j] = valueArray[i, j + 1];
                    }
                }
                dt.LoadDataRow(singleDValue, System.Data.LoadOption.PreserveChanges);
            }
            #endregion


            return (dt);
        }


    }
}