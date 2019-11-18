using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.RDServicePlugin
{
    class PluginConfig
    {
        private static string pluginname => "rdservice";
        private static Config _globallocal = null;
        public static Config globallocal
        {
            get
            {
                if (_globallocal == null)
                {
                    _globallocal = Config.local;
                }
                return _globallocal;
            }
        }
        public static byte[] jwt { get { return globallocal.GetProperty<byte[]>(pluginname, null); } set { globallocal.SetProperty(pluginname, value); } }
        public static byte[] entropy { get { return globallocal.GetProperty<byte[]>(pluginname, null); } set { globallocal.SetProperty(pluginname, value); } }
        public static string tempjwt { get { return globallocal.GetProperty<string>(pluginname, null); } set { globallocal.SetProperty(pluginname, value); } }
        public static bool usefreerdp { get { return globallocal.GetProperty<bool>(pluginname, true); } set { globallocal.SetProperty(pluginname, value); } }
        private void loadEntropy()
        {
            if (entropy == null || entropy.Length == 0)
            {
                entropy = new byte[20];
                using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(entropy);
                }
            }
        }
        public byte[] ProtectString(string data)
        {
            loadEntropy();
            // Data to protect.
            byte[] plaintext = Encoding.UTF8.GetBytes(data);

            // Generate additional entropy (will be used as the Initialization vector)
            byte[] ciphertext = ProtectedData.Protect(plaintext, entropy, DataProtectionScope.CurrentUser);
            return ciphertext;
        }
        public SecureString UnprotectString(byte[] data)
        {
            loadEntropy();
            // Data to protect.
            // byte[] plaintext = Encoding.UTF8.GetBytes(data);

            SecureString SecureData = new SecureString();
            byte[] ciphertext = ProtectedData.Unprotect(data, entropy, DataProtectionScope.CurrentUser);
            foreach (var c in Encoding.Default.GetString(ciphertext))
            {
                SecureData.AppendChar(c);
            }
            return SecureData;
        }
    }
}
