using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Newtonsoft.Json;

namespace OpenRPA.Storage.LiteDB
{
    public class UseIgnoreMapper : BsonMapper
    {
        protected override IEnumerable<MemberInfo> GetTypeMembers(Type type)
        {
            var before = base.GetTypeMembers(type);
            //var after = before.Where(m => !m.IsDefined(typeof(NonSerializedAttribute), true) && !m.IsDefined(typeof(JsonIgnoreAttribute), true));
            var after = before.Where(m => !m.IsDefined(typeof(NonSerializedAttribute), true) &&
            !m.CustomAttributes.Select(x => x.AttributeType.Name).Contains("BsonIgnoreAttribute"));
            return after;
            ;
        }
    }
}
