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
    [System.ComponentModel.Designer(typeof(GetQueuesDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetQueues), "Resources.toolbox.getimage.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class GetQueues : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Key { get; set; }
        [RequiredArgument]
        public OutArgument<Queues[]> Results { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var key = Key.Get(context);
            var res = SimpleRequests.GET("https://api.elis.rossum.ai/v1/queues?page_size=1", key);
            var qs = JsonConvert.DeserializeObject<QueuesResult>(res);
            context.SetValue(Results, qs.results);
        }
    }
}