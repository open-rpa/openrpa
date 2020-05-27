using SAPFEWSELib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.SAP
{
    public static class Extensions
    {
        public static string GetDetailType(this GuiComponent comp)
        {
            if (comp.Type == "GuiSplitterShell")
            {
                return "GuiSplit";
            }
            else if (comp is GuiShell)
            {
                string type = "Gui" + (comp as GuiShell).SubType;
                if (type == "GuiTextEdit")
                    type = "GuiTextedit";
                if (type == "GuiToolbar")
                    type = "GuiToolbarControl";
                return type;
            }
            else
            {
                return comp.Type;
            }
        }

    }
}
