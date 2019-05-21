using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Activities
{
    public class vKey
    {
        public vKey() { }
        public vKey(int KeyValue, bool up)
        {
            this.KeyValue = KeyValue;
            this.up = up;
        }
        public vKey(FlaUI.Core.WindowsAPI.VirtualKeyShort k, bool up)
        {
            KeyCode = k;
            this.up = up;
        }
        public FlaUI.Core.WindowsAPI.VirtualKeyShort KeyCode { get; set; }
        public int KeyValue { get; set; }
        public bool up { get; set; }
    }
}
