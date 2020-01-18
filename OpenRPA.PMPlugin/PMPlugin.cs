using FlaUI.Core.AutomationElements;
using Newtonsoft.Json.Linq;
using OpenRPA.Input;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using OpenRPA.Interfaces.Input;
using OpenRPA.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.PMPlugin
{
    public class PMPlugin : ObservableObject, IRunPlugin
    {
        public string Name
        {
            get
            {
                return "PMPlugin";
            }
        }
        public static PMPlugin instance = null;
        public IOpenRPAClient client = null;
        public void Initialize(IOpenRPAClient client)
        {
            _ = PluginConfig.collectionname;
            _ = PluginConfig.enabled_keyboard_recording;
            _ = PluginConfig.enabled_mouse_recording;
            this.client = client;
            instance = this;
            Initialize();
        }
        public void Initialize()
        {
            if (PluginConfig.enabled_mouse_recording) StartMouse(); else StopMouse();
            if (PluginConfig.enabled_keyboard_recording) StartKeyboard(); else StopKeyboard();
        }
        private Views.PluginView view;
        public System.Windows.Controls.UserControl editor
        {
            get
            {
                if (view == null)
                {
                    view = new Views.PluginView(this);
                    view.PropertyChanged += (s, e) =>
                    {
                        NotifyPropertyChanged("Entity");
                        NotifyPropertyChanged("Name");
                    };
                }
                return view;
            }
        }
        private void OnMouseUp(InputEventArgs e)
        {
            try
            {
                //if (e.Element == null) return;
                //var pathToRoot = new List<AutomationElement>();
                //AutomationElement element = e.Element.RawElement;
                //WindowsSelector selector = new WindowsSelector(element, null, false);
                //var json = selector.ToString();
                //var i = new dbitem() { Selector = JArray.Parse(json) };
                //_ = global.webSocketClient.InsertOne(PluginConfig.collectionname, 0, false, i);
                IRecordEvent re = new RecordEvent
                {
                    Button = e.Button
                };
                re.Selector = new WindowsSelector(e.Element.RawElement, null, false);
                if (re.Selector == null) return;
                if (re.Selector.Count < 2) return;
                re.OffsetX = e.X - e.Element.Rectangle.X;
                re.OffsetY = e.Y - e.Element.Rectangle.Y;
                re.UIElement = e.Element;
                re.Element = e.Element;
                re.X = e.X;
                re.Y = e.Y;

                foreach (var p in Plugins.recordPlugins)
                {
                    // if (p.Name != "Windows")
                    if (p.Name == "IE" || p.Name == "Java" || p.Name == "NM")
                    {
                        try
                        {
                            if (p.ParseUserAction(ref re)) continue;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    }
                }

                if (e.Element == null) return;
                var i = new mouseevent(re);
                _ = global.webSocketClient.InsertOne(PluginConfig.collectionname, 0, false, i);

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private void OnKeyUp(Input.InputEventArgs e)
        {
            var key = new Interfaces.Input.vKey((FlaUI.Core.WindowsAPI.VirtualKeyShort)e.Key, true);
            var i = new keyboardevent(key);
            _ = global.webSocketClient.InsertOne(PluginConfig.collectionname, 0, false, i);
        }
        public void StartMouse()
        {
            InputDriver.Instance.OnMouseUp += OnMouseUp;
        }
        public void StopMouse()
        {
            InputDriver.Instance.OnMouseUp -= OnMouseUp;
        }
        public void StartKeyboard()
        {
            InputDriver.Instance.OnKeyUp += OnKeyUp;
        }
        public void StopKeyboard()
        {
            InputDriver.Instance.OnKeyUp -= OnKeyUp;
        }
        public bool onWorkflowStarting(ref IWorkflowInstance e, bool resumed)
        {
            return true;
        }
        public bool onWorkflowResumeBookmark(ref IWorkflowInstance e, string bookmarkName, object value)
        {
            return true;
        }
        public void onWorkflowCompleted(ref IWorkflowInstance e)
        {
        }
        public void onWorkflowAborted(ref IWorkflowInstance e)
        {
        }
        public void onWorkflowIdle(ref IWorkflowInstance e)
        {
        }
    }
    public class keyboardevent : apibase
    {
        public keyboardevent()
        {
            _type = "keyboardevent";
            host = Environment.MachineName.ToLower();
            fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
            AddRight("5e22f9bfffaf1a05a5e6ced3", "process_mining_data", new ace_right[] { ace_right.create, ace_right.read, ace_right.update, ace_right.delete, ace_right.invoke });
        }
        public keyboardevent(vKey key)
        {
            _type = "keyboardevent";
            host = Environment.MachineName.ToLower();
            fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
            AddRight("5e22f9bfffaf1a05a5e6ced3", "process_mining_data", new ace_right[] { ace_right.create, ace_right.read, ace_right.update, ace_right.delete, ace_right.invoke });
            this.key = key;
        }
        public vKey key { get; set; }
        public string host { get; set; }
        public string fqdn { get; set; }
    }
    public class mouseevent : apibase
    {
        public mouseevent()
        {
            _type = "mouseevent";
            host = Environment.MachineName.ToLower();
            fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
            AddRight("5e22f9bfffaf1a05a5e6ced3", "process_mining_data", new ace_right[] { ace_right.create, ace_right.read, ace_right.update, ace_right.delete, ace_right.invoke });
        }
        public mouseevent(IRecordEvent e)
        {
            _type = "mouseevent";
            host = Environment.MachineName.ToLower();
            fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
            AddRight("5e22f9bfffaf1a05a5e6ced3", "process_mining_data", new ace_right[] { ace_right.create, ace_right.read, ace_right.update, ace_right.delete, ace_right.invoke });
            Selector = JArray.Parse(e.Selector.ToString());
            button = e.Button;
            x = e.X;
            y = e.Y;
            offsetx = e.OffsetX;
            offsety = e.OffsetY;
            try
            {
                name = e.Element.Name;
                if(string.IsNullOrEmpty(name))
                {
                    name = e.Element.ToString();
                }
            }
            catch (Exception)
            {
            }
        }
        public JArray Selector { get; set; }
        public string host { get; set; }
        public string fqdn { get; set; }
        public MouseButton button { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public int offsetx { get; set; }
        public int offsety { get; set; }
    }
}
