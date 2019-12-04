//using rpaactivities.activityextension;
using OpenRPA.Interfaces;
using System;
using System.Activities.Presentation.View;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenRPA.Office.Activities
{
    // Interaction logic for addinputDesigner.xaml
    public partial class ReadCellDesigner : INotifyPropertyChanged
    {
        public ReadCellDesigner()
        {
            InitializeComponent();
        }
        protected override void OnModelItemChanged(Object newItem)
        {
            base.OnModelItemChanged(newItem);
            GenericArgumentTypeUpdater.Attach(ModelItem);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            //openFileDialog1.Filter = "Binary Excel files (2.0-2003 format)|*.xls|OpenXml Excel files (2007 format)|*.xlsx|Comma-separated values (csv format)|*.csv|All files (*.*)|*.*";
            openFileDialog1.Filter = "Excel and CSV files|*.xls;*.xlsx;*.csv|All files (*.*)|*.*";
            if (openFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            ModelItem.Properties["Filename"].SetValue(
                new System.Activities.InArgument<string>()
                {
                    Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<string>("\"" + openFileDialog1.FileName.replaceEnvironmentVariable() + "\"")
                });
        }

    }
}