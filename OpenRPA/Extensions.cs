using OpenRPA.Net;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public class global
    {
        public static WebSocketClient webSocketClient = null;
        public static bool isConnected
        {
            get
            {
                if (webSocketClient == null || !webSocketClient.isConnected ) return false;
                return true;
            }
        }
    }
    public static class Extensions
    {
        public static string projectsDirectory
        {
            get
            {
                return System.IO.Directory.GetCurrentDirectory();
            }
        }
        public static bool TryCast<T>(this object obj, out T result)
        {
            if (obj is T)
            {
                result = (T)obj;
                return true;
            }
            if (obj is System.Activities.Expressions.Literal<T>)
            {
                result = (T)((System.Activities.Expressions.Literal<T>)obj).Value;
                return true;
            }

            result = default(T);
            return false;
        }
        public static T TryCast<T>(this object obj)
        {
            T result = default(T);
            if (TryCast<T>(obj, out result))
                return result;
            return result;
        }
        public static T GetValue<T>(this System.Activities.Presentation.Model.ModelItem model, string name)
        {
            T result = default(T);
            if (model.Properties[name] != null)
            {
                if (model.Properties[name].Value == null) return result;
                if (model.Properties[name].Value.Properties["Expression"] != null)
                {
                    result = model.Properties[name].Value.Properties["Expression"].ComputedValue.TryCast<T>();
                    return result;
                }
                result = model.Properties[name].ComputedValue.TryCast<T>();
                return result;
            }
            return result;
        }
        public static bool IsSerializable2(this Type t)
        {
            return Attribute.IsDefined(t, typeof(System.Runtime.Serialization.DataContractAttribute)) || t.IsSerializable;

        }
    }
}
