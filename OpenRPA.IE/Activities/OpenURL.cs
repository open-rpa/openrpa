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

namespace OpenRPA.IE
{
    [System.ComponentModel.Designer(typeof(OpenURLDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(OpenURL), "Resources.toolbox.gethtmlelement.png")]
    [LocalizedToolboxTooltip("activity_openurl_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_openurl", typeof(Resources.strings))]
    public class OpenURL : NativeActivity
    {
        [RequiredArgument]
        public InArgument<string> Url { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            var url = Url.Get(context);
            var browser = Browser.GetBrowser(url);
            var timeout = TimeSpan.FromSeconds(3);
            var doc = browser.Document;
            if (!string.IsNullOrEmpty(url))
            {
                if(doc.url != url) doc.url = url;
            }
            browser.Show();
            var sw = new Stopwatch();
            sw.Start();
            while (sw.Elapsed < timeout && doc.readyState != "complete" && doc.readyState != "interactive")
            {
                Log.Debug("pending complete, readyState: " + doc.readyState);
                System.Threading.Thread.Sleep(100);
            }

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