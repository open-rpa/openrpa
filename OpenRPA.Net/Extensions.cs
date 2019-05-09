using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public static class Extensions
    {
        public static IEnumerable<string> Split(this string str, int chunkSize)
        {
            // return Enumerable.Range(0, str.Length / chunkSize).Select(i => str.Substring(i * chunkSize, chunkSize));
            return Enumerable.Range(0, (str.Length + chunkSize - 1) / chunkSize).Select(i => str.Substring(i * chunkSize, Math.Min(str.Length - i * chunkSize, chunkSize)));
        }
        public static Task WaitOneAsync(this WaitHandle waitHandle)
        {
            if (waitHandle == null) throw new ArgumentNullException("waitHandle");
            var tcs = new TaskCompletionSource<bool>();
            var rwh = ThreadPool.RegisterWaitForSingleObject(waitHandle, delegate { tcs.TrySetResult(true); }, null, -1, true);
            var t = tcs.Task;
            t.ContinueWith((antecedent) => rwh.Unregister(null));
            return t;
        }
    }
}
