using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Database
{
    class CommandTypeEditor : CustomSelectEditor
    {
        public override DataTable options
        {
            get
            {
                DataTable lst = new DataTable();
                lst.Columns.Add("ID", typeof(string));
                lst.Columns.Add("TEXT", typeof(string));
                lst.Rows.Add("Text", "Text");
                lst.Rows.Add("StoredProcedure", "StoredProcedure");
                lst.Rows.Add("TableDirect", "TableDirect");
                return lst;
            }
        }
    }
}
