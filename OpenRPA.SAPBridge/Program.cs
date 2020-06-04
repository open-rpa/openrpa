using OpenRPA.NamedPipeWrapper;
using SAPFEWSELib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace OpenRPA.SAPBridge
{
    class Program
    {
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
                SAPHook.Instance.RefreshSessions();
                SAPHook.Instance.RefreshUIElements();
                isMoving = false;

                if (MouseMove)
                {
                    Program.log("hook OnMouseMove");
                    InputDriver.Instance.OnMouseMove += OnMouseMove;
                }
                Program.log("hook OnMouseDown");
                InputDriver.Instance.OnMouseDown += OnMouseDown;
            });
        }
        public static void StopMonitorMouse()
        {
            Program.log("unhook OnMouseMove");
            InputDriver.Instance.OnMouseMove -= OnMouseMove;
            Program.log("unhook OnMouseDown");
            InputDriver.Instance.OnMouseDown -= OnMouseDown;
        }
        private static void OnMouseMove(InputEventArgs e)
        {
            System.Diagnostics.Trace.WriteLine("OnMouseMove: " + isMoving);
            if (isMoving) return;
            isMoving = true;
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
                    LastElement = Element;
                    if (SAPHook.Instance.Connections.Count() == 0) SAPHook.Instance.RefreshSessions();
                    if (SAPHook.Instance.UIElements.Count() == 0) SAPHook.Instance.RefreshUIElements();
                    SAPEventElement[] elements = new SAPEventElement[] { };
                    lock (SAPHook.Instance.UIElements)
                    {
                        elements = SAPHook.Instance.UIElements.Where(x => x.Rectangle.Contains(e.X, e.Y)).ToArray();
                    }
                    if (elements.Count() > 0)
                    {
                        var last = elements.OrderBy(x => x.Id.Length).Last();
                        SAPEvent message = new SAPEvent("mousemove");
                        message.Set(last);
                        form.AddText("[send] " + message.action + " " + last.Id);
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
            isMoving = false;
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
                    LastElement = Element;
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
        private static System.Windows.Automation.AutomationElement LastElement;
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
                            StartMonitorMouse(recinfo.mousemove);
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
                        if(SAPHook.Instance.Login(login))
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

                }
                if (message.action == "getitem")
                {
                    try
                    {
                        var msg = message.Get<SAPEventElement>();
                        if (string.IsNullOrEmpty(msg.SystemName)) throw new ArgumentException("System Name is mandatory right now!");
                        var session = SAPHook.Instance.GetSession(msg.SystemName);
                        if (session != null)
                        {
                            GuiComponent comp = session.GetSAPComponentById<GuiComponent>(msg.Id);
                            if (comp is null) throw new ArgumentException("Item with id " + msg.Id + " was not found");

                            msg.Id = comp.Id; msg.Name = comp.Name;
                            msg.SystemName = session.Info.SystemName;
                            msg.ContainerType = comp.ContainerType; msg.type = comp.Type;
                            var p = comp.Parent as GuiComponent;
                            string parent = (p != null) ? p.Id : null;
                            msg.Parent = parent;
                            // msg.LoadProperties(comp, true);
                            msg.LoadProperties(comp, false);
                            var children = new List<SAPEventElement>();
                            if (comp.ContainerType)
                            {
                                var cont = comp as GuiVContainer;

                                if (comp is GuiVContainer vcon)
                                {
                                    for (var i = 0; i < vcon.Children.Count; i++)
                                    {
                                        GuiComponent Element = vcon.Children.ElementAt(i);
                                        p = Element.Parent as GuiComponent;
                                        parent = (p != null) ? p.Id : null;
                                        var _newchild = new SAPEventElement(Element, session.Info.SystemName, parent, false);
                                        children.Add(_newchild);
                                    }
                                }
                                else if (comp is GuiContainer con)
                                {
                                    for (var i = 0; i < con.Children.Count; i++)
                                    {
                                        GuiComponent Element = con.Children.ElementAt(i);
                                        p = Element.Parent as GuiComponent;
                                        parent = (p != null) ? p.Id : null;
                                        children.Add(new SAPEventElement(Element, session.Info.SystemName, parent, false));
                                    }
                                }
                                else if (comp is GuiStatusbar sbar)
                                {
                                    for (var i = 0; i < sbar.Children.Count; i++)
                                    {
                                        GuiComponent Element = sbar.Children.ElementAt(i);
                                        p = Element.Parent as GuiComponent;
                                        parent = (p != null) ? p.Id : null;
                                        children.Add(new SAPEventElement(Element, session.Info.SystemName, parent, false));
                                    }
                                }
                                else
                                {
                                    throw new Exception("Unknown container type " + comp.Type + "!");
                                }
                            }

                            msg.Children = children.ToArray();
                        }
                        else
                        {
                            message.error = "SAP not running, or session " + msg.SystemName + " not found.";
                        }
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
