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
    public class Config
    {
        public Dictionary<string, object> settings = new Dictionary<string, object>();
        public Dictionary<string, object> _properties = null;
        public Dictionary<string, object> properties { get { return GetProperty(null, new Dictionary<string, object>()); } set { SetProperty(null, value); } }
        public string wsurl { get { return GetProperty(null, "wss://app.openiap.io/"); } set { SetProperty(null, value); } }
        public string username { get { return GetProperty(null, ""); } set { SetProperty(null, value); } }
        public byte[] jwt { get { return GetProperty<byte[]>(null, null); } set { SetProperty(null, value); } }
        public byte[] password { get { return GetProperty<byte[]>(null, null); } set { SetProperty(null, value); } }
        public byte[] entropy { get { return GetProperty<byte[]>(null, null); } set { SetProperty(null, value); } }
        public string cancelkey { get { return GetProperty(null, "{ESCAPE}"); } set { SetProperty(null, value); } }
        public bool isagent { get { return GetProperty(null, false); } set { SetProperty(null, value); } }
        public bool showloadingscreen { get { return GetProperty(null, true); } set { SetProperty(null, value); } }
        public string culture { get { return GetProperty(null, ""); } set { SetProperty(null, value); } }
        public string ocrlanguage { get { return GetProperty(null, "eng"); } set { SetProperty(null, value); } }
        public string[] openworkflows { get { return GetProperty(null, new string[] { }); } set { SetProperty(null, value); } }
        public string[] files_pending_deletion { get { return GetProperty(null, new string[] { }); } set { SetProperty(null, value); } }
        public System.Drawing.Rectangle mainwindow_position { get { return GetProperty(null, System.Drawing.Rectangle.Empty); } set { SetProperty(null, value); } }
        public string designerlayout { get { return GetProperty(null, ""); } set { SetProperty(null, value); } }
        public bool record_overlay { get { return GetProperty(null, true); } set { SetProperty(null, value); } }
        public int querypagesize { get { return GetProperty(null, 50); } set { SetProperty(null, value); } }
        public DateTime lastupdatecheck { get { return GetProperty(null, DateTime.Now); } set { SetProperty(null, value); } }
        public TimeSpan updatecheckinterval { get { return GetProperty(null, TimeSpan.FromDays(1)); } set { SetProperty(null, value); } }
        public bool doupdatecheck { get { return GetProperty(null, true); } set { SetProperty(null, value); } }
        public bool autoupdateupdater { get { return GetProperty(null, false); } set { SetProperty(null, value); } }
        public bool log_to_file { get { return GetProperty(null, false); } set { SetProperty(null, value); } }
        public int log_file_level_minimum { get { return GetProperty(null, NLog.LogLevel.Info.Ordinal); } set { SetProperty(null, value); } }
        public int log_file_level_maximum { get { return GetProperty(null, NLog.LogLevel.Fatal.Ordinal); } set { SetProperty(null, value); } }
        public bool log_verbose { get { return GetProperty(null, false); } set { SetProperty(null, value); } }
        public bool log_activity { get { return GetProperty(null, false); } set { SetProperty(null, value); } }
        public bool log_debug { get { return GetProperty(null, false); } set { SetProperty(null, value); } }
        public bool log_selector { get { return GetProperty(null, false); } set { SetProperty(null, value); } }
        public bool log_selector_verbose { get { return GetProperty(null, false); } set { SetProperty(null, value); } }
        public bool log_information { get { return GetProperty(null, true); } set { SetProperty(null, value); } }
        public bool log_output { get { return GetProperty(null, true); } set { SetProperty(null, value); } }
        public bool log_warning { get { return GetProperty(null, true); } set { SetProperty(null, value); } }
        public bool log_error { get { return GetProperty(null, true); } set { SetProperty(null, value); } }
        public bool use_sendkeys { get { return GetProperty(null, false); } set { SetProperty(null, value); } }
        public bool use_virtual_click { get { return GetProperty(null, true); } set { SetProperty(null, value); } }
        public bool use_animate_mouse { get { return GetProperty(null, false); } set { SetProperty(null, value); } }
        public TimeSpan use_postwait { get { return GetProperty(null, TimeSpan.Zero); } set { SetProperty(null, value); } }
        public bool minimize { get { return GetProperty(null, true); } set { SetProperty(null, value); } }
        public bool recording_add_to_designer { get { return GetProperty(null, true); } set { SetProperty(null, value); } }
        public TimeSpan reloadinterval { get { return GetProperty(null, TimeSpan.FromMinutes(5)); } set { SetProperty(null, value); } }
        public TimeSpan move_animation_run_time { get { return GetProperty(null, TimeSpan.FromMilliseconds(500)); } set { SetProperty(null, value); } }
        public int move_animation_steps { get { return GetProperty(null, 20); } set { SetProperty(null, value); } }
        public bool remote_allow_multiple_running { get { return GetProperty(null, false); } set { SetProperty(null, value); } }
        public bool remote_allowed { get { return GetProperty(null, true); } set { SetProperty(null, value); } }
        public int remote_allow_multiple_running_max { get { return GetProperty(null, 2); } set { SetProperty(null, value); } }
        public string cef_useragent { get { return GetProperty(null, ""); } set { SetProperty(null, value); } }
        public bool show_getting_started { get { return GetProperty(null, true); } set { SetProperty(null, value); } }
        public string getting_started_url { get { return GetProperty(null, ""); } set { SetProperty(null, value); } }
        public bool notify_on_workflow_remote_start { get { return GetProperty(null, true); } set { SetProperty(null, value); } }
        public bool notify_on_workflow_end { get { return GetProperty(null, true); } set { SetProperty(null, value); } }
        public bool notify_on_workflow_remote_end { get { return GetProperty(null, false); } set { SetProperty(null, value); } }
        public bool log_busy_warning { get { return GetProperty(null, true); } set { SetProperty(null, value); } }
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
        public static Config local
        {
            get
            {
                if (_local == null)
                {
                    string filename = "settings.json";
                    var fi = new System.IO.FileInfo(filename);
                    var _fileName = System.IO.Path.GetFileName(filename);
                    var di = fi.Directory;
                    if (System.IO.File.Exists(System.IO.Path.Combine(Extensions.ProjectsDirectory, "settings.json")))
                    {
                            filename = System.IO.Path.Combine(Extensions.ProjectsDirectory, "settings.json");
                    }
                    else if (System.IO.File.Exists(filename))
                    {
                    }
                    else if (System.IO.File.Exists(System.IO.Path.Combine(di.Parent.FullName, "settings.json")))
                    {
                        filename = System.IO.Path.Combine(di.Parent.FullName, "settings.json");
                    }
                    else
                    {
                        // Will create a new file in ProjectsDirectory
                        filename = System.IO.Path.Combine(Extensions.ProjectsDirectory, "settings.json");
                    }
                    _local = new Config();
                    if (System.IO.File.Exists(filename))
                    {
                        var json = System.IO.File.ReadAllText(filename);
                        _local.settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    }
                    // _local = Load(filename);
                    // Hack to force updating old clients for new domain names
                    if (_local.wsurl == "wss://demo1.openrpa.dk/" || _local.wsurl == "wss://demo1.openrpa.dk")
                    {
                        _local.wsurl = "wss://app.openiap.io/";
                    }
                }
                return _local;
            }
        }
        public static void Save()
        {
            try
            {
                _local.Save(System.IO.Path.Combine(Extensions.ProjectsDirectory, "settings.json"));
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public void Save(string filename)
        {
            try
            {
                _ = wsurl;
                _ = username;
                _ = jwt;
                _ = password;
                _ = entropy;
                _ = cancelkey;
                _ = isagent;
                _ = showloadingscreen;
                _ = culture;
                _ = ocrlanguage;
                _ = openworkflows;
                _ = mainwindow_position;
                _ = designerlayout;
                _ = record_overlay;
                _ = querypagesize;
                _ = lastupdatecheck;
                _ = updatecheckinterval;
                _ = doupdatecheck;
                _ = autoupdateupdater;
                _ = log_to_file;
                _ = log_file_level_minimum;
                _ = log_file_level_maximum;
                _ = log_verbose;
                _ = log_activity;
                _ = log_debug;
                _ = log_selector;
                _ = log_selector_verbose;
                _ = log_information;
                _ = log_output;
                _ = log_warning;
                _ = log_error;
                _ = use_sendkeys;
                _ = use_virtual_click;
                _ = use_animate_mouse;
                _ = use_postwait;
                _ = minimize;
                _ = recording_add_to_designer;
                _ = reloadinterval;
                _ = move_animation_run_time;
                _ = move_animation_steps;
                _ = remote_allow_multiple_running;
                _ = remote_allow_multiple_running_max;
                _ = remote_allowed;
                _ = cef_useragent;
                _ = show_getting_started;
                _ = getting_started_url;
                _ = notify_on_workflow_remote_start;
                _ = notify_on_workflow_end;
                _ = notify_on_workflow_remote_end;
                _ = log_busy_warning;
                // settings
                // _properties
                var p = this.settings.OrderByDescending(kvp => kvp.Key);
                var d = new Dictionary<string, object>();
                foreach (var k in p) if (k.Key != "properties") d.Add(k.Key, k.Value);
                d.Add("properties", properties);
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(d, Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText(filename, json);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public static void Reload()
        {
            _local = null;
        }
        private bool? hasLocalMachine = null;
        private bool? hasCurrentUser = null;
        public bool GetRegistryProperty(string propertyname, out object value)
        {
            value = null;
            try
            {
                Microsoft.Win32.RegistryKey rk = null;
                if (hasLocalMachine == null)
                {
                    hasLocalMachine = false;
                    try
                    {
                        rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\OpenRPA", false);
                        if (rk != null) hasLocalMachine = true;
                    }
                    catch (Exception)
                    {
                    }
                }
                else if (hasLocalMachine == true)
                {
                    rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\OpenRPA", false);
                }
                if (rk != null)
                {
                    var tempvalue = rk.GetValue(propertyname);
                    if (tempvalue != null)
                    {
                        value = tempvalue;
                        return true;
                    }

                }
                if (hasCurrentUser == null)
                {
                    hasCurrentUser = false;
                    try
                    {
                        rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\OpenRPA", false);
                        if (rk != null) hasCurrentUser = true;
                    }
                    catch (Exception)
                    {
                    }
                }
                else if (hasCurrentUser == true)
                {
                    rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\OpenRPA", false);
                }
                if (rk != null)
                {
                    var tempvalue = rk.GetValue(propertyname);
                    if (tempvalue != null)
                    {
                        value = tempvalue;
                        return true;
                    }
                }
            }
            catch (Exception)
            {
            }
            return false;
        }
        public T GetProperty<T>(string pluginname, T mydefault, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            try
            {
                if (propertyName == null)
                {
                    throw new ArgumentNullException(nameof(propertyName));
                }
                string _propertyName = pluginname + "_" + propertyName;
                if (string.IsNullOrEmpty(pluginname)) _propertyName = propertyName;
                object value;
                if(GetRegistryProperty(_propertyName, out value))
                {
                }
                else if(propertyName != "properties")
                {
                    if (properties != null && !properties.TryGetValue(pluginname + "_" + propertyName, out value))
                    {
                    }
                }
                if (string.IsNullOrEmpty(pluginname) && value == null)
                {
                    if (settings != null && settings.TryGetValue(propertyName, out value))
                    {
                    }
                }
                if (value != null)
                {
                    if (typeof(T) == typeof(Dictionary<string, object>))
                    {
                        if (value.GetType() == typeof(Dictionary<string, object>))
                        {
                            return (T)value;
                        }
                        else if (!string.IsNullOrEmpty(value.ToString()))
                        {
                            value = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(value.ToString());
                        }
                        else
                        {
                            return default(T);
                        }
                    }
                    if (typeof(T) == typeof(byte[]))
                    {
                        if (value is string && !string.IsNullOrEmpty(value.ToString()))
                        {
                            value = Newtonsoft.Json.JsonConvert.DeserializeObject<byte[]>("\"" + value.ToString() + "\"");
                        }
                        else if (value is byte[])
                        {
                            return (T)value;
                        } else
                        {
                            return default(T);
                        }
                    }
                    if (typeof(T) == typeof(int) && value is long) value = int.Parse(value.ToString());
                    if (typeof(T) == typeof(bool)) value = bool.Parse(value.ToString());
                    if (typeof(T) == typeof(System.Drawing.Rectangle)) {
                        if (value.GetType() == typeof(System.Drawing.Rectangle))
                        {
                        } 
                        else
                        {
                            var c = new System.Drawing.RectangleConverter();
                            value = c.ConvertFromString(null, new System.Globalization.CultureInfo("en-US"), value.ToString());
                        }
                    }
                    if (typeof(T) == typeof(TimeSpan) && value != null)
                    {
                        TimeSpan ts = TimeSpan.Zero;
                        if (TimeSpan.TryParse(value.ToString(), out ts))
                        {
                            return (T)(object)ts;
                        }
                    }
                    if (typeof(T) == typeof(string[]) && value != null)
                    {
                        object o = null;
                        if (value.GetType() == typeof(string[])) o = value;
                        if (value.GetType() == typeof(Newtonsoft.Json.Linq.JArray)) o = ((Newtonsoft.Json.Linq.JArray)value).ToObject<string[]>();
                        return (T)o;
                    }
                    return (T)value;
                }
                SetProperty(pluginname, mydefault, propertyName);
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
                if (string.IsNullOrEmpty(pluginname))
                {
                    if (settings == null) settings = new Dictionary<string, object>();
                    settings[propertyName] = newValue;
                }
                else
                {
                    if (properties == null) properties = new Dictionary<string, object>();
                    properties[pluginname + "_" + propertyName] = newValue;
                    properties = properties;
                }
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

