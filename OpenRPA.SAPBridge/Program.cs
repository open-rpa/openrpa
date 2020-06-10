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
                            msg.LoadProperties(comp, msg.GetAllProperties);
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
                                        if (msg.MaxItem > 0)
                                        {
                                            if (children.Count >= msg.MaxItem) break;
                                        }

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
                                        if (msg.MaxItem > 0)
                                        {
                                            if (children.Count >= msg.MaxItem) break;
                                        }
                                    }
                                }
                                else if (comp is GuiStatusbar sbar)
                                {
                                    msg.type = "GuiStatusbar";
                                    for (var i = 0; i < sbar.Children.Count; i++)
                                    {
                                        GuiComponent Element = sbar.Children.ElementAt(i);
                                        p = Element.Parent as GuiComponent;
                                        parent = (p != null) ? p.Id : null;
                                        children.Add(new SAPEventElement(Element, session.Info.SystemName, parent, false));
                                        if (msg.MaxItem > 0)
                                        {
                                            if (children.Count >= msg.MaxItem) break;
                                        }
                                    }
                                }
                                else
                                {
                                    throw new Exception("Unknown container type " + comp.Type + "!");
                                }
                            }
                            if (comp is GuiTree tree)
                            {
                                msg.type = "GuiTree";
                                GuiCollection keys = null;
                                if (string.IsNullOrEmpty( msg.Path))
                                {
                                    keys = tree.GetNodesCol() as GuiCollection;
                                } else
                                {
                                    keys = tree.GetSubNodesCol(msg.Path) as GuiCollection;
                                }
                                if (keys!=null)
                                {
                                    foreach (string key in keys)
                                    {
                                        var _msg = new SAPEventElement(msg, tree, msg.Path, key, session.Info.SystemName);
                                        _msg.type = "GuiTreeNode";
                                        children.Add(_msg);
                                        System.Diagnostics.Trace.WriteLine(_msg.ToString());
                                        if (msg.MaxItem > 0)
                                        {
                                            if (children.Count >= msg.MaxItem) break;
                                        }
                                    }
                                }
                            }
                            if (comp is GuiTableControl table)
                            {
                                msg.type = "GuiTable";
                                var columns = new List<string>();
                                for (var i = 0; i < table.Columns.Count; i++)
                                {
                                    columns.Add(table.Columns.ElementAt(i).ToString());
                                }
                                for (var i = 0; i < table.RowCount; i++)
                                {
                                    var row = table.Rows.ElementAt(i) as GuiTableRow;
                                    // var _msg = new SAPEventElement(msg, tree, msg.Path, key, session.Info.SystemName);
                                    // children.Add(_msg);
                                    // System.Diagnostics.Trace.WriteLine(_msg.ToString());
                                    System.Diagnostics.Trace.WriteLine(row.ToString());
                                    if (msg.MaxItem > 0)
                                    {
                                        if (i >= msg.MaxItem) break;
                                    }

                                }
                            }
                            if (comp is GuiGridView grid)
                            {
                                msg.type = "GuiGrid";
                                if (string.IsNullOrEmpty(msg.Path))
                                {
                                    for (var i = 0; i < grid.RowCount; i++)
                                    {
                                        var _msg = new SAPEventElement(msg, grid, msg.Path, i, session.Info.SystemName);
                                        _msg.type = "GuiGridNode";
                                        children.Add(_msg);
                                        System.Diagnostics.Trace.WriteLine(_msg.ToString());
                                        if(msg.MaxItem > 0)
                                        {
                                            if (i >= msg.MaxItem) break;
                                        }
                                    }
                                } 
                                else
                                {
                                    msg = new SAPEventElement(msg, grid, msg.Path, int.Parse(msg.Path), session.Info.SystemName);
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
