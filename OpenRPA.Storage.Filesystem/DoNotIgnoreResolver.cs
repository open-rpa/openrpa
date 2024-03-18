using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Storage.Filesystem
{
    public class DoNotIgnoreResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (property.PropertyName == "isLocalOnly")
            {
                property.Ignored = false;
            }
            if (property.PropertyName == "isDirty")
            {
                property.Ignored = false;
            }
            if (property.PropertyName == "isDeleted")
            {
                property.Ignored = false;
            }
            if (property.PropertyName == "current_version")
            {
                property.Ignored = false;
            }
            if (property.PropertyName == "RelativeFilename")
            {
                property.Ignored = false;
            }
            if (property.PropertyName == "State")
            {
                property.Ignored = false;
            }
            if (property.PropertyName == "IsExpanded")
            {
                property.Ignored = false;
            }
            if (property.PropertyName == "IsSelected")
            {
                property.Ignored = false;
            }
            return property;
        }
    }

}
