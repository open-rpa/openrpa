using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Utilities
{
    public static class Extensions
    {
        public static void ToCSV(this DataTable dtDataTable, string strFilePath, string delimeter = ";", bool includeHeader = true)
        {
            System.IO.StreamWriter sw = new System.IO.StreamWriter(strFilePath, false, Encoding.UTF8 );
            if (includeHeader)
            {
                for (int i = 0; i < dtDataTable.Columns.Count; i++)
                {
                    sw.Write(dtDataTable.Columns[i]);
                    if (i < dtDataTable.Columns.Count - 1)
                    {
                        sw.Write(delimeter);
                    }
                }
                sw.Write(sw.NewLine);
            }
            foreach (DataRow dr in dtDataTable.Rows)
            {
                for (int i = 0; i < dtDataTable.Columns.Count; i++)
                {
                    if (!Convert.IsDBNull(dr[i]))
                    {
                        string value = dr[i].ToString();
                        if (value.Contains(delimeter))
                        {
                            value = String.Format("\"{0}\"", value);
                            sw.Write(value);
                        }
                        else
                        {
                            sw.Write(dr[i].ToString());
                        }
                    }
                    if (i < dtDataTable.Columns.Count - 1)
                    {
                        sw.Write(delimeter);
                    }
                }
                sw.Write(sw.NewLine);
            }
            sw.Close();
        }
    }
}
