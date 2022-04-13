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
    public partial class NewMailItemDesigner
    {
        public ObservableCollection<Microsoft.Office.Interop.Outlook.Account> Accounts { get; set; } = new ObservableCollection<Account>();
        
        public NewMailItemDesigner()
        {
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
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var curAccount = ModelItem.GetValue<string>("Account");
                Accounts.Clear();
                var outlookApplication = CreateOutlookInstance();
                for (int i = 1; i <= outlookApplication.Session.Accounts.Count; i++)
                {
                    Accounts.Add(outlookApplication.Session.Accounts[i]);
                }
                ModelItem.SetValueInArg("Account", curAccount);
            }
            catch (System.Exception ex)
            {
                Log.Error(ex.ToString());
            }

        }
    }
}