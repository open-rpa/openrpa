using OpenRPA.Interfaces;
using System.Activities.Presentation.Model;
using System.Windows;
using System.Windows.Media;


namespace OpenRPA.Office.Activities
{
    public partial class WriteRangeDesigner
    {
        public WriteRangeDesigner()
        {
            InitializeComponent();
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
                    Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<string>("\"" + openFileDialog1.FileName.ReplaceEnvironmentVariable() + "\"")
                });
        }
    }
}