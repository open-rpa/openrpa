using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{

    public class HelpURLAttribute : Attribute
    {
        public HelpURLAttribute() { }
        public HelpURLAttribute(string HelpURL)
        {
            this.HelpURL = HelpURL;
        }
        public virtual string HelpURL { get; set; }

    }
    public class LocalizedHelpURLAttribute : HelpURLAttribute
    {
        private readonly PropertyInfo nameProperty;
        public LocalizedHelpURLAttribute(string HelpURLKey, Type resourceType) : base(HelpURLKey)
        {
            if (resourceType != null) nameProperty = resourceType.GetProperty(base.HelpURL, BindingFlags.Static | BindingFlags.Public);
        }
        public override string HelpURL
        {
            get
            {
                if (nameProperty == null) return base.HelpURL;
                return (string)nameProperty.GetValue(nameProperty.DeclaringType, null);
            }
        }
    }

}
