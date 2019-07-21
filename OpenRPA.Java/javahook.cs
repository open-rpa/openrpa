using NamedPipeWrapper;
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

        public delegate void JavaShutDownDelegate(int vmID);
        public event JavaShutDownDelegate OnJavaShutDown;
        public delegate void MouseClickedDelegate(int vmID, AccessibleContextNode ac);
        public event MouseClickedDelegate OnMouseClicked;
        public delegate void MouseEnteredDelegate(int vmID, AccessibleContextNode ac);
        public event MouseEnteredDelegate OnMouseEntered;
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
                if (Initilized) return;
                Initilized = false;
                _windowCache = new HwndCache();
                Log.Debug("javahook.init()");
                accessBridge.Initilized += (e1, e2) =>
                {
                    Initilized = true;
                    Log.Information("javahook._accessBridge.Initilized");
                    OnInitilized?.Invoke(accessBridge);
                };
                accessBridge.Initialize();
                refreshJvms(200);
                EnsureJavaBridge();
                pipeclient = new NamedPipeClient<JavaEvent>("openrpa_javabridge");
                pipeclient.ServerMessage += Pipeclient_ServerMessage;
                pipeclient.AutoReconnect = true;
                pipeclient.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
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
                var filename = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "java\\OpenRPA.JavaBridge.exe");
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
        public  void refreshJvms()
        {
            _windowCache.Clear();
            jvms = EnumJvms();
        }
        public  void refreshJvms(int miliseconds)
        {
            _delayedRefresh.Post(TimeSpan.FromMilliseconds(miliseconds), () =>
            {
                try
                {
                    _windowCache.Clear();
                    var _jvms = EnumJvms();
                    Log.Debug("jvms: " + _jvms.Count);
                    foreach (var jvm in _jvms)
                    {
                        if (!jvms.Contains(jvm))
                        {
                            jvms.Add(jvm);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
                finally
                {
                }
            });
        }
        public List<AccessibleJvm> EnumJvms()
        {
            return accessBridge.EnumJvms(hwnd => _windowCache.Get(accessBridge, hwnd));
        }

    }
}
