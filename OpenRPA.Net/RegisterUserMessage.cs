using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public class RegisterUserMessage : SocketCommand
    {
        public RegisterUserMessage() : base()
        {
            msg.command = "registerqueue";
        }
        public RegisterUserMessage(string name, string username, string password) : base()
        {
            this.name = name;
            this.username = username;
            this.password = password;
            msg.command = "registeruser";
        }
        public string name { get; set; }
        public string username { get; set; }
        public string password { get; set; }
    }
}
