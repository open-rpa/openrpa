using NamedPipeWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRPA.JavaBridge
{
    [Serializable]
    public class JavaEvent : PipeMessage
    {
        public string action { get; set; }
        public int vmID { get; set; }
        public Int64 jevent { get; set; }
        public Int64 ac { get; set; }
        public JavaEvent() : base()
        {
        }
        public JavaEvent(string action) : base()
        {
            this.action = action;
        }
        public JavaEvent(string action, int vmID) : base()
        {
            this.action = action;
            this.vmID = vmID;
        }
        public JavaEvent(string action, int vmID, IntPtr jevent, IntPtr ac) : base()
        {
            this.action = action;
            this.vmID = vmID;
            this.jevent = jevent.ToInt64();
            this.ac = ac.ToInt64();
        }
    }
}
