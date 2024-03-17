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
            //var after = before.Where(m => !m.IsDefined(typeof(NonSerializedAttribute), true) &&
            //    !m.IsDefined(typeof(JsonIgnoreAttribute), true));
            var after = before.Where(m => !m.IsDefined(typeof(NonSerializedAttribute), true) &&
            !m.CustomAttributes.Select(x => x.AttributeType.Name).Contains("BsonIgnoreAttribute"));
            var names = after.Select(m => m.Name).ToList();
            var names2 = before.Select(m => m.DeclaringType.Name).ToList();
            return after;
            ;
        }
        //protected override EntityMapper BuildEntityMapper(Type type)
        //{
        //    // var ignoreAttr = typeof(BsonIgnoreAttribute);
        //    var ignoreAttr = typeof(JsonIgnoreAttribute);

        //    var mapper = base.BuildEntityMapper(type);
        //    foreach(var m in mapper.Members)
        //    {
        //        var members = this.GetTypeMembers(m.UnderlyingType);
        //        var memberInfo = members.Where(x => x.Name == m.FieldName).FirstOrDefault();
        //        if (memberInfo == null) continue;
                
        //        if (CustomAttributeExtensions.IsDefined(memberInfo, ignoreAttr, true)) continue;
        //    }

        //    var id = m.Members.SingleOrDefault(x => x.Name == "_id");

        //    if (id != null)
        //    {
        //        // clone same id properties into new member
        //        var member = new MemberMapper
        //        {
        //            AutoId = false,
        //            FieldName = id.MemberName, // keep FieldName == MemberName
        //            MemberName = id.MemberName,
        //            DataType = id.DataType,
        //            UnderlyingType = id.DataType,
        //            Getter = id.Getter,
        //            Setter = id.Setter
        //        };

        //        // add this new Member
        //        mapper.Members.Add(member);
        //    }

        //    return mapper;
        //}
    }
}
