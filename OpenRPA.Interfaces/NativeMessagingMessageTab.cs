using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    [Serializable]
    public class NativeMessagingMessageTab
    {
        public string browser { get; set; }
        public bool active { get; set; }
        public bool audible { get; set; }
        public bool autoDiscardable { get; set; }
        public bool discarded { get; set; }
        public string favIconUrl { get; set; }
        public int height { get; set; }
        public bool highlighted { get; set; }
        public int id { get; set; }
        public bool incognito { get; set; }
        public int index { get; set; }
        public bool pinned { get; set; }
        public bool selected { get; set; }
        public string status { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public int width { get; set; }
        public int windowId { get; set; }
        public DateTime? lastready { get; set; }
    }
}
