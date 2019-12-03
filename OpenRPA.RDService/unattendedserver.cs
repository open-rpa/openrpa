using OpenRPA.Interfaces.entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.RDService
{
    public class unattendedserver : apibase
    {
        public unattendedserver() { _type = "unattendedserver"; }
        public string computername { get; set; }
        public string computerfqdn { get; set; }
        public bool enabled { get; set; }
    }
}
