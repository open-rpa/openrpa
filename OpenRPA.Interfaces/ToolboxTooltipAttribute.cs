using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public class ToolboxTooltipAttribute : Attribute
    {
        public ToolboxTooltipAttribute() { }
        public ToolboxTooltipAttribute(string Text)
        {
            this.Text = Text;
        }
        public string Text { get; set; }

    }
}
