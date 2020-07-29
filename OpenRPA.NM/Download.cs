using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.NM
{
    public class Download
    {
        public long bytesReceived { get; set; }
        public bool canResume { get; set; }
        public string danger { get; set; }
        public bool exists { get; set; }
        public long fileSize { get; set; }
        public string filename { get; set; }
        public string finalUrl { get; set; }
        public string id { get; set; }
        public bool incognito { get; set; }
        public string mime { get; set; }
        public bool paused { get; set; }
        public string referrer { get; set; }
        public string state { get; set; }
        public long totalBytes { get; set; }
        public string url { get; set; }
        public DateTime startTime { get; set; }
        public DateTime endTime { get; set; }
    }
}
