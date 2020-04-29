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
    // Interaction logic for addinputDesigner.xaml
    public partial class MoveMailItemDesigner 
    {
        public ObservableCollection<outlookfolder> folders { get; set; }
        public MoveMailItemDesigner()
        {
            folders = new ObservableCollection<outlookfolder>();
            folders.Add(new outlookfolder() { name = "", _id = "" });
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
            try
            {
                var outlookApplication = CreateOutlookInstance();
                MAPIFolder inBox = (MAPIFolder)outlookApplication.ActiveExplorer().Session.GetDefaultFolder(OlDefaultFolders.olFolderInbox);
                MAPIFolder folderbase = inBox.Store.GetRootFolder();
                foreach (MAPIFolder folder in folderbase.Folders)
                {
                    GetFolders(folder, 0);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public void GetFolders(MAPIFolder folder, int ident)
        {
            if (folder.Folders.Count == 0)
            {
                //if (folder.Name == "Folder Name")
                //{
                //    mailsFromThisFolder = folder;
                //}
                folders.Add(new outlookfolder() { name = space(ident * 5) + folder.Name, _id = folder.FullFolderPath });
            }
            else
            {
                foreach (MAPIFolder subFolder in folder.Folders)
                {
                    folders.Add(new outlookfolder() { name = space(ident * 5) + folder.Name, _id = folder.FullFolderPath });
                    GetFolders(subFolder, (ident + 1));
                }
            }
        }
        public string space(int num)
        {
            return new String(' ', num);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            folders.Clear();
            ActivityDesigner_Loaded(null, null);
        }
    }
}