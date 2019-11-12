using OpenRPA.Interfaces.entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.RDService
{
    public class unattendedclient : apibase
    {
        public unattendedclient() { _type = "unattendedclient"; _encrypt = new string[] { "windowspassword" }; }
        public string windowsusername { get; set; }
        public string windowspassword { get; set; }
        public string computername { get; set; }
        public string computerfqdn { get; set; }
        public string openrpapath { get; set; } = @"%windir%\system32\notepad.exe";
        public TimeSpan autorestart { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan rdpretry { get; set; } = TimeSpan.FromMinutes(30);
    }
}
