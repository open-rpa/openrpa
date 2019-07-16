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
    //[designer.ToolboxTooltip(Text = "Add inline comments, a supplement to the built-in annotation feature")]
    public class GetMails : NativeActivity, System.Activities.Presentation.IActivityTemplateFactory
    {
        public GetMails()
        {
            UnreadOnly = false;
        }
        [RequiredArgument]
        [System.ComponentModel.Category("Input")]
        public InArgument<bool> UnreadOnly { get; set; }
        [RequiredArgument]
        [System.ComponentModel.Category("Input")]
        public InArgument<string> Folder { get; set; }
        [System.ComponentModel.Category("Output")]
        public OutArgument<System.Collections.Generic.IEnumerable<email>> Emails { get; set; }

        [System.ComponentModel.Browsable(false)]
        public ActivityAction<email> Body { get; set; }
        private Variable<IEnumerator<email>> _elements = new Variable<IEnumerator<email>>("_elements");
        private Microsoft.Office.Interop.Outlook.Application CreateOutlookInstance()
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

        protected override void Execute(NativeActivityContext context)
        {
            var folder = Folder.Get(context);
            if (string.IsNullOrEmpty(folder)) return;
            var outlookApplication = CreateOutlookInstance();
            if (outlookApplication.ActiveExplorer() == null) {
                Log.Warning("Outlook not running!");
                return;
            }
            MAPIFolder inBox = (MAPIFolder)outlookApplication.ActiveExplorer().Session.GetDefaultFolder(OlDefaultFolders.olFolderInbox);
            MAPIFolder folderbase = inBox.Store.GetRootFolder();
            MAPIFolder mfolder = GetFolder(folderbase, folder);

            Items Items = mfolder.Items;
            var unreadonly = UnreadOnly.Get(context);
            if (unreadonly)
            {
                var Filter = "[Unread]=true";
                // var Filter = "@SQL=" + (char)34 + "urn:schemas:httpmail:hasattachment" + (char)34 + "=1 AND " +
                // var Filter = "@SQL=" + (char)34 + "urn:schemas:httpmail:read" + (char)34 + "=0";
                Items.Restrict(Filter);
            }
            var result = new List<email>();
            foreach(var folderItem in Items)
            {

                Microsoft.Office.Interop.Outlook.MailItem mailItem = folderItem as Microsoft.Office.Interop.Outlook.MailItem;
                if (mailItem != null)
                {
                    var _e = new email(mailItem);
                    if(unreadonly)
                    {
                        if (_e.UnRead) result.Add(_e);
                    }
                    else
                    {
                        result.Add(_e);
                    }
                    
                    
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
            if (more)
            {
                context.ScheduleAction<email>(Body, _enum.Current, OnBodyComplete);
            }
            else
            {
            }
        }
        private void LoopActionComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            Execute(context);
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


        public MAPIFolder GetFolder(MAPIFolder folder, string FullFolderPath)
        {
            if (folder.Folders.Count == 0)
            {
                if (folder.FullFolderPath == FullFolderPath)
                {
                    return folder;
                }
            }
            else
            {
                foreach (MAPIFolder subFolder in folder.Folders)
                {
                    if (folder.FullFolderPath == FullFolderPath)
                    {
                        return folder;
                    }
                    var temp = GetFolder(subFolder, FullFolderPath);
                    if (temp != null) return temp;
                }
            }
            return null;
        }

    }

}