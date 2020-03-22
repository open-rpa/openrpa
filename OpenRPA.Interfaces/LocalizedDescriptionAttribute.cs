using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        private readonly PropertyInfo nameProperty;
        public LocalizedDescriptionAttribute(string DescriptionKey, Type resourceType = null) : base(DescriptionKey)
        {
            if (resourceType != null) nameProperty = resourceType.GetProperty(base.Description, BindingFlags.Static | BindingFlags.Public);
        }
        public override string Description
        {
            get
            {
                if (nameProperty == null) return base.Description;
                return (string)nameProperty.GetValue(nameProperty.DeclaringType, null);
            }
        }
    }
}
