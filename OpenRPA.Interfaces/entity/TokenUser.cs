using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.entity
{
    public class TokenUser
    {
        public string _id { get; set; }
        public string name { get; set; }
        public string username { get; set; }
        public Rolemember[] roles { get; set; }
        public bool hasRole(string role)
        {
            foreach (var r in roles)
            {
                if (r.name == role || r._id == role)
                {
                    return true;
                }
            }
            return false;
        }
        public override string ToString()
        {
            return name + " " + username + " [" + string.Join(",", roles.Select(w => w.name)) + "]";
        }

    }
}
