using Newtonsoft.Json;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public class SocketCommand : Interfaces.ISocketCommand
    {
        public SocketCommand()
        {
            msg = new Message("ping");
        }
        public string error { get; set; }
        public string jwt { get; set; }
        [JsonIgnore]
        public Message msg { get; set; }
        IMessage ISocketCommand.msg { get => msg; set => msg = value as Message; }
        public async Task<T> SendMessage<T>(WebSocketClient ws) where T : new()
        {
            msg.data = JsonConvert.SerializeObject(this);
            var reply = await ws.SendMessage(msg);
            if (reply == null) return new T();
            if (reply.command == "error")
            {
                throw new Exception("server error: " + reply.data);
            }
            try
            {
                if (string.IsNullOrEmpty(reply.data)) return new T();
                if (reply.data == "{}") return new T();

                var result = JsonConvert.DeserializeObject<T>(reply.data, new JsonSerializerSettings
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Local
                });
                if (result == null)
                {
                    return result;
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(reply.data);
                Console.WriteLine(ex.ToString());
                throw;
            }
        }
    }
}
