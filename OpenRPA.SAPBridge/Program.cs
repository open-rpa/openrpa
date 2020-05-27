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

                throw;
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
                form = new MainWindow();
                var SessionId = System.Diagnostics.Process.GetCurrentProcess().SessionId;
                pipe = new NamedPipeServer<SAPEvent>(SessionId + "_openrpa_sapbridge");
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
                        if (recinfo != null) overlay = recinfo.overlay;
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
                        SAPHook.Instance.Login(message.Get<SAPLoginEvent>());
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
                if(message.action == "getitem")
                {
                    try
                    {
                        var msg = message.Get<SAPEventElement>();
                        if (string.IsNullOrEmpty(msg.SystemName)) throw new ArgumentException("System Name is mandatory right now!");
                        var session = SAPHook.Instance.GetSession(msg.SystemName);
                        GuiComponent comp = session.GetSAPComponentById<GuiComponent>(msg.Id);
                        if (comp is null) throw new ArgumentException("Item with id " + msg.Id + " was not found");

                        msg.Id = comp.Id; msg.Name = comp.Name;
                        msg.SystemName = session.Info.SystemName;
                        msg.ContainerType = comp.ContainerType; msg.type = comp.Type;
                        var p = comp.Parent as GuiComponent;
                        string parent = (p != null) ? p.Id : null;
                        msg.Parent = parent;
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
                                    children.Add(new SAPEventElement() { Id = Element.Id, Name = Element.Name, SystemName = session.Info.SystemName, ContainerType = Element.ContainerType, type = Element.Type, Parent = parent });
                                }
                            }
                            else if (comp is GuiContainer con)
                            {
                                for (var i = 0; i < con.Children.Count; i++)
                                {
                                    GuiComponent Element = con.Children.ElementAt(i);
                                    p = Element.Parent as GuiComponent;
                                    parent = (p != null) ? p.Id : null;
                                    children.Add(new SAPEventElement() { Id = Element.Id, Name = Element.Name, SystemName = session.Info.SystemName, ContainerType = Element.ContainerType, type = Element.Type, Parent = parent });
                                }
                            }
                            else if (comp is GuiStatusbar sbar)
                            {
                                for (var i = 0; i < sbar.Children.Count; i++)
                                {
                                    GuiComponent Element = sbar.Children.ElementAt(i);
                                    p = Element.Parent as GuiComponent;
                                    parent = (p != null) ? p.Id : null;
                                    children.Add(new SAPEventElement() { Id = Element.Id, Name = Element.Name, SystemName = session.Info.SystemName, ContainerType = Element.ContainerType, type = Element.Type, Parent = parent });
                                }
                            }
                            else
                            {
                                throw new Exception("Unknown container type " + comp.Type + "!");
                            }
                        }

                        msg.Children = children.ToArray();
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
                if (message.action == "invokemethod" || message.action == "setproperty")
                {
                    try
                    {
                        var step = message.Get<SAPInvokeMethod>();
                        if (step != null)
                        {
                            var sesion = SAPHook.Instance.GetSession(step.SystemName);
                            GuiComponent comp = sesion.GetSAPComponentById<GuiComponent>(step.Id);
                            if(comp == null)
                            {
                                if(step.Id.Contains("/tbar[1]/"))
                                {
                                    comp = sesion.GetSAPComponentById<GuiComponent>(step.Id.Replace("/tbar[1]/", "/tbar[0]/"));
                                }
                            }
                            if (comp == null) throw new Exception(string.Format("Can't find component using id {0}", step.Id));
                            string typeName = _prefix + comp.Type;
                            Type t = SAPGuiApiAssembly.GetType(typeName);
                            if (t == null)
                                throw new Exception(string.Format("Can't find type {0}", typeName));
                            var Parameters = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(step.Parameters);

                            if(message.action == "invokemethod") step.Result = t.InvokeMember(step.ActionName, System.Reflection.BindingFlags.InvokeMethod, null, comp, Parameters);
                            if (message.action == "setproperty") step.Result = t.InvokeMember(step.ActionName, System.Reflection.BindingFlags.SetProperty, null, comp, Parameters);
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
