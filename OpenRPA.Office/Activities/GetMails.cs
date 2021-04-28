using Microsoft.Office.Interop.Outlook;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Office.Activities
{
    [System.ComponentModel.Designer(typeof(GetMailsDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.outlook.png")]
    [LocalizedToolboxTooltip("activity_getmails_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_getmails", typeof(Resources.strings))]
    public class GetMails : BreakableLoop, System.Activities.Presentation.IActivityTemplateFactory
    {
        public GetMails()
        {
            UnreadOnly = false;
        }
        [RequiredArgument]
        [Category("Input")]
        public InArgument<bool> UnreadOnly { get; set; }
        [RequiredArgument]
        [Category("Input")]
        public InArgument<string> Folder { get; set; }
        [Category("Input")]
        public InArgument<int> MaxResults { get; set; }
        [Category("Input")]
        public InArgument<string> Filter { get; set; }
        [Category("Output")]
        public OutArgument<System.Collections.Generic.IEnumerable<email>> Emails { get; set; }
        [Browsable(false)]
        public ActivityAction<email> Body { get; set; }
        private readonly Variable<IEnumerator<email>> _elements = new Variable<IEnumerator<email>>("_elements");
        private Application CreateOutlookInstance()
        {
            var outlookApplication = new Microsoft.Office.Interop.Outlook.Application();
            if (outlookApplication.ActiveExplorer() == null)
            {
                // mOutlookExplorer = mOutlookApplication.Session.GetDefaultFolder(OlDefaultFolders.olFolderCalendar).GetExplorer();
                var mOutlookExplorer = outlookApplication.Session.GetDefaultFolder(OlDefaultFolders.olFolderInbox).GetExplorer();
                mOutlookExplorer.Activate();
            }
            return outlookApplication;
        }
        protected override void StartLoop(NativeActivityContext context)
        {
            var folder = Folder.Get(context);
            var maxresults = MaxResults.Get(context);
            var filter = Filter.Get(context);
            if (string.IsNullOrEmpty(folder)) return;
            var outlookApplication = CreateOutlookInstance();
            if (outlookApplication.ActiveExplorer() == null)
            {
                Log.Warning("Outlook not running!");
                return;
            }
            MAPIFolder mfolder = GetFolder(outlookApplication, folder);
            Items Items = mfolder.Items;
            var unreadonly = UnreadOnly.Get(context);
            if (unreadonly)
            {
                if (string.IsNullOrEmpty(filter)) filter = "";
                if (string.IsNullOrEmpty(filter))
                {
                    filter = "[Unread]=true";
                }
                else
                {
                    filter += "and [Unread]=true";
                }
            }
            if (!string.IsNullOrEmpty(filter))
            {
                Items = Items.Restrict(filter);
            }
            var result = new List<email>();
            foreach (var folderItem in Items)
            {

                MailItem mailItem = folderItem as MailItem;
                if (mailItem != null)
                {
                    var _e = new email(mailItem);
                    result.Add(_e);
                    if (result.Count == maxresults) break;
                }
            }
            Emails.Set(context, result);
            IEnumerator<email> _enum = result.ToList().GetEnumerator();
            context.SetValue(_elements, _enum);
            bool more = _enum.MoveNext();
            if (more)
            {
                context.ScheduleAction(Body, _enum.Current, OnBodyComplete);
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<email> _enum = _elements.Get(context);
            bool more = _enum.MoveNext();
            if (more && !breakRequested)
            {
                context.ScheduleAction<email>(Body, _enum.Current, OnBodyComplete);
            }
            else
            {
            }
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            Interfaces.Extensions.AddCacheArgument(metadata, "UnreadOnly", UnreadOnly);
            Interfaces.Extensions.AddCacheArgument(metadata, "Folder", Folder);
            Interfaces.Extensions.AddCacheArgument(metadata, "Emails", Emails);

            metadata.AddImplementationVariable(_elements);
            base.CacheMetadata(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            var fef = new GetMails();
            var aa = new ActivityAction<email>();
            var da = new DelegateInArgument<email>();
            da.Name = "item";
            fef.Body = aa;
            aa.Argument = da;
            return fef;
        }
        //public MAPIFolder GetFolder(MAPIFolder folder, string FullFolderPath)
        //{
        //    if (folder.Folders.Count == 0)
        //    {
        //        if (folder.FullFolderPath == FullFolderPath)
        //        {
        //            return folder;
        //        }
        //    }
        //    else
        //    {
        //        foreach (MAPIFolder subFolder in folder.Folders)
        //        {
        //            if (folder.FullFolderPath == FullFolderPath)
        //            {
        //                return folder;
        //            }
        //            var temp = GetFolder(subFolder, FullFolderPath);
        //            if (temp != null) return temp;
        //        }
        //    }
        //    return null;
        //}
        //public MAPIFolder GetFolder(NameSpace folder, string FullFolderPath)
        //{
        //    foreach (MAPIFolder subFolder in folder.Folders)
        //    {
        //        var temp = GetFolder(subFolder, FullFolderPath);
        //        if (temp != null) return temp;
        //    }
        //    return null;
        //}
        private Microsoft.Office.Interop.Outlook.Folder GetFolder(Application application, string folderPath)
        {
            Microsoft.Office.Interop.Outlook.Folder folder;
            string backslash = @"\";
            try
            {
                if (folderPath.StartsWith(@"\\"))
                {
                    folderPath = folderPath.Remove(0, 2);
                }
                String[] folders =
                    folderPath.Split(backslash.ToCharArray());
                folder = application.Session.Folders[folders[0]] as Microsoft.Office.Interop.Outlook.Folder;
                if (folder != null)
                {
                    for (int i = 1; i <= folders.GetUpperBound(0); i++)
                    {
                        Microsoft.Office.Interop.Outlook.Folders subFolders = folder.Folders;
                        folder = subFolders[folders[i]] as Microsoft.Office.Interop.Outlook.Folder;
                        if (folder == null)
                        {
                            return null;
                        }
                    }
                }
                return folder;
            }
            catch { return null; }
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