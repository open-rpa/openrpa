using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public class SocketCommand
    {
        public SocketCommand()
        {
            msg = new Message("ping");
        }
        public string error { get; set; }
        public string jwt { get; set; }
        [JsonIgnore]
        public Message msg { get; set; }

        public async Task<T> SendMessage<T>(WebSocketClient ws)
        {
            msg.data = JsonConvert.SerializeObject(this);
            var reply = await ws.SendMessage(msg);
            var result = JsonConvert.DeserializeObject<T>(reply.data);
            return result;
        }
    }
}
