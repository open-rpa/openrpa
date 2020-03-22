using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.NativeMessagingHost
{
    using System.Collections.Generic;
    using System.Linq;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Timers;
    using System.Text;
    using Newtonsoft.Json;
    using NamedPipeWrapper;
    using OpenRPA.Interfaces;

    class Program
    {
        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                aTimer.Interval = 5000;
                var r2 = new NativeMessagingMessage() { functionName = "ping" };
                handler.sendMessage(r2);
                aTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
        }
        static Timer aTimer = null;
        static messagehandler handler = null;
        public static NamedPipeServer<NativeMessagingMessage> pipe { get; set; }
        public const string PIPE_NAME = "openrpa_nativebridge";
        private static void Server_OnReceivedMessage(NamedPipeConnection<NativeMessagingMessage, NativeMessagingMessage> connection, NativeMessagingMessage message)
        {
            try
            {
                //if (string.IsNullOrEmpty(e.Message)) return;
                //var msg = JsonConvert.DeserializeObject<NativeMessagingMessage>(e.Message);
                System.Diagnostics.Trace.WriteLine("[resv]" + message.functionName + " for tab " + message.tabid + " - " + message.messageid);
                handler.sendMessage(message);
                //e.replyhandled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
        }
        private static object _lock = new object();
        private static string[] ignore = new string[] {
            "ping", "windowcreated", "windowremoved", "tabcreated", "tabupdated", "tabremoved",
            "mousemove", "mousedown", "click",
        "keydown", "keyup" };
        private static void onMessage(NativeMessagingMessage msg)
        {
            try
            {
                lock (_lock)
                {
                    if (pipe == null && !string.IsNullOrEmpty(msg.browser))
                    {
                        pipe = new NamedPipeServer<NativeMessagingMessage>(PIPE_NAME + "_" + msg.browser);
                        pipe.ClientMessage += Server_OnReceivedMessage;
                        //pipe.OnReceivedMessage += Server_OnReceivedMessage;
                        pipe.Start();
                    }
                }
                if (msg.functionName == "ping") return;
                //if (ignore.Contains(msg.functionName)) return;
                string json = JsonConvert.SerializeObject(msg, Formatting.Indented);
                if (!ignore.Contains(msg.functionName))
                {
                    System.Diagnostics.Trace.WriteLine("[send]" + msg.functionName + " for tab " + msg.tabid + " - " + msg.messageid);
                }
                try
                {
                    if (pipe != null)
                    {
                        pipe.PushMessage(msg);
                        //System.Diagnostics.Trace.WriteLine(result);
                        //var result = pipe.PushMessage(msg);
                        //System.Diagnostics.Trace.WriteLine(result);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.ToString());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
        }
        static void Main(string[] args)
        {
            aTimer = new System.Timers.Timer();
            // System.Diagnostics.Debugger.Launch();
            // System.Diagnostics.Debugger.Break();

            handler = new messagehandler();
            handler.onMessage += onMessage;

            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.AutoReset = false;
            aTimer.Interval = 1000;
            aTimer.Enabled = true;
            handler.waitForExit();
            Console.WriteLine("END");
        }
    }

}
