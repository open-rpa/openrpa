using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(QueryDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.getentities.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class Query : AsyncTaskCodeActivity<JObject[]>
    {
        [RequiredArgument]
        public InArgument<bool> IgnoreErrors { get; set; } = false;
        [RequiredArgument]
        public InArgument<string> QueryString { get; set; }
        [RequiredArgument]
        public InArgument<string> Collection { get; set; } = "entities";
        protected async override Task<JObject[]> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var ignoreErrors = IgnoreErrors.Get(context);
            var collection = Collection.Get(context);
            var querystring = QueryString.Get(context);
            if (string.IsNullOrEmpty(collection)) collection = "entities";
            JObject[] result = null;
            result = await global.webSocketClient.Query<JObject>(collection, querystring);
            System.Windows.Forms.Application.DoEvents();
            return result;
        }
    }
}
