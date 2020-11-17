using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.NM
{
    [System.ComponentModel.Designer(typeof(ExecuteScriptDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ExecuteScript), "Resources.toolbox.gethtmlelement.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    [LocalizedToolboxTooltip("activity_executescript_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_executescript", typeof(Resources.strings))]
    public class ExecuteScript : NativeActivity
    {
        [RequiredArgument]
        public InArgument<string> Script { get; set; }
        public InArgument<int> FrameId { get; set; }
        public InArgument<string> Browser { get; set; }
        public OutArgument<object> Result { get; set; }
        public OutArgument<object[]> Results { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            var script = Script.Get(context);
            var frameid = FrameId.Get(context);
            var browser = Browser.Get(context);
            var timeout = TimeSpan.FromSeconds(3);
            script = Interfaces.Selector.Selector.ReplaceVariables(script, context.DataContext);
            if (browser != "chrome" && browser != "ff" && browser != "edge") browser = "chrome";
            if (!script.Contains(Environment.NewLine) && !script.Contains(";") && !script.Contains("return")) script = "return " + script;
            var result = NMHook.ExecuteScript(browser, frameid, -1, script, timeout);
            if (result == null) { result = "[]"; }
            var results = JsonConvert.DeserializeObject<object[]>(result.ToString());
            Result.Set(context, results[0]);
            Results.Set(context, results);
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
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