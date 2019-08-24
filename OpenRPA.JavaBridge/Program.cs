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
        private static JavaHook hook;
        public static NamedPipeServer<JavaEvent> pipe { get; set; }
        private static MainWindow form;
        static void Main(string[] args)
        {
            try
            {
                form = new MainWindow();
                pipe = new NamedPipeServer<JavaEvent>("openrpa_javabridge");
                pipe.ClientMessage += Server_OnReceivedMessage;
                pipe.Start();

                hook = new JavaHook();
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
                System.IO.File.AppendAllText("log.txt", ex.ToString());
                throw;
            }
        }
        private static void OnJavaShutDown(int vmID)
        {
            var e = new JavaEvent("JavaShutDown", vmID);
            string json = JsonConvert.SerializeObject(e);
            form.AddText("JavaShutDown vmID:" + vmID);
            pipe.PushMessage(e);
        }
        private static void OnMouseEntered(int vmID, IntPtr jevent, IntPtr ac)
        {
            var e = new JavaEvent("MouseEntered", vmID, jevent, ac);
            string json = JsonConvert.SerializeObject(e);
            form.AddText("MouseEntered vmID:" + vmID);
            pipe.PushMessage(e);
        }
        private static void OnMouseExited(int vmID, IntPtr jevent, IntPtr ac)
        {
            var e = new JavaEvent("MouseExited", vmID, jevent, ac);
            string json = JsonConvert.SerializeObject(e);
            form.AddText("MouseExited vmID:" + vmID);
            pipe.PushMessage(e);
        }
        private static void OnMouseClicked(int vmID, IntPtr jevent, IntPtr ac)
        {
            var e = new JavaEvent("MouseClicked", vmID, jevent, ac);
            string json = JsonConvert.SerializeObject(e);
            form.AddText("MouseClicked vmID:" + vmID);
            pipe.PushMessage(e);
        }
        private static void OnMousePressed(int vmID, IntPtr jevent, IntPtr ac)
        {
            var e = new JavaEvent("MousePressed", vmID, jevent, ac);
            string json = JsonConvert.SerializeObject(e);
            form.AddText("MousePressed vmID:" + vmID);
            pipe.PushMessage(e);
        }
        private static void OnMouseReleased(int vmID, IntPtr jevent, IntPtr ac)
        {
            var e = new JavaEvent("MouseReleased", vmID, jevent, ac);
            string json = JsonConvert.SerializeObject(e);
            form.AddText("MouseReleased vmID:" + vmID);
            pipe.PushMessage(e);
        }


        private static void Server_OnReceivedMessage(NamedPipeConnection<JavaEvent, JavaEvent> connection, JavaEvent message)
        {
            try
            {
                if (message == null)
                {
                    form.AddText(message.action);
                    return;
                }
                form.AddText(message.action);
            }
            catch (Exception ex)
            {
                form.AddText(ex.ToString());
            }
        }
    }
}
