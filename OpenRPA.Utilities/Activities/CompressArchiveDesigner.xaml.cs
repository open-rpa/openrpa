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
using OpenRPA.Interfaces;

namespace OpenRPA.Utilities
{
    public partial class CompressArchiveDesigner
    {
        public CompressArchiveDesigner()
        {
            InitializeComponent();
        }
        private void PathButton_Click(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog.ShowNewFolderButton = true;
            System.Windows.Forms.DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ModelItem.Properties["Path"].SetValue(
                new InArgument<string>()
                {
                    Expression = new VisualBasicValue<string>("\"" + folderBrowserDialog.SelectedPath.ReplaceEnvironmentVariable() + "\"")
                });

            }
        }
        private void FileButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog1.Filter = "Zip File (ZIP)|*.zip";
            if (saveFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            ModelItem.Properties["Filename"].SetValue(
                new InArgument<string>()
                {
                    Expression = new VisualBasicValue<string>("\"" + saveFileDialog1.FileName.ReplaceEnvironmentVariable() + "\"")
                });
        }
    }
}