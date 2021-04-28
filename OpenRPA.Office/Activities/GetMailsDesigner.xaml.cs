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
            // folders.Add(new outlookfolder() { name = "", _id = "" });
            InitializeComponent();
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
                foreach (var folder in outlookApplication.Session.Folders)
                {
                    GetFolders(folder as MAPIFolder, 0);
                }
                //var oNS = outlookApplication.GetNamespace("MAPI");
                //foreach (MAPIFolder folder in oNS.Folders)
                //{
                //    GetFolders(folder, 0);
                //}

                //MAPIFolder inBox = (MAPIFolder)outlookApplication.ActiveExplorer().Session.GetDefaultFolder(OlDefaultFolders.olFolderInbox);
                //MAPIFolder folderbase = inBox.Store.GetRootFolder();
                //foreach (MAPIFolder folder in folderbase.Folders)
                //{
                //    GetFolders(folder, 0);
                //}
                ModelItem.SetValueInArg("Folder", curfolder);
            }
            catch (System.Exception ex)
            {
                Log.Error(ex.ToString());
            }

        }
        public void GetFolders(MAPIFolder folder, int ident)
        {
            if (folders.Count > PluginConfig.get_emails_max_folders) return;
            if (folder.Folders.Count == 0)
            {
                folders.Add(new outlookfolder() { name = space(ident * 5) + folder.Name, _id = folder.FullFolderPath });
            }
            else
            {
                folders.Add(new outlookfolder() { name = space(ident * 5) + folder.Name, _id = folder.FullFolderPath });
                foreach (MAPIFolder subFolder in folder.Folders)
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