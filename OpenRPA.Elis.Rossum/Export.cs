using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Elis.Rossum
{
    public class QueuesResult
    {
        public Pagination pagination { get; set; }
        public Queues[] results { get; set; }
    }
    public class Queues
    {
        public string id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public string workspace { get; set; }
        public string connector { get; set; }
        // public JObject webhooks { get; set; }
        public string schema { get; set; }
        public string inbox { get; set; }
        public TimeSpan session_timeout { get; set; }
        public string rir_url { get; set; }
        // public JObject rir_params { get; set; }
        public Counts counts { get; set; }
        public string default_score_threshold { get; set; }
        public bool automation_enabled { get; set; }
        public string automation_level { get; set; }
        public string locale { get; set; }
        // public JObject metadata { get; set; }
    }
    public class Counts
    {
        public int importing { get; set; }
        public int split { get; set; }
        public int failed_import { get; set; }
        public int to_review { get; set; }
        public int reviewing { get; set; }
        public int exporting { get; set; }
        public int postponed { get; set; }
        public int failed_export { get; set; }
        public int exported { get; set; }
        public int deleted { get; set; }
        public int purged { get; set; }
    }
    public class ExportResult
    {
        public Pagination pagination { get; set; }
        public Export[] results { get; set; }
        public Datapoint[] alldata { get; set; }
        public string invoice_id { get; set; }
        public DateTime date_issue { get; set; }
        public string date_due { get; set; }
        public string sender_ic { get; set; }
        private List<Datapoint> getDatapoint(Datapoint data)
        {
            var result = new List<Datapoint>();
            result.Add(data);
            if (data.children != null)
            {
                foreach (var c in data.children)
                {
                    foreach (var d in c.children)
                    {
                        result.AddRange(getDatapoint(d));
                    }
                }
            }
            return result;
        }
        public void LoadGeneralData()
        {
            var points = new List<Datapoint>();
            foreach (var r in results)
            {
                foreach (var e in r.content)
                {
                    foreach (var d in e.children)
                    {
                        points.AddRange(getDatapoint(d));
                    }
                }
            }
            alldata = points.ToArray();
            var f = alldata.Where(x => x.schema_id == "invoice_id").FirstOrDefault();
            if (f != null) invoice_id = f.value;
            f = alldata.Where(x => x.schema_id == "date_issue").FirstOrDefault();
            if (f != null)
            {
                var o = JObject.Parse("{\"datetime\": \"" + f.value + "\"}");
                date_issue = o["datetime"].Value<DateTime>();
            }
            f = alldata.Where(x => x.schema_id == "date_due").FirstOrDefault();
            if (f != null) date_due = f.value;
            f = alldata.Where(x => x.schema_id == "sender_ic").FirstOrDefault();
            if (f != null) sender_ic = f.value;
        }
    }
    public class Pagination
    {
        public int total { get; set; }
        public int total_pages { get; set; }
        public string next { get; set; }
        public string previous { get; set; }
    }
    public class Document
    {
        public string url { get; set; }
        public string file_name { get; set; }
        public string file { get; set; }
    }
    public class Schema
    {
        public string url { get; set; }
    }
    public class user
    {
        public string url { get; set; }
        public string username { get; set; }
    }
    public class Export
    {
        public string url { get; set; }
        public string status { get; set; }
        public DateTime arrived_at { get; set; }
        public DateTime exported_at { get; set; }
        public Document document { get; set; }
        public user modifier { get; set; }
        public Schema schema { get; set; }
        // public JObject metadata { get; set; }
        public ExportContent[] content { get; set; }
    }
    public class Datapoint
    {
        public string category { get; set; }
        public string schema_id { get; set; }
        public string value { get; set; }
        public string type { get; set; }
        public string rir_confidence { get; set; }
        public ExportContent[] children { get; set; }
        public override string ToString()
        {
            return schema_id + ":" + value;
        }
    }
    public class ExportContent
    {
        public string category { get; set; }
        public string schema_id { get; set; }
        public Datapoint[] children { get; set; }
    }
}
