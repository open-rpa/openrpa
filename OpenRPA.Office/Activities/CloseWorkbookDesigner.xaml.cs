using OpenRPA.Interfaces;
using System.Activities.Presentation.Model;
using System.Windows;
using System.Windows.Media;

namespace OpenRPA.Office.Activities
{
    public partial class CloseWorkbookDesigner
    {
        public CloseWorkbookDesigner()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            //openFileDialog1.Filter = "Binary Excel files (2.0-2003 format)|*.xls|OpenXml Excel files (2007 format)|*.xlsx|Comma-separated values (csv format)|*.csv|All files (*.*)|*.*";
            saveFileDialog1.Filter = "Excel and CSV files|*.xls;*.xlsx;*.csv|All files (*.*)|*.*";
            if (saveFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            ModelItem.Properties["Filename"].SetValue(
                new System.Activities.InArgument<string>()
                {
                    Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<string>("\"" + saveFileDialog1.FileName.replaceEnvironmentVariable() + "\"")
                });
        }
    }
}