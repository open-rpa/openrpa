using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.RDService
{
    public class PluginConfig
    {
        private static string pluginname => "rdservice";
        private static Config _globallocal = null;
        public static Config globallocal
        {
            get
            {
                if (_globallocal == null)
                {
                    var asm = System.Reflection.Assembly.GetEntryAssembly();
                    var filepath = asm.CodeBase.Replace("file:///", "");
                    var path = System.IO.Path.GetDirectoryName(filepath);
                    _globallocal = AppSettings<Config>.Load(System.IO.Path.Combine(path, "settings.json")); ;
                }
                return _globallocal;
            }
        }
        public static TimeSpan reloadinterval  { get { return globallocal.GetProperty<TimeSpan>(pluginname, TimeSpan.FromSeconds(1)); } set { globallocal.SetProperty(pluginname, value); } }
        public static string jwt { get { return globallocal.GetProperty<string>(pluginname, null); } set { globallocal.SetProperty(pluginname, value); } }
        public static string entropy { get { return globallocal.GetProperty<string>(pluginname, null); } set { globallocal.SetProperty(pluginname, value); } }
        public static string tempjwt { get { return globallocal.GetProperty<string>(pluginname, null); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool usefreerdp { get { return globallocal.GetProperty<bool>(pluginname, true); } set { globallocal.SetProperty(pluginname, value); } }
        private static void loadEntropy()
        {
            if (string.IsNullOrEmpty(entropy))
            {
                var buffer = new byte[20];
                using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(buffer);
                }
                entropy = System.Text.Encoding.UTF8.GetString(buffer);
            }
        }
        public static byte[] ProtectString(string data)
        {
            loadEntropy();
            // Data to protect.
            byte[] plaintext = Encoding.UTF8.GetBytes(data);

            // Generate additional entropy (will be used as the Initialization vector)
            var buffer = System.Text.Encoding.UTF8.GetBytes(entropy);
            byte[] ciphertext = ProtectedData.Protect(plaintext, buffer, DataProtectionScope.CurrentUser);
            return ciphertext;
        }
        public static SecureString UnprotectString(byte[] data)
        {
            loadEntropy();
            // Data to protect.
            // byte[] plaintext = Encoding.UTF8.GetBytes(data);

            SecureString SecureData = new SecureString();
            var buffer = System.Text.Encoding.UTF8.GetBytes(entropy);
            byte[] ciphertext = ProtectedData.Unprotect(data, buffer, DataProtectionScope.CurrentUser);
            foreach (var c in Encoding.Default.GetString(ciphertext))
            {
                SecureData.AppendChar(c);
            }
            return SecureData;
        }
        public static void Save()
        {
            var asm = System.Reflection.Assembly.GetEntryAssembly();
            var filepath = asm.CodeBase.Replace("file:///", "");
            var path = System.IO.Path.GetDirectoryName(filepath);
            _globallocal.Save(System.IO.Path.Combine(path, "settings.json"));

        }

    }
}
