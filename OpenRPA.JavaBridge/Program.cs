using OpenRPA.NamedPipeWrapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRPA.JavaBridge
{
    class Program
    {
        private static IJavaHook hook;
        public static NamedPipeServer<JavaEvent> pipe { get; set; }
        private static MainWindow form;
        private static void log(string message)
        {
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
                pipe = new NamedPipeServer<JavaEvent>(SessionId + "_openrpa_javabridge");
                pipe.ClientMessage += Server_OnReceivedMessage;
                pipe.Start();
                if (IntPtr.Size == 4)
                {
                    hook = new JavaHook_32();
                }
                else
                {
                    hook = new JavaHook_64();
                }
                hook.OnJavaShutDown += OnJavaShutDown;
                hook.OnMouseClicked += OnMouseClicked;
                hook.OnMouseEntered += OnMouseEntered;
                hook.OnMouseExited += OnMouseExited;

                hook.OnMousePressed += OnMousePressed;
                hook.OnMouseReleased += OnMouseReleased;

                System.Windows.Forms.Application.Run(form);
            }
            catch (Exception ex)
            {
                log(ex.ToString());
            }
        }
        private static void OnJavaShutDown(int vmID)
        {
            try
            {
                var e = new JavaEvent("JavaShutDown", vmID);
                string json = JsonConvert.SerializeObject(e);
                form.AddText("JavaShutDown vmID:" + vmID);
                pipe.PushMessage(e);
            }
            catch (Exception ex)
            {
                log(ex.ToString());
            }
        }
        private static void OnMouseEntered(int vmID, IntPtr jevent, IntPtr ac)
        {
            try
            {
                var e = new JavaEvent("MouseEntered", vmID, jevent, ac);
                string json = JsonConvert.SerializeObject(e);
                form.AddText("MouseEntered vmID:" + vmID);
                pipe.PushMessage(e);
            }
            catch (Exception ex)
            {
                log(ex.ToString());
            }
        }
        private static void OnMouseExited(int vmID, IntPtr jevent, IntPtr ac)
        {
            try
            {
                var e = new JavaEvent("MouseExited", vmID, jevent, ac);
                string json = JsonConvert.SerializeObject(e);
                form.AddText("MouseExited vmID:" + vmID);
                pipe.PushMessage(e);
            }
            catch (Exception ex)
            {
                log(ex.ToString());
            }
        }
        private static void OnMouseClicked(int vmID, IntPtr jevent, IntPtr ac)
        {
            try
            {
                var e = new JavaEvent("MouseClicked", vmID, jevent, ac);
                string json = JsonConvert.SerializeObject(e);
                form.AddText("MouseClicked vmID:" + vmID);
                pipe.PushMessage(e);
            }
            catch (Exception ex)
            {
                log(ex.ToString());
            }
        }
        private static void OnMousePressed(int vmID, IntPtr jevent, IntPtr ac)
        {
            try
            {
                var e = new JavaEvent("MousePressed", vmID, jevent, ac);
                string json = JsonConvert.SerializeObject(e);
                form.AddText("MousePressed vmID:" + vmID);
                pipe.PushMessage(e);
            }
            catch (Exception ex)
            {
                log(ex.ToString());
            }
        }
        private static void OnMouseReleased(int vmID, IntPtr jevent, IntPtr ac)
        {
            try
            {
                var e = new JavaEvent("MouseReleased", vmID, jevent, ac);
                string json = JsonConvert.SerializeObject(e);
                form.AddText("MouseReleased vmID:" + vmID);
                pipe.PushMessage(e);
            }
            catch (Exception ex)
            {
                log(ex.ToString());
            }
        }
        private static void Server_OnReceivedMessage(NamedPipeConnection<JavaEvent, JavaEvent> connection, JavaEvent message)
        {
            try
            {
                if (message == null) return;
                form.AddText(message.action);
            }
            catch (Exception ex)
            {
                log(ex.ToString());
                form.AddText(ex.ToString());
            }
        }
    }
}
