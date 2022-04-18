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
    [System.ComponentModel.Designer(typeof(OpenURLDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(OpenURL), "Resources.toolbox.gethtmlelement.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    [LocalizedToolboxTooltip("activity_openurl_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_openurl", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_openurl_helpurl", typeof(Resources.strings))]
    public class OpenURL : NativeActivity
    {
        [RequiredArgument]
        public InArgument<string> Url { get; set; }
        public InArgument<string> Browser { get; set; }
        public InArgument<bool> NewTab { get; set; }
        [System.ComponentModel.Category("Misc")]
        [Editor(typeof(UserDataFolderModeEditor), typeof(System.Activities.Presentation.PropertyEditing.ExtendedPropertyValueEditor))]
        public InArgument<string> UserDataFolderMode { get; set; }
        [System.ComponentModel.Category("Misc")]
        public InArgument<string> UserDataFolderPath { get; set; }        
        public OpenURL()
        {
        }
        protected override void Execute(NativeActivityContext context)
        {
            var url = Url.Get(context);
            var browser = Browser.Get(context);
            var timeout = TimeSpan.FromSeconds(3);
            var newtab = NewTab.Get(context);
            if (browser != "chrome" && browser != "ff" && browser != "edge") browser = "chrome";
            if (!string.IsNullOrEmpty(url))
            {
                NMHook.enumtabs();
                var tab = NMHook.FindTabByURL(browser, url);
                if (tab != null)
                {
                    if (!tab.highlighted || !tab.selected)
                    {
                        var _tab = NMHook.selecttab(browser, tab.id);
                    }
                }
            }
            var userDataFolderMode = UserDataFolderMode.Get(context);
            var userDataFolderPath = UserDataFolderPath.Get(context);
            string profilepath = "";
            string profilename = "";
            if (string.IsNullOrEmpty(userDataFolderMode)) userDataFolderMode = "automatic";
            userDataFolderMode = userDataFolderMode.ToLower();
            if (userDataFolderMode == "automatic") // child session uses a different folder than the default mode, automatically generated if UserDataFolderPath is not set
            {
                if(Plugin.client.isRunningInChildSession)
                {
                    profilepath = userDataFolderPath;
                    if (string.IsNullOrEmpty(profilepath))
                    {
                        profilepath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\openrpa\\ChildSession\\" + browser;
                        profilename = "ChildSession";
                    }
                    
                }
            }
            else if (userDataFolderMode == "customfolder")
            {
                profilepath = userDataFolderPath;
                if (string.IsNullOrEmpty(profilepath))
                {
                    if (Plugin.client.isRunningInChildSession)
                    {
                        profilepath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\openrpa\\ChildSession\\" + browser;
                        profilename = "ChildSession";
                    } 
                    else
                    {
                        profilepath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\openrpa\\" + browser;
                        profilename = "openrpa";
                    }
                }
            }
            else if (userDataFolderMode == "defaultfolder")
            {
                // Uses the default browser folder, no matter what
            }

            NMHook.openurl(browser, url, newtab, profilename, profilepath);
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
    class UserDataFolderModeEditor : CustomSelectEditor
    {
        public override System.Data.DataTable options
        {
            get
            {
                var lst = new System.Data.DataTable();
                lst.Columns.Add("ID", typeof(string));
                lst.Columns.Add("TEXT", typeof(string));
                lst.Rows.Add("automatic", "Automatic");
                lst.Rows.Add("defaultfolder", "Default Folder");
                lst.Rows.Add("customfolder", "Custom Folder");
                return lst;
            }
        }
    }
}