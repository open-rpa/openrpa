using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public virtual string Text { get; set; }

    }
    public class LocalizedToolboxTooltipAttribute : ToolboxTooltipAttribute
    {
        public LocalizedToolboxTooltipAttribute() { }
        private readonly PropertyInfo nameProperty;
        public LocalizedToolboxTooltipAttribute(string Text, Type resourceType = null) : base(Text)
        {
            if (resourceType != null) nameProperty = resourceType.GetProperty(base.Text, BindingFlags.Static | BindingFlags.Public);
        }
        public override string Text
        {
            get
            {
                if (nameProperty == null) return base.Text;
                return (string)nameProperty.GetValue(nameProperty.DeclaringType, null);
            }
        }
    }

}
