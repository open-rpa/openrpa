using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
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
    class rightsConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(string)) || (objectType == typeof(int));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            if (token.Type == JTokenType.Integer)
            {
                return token.ToObject<int>();
            }
            if (token.Type == JTokenType.String)
            {
                return token.ToObject<string>();
            }
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if(!string.IsNullOrEmpty(value.ToString()))
            {
                var v = value.ToString();
                bool isIntString = v.All(char.IsDigit);
                if(isIntString && int.TryParse(v, out int i))
                {
                    serializer.Serialize(writer, i);
                    return;
                }
            }
            serializer.Serialize(writer, value);
        }
    }

    public class ace : IEquatable<ace>
    {
        public bool isInteger(object s)
        {
            if (s == null) return true;
            return s.ToString().All(char.IsDigit);
        }
        public ace()
        {
            rights = 65535;
            //    rights = "//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////8=";
        }
        public ace(ace a)
        {
            deny = a.deny;
            rights = a.rights;
            _id = a._id;
            name = a.name;
        }
        public bool deny { get; set; }
        [JsonConverter(typeof(rightsConverter))]
        [JsonProperty("rights")]
        public object rights { get; set; }
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
            if(isInteger(rights))
            {
                rights = (int)rights | (1 << (int)bit);
                return;
            }
            byte[] view = Convert.FromBase64String(rights.ToString());
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
            if (isInteger(rights))
            {
                rights = (int)rights & ~(1 << (int)bit);
                return;
            }
            byte[] view = Convert.FromBase64String(rights.ToString());
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
            if (isInteger(rights))
            {
                var isset = ((int)rights & (1 << (int)bit)) != 0;
                return isset;
            }
            byte[] view = Convert.FromBase64String(rights.ToString());
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
