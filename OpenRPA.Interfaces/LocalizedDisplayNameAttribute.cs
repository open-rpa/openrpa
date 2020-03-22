using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public class LocalizedDisplayNameAttribute : DisplayNameAttribute
    {
        private readonly PropertyInfo nameProperty;
        public LocalizedDisplayNameAttribute(string displayNameKey, Type resourceType = null) : base(displayNameKey)
        {
            if (resourceType != null) nameProperty = resourceType.GetProperty(base.DisplayName, BindingFlags.Static | BindingFlags.Public);
        }
        public override string DisplayName
        {
            get
            {
                if (nameProperty == null) return base.DisplayName;
                return (string)nameProperty.GetValue(nameProperty.DeclaringType, null);
            }
        }
    }
}
