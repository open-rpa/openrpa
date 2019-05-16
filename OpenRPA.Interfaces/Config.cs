using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public class Config : AppSettings<Config>
    {
        public string wsurl = "wss://demo1.openrpa.dk/";
        public string username = "";
        public byte[] password;
        public byte[] entropy;
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
        private static Config _local = null;
        public static Config local {
            get {
                if (_local == null) {
                    _local = Load("settings.json");
                }
                return _local;
            }
        }
        public static void Save()
        {
            local.Save("settings.json");
        }

        

    }
}

