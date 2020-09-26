using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.PS
{
    public static class Extensions
    {
        public static string toJson(this PSObject SourceObject)
        {
            //var requestObject = new ExpandoObject() as IDictionary<string, Object>;
            //foreach (var pair in SourceObject.Properties)
            //{
            //    // && pair.MemberType != PSMemberTypes.CodeMethod && pair.MemberType != PSMemberTypes.ScriptMethod && pair.MemberType != PSMemberTypes.Event
            //    if (pair.MemberType != PSMemberTypes.Method && pair.MemberType != PSMemberTypes.Methods
            //        && pair.MemberType != PSMemberTypes.ScriptMethod && pair.MemberType != PSMemberTypes.Event
            //        && pair.MemberType != PSMemberTypes.CodeMethod)
            //    {

            //        requestObject.Add(pair.Name, pair.Value);
            //    }

            //}
            //return JsonConvert.SerializeObject(requestObject);
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Error = (serializer, err) =>
            {
                err.ErrorContext.Handled = true;
            };
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            if (string.IsNullOrEmpty(SourceObject.BaseObject.ToString()))
            {
                //var result = new JArray();
                var result = new JObject();
                foreach (var p in SourceObject.Properties)
                {
                    var token = JToken.Parse(JsonConvert.SerializeObject(p.Value, settings));
                    result.Add(p.Name, token);
                }
                return result.ToString();
            }
            else
            {
                var json = JsonConvert.SerializeObject(SourceObject.BaseObject, settings);

                return json;

            }
        }
        public static PSObject JsonToPSObject(this string json)
        {
            dynamic content = (ExpandoObject)JsonConvert.DeserializeObject(json, typeof(ExpandoObject));
            //WriteObject(content);
            PSObject responseObject = new PSObject();
            foreach (var pair in content)
            {
                if (pair.Key != "@odata.context")
                {
                    responseObject.Members.Add(new PSNoteProperty(pair.Key, pair.Value));
                }
            }
            return responseObject;
        }
        public static PSObject toPSObject(this JObject o)
        {
            dynamic content = (ExpandoObject)JsonConvert.DeserializeObject(o.ToString(), typeof(ExpandoObject));
            //WriteObject(content);
            PSObject responseObject = new PSObject();
            foreach (var pair in content)
            {
                if (pair.Key != "@odata.context")
                {
                    responseObject.Members.Add(new PSNoteProperty(pair.Key, pair.Value));
                }
            }
            return responseObject;
        }
        public static bool IsUpper(this string value)
        {
            // Consider string to be uppercase if it has no lowercase letters.
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsLower(value[i]))
                {
                    return false;
                }
            }
            return true;
        }
        public static bool IsLower(this string value)
        {
            // Consider string to be lowercase if it has no uppercase letters.
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsUpper(value[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
