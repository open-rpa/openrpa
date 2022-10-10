using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OpenRPA.NM
{
    [System.ComponentModel.Designer(typeof(GetTableDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetTable), "Resources.toolbox.gettable.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    [LocalizedToolboxTooltip("activity_gettable_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_gettable", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_gettable_helpurl", typeof(Resources.strings))]
    public class GetTable : NativeActivity
    {
        [RequiredArgument]
        public InArgument<string> xPath { get; set; }
        public InArgument<string> Rowsxpath { get; set; }
        public InArgument<string> Cellsxpath { get; set; }
        public InArgument<string> Cellxpath { get; set; }
        public InArgument<string> Headerrowsxpath { get; set; }
        public InArgument<string> Headerrowxpath { get; set; }
        public InArgument<int> HeaderRowIndex { get; set; }
        public InArgument<string> Browser { get; set; }
        //public InArgument<string> Culture { get; set; }
        public InArgument<bool> SkipTypeCheck { get; set; }
        public OutArgument<DataTable> Result { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            var xpath = xPath.Get(context);
            var rowsxpath = Rowsxpath.Get(context);
            var cellsxpath = Cellsxpath.Get(context);
            var cellxpath = Cellxpath.Get(context);
            var headerrowsxpath = Headerrowsxpath.Get(context);
            var headerrowxpath = Headerrowxpath.Get(context);
            var headerrowindex = HeaderRowIndex.Get(context);
            var skiptypecheck = SkipTypeCheck.Get(context);
            var browser = Browser.Get(context);
            // var culture = Culture.Get(context);
            var timeout = TimeSpan.FromSeconds(3);
            if (browser != "chrome" && browser != "ff" && browser != "edge") browser = "chrome";
            var result = NMHook.GetTablev1(browser, xpath, rowsxpath, cellxpath, cellsxpath, headerrowsxpath, headerrowxpath, headerrowindex, skiptypecheck, timeout);
            if(result != null)
            {
                var json = result.ToString();
                var settings = new JsonSerializerSettings();
                //if(!string.IsNullOrEmpty(culture))
                //{
                //    settings.Culture = System.Globalization.CultureInfo.GetCultureInfo(culture);
                //}
                DataTable ds = JsonConvert.DeserializeObject<DataTable>(json, settings);
                context.SetValue(Result, ds);

            }
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
        }
        [LocalizedDisplayName("activity_displayname", typeof(Resources.strings)), LocalizedDescription("activity_displayname_help", typeof(Resources.strings))]
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