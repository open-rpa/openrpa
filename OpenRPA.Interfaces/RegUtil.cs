using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public static class RegUtil
    {
        public static Func<string, bool> hklmExists = delegate (string KeyLocation)
        {
            Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(KeyLocation);
            if ((rk) == null) return false;
            return true;
        };
        public static Action<string> hklmCreate = delegate (string KeyLocation)
        {
            Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(KeyLocation);
        };
        public static Func<string, bool> hkcuExists = delegate (string KeyLocation)
        {
            Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(KeyLocation);
            if ((rk) == null) return false;
            return true;
        };
        public static Action<string> hkcuCreate = delegate (string KeyLocation)
        {
            Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(KeyLocation);
        };
        public static Func<string, string, bool> ensureValue = delegate (string KeyLocation, string defaultValue)
        {
            Microsoft.Win32.RegistryKey rk = (Microsoft.Win32.RegistryKey)Microsoft.Win32.Registry.GetValue(KeyLocation, defaultValue, null);
            if ((rk) == null) return false;
            return true;
        };
        public static Action<string, string, object> hkcuSetValue = delegate (string KeyLocation, string Key, object Value)
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(KeyLocation, true);
            key.SetValue(Key, Value);
        };
    }
}
