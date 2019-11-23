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

namespace OpenRPA.OpenFlowDB
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
        public InArgument<string> Projection { get; set; }
        public InArgument<string> Orderby { get; set; }
        public InArgument<int> Top { get; set; }
        public InArgument<int> Skip { get; set; }
        protected async override Task<JObject[]> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var ignoreErrors = IgnoreErrors.Get(context);
            var collection = Collection.Get(context);
            var querystring = QueryString.Get(context);
            var projection = Projection.Get(context);
            var top = Top.Get(context);
            var skip = Skip.Get(context);
            if (top < 1) top = 1;
            if (skip < 0) skip = 0;
            var orderby = Orderby.Get(context);
            if (string.IsNullOrEmpty(collection)) collection = "entities";
            JObject[] result = null;
            result = await global.webSocketClient.Query<JObject>(collection, querystring, projection, top, skip, orderby);
            System.Windows.Forms.Application.DoEvents();
            return result;
        }
    }
}
