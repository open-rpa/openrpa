using Newtonsoft.Json;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.NM
{
    public class NMHook
    {
        public static event Action<NativeMessagingMessage> onMessage;
        public static event Action<string> onDisconnected;
        public static event Action<string> Connected;

        //public static List<rpa.pipe.NamedPipeClient> pipeclients = new List<rpa.pipe.NamedPipeClient>();
        //private static NamedPipeClientAsync<NativeMessagingMessage> chromepipe = null;
        //private static NamedPipeClientAsync<NativeMessagingMessage> ffpipe = null;
        public const string PIPE_NAME = "zeniverse_nativebridge";

        //public static bool chromeconnected
        //{
        //    get
        //    {
        //        if (chromepipe == null) return false;
        //        return chromepipe.isConnected;
        //    }
        //}
        //public static bool ffconnected
        //{
        //    get
        //    {
        //        if (ffpipe == null) return false;
        //        return ffpipe.isConnected;
        //    }
        //}
        //public static bool connected
        //{
        //    get
        //    {
        //        if (chromeconnected || ffconnected) return true;
        //        return false;
        //    }
        //}

        internal static Func<string, bool> hklmExists = delegate (string KeyLocation)
        {
            Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(KeyLocation);
            if ((rk) == null) return false;
            return true;
        };
        internal static Action<string> hklmCreate = delegate (string KeyLocation)
        {
            Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(KeyLocation);
        };
        internal static Func<string, bool> hkcuExists = delegate (string KeyLocation)
        {
            Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(KeyLocation);
            if ((rk) == null) return false;
            return true;
        };
        internal static Action<string> hkcuCreate = delegate (string KeyLocation)
        {
            Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(KeyLocation);
        };
        internal static Func<string, string, bool> regValue = delegate (string KeyLocation, string Value)
        {
            Microsoft.Win32.RegistryKey rk = (Microsoft.Win32.RegistryKey)Microsoft.Win32.Registry.GetValue(KeyLocation, Value, null);
            if ((rk) == null) return false;
            return true;
        };
        public static void registreChromeNativeMessagingHost(bool localMachine)
        {
            try
            {
                if (localMachine)
                {
                    if (!hklmExists(@"SOFTWARE\Google")) return;
                    if (!hklmExists(@"SOFTWARE\Google\Chrome")) return;
                    if (!hklmExists(@"SOFTWARE\Google\Chrome\NativeMessagingHosts")) hklmCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts");
                    if (!hklmExists(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\com.zenamic.msg")) hklmCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\com.zenamic.msg");
                }
                else
                {
                    if (!hkcuExists(@"SOFTWARE\Google")) return;
                    if (!hkcuExists(@"SOFTWARE\Google\Chrome")) return;
                    if (!hkcuExists(@"SOFTWARE\Google\Chrome\NativeMessagingHosts")) hkcuCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts");
                    if (!hkcuExists(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\com.zenamic.msg")) hkcuCreate(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\com.zenamic.msg");
                }
                //if(!regKey(@"HKEY_CURRENT_USER\stuff  ...  ", "value"))
                var basepath = System.IO.Directory.GetCurrentDirectory();
                var filename = System.IO.Path.Combine(basepath, "chromemanifest.json");
                string json = System.IO.File.ReadAllText(filename);
                dynamic jsonObj = JsonConvert.DeserializeObject(json);
                jsonObj["path"] = System.IO.Path.Combine(basepath, "OpenRPA.NativeMessagingHost.exe");
                string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                System.IO.File.WriteAllText(filename, output);

                Microsoft.Win32.RegistryKey Chrome = null;
                if (localMachine) Chrome = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Google\\Chrome\\NativeMessagingHosts\\com.zenamic.msg", true);
                if (!localMachine) Chrome = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Google\\Chrome\\NativeMessagingHosts\\com.zenamic.msg", true);
                Chrome.SetValue("", filename);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public static void registreffNativeMessagingHost(bool localMachine)
        {
            try
            {
                if (localMachine)
                {
                    if (!hklmExists(@"Software\Mozilla")) return;
                    if (!hklmExists(@"SOFTWARE\Mozilla\NativeMessagingHosts")) hklmCreate(@"SOFTWARE\Mozilla\NativeMessagingHosts");
                    if (!hklmExists(@"SOFTWARE\Mozilla\NativeMessagingHosts\com.zenamic.msg")) hklmCreate(@"SOFTWARE\Mozilla\NativeMessagingHosts\com.zenamic.msg");
                }
                else
                {
                    if (!hkcuExists(@"SOFTWARE\Mozilla")) return;
                    if (!hkcuExists(@"SOFTWARE\Mozilla\NativeMessagingHosts")) hkcuCreate(@"SOFTWARE\Mozilla\NativeMessagingHosts");
                    if (!hkcuExists(@"SOFTWARE\Mozilla\NativeMessagingHosts\com.zenamic.msg")) hkcuCreate(@"SOFTWARE\Mozilla\NativeMessagingHosts\com.zenamic.msg");
                }

                var basepath = System.IO.Directory.GetCurrentDirectory();
                var filename = System.IO.Path.Combine(basepath, "ffmanifest.json");
                string json = System.IO.File.ReadAllText(filename);
                dynamic jsonObj = JsonConvert.DeserializeObject(json);
                jsonObj["path"] = System.IO.Path.Combine(basepath, "OpenRPA.NativeMessagingHost.exe");
                string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                System.IO.File.WriteAllText(filename, output);

                Microsoft.Win32.RegistryKey Chrome = null;
                if (localMachine) Chrome = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Mozilla\\NativeMessagingHosts\\com.zenamic.msg", true);
                if (!localMachine) Chrome = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Mozilla\\NativeMessagingHosts\\com.zenamic.msg", true);
                Chrome.SetValue("", filename);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

    }
}
