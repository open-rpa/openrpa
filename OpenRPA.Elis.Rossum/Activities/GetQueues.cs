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
    [LocalizedToolboxTooltip("activity_getqueues_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_getqueues", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_getqueues_helpurl", typeof(Resources.strings))]
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