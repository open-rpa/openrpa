using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Elis.Rossum
{
    [System.ComponentModel.Designer(typeof(ExportFileDataDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ExportFileData), "Resources.toolbox.exportfiledata.png")]
    [LocalizedToolboxTooltip("activity_exportfiledata_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_exportfiledata", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_exportfiledata_helpurl", typeof(Resources.strings))]
    public class ExportFileData : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Key { get; set; }
        [RequiredArgument]
        public InArgument<string> Queue { get; set; }
       
        [RequiredArgument]
        public InArgument<string> Fileurl { get; set; }
        [RequiredArgument]
        public OutArgument<ExportResult> Result { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var key = Key.Get(context);
            var queue = Queue.Get(context);
            var fileurl = Fileurl.Get(context);
            var status = "";
            while (status != "to_review" && status != "exported")
            {
                var statusres = SimpleRequests.GET(fileurl, key);
                var statuso = JObject.Parse(statusres);
                status = statuso["status"].ToString();
            }
            var fileid = fileurl.Substring(fileurl.LastIndexOf("/") + 1);
            var res = SimpleRequests.GET(queue + "/export?status=exported&format=json&id=" + fileid, key);
            var results = JsonConvert.DeserializeObject<ExportResult>(res);
            try
            {
                results.LoadGeneralData();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            context.SetValue(Result, results);
        }
        public new string DisplayName
        {
            get
            {
                var displayName = base.DisplayName;
                if (displayName == this.GetType().Name)
                {
                    var displayNameAttribute = this.GetType().GetCustomAttributes(typeof(DisplayNameAttribute), true).FirstOrDefault() as DisplayNameAttribute;
                    if (displayNameAttribute != null) displayName = displayNameAttribute.DisplayName;
                }
                return displayName;
            }
            set
            {
                base.DisplayName = value;
            }
        }
    }
}