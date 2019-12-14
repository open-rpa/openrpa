using Microsoft.VisualBasic.Activities;
using OpenRPA.Interfaces;
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

namespace OpenRPA.AviRecorder.Activities
{
    public partial class StartRecordingDesigner
    {
        public StartRecordingDesigner()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            using (var openFileDialog1 = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (openFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
                var path = openFileDialog1.SelectedPath;
                ModelItem.Properties["Folder"].SetValue(new InArgument<string>(){
                        Expression = new VisualBasicValue<string>("\"" + path.ReplaceEnvironmentVariable() + "\"")
                    });

            }

        }

    }
}