using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.entity
{
    public enum ace_right
    {
        create = 1,
        read = 2,
        update = 3,
        delete = 4,
        invoke = 5
    }
    public class ace : IEquatable<ace>
    {
        public ace()
        {
            rights = "//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////8=";
        }
        public ace(ace a)
        {
            deny = a.deny;
            rights = a.rights;
            _id = a._id;
            name = a.name;
        }
        public bool deny { get; set; }
        public string rights { get; set; }
        public string _id { get; set; }
        public string name { get; set; }

        [Newtonsoft.Json.JsonIgnore()]
        public bool Read
        {
            get
            {
                return getBit((decimal)ace_right.read);
            }
            set
            {
                if (value == true)
                {
                    setBit((decimal)ace_right.read);
                    return;
                }
                unsetBit((decimal)ace_right.read);
            }
        }
        [Newtonsoft.Json.JsonIgnore()]
        public bool Update
        {
            get
            {
                return getBit((decimal)ace_right.update);
            }
            set
            {
                if (value == true)
                {
                    setBit((decimal)ace_right.update);
                    return;
                }
                unsetBit((decimal)ace_right.update);
            }
        }
        [Newtonsoft.Json.JsonIgnore()]
        public bool Delete
        {
            get
            {
                return getBit((decimal)ace_right.delete);
            }
            set
            {
                if (value == true)
                {
                    setBit((decimal)ace_right.delete);
                    return;
                }
                unsetBit((decimal)ace_right.delete);
            }
        }
        [Newtonsoft.Json.JsonIgnore()]
        public bool Invoke
        {
            get
            {
                return getBit((decimal)ace_right.invoke);
            }
            set
            {
                if (value == true)
                {
                    setBit((decimal)ace_right.invoke);
                    return;
                }
                unsetBit((decimal)ace_right.invoke);
            }
        }
        double getMask(double bit)
        {
            return Math.Pow(2, bit);
        }
        public void setBit(decimal bit)
        {
            bit--;
            byte[] view = Convert.FromBase64String(rights);
            var octet = Math.Floor(bit / 8);
            var currentValue = view[(int)octet];
            var _bit = (bit % 8);
            var mask = getMask((double)_bit);
            var newValue = currentValue | (byte)mask;
            view[(int)octet] = (byte)newValue;
            rights = Convert.ToBase64String(view);
        }
        public void unsetBit(decimal bit)
        {
            bit--;
            byte[] view = Convert.FromBase64String(rights);
            var octet = Math.Floor(bit / 8);
            var currentValue = view[(int)octet];
            var _bit = (bit % 8);
            var mask = getMask((double)_bit);
            var newValue = currentValue &= (byte)~(byte)mask;
            view[(int)octet] = (byte)newValue;
            rights = Convert.ToBase64String(view);
        }
        public bool getBit(decimal bit)
        {
            bit--;
            byte[] view = Convert.FromBase64String(rights);
            var octet = Math.Floor(bit / 8);
            var currentValue = view[(int)octet];
            var _bit = (bit % 8);
            var bitValue = Math.Pow(2, (double)_bit);
            return (currentValue & (byte)bitValue) != 0;
        }
        public bool Equals(ace other)
        {
            if (other is null)
                return false;
            return _id == other._id && rights == other.rights;
        }
        public override bool Equals(object obj) => Equals(obj as ace);
        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + _id.GetHashCode();
            hash = (hash * 7) + rights.GetHashCode();
            return hash;
        }
    }
}
