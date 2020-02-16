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

    public class messagehandler
    {
        public event Action<NativeMessagingMessage> onMessage;
        private System.Threading.AutoResetEvent autoReset = null;
        public messagehandler()
        {
            Task.Run(async () =>
            {
                //var decoder = new TextDecoder("utf-8");

                while (true) // Loop runs only once per line received
                {
                    using (var stdin = Console.OpenStandardInput())
                    {
                        string input = "";
                        try
                        {
                            int length = 0;
                            byte[] bytes = new byte[4];
                            await stdin.ReadAsync(bytes, 0, 4);
                            length = BitConverter.ToInt32(bytes, 0);

                            //for (int i = 0; i < length; i++)
                            //{
                            //    input += (char)stdin.ReadByte();
                            //}

                            byte[] buff = new byte[length];
                            stdin.Read(buff, 0, buff.Length);

                            input += Encoding.UTF8.GetString(buff);
                            // input += Encoding.Default.GetString(buff);

                            // make sure to get message as UTF-8 format
                            //String msgStr = new String(msg, "UTF-8");

                            if (string.IsNullOrEmpty(input))
                            {
                                if (autoReset != null) autoReset.Set();
                                return;
                            }

                            //sstring msgStr = new string(input, "UTF-8");

                            var msg = JsonConvert.DeserializeObject<NativeMessagingMessage>(input);

                            if (msg.functionName == "zeniversescript")
                            {
                                var r2 = new NativeMessagingMessage() { functionName = msg.functionName };
                                loadscript(ref r2, "zeniverse");
                                sendMessage(r2);
                            }
                            if (msg.functionName == "contentscript")
                            {
                                var r2 = new NativeMessagingMessage() { functionName = msg.functionName };
                                loadscript(ref r2, "content");
                                sendMessage(r2);
                            }

                            if (msg.functionName == "backgroundscript")
                            {
                                var r2 = new NativeMessagingMessage() { functionName = msg.functionName };
                                loadscript(ref r2, "background");
                                sendMessage(r2);
                            }
#pragma warning disable 4014
                            Task.Run(() => { onMessage?.Invoke(msg); });
#pragma warning restore 4014
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Trace.WriteLine(ex.ToString());
                            System.Diagnostics.Trace.WriteLine(input);
                        }
                    }
                }
            });
        }
        static internal string GetStringFromResource(string resourceName)
        {
            string[] names = typeof(messagehandler).Assembly.GetManifestResourceNames();
            foreach (var name in names)
            {
                if (name.EndsWith(resourceName))
                {
                    using (Stream stream = typeof(messagehandler).Assembly.GetManifestResourceStream(name))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string result = reader.ReadToEnd();
                        return result;
                    }
                }
            }
            return null;
        }

        public static void loadscript(ref NativeMessagingMessage msg, string name)
        {
            msg.script = GetStringFromResource(name + ".js");
        }
        public void waitForExit()
        {
            autoReset = new System.Threading.AutoResetEvent(false);
            autoReset.WaitOne();
        }
        public void sendMessage(NativeMessagingMessage msg)
        {
            if (msg.functionName == "windowcreated" && !windows.Contains(msg.windowId)) windows.Add(msg.windowId);
            if (msg.functionName == "windowremoved" && windows.Contains(msg.windowId)) windows.Remove(msg.windowId);
            if (msg.functionName == "tabcreated") tabs.Add(msg.tab);
            if (msg.functionName == "tabremoved" || msg.functionName == "tabupdated")
            {
                var tab = tabs.Where(x => x.id == msg.tabid).FirstOrDefault();
                if (tab != null) tabs.Remove(tab);
                if (msg.functionName == "tabupdated") tabs.Add(msg.tab);
            }
            OpenStandardStreamOut(JsonConvert.SerializeObject(msg));
        }
        private static void OpenStandardStreamOut(string msgdata)
        {
            //// We need to send the 4 btyes of length information
            //string msgdata = "{\"text\":\"" + stringData + "\"}";
            int DataLength = msgdata.Length;
            Stream stdout = Console.OpenStandardOutput();
            stdout.WriteByte((byte)((DataLength >> 0) & 0xFF));
            stdout.WriteByte((byte)((DataLength >> 8) & 0xFF));
            stdout.WriteByte((byte)((DataLength >> 16) & 0xFF));
            stdout.WriteByte((byte)((DataLength >> 24) & 0xFF));
            //Available total length : 4,294,967,295 ( FF FF FF FF )
            Console.Write(msgdata);
        }

        public static List<int> windows = new List<int>();
        public static List<NativeMessagingMessageTab> tabs = new List<NativeMessagingMessageTab>();
    }

    class Program
    {
        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            aTimer.Interval = 5000;
            var r2 = new NativeMessagingMessage() { functionName = "ping" };
            handler.sendMessage(r2);
            aTimer.Start();
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
        static void Main(string[] args)
        {
            aTimer = new System.Timers.Timer();
            //System.Diagnostics.Debugger.Launch();
            //System.Diagnostics.Debugger.Break();

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
