using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRPA.NM
{
    public static class Extensions
    {
        public static Task WaitOneAsync(this WaitHandle waitHandle)
        {
            if (waitHandle == null) throw new ArgumentNullException("waitHandle");
            var tcs = new TaskCompletionSource<bool>();
            var rwh = ThreadPool.RegisterWaitForSingleObject(waitHandle, delegate { tcs.TrySetResult(true); }, null, -1, true);
            var t = tcs.Task;
            t.ContinueWith((antecedent) => rwh.Unregister(null));
            return t;
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

    }
}
