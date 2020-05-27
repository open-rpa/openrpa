using OpenRPA.NamedPipeWrapper;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsAccessBridgeInterop;

namespace OpenRPA.Java
{
    public  class Javahook
    {
        public delegate void OnInitilizedDelegate(AccessBridge accessBridge);
        public event OnInitilizedDelegate OnInitilized;
        public delegate void OnNewjvmDelegate(AccessBridge accessBridge, AccessibleJvm[] newjvms);
        public event OnNewjvmDelegate OnNewjvm;
        public delegate void JavaShutDownDelegate(int vmID);
        public event JavaShutDownDelegate OnJavaShutDown;
        public delegate void MouseClickedDelegate(int vmID, AccessibleContextNode ac);
        public event MouseClickedDelegate OnMouseClicked;
        public delegate void MouseEnteredDelegate(int vmID, AccessibleContextNode ac);
        public event MouseEnteredDelegate OnMouseEntered;
        public delegate void JavaHandler();
        public event JavaHandler Connected;
        public event JavaHandler Disconnected;

        public NamedPipeClient<JavaEvent> pipeclient = null;
        private HwndCache _windowCache = null;
        private  SingleDelayedTask _delayedRefresh = new SingleDelayedTask();
        private  SingleDelayedTask _delayedAddEvents = new SingleDelayedTask();
        public  List<AccessibleJvm> jvms = new List<AccessibleJvm>();
        public  bool Initilized = false;
        public  AccessBridge accessBridge { get; set; } = new AccessBridge();
        public bool IsLoaded
        {
            get
            {
                return accessBridge.IsLoaded;
            }
        }
        public bool IsLegacy
        {
            get
            {
                return accessBridge.IsLegacy;
            }
        }
        private static Javahook _instance = null;
        public static Javahook Instance {
            get
            {
                if(_instance == null)
                {
                    _instance = new Javahook();
                    // _instance.init();
                }
                return _instance;
            }
        }
        public void init()
        {
            try
            {
                if (PluginConfig.auto_launch_java_bridge)
                {
                    EnsureJavaBridge();
                }
                if (pipeclient == null)
                {
                    var SessionId = System.Diagnostics.Process.GetCurrentProcess().SessionId;
                    pipeclient = new NamedPipeClient<JavaEvent>(SessionId + "_openrpa_javabridge");
                    pipeclient.ServerMessage += Pipeclient_ServerMessage;
                    pipeclient.Connected += Pipeclient_Connected;
                    pipeclient.Disconnected += Pipeclient_Disconnected;
                    pipeclient.AutoReconnect = true;
                    pipeclient.Start();
                }
                if (Initilized) return;
                GenericTools.RunUI(() =>
                {
                    Initilized = false;
                    _windowCache = new HwndCache();
                    Log.Debug("javahook.init()");
                    accessBridge.Initilized += (e1, e2) =>
                    {
                        Initilized = true;
                        Log.Information("javahook._accessBridge.Initilized");
                        OnInitilized?.Invoke(accessBridge);
                    };
                    if (IntPtr.Size == 4)
                    {
                        // accessBridge.Initialize(true);
                        accessBridge.Initialize(false);
                    } else
                    {
                        accessBridge.Initialize(false);
                    }
                        
                    refreshJvms(200);
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private void Pipeclient_Disconnected(NamedPipeConnection<JavaEvent, JavaEvent> connection)
        {
            Disconnected?.Invoke();
        }
        private void Pipeclient_Connected(NamedPipeConnection<JavaEvent, JavaEvent> connection)
        {
            Connected?.Invoke();
        }
        public static void EnsureJavaBridge()
        {
            bool isrunning = false;
            var me = System.Diagnostics.Process.GetCurrentProcess();
            foreach (var process in System.Diagnostics.Process.GetProcessesByName("OpenRPA.JavaBridge"))
            {
                if (process.Id != me.Id && process.SessionId == me.SessionId)
                {
                    isrunning = true;
                }
            }
            if (!isrunning)
            {
                var filename = System.IO.Path.Combine(Interfaces.Extensions.PluginsDirectory, "OpenRPA.JavaBridge.exe");
                if (!System.IO.File.Exists(filename))
                {
                    filename = System.IO.Path.Combine(Interfaces.Extensions.PluginsDirectory, "java\\OpenRPA.JavaBridge.exe");
                }
                if(System.IO.File.Exists(filename))
                {
                    try
                    {
                        var _childProcess = new System.Diagnostics.Process();
                        _childProcess.StartInfo.FileName = filename;
                        _childProcess.StartInfo.UseShellExecute = false;
                        if (!_childProcess.Start())
                        {
                            throw new Exception("Failed starting OpenRPA JavaBridge");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed launching OpenRPA.JavaBridge.exe");
                    }
                }
            }
        }
        public static void KillJavaBridge()
        {
            var me = System.Diagnostics.Process.GetCurrentProcess();
            foreach (var process in System.Diagnostics.Process.GetProcessesByName("OpenRPA.JavaBridge"))
            {
                if (process.Id != me.Id && process.SessionId == me.SessionId)
                {
                    process.Kill();
                }
            }
        }
        private void Pipeclient_ServerMessage(NamedPipeConnection<JavaEvent, JavaEvent> connection, JavaEvent message)
        {
            if (message.action == "javaShutDown") OnJavaShutDown?.Invoke(message.vmID);
            if (message.action == "MouseClicked") OnMouseClicked?.Invoke(message.vmID, new AccessibleContextNode(accessBridge, new JavaObjectHandle(message.vmID, new JOBJECT64(message.ac))));
            if (message.action == "MouseEntered") OnMouseEntered?.Invoke(message.vmID, new AccessibleContextNode(accessBridge, new JavaObjectHandle(message.vmID, new JOBJECT64(message.ac))));
        }
        public  void dispose()
        {
            accessBridge.Dispose();
        }
        public void refreshJvms()
        {
            _windowCache.Clear();
            jvms.Clear();
            refreshJvms(0);
            // jvms = EnumJvms();
        }
        public void refreshJvms(int miliseconds)
        {
            if (miliseconds == 0)
            {
                _refreshJvms();
                return;
            }
            _delayedRefresh.Post(TimeSpan.FromMilliseconds(miliseconds), () =>
            {
                _refreshJvms();
                refreshJvms(10000);
            });
        }
        public void _refreshJvms()
        {
            try
            {
                bool added = false;
                List<AccessibleJvm> newjvms = new List<AccessibleJvm>();
                _windowCache.Clear();
                var _jvms = EnumJvms();
                foreach (var jvm in _jvms)
                {
                    if (!jvms.Contains(jvm))
                    {
                        added = true;
                        jvms.Add(jvm);
                        newjvms.Add(jvm);
                    }
                }
                if (added)
                {
                    OnNewjvm?.Invoke(accessBridge, newjvms.ToArray());
                }
                // refreshJvms(1000);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }
        private List<AccessibleJvm> EnumJvms()
        {
            return accessBridge.EnumJvms(hwnd => _windowCache.Get(accessBridge, hwnd));
        }
    }
}
