using Newtonsoft.Json;
using OpenRPA.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public class KeyboardDetectorPlugin : ObservableObject, IDetectorPlugin
    {
        //object IDetectorPlugin.Entity { get => Entity; set => Entity = value as entity.KeyboardDetector; }
        public entity.Detector Entity { get; set; }
        public string Name
        {
            get
            {
                if (Entity != null && string.IsNullOrEmpty(Entity.name)) Entity.name = "KeyboardSequence";
                if (Entity != null && !string.IsNullOrEmpty(Entity.name)) return Entity.name;
                return "KeyboardSequence";
            }
        }
        private Views.KeyboardDetectorView view;
        public System.Windows.Controls.UserControl editor
        {
            get
            {
                if (view == null)
                {
                    view = new Views.KeyboardDetectorView(this);
                    view.PropertyChanged += (s, e) =>
                    {
                        NotifyPropertyChanged("Entity");
                        NotifyPropertyChanged("Name");
                    };
                }
                return view;
            }
        }
        public event DetectorDelegate OnDetector;
        public void Initialize(entity.Detector InEntity)
        {
            Entity = InEntity;
            Start();
        }
        public string Keys
        {
            get
            {
                if (Entity == null) return null;
                if (!Entity.Properties.ContainsKey("Keys")) return null;
                var _val = Entity.Properties["Keys"];
                if (_val == null) return null;
                return _val.ToString();
            }
            set
            {
                if (Entity == null) return;
                Entity.Properties["Keys"] = value;
            }
        }
        public string Processname
        {
            get
            {
                if (Entity == null) return null;
                if (!Entity.Properties.ContainsKey("Processname")) return null;
                var _val = Entity.Properties["Processname"];
                if (_val == null) return null;
                return _val.ToString();
            }
            set
            {
                if (Entity == null) return;
                Entity.Properties["Processname"] = value;
            }
        }
        public void Start()
        {
            InputDriver.Instance.OnKeyDown += OnKeyDown;
            InputDriver.Instance.OnKeyUp += OnKeyUp;
            ParseText(Keys);
        }
        public void Stop()
        {
            InputDriver.Instance.OnKeyUp -= OnKeyUp;
        }
        private void RaiseDetector()
        {
            if(!string.IsNullOrEmpty(Processname))
            {
                using (var automation = AutomationUtil.getAutomation())
                {
                    var current = automation.FocusedElement();
                    if(current.Properties.ProcessId.IsSupported)
                    {
                        var ProcessId = automation.FocusedElement().Properties.ProcessId.Value;
                        var p = System.Diagnostics.Process.GetProcessById(automation.FocusedElement().Properties.ProcessId.Value);
                        if (!PatternMatcher.FitsMask(p.ProcessName.ToLower(), Processname.ToLower()))
                        {
                            Log.Information("KeyboardDetector skipped, expected " + Processname + ", but got " + p.ProcessName);
                            return;
                        }
                    }
                }
            }
            OnDetector?.Invoke(this, new DetectorEvent(), EventArgs.Empty);
        }
        private int keysindex = 0;
        private bool isMatch(keyset k, InputEventArgs e, keytype keytype)
        {

            //if ((keytype == keytype.down && (k.press == keytype.down || k.press == keytype.press)) || keytype == k.press)
            if (keytype == k.press)
            {
                if (k.Key > 0)
                {
                    if (k.Key == e.Key) { return true; } // else { Console.WriteLine(k.Key + " != " + e.Key); }
                }
                else
                {
                    if (k.c == (char)e.KeyValue) { return true; } // else { Console.WriteLine(k.c + " != " + (char)e.KeyValue); }
                }
            }
            return false;
        }
        private void OnKeyDown(InputEventArgs e)
        {
            if (keys.Count == 0) return;
            if (keysindex > 0)
            {
                var lastk = keys[keysindex - 1];
                if (isMatch(lastk, e, keytype.down)) return;
            }
            var k = keys[keysindex];
            if(isMatch(k, e, keytype.down))
            {
                keysindex++;
            } else if (keysindex>0)
            {
                var lastk = keys[keysindex - 1];
                if (!isMatch(lastk, e, keytype.down)) keysindex = 0;
            }
            // Console.WriteLine(keysindex + " / " + keys.Count);
            if (keysindex >= keys.Count)
            {
                keysindex = 0;
                RaiseDetector();
            }

        }
        private void OnKeyUp(InputEventArgs e)
        {
            if (keys.Count == 0) return;
            if (keysindex > 0)
            {
                var lastk = keys[keysindex - 1];
                if (isMatch(lastk, e, keytype.up)) return;
            }
            try
            {
                var k = keys[keysindex];
                if (isMatch(k, e, keytype.up))
                {
                    keysindex++;
                }
                else if (keysindex > 0)
                {
                    var lastk = keys[keysindex - 1];
                    if (!isMatch(lastk, e, keytype.up)) keysindex = 0;
                }
                // Console.WriteLine(keysindex + " / " + keys.Count);
                if (keysindex >= keys.Count)
                {
                    keysindex = 0;
                    RaiseDetector();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public enum keytype
        {
            up, down, press
        }
        [DllImport("user32.dll")] static extern short VkKeyScan(char ch);
        [StructLayout(LayoutKind.Explicit)]
        struct Helper
        {
            [FieldOffset(0)] public short Value;
            [FieldOffset(0)] public byte Low;
            [FieldOffset(1)] public byte High;
        }
        private class keyset
        {
            public keyset(keytype press, KeyboardKey Key) { this.Key = Key; this.press = press; }
            public keyset(keytype press, char c) {
                
                this.c = c; this.press = press;
                var helper = new Helper { Value = VkKeyScan(c) };
                Key = (KeyboardKey)helper.Low;
            }
            public KeyboardKey Key { get; set; }
            public char c { get; set; }
            public keytype press { get; set; }
        }
        private List<keyset> keys = new List<keyset>();
        internal void ParseText(string text)
        {
            keys.Clear();
            if (string.IsNullOrEmpty(text)) return;
            for (var i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '{')
                {
                    int indexEnd = text.IndexOf('}', i + 1);
                    int indexNextStart = text.IndexOf('{', indexEnd + 1);
                    int indexNextEnd = text.IndexOf('}', indexEnd + 1);
                    if (indexNextStart > indexNextEnd || (indexNextStart == -1 && indexNextEnd > -1)) indexEnd = indexNextEnd;
                    var sub = text.Substring(i + 1, (indexEnd - i) - 1);
                    i = indexEnd;
                    foreach (var k in sub.Split(','))
                    {
                        string key = k.Trim();
                        bool down = false;
                        bool up = false;
                        if (key.EndsWith("down"))
                        {
                            down = true;
                            key = key.Replace(" down", "");
                        }
                        else if (key.EndsWith("up"))
                        {
                            up = true;
                            key = key.Replace(" up", "");
                        }
                        //Keys specialkey;
                        KeyboardKey vk;
                        Enum.TryParse<KeyboardKey>(key, true, out vk);
                        if (down)
                        {
                            if (vk > 0)
                            {
                                keys.Add(new keyset(keytype.down, vk));
                            }
                            else
                            {
                                keys.Add(new keyset(keytype.down, key[0]));
                            }
                        }
                        else if (up)
                        {
                            if (vk > 0)
                            {
                                keys.Add(new keyset(keytype.up, vk));
                            }
                            else
                            {
                                keys.Add(new keyset(keytype.up, key[0]));
                            }
                        }
                        else
                        {
                            if (vk > 0)
                            {
                                //keys.Add(new keyset(keytype.press, vk));
                                keys.Add(new keyset(keytype.down, vk));
                                keys.Add(new keyset(keytype.up, vk));
                            }
                            else
                            {
                                //keys.Add(new keyset(keytype.press, key[0]));
                                keys.Add(new keyset(keytype.down, key[0]));
                                keys.Add(new keyset(keytype.up, key[0]));
                            }
                        }
                    }
                }
                else
                {
                    //keys.Add(new keyset(keytype.press, c));
                    keys.Add(new keyset(keytype.down, c));
                    keys.Add(new keyset(keytype.up, c));
                }
            }
        }
    }

    public class DetectorEvent : IDetectorEvent
    {
        public IElement element { get; set; }
        public string host { get; set; }
        public string fqdn { get; set; }
        public entity.TokenUser user { get; set; }
        public DetectorEvent()
        {
            //this.element = element;
            host = Environment.MachineName.ToLower();
            fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
        }

    }

}
