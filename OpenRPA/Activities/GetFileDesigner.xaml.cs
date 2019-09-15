using Microsoft.VisualBasic.Activities;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenRPA.Activities
{
    public partial class GetFileDesigner : INotifyPropertyChanged
    {
        public GetFileDesigner()
        {
            InitializeComponent();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            //openFileDialog1.Filter = "Binary Excel files (2.0-2003 format)|*.xls|OpenXml Excel files (2007 format)|*.xlsx|Comma-separated values (csv format)|*.csv|All files (*.*)|*.*";
            // openFileDialog1.Filter = "All files (*.*)|*.*";
            if (openFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            ModelItem.Properties["LocalPath"].SetValue(
                new System.Activities.InArgument<string>()
                {
                    Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<string>("\"" + openFileDialog1.SelectedPath.replaceEnvironmentVariable() + "\"")
                });

        }
    }
}