using Microsoft.Office.Interop.Outlook;
using OpenRPA.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenRPA.Office.Activities
{
    public class outlookfolder
    {
        public string name { get; set; }
        public string _id { get; set; }
    }
    public partial class GetMailsDesigner
    {
        public ObservableCollection<outlookfolder> folders { get; set; }
        public GetMailsDesigner()
        {
            folders = new ObservableCollection<outlookfolder>();
            InitializeComponent();
            Loaded += (sender, e) =>
            {
                var Variables = ModelItem.Properties[nameof(GetMails.Variables)].Collection;
                if (Variables != null && Variables.Count == 0)
                {
                    Variables.Add(new System.Activities.Variable<int>("Index", 0));
                    Variables.Add(new System.Activities.Variable<int>("Total", 0));
                }
            };
        }
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
        private void ActivityDesigner_Loaded(object sender, RoutedEventArgs e)
        {
        }
        public void Reload()
        {
            try
            {
                var curfolder = ModelItem.GetValue<string>("Folder");
                folders.Clear();
                var outlookApplication = CreateOutlookInstance();
                string PublicFolderPath = "";
                try
                {
                    MAPIFolder pubBox = outlookApplication.ActiveExplorer().Session.GetDefaultFolder(OlDefaultFolders.olPublicFoldersAllPublicFolders);
                    PublicFolderPath = pubBox.FullFolderPath;
                }
                catch (System.Exception)
                {
                }
                foreach (Folder folder in outlookApplication.Session.Folders)
                {
                    if (!string.IsNullOrEmpty(PublicFolderPath) && folder.FullFolderPath == PublicFolderPath && PluginConfig.get_emails_skip_public) continue;
                    GetFolders(folder, 0);
                }
                ModelItem.SetValueInArg("Folder", curfolder);
            }
            catch (System.Exception ex)
            {
                Log.Error(ex.ToString());
            }

        }
        public void GetFolders(Microsoft.Office.Interop.Outlook.Folder folder, int ident)
        {
            if (folders.Count > PluginConfig.get_emails_max_folders) return;
            if (folder.Folders.Count == 0)
            {
                folders.Add(new outlookfolder() { name = space(ident * 5) + folder.Name, _id = folder.FullFolderPath });
            }
            else
            {
                folders.Add(new outlookfolder() { name = space(ident * 5) + folder.Name, _id = folder.FullFolderPath });
                foreach (Microsoft.Office.Interop.Outlook.Folder subFolder in folder.Folders)
                {
                    GetFolders(subFolder, (ident + 1));
                    if (folders.Count > PluginConfig.get_emails_max_folders) return;
                }
            }
        }
        public string space(int num)
        {
            return new String(' ', num);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Reload();
        }
    }
}