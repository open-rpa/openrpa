using OpenRPA.NamedPipeWrapper;
using SAPFEWSELib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OpenRPA.SAPBridge
{
    class Program
    {
        private static SAPEventElement LastElement;
        private static MainWindow form;
        public static NamedPipeServer<SAPEvent> pipe;
        public static void log(string message)
        {
            try
            {
                form.AddText(message);
            }
            catch (Exception)
            {
            }
            try
            {
                Console.WriteLine(message);
                System.IO.File.AppendAllText("log.txt", message);
                return;
            }
            catch (Exception)
            {
            }
            try
            {
                var dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "log.txt"), message);
                return;
            }
            catch (Exception)
            {
            }
        }
        static void Main(string[] args)
        {
            try
            {
                try
                {
                    _ = SAPGuiApiAssembly;
                    InputDriver.Instance.OnMouseDown -= OnMouseDown;
                }
                catch (Exception)
                {
                    throw;
                }
                form = new MainWindow();
                var SessionId = System.Diagnostics.Process.GetCurrentProcess().SessionId;
                pipe = new NamedPipeServer<SAPEvent>(SessionId + "_openrpa_sapbridge");
                pipe.ClientConnected += Pipe_ClientConnected;
                pipe.ClientMessage += Server_OnReceivedMessage;
                pipe.Start();
                // SAPHook.Instance.

                System.Windows.Forms.Application.Run(form);
            }
            catch (Exception ex)
            {
                log(ex.ToString());
            }
        }
        public static void StartMonitorMouse(bool MouseMove)
        {
            Task.Run(() =>
            {
                try
                {
                    SAPHook.Instance.RefreshSessions();
                    SAPHook.Instance.RefreshUIElements();
                    isMoving = false;
                    InputDriver.Instance.OnMouseMove -= OnMouseMove;
                    InputDriver.Instance.OnMouseDown -= OnMouseDown;

                    if (MouseMove)
                    {
                        Program.log("hook OnMouseMove");
                        InputDriver.Instance.OnMouseMove += OnMouseMove;
                    }
                    Program.log("hook OnMouseDown");
                    InputDriver.Instance.OnMouseDown += OnMouseDown;
                }
                catch (Exception ex)
                {
                    Program.log(ex.ToString());
                }
            });
        }
        public static void StopMonitorMouse()
        {
            Program.log("unhook OnMouseMove");
            InputDriver.Instance.OnMouseMove -= OnMouseMove;
            Program.log("unhook OnMouseDown");
            InputDriver.Instance.OnMouseDown -= OnMouseDown;
        }
        private static object _lock = new object();
        private static void OnMouseMove(InputEventArgs e)
        {
            lock(_lock)
            {
                if (isMoving) return;
                isMoving = true;
            }
            try
            {
                if (SAPHook.Instance.Connections.Count() == 0 || SAPHook.Instance.UIElements.Count() == 0)
                {
                    lock (_lock)
                    {
                        isMoving = false;
                    }
                    return;
                }
                var Element = System.Windows.Automation.AutomationElement.FromPoint(new System.Windows.Point(e.X, e.Y));
                if (Element != null)
                {
                    var ProcessId = Element.Current.ProcessId;
                    if (ProcessId < 1)
                    {
                        lock (_lock)
                        {
                            isMoving = false;
                        }
                        return;
                    }
                    if (SAPProcessId > 0 && SAPProcessId != ProcessId)
                    {
                        lock (_lock)
                        {
                            isMoving = false;
                        }
                        return;
                    }
                    if (SAPProcessId != ProcessId)
                    {
                        var p = System.Diagnostics.Process.GetProcessById(ProcessId);
                        if (p.ProcessName.ToLower() == "saplogon") SAPProcessId = p.Id;
                        if (p.ProcessName.ToLower() != "saplogon")
                        {
                            lock (_lock)
                            {
                                isMoving = false;
                            }
                            return;
                        }
                    }
                    if (SAPHook.Instance.Connections.Count() == 0) SAPHook.Instance.RefreshSessions();
                    if (SAPHook.Instance.UIElements.Count() == 0) SAPHook.Instance.RefreshUIElements();
                    SAPEventElement[] elements = new SAPEventElement[] { };
                    lock (SAPHook.Instance.UIElements)
                    {
                        elements = SAPHook.Instance.UIElements.Where(x => x.Rectangle.Contains(e.X, e.Y)).ToArray();
                    }
                    if (elements.Count() > 0)
                    {
                        var found = elements.OrderBy(x => x.Id.Length).Last();

                        if(found.Items != null && found.Items.Length > 0)
                        {
                            var found2 = found.Items.Where(x => x.Rectangle.Contains(e.X, e.Y)).ToArray();
                            if(found2.Length > 0)
                            {
                                found = found2.First();
                            }
                        }
                        
                        if (LastElement != null && (found.Id == LastElement.Id  && found.Path == LastElement.Path))
                        {
                            form.AddText("[SKIP] mousemove " + LastElement.Id);
                            lock (_lock)
                            {
                                isMoving = false;
                            }
                            return;
                        }
                        LastElement = found;
                        SAPEvent message = new SAPEvent("mousemove");
                        message.Set(LastElement);
                        form.AddText("[send] " + message.action + " " + LastElement.Id);
                        pipe.PushMessage(message);
                    }
                    else
                    {
                        log("Mouseover " + e.X + "," + e.Y + " not found in UI List");
                    }
                }
            }
            catch (Exception)
            {
            }
            lock (_lock)
            {
                isMoving = false;
            }
        }
        private static void OnMouseDown(InputEventArgs e)
        {
            try
            {
                if (SAPHook.Instance.Connections.Count() == 0) return;
                if (SAPHook.Instance.UIElements.Count() == 0) return;
                var Element = System.Windows.Automation.AutomationElement.FromPoint(new System.Windows.Point(e.X, e.Y));
                if (Element != null)
                {
                    var ProcessId = Element.Current.ProcessId;
                    if (ProcessId < 1) return;
                    if (SAPProcessId > 0 && SAPProcessId != ProcessId) return;
                    if (SAPProcessId != ProcessId)
                    {
                        var p = System.Diagnostics.Process.GetProcessById(ProcessId);
                        if (p.ProcessName.ToLower() == "saplogon") SAPProcessId = p.Id;
                        if (p.ProcessName.ToLower() != "saplogon") return;
                    }
                    SAPEventElement[] elements = new SAPEventElement[] { };
                    lock (SAPHook.Instance.UIElements)
                    {
                        elements = SAPHook.Instance.UIElements.Where(x => x.Rectangle.Contains(e.X, e.Y)).ToArray();
                    }
                    if (elements.Count() > 0)
                    {
                        var last = elements.OrderBy(x => x.Id.Length).Last();
                        SAPEvent message = new SAPEvent("mousedown");
                        message.Set(last);
                        form.AddText("[send] " + message.action + " " + last.Id);
                        pipe.PushMessage(message);
                    }
                    else
                    {
                        log("OnMouseDown " + e.X + "," + e.Y + " not found in UI List");
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        private static int SAPProcessId = -1;
        private static bool isMoving = false;
        private static void Pipe_ClientConnected(NamedPipeConnection<SAPEvent, SAPEvent> connection)
        {
            log("Client Connected");
        }
        private static string _prefix = "SAPFEWSELib.";
        private static System.Reflection.Assembly _sapGuiApiAssembly = null;
        public static System.Reflection.Assembly SAPGuiApiAssembly 
        { 
            get {      
                if(_sapGuiApiAssembly == null)
                {
                    var stm = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("OpenRPA.SAPBridge.Resources.Interop.SAPFEWSELib.dll");
                    var bs = new byte[(int)stm.Length];
                    stm.Read(bs, 0, (int)stm.Length);
                    stm.Close();
                    _sapGuiApiAssembly = System.Reflection.Assembly.Load(bs);
                }
                return _sapGuiApiAssembly; 
            } 
        }
        public static bool recordstarting = false;
        public static GuiVComponent _lastHighlight = null;

        public static string StripSession(string id)
        {
            // /app/con[0]/ses[0]/wnd
            if (id.StartsWith("/app")) id = id.Substring(id.IndexOf("/", 1));
            if (id.StartsWith("/con[")) id = id.Substring(id.IndexOf("]") + 1);
            if (id.StartsWith("/ses[")) id = id.Substring(id.IndexOf("]") + 1);
            if (id.StartsWith("/")) id = id.Substring(1);
            return id;
        }
        public static int ExtractIndex(ref string part)
        {
            if(part.Contains("["))
            {
                var strindex = part.Substring(part.IndexOf("[") + 1);
                strindex = strindex.Substring(0, strindex.IndexOf("]"));
                part = part.Substring(0, part.IndexOf("["));
                return int.Parse(strindex);
            }
            return -1;
        }
        private static void Server_OnReceivedMessage(NamedPipeConnection<SAPEvent, SAPEvent> connection, SAPEvent message)
        {
            try
            {
                if (message == null) return;
                form.AddText("[resc] " + message.action);
                if (message.action == "beginrecord")
                {
                    try
                    {
                        recordstarting = true;
                        var recinfo = message.Get<SAPToogleRecordingEvent>();
                        var overlay = false;
                        if (recinfo != null)
                        {
                            overlay = recinfo.overlay;
                            //StartMonitorMouse(recinfo.mousemove);
                            StartMonitorMouse(true);
                        }
                        SAPHook.Instance.BeginRecord(overlay);
                        form.AddText("[send] " + message.action);
                        pipe.PushMessage(message);
                        recordstarting = false;
                    }
                    catch (Exception ex)
                    {
                        message.error = ex.Message;
                        form.AddText("[send] " + message.action);
                        pipe.PushMessage(message);
                    }
                }
                if (message.action == "endrecord")
                {
                    try
                    {
                        StopMonitorMouse();
                        SAPHook.Instance.EndRecord();
                        form.AddText("[send] " + message.action);
                        pipe.PushMessage(message);
                    }
                    catch (Exception ex)
                    {
                        message.error = ex.Message;
                        form.AddText("[send] " + message.action);
                        pipe.PushMessage(message);
                    }
                }
                if (message.action == "login")
                {
                    try
                    {
                        var login = message.Get<SAPLoginEvent>();
                        bool dologin = true;
                        if (SAPHook.Instance.Sessions != null)
                            foreach (var session in SAPHook.Instance.Sessions)
                            {
                                if (session.Info.SystemName.ToLower() == login.SystemName.ToLower()) { dologin = false; break; }
                            }
                        if(dologin)
                        {
                            if (SAPHook.Instance.Login(login))
                            {
                                var session = SAPHook.Instance.GetSession(login.SystemName);
                                if (session == null)
                                {
                                    message.error = "Login failed";
                                }
                            }
                            else
                            {
                                message.error = "Login failed";
                            }
                        }
                        form.AddText("[send] " + message.action);
                        pipe.PushMessage(message);
                    }
                    catch (Exception ex)
                    {
                        message.error = ex.Message;
                        form.AddText("[send] " + message.action);
                        pipe.PushMessage(message);
                    }
                }
                if(message.action == "getconnections")
                {
                    try
                    {
                        var result = new SAPGetSessions();
                        SAPHook.Instance.RefreshSessions();
                        result.Connections = SAPHook.Instance.Connections;
                        message.Set(result);
                        form.AddText("[send] " + message.action);
                        pipe.PushMessage(message);
                    }
                    catch (Exception ex)
                    {
                        message.error = ex.Message;
                        form.AddText("[send] " + message.action);
                        pipe.PushMessage(message);
                    }
                }

                if (message.action == "getitems")
                {
                    try
                    {
                        var msg = message.Get<SAPEventElement>();
                        var session = SAPHook.Instance.GetSession(msg.SystemName);
                        var sapelements = new List<GuiComponent>();
                        msg.Id = StripSession(msg.Id);
                        var paths = msg.Id.Split(new string[] { "/"}, StringSplitOptions.None);
                        for(var i = 0; i < paths.Length; i++)
                        {
                            string part = paths[i];
                            int index = ExtractIndex(ref part);
                            if (i == 0)
                            {
                                if (index > -1) sapelements.Add(session.Children.ElementAt(index));
                                if(index == -1)
                                {
                                    for(var x = 0; x < session.Children.Count; x++)
                                    {
                                        sapelements.Add(session.Children.ElementAt(x));
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        message.error = ex.Message;
                        form.AddText("[send] " + message.action);
                        pipe.PushMessage(message);
                    }
                }
                if (message.action == "getitem")
                {
                    try
                    {
                        var msg = message.Get<SAPEventElement>();
                        // if (string.IsNullOrEmpty(msg.SystemName)) throw new ArgumentException("System Name is mandatory right now!");
                        var session = SAPHook.Instance.GetSession(msg.SystemName);
                        if (session == null)
                        {
                            msg.Id = null;
                            message.Set(msg);
                            form.AddText("[send] " + message.action);
                            pipe.PushMessage(message);
                            return;
                        }
                        // msg.Id = StripSession(msg.Id);
                        GuiComponent comp = session.GetSAPComponentById<GuiComponent>(msg.Id);
                        if (comp is null) 
                        {
                            msg.Id = null;
                            message.Set(msg);
                            form.AddText("[send] " + message.action);
                            pipe.PushMessage(message);
                            return;
                        }
                        msg = new SAPEventElement(comp, session.Info.SystemName, msg.GetAllProperties, msg.Path, null, msg.Flat, true, msg.MaxItem);
                        message.Set(msg);
                        form.AddText("[send] " + message.action);
                        pipe.PushMessage(message);
                    }
                    catch (Exception ex)
                    {
                        message.error = ex.Message;
                        form.AddText("[send] " + message.action);
                        pipe.PushMessage(message);
                    }
                }
                if (message.action == "invokemethod" || message.action == "setproperty" || message.action == "getproperty" || message.action == "highlight")
                {
                    try
                    {
                        var step = message.Get<SAPInvokeMethod>();
                        if (step != null)
                        {
                            var session = SAPHook.Instance.GetSession(step.SystemName);
                            if(session != null)
                            {
                                GuiComponent comp = session.GetSAPComponentById<GuiComponent>(step.Id);
                                if (comp == null)
                                {
                                    if (step.Id.Contains("/tbar[1]/"))
                                    {
                                        comp = session.GetSAPComponentById<GuiComponent>(step.Id.Replace("/tbar[1]/", "/tbar[0]/"));
                                    }
                                }
                                if (comp == null) throw new Exception(string.Format("Can't find component using id {0}", step.Id));
                                string typeName = _prefix + comp.Type;
                                Type t = SAPGuiApiAssembly.GetType(typeName);
                                if (t == null)
                                    throw new Exception(string.Format("Can't find type {0}", typeName));
                                var Parameters = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(step.Parameters);

                                if (message.action == "invokemethod") step.Result = t.InvokeMember(step.ActionName, System.Reflection.BindingFlags.InvokeMethod, null, comp, Parameters);
                                if (message.action == "setproperty") step.Result = t.InvokeMember(step.ActionName, System.Reflection.BindingFlags.SetProperty, null, comp, Parameters);
                                if (message.action == "getproperty") step.Result = t.InvokeMember(step.ActionName, System.Reflection.BindingFlags.GetProperty, null, comp, Parameters);
                                var vcomp = comp as GuiVComponent;
                                
                                if (message.action == "highlight" && vcomp != null)
                                {
                                    try
                                    {
                                        if (_lastHighlight != null) _lastHighlight.Visualize(false);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                    _lastHighlight = null;
                                    _lastHighlight = comp as GuiVComponent;
                                    _lastHighlight.Visualize(true);
                                }
                            }
                            else
                            {
                                message.error = "SAP not running, or session " + step.SystemName + " not found.";
                            }
                            message.Set(step);
                        }
                        form.AddText("[send] " + message.action);
                        pipe.PushMessage(message);
                    }
                    catch (Exception ex)
                    {
                        message.error = ex.Message;
                        if (ex.InnerException!=null)
                        {
                            message.error = ex.InnerException.Message;
                        }
                        form.AddText("[send] " + message.action);
                        pipe.PushMessage(message);
                    }

                }
            }
            catch (Exception ex)
            {
                log(ex.ToString());
                form.AddText(ex.ToString());
            }
        }
    }
}
