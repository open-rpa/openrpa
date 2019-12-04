using OpenRPA.Interfaces.entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public class SigninMessage : SocketCommand
    {
        public SigninMessage() : base()
        {
            msg.command = "signin";
        }
        public SigninMessage(string username, SecureString password) : base()
        {
            msg.command = "signin";
            this.username = username;
            this.password = new System.Net.NetworkCredential(string.Empty, password).Password;
        }
        public SigninMessage(string jwt) : base()
        {
            msg.command = "signin";
            this.jwt = jwt;
        }
        public SigninMessage(SecureString jwt) : base()
        {
            msg.command = "signin";
            this.jwt = new System.Net.NetworkCredential(string.Empty, jwt).Password; 
        }
        public bool validate_only { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public TokenUser user { get; set; }
    }
}
