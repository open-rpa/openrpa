using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsAccessBridgeInterop;

namespace OpenRPA.Java
{
    public class HwndCache
    {
        private readonly ConcurrentDictionary<IntPtr, AccessibleWindow> _cache = new ConcurrentDictionary<IntPtr, AccessibleWindow>();

        public AccessibleWindow Get(AccessBridge accessBridge, IntPtr hwnd)
        {
            return _cache.GetOrAdd(hwnd, key => accessBridge.CreateAccessibleWindow(key));
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public IEnumerable<AccessibleWindow> Windows
        {
            get { return _cache.Values.Where(x => x != null); }
        }
    }
}
