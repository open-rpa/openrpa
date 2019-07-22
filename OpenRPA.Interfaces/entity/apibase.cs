using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.entity
{
    public class apibase : ObservableObject
    {
        public string _id { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string _type { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string name { get { return GetProperty<string>(); } set { SetProperty(value); } }

        public DateTime _modified { get { return GetProperty<DateTime>(); } set { SetProperty(value); } }
        public string _modifiedby { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string _modifiedbyid { get { return GetProperty<string>(); } set { SetProperty(value); } }

        public DateTime _created { get { return GetProperty<DateTime>(); } set { SetProperty(value); } }
        public string _createdby { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string _createdbyid { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public ace[] _acl  { get { return GetProperty<ace[]>(); } set { SetProperty(value); } }

        public bool hasRight(apiuser user, ace_right bit)
        {
            var ace = _acl.Where(x => x._id == user._id).FirstOrDefault();
            if (ace != null) { if (ace.getBit((decimal)bit)) return true; }
            foreach (var role in user.roles)
            {
                ace = _acl.Where(x => x._id == role._id).FirstOrDefault();
                if (ace != null) { if (ace.getBit((decimal)bit)) return true; }
            }
            return false;
        }
        public bool hasRight(TokenUser user, ace_right bit)
        {
            var ace = _acl.Where(x => x._id == user._id).FirstOrDefault();
            if (ace != null) { if (ace.getBit((decimal)bit)) return true; }
            foreach (var role in user.roles)
            {
                ace = _acl.Where(x => x._id == role._id).FirstOrDefault();
                if (ace != null) { if (ace.getBit((decimal)bit)) return true; }
            }
            return false;
        }

        //public void delete()
        //{
        //    rpaactivities.socketService.instance.DELETE(_id, "workflows");
        //}
        //public void save()
        //{
        //    if (string.IsNullOrEmpty(_id))
        //    {
        //        var result = rpaactivities.socketService.instance.POST(this, "workflows");
        //        if (result != null)
        //        {
        //            _id = result._id;
        //            _type = result._type;
        //            _modified = result._modified;
        //            _modifiedby = result._modifiedby;
        //            _modifiedbyid = result._modifiedbyid;
        //            _created = result._created;
        //            _createdby = result._createdby;
        //            _createdbyid = result._createdbyid;
        //        }
        //    }
        //    else
        //    {
        //        var result = rpaactivities.socketService.instance.PUT(this, "workflows");
        //        if (result != null)
        //        {
        //            _modified = result._modified;
        //            _modifiedby = result._modifiedby;
        //            _modifiedbyid = result._modifiedbyid;
        //        }
        //    }
        //}

    }
}
