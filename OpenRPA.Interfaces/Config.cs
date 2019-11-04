using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public class Config : AppSettings<Config>
    {
        public string wsurl = "wss://demo1.openrpa.dk/";
        public string username = "";
        public byte[] jwt;
        public byte[] password;
        public byte[] entropy;
        public string cancelkey = "{ESCAPE}";
        public string ocrlanguage = "eng";
        public string[] openworkflows = new string[] { };
        public string designerlayout = "";
        public Dictionary<string, object> properties = new Dictionary<string, object>();
        public bool record_overlay = false;
        public int querypagesize = 50;
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
        public static void Reload()
        {
            _local = Load("settings.json");
        }
        public T GetProperty<T>(string pluginname, T mydefault, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            try
            {
                if (propertyName == null)
                {
                    throw new ArgumentNullException(nameof(propertyName));
                }
                object value;
                if (properties.TryGetValue(pluginname + "_" + propertyName, out value))
                {
                    return (T)value;
                }
                return mydefault;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
        }
        public bool SetProperty<T>(string pluginname, T newValue, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            try
            {
                if (propertyName == null)
                {
                    throw new ArgumentNullException(nameof(propertyName));
                }
                // if (IsEqual(GetProperty<T>(pluginname + "_" + propertyName, default(T)), newValue)) return false;
                properties[pluginname + "_" + propertyName] = newValue;
                Type typeParameterType = typeof(T);
                if (typeParameterType.Name.ToLower().Contains("readonly"))
                {
                    return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
        }
        private bool IsEqual<T>(T field, T newValue)
        {
            // Alternative: EqualityComparer<T>.Default.Equals(field, newValue);
            return Equals(field, newValue);
        }
        private string GetNameFromExpression<T>(Expression<Func<T>> selectorExpression)
        {
            var body = (MemberExpression)selectorExpression.Body;
            var propertyName = body.Member.Name;
            return propertyName;
        }


    }
}

