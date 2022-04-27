using OpenRPA.Interfaces;
using System.Activities.Presentation.Model;
using System.Windows;
using System.Windows.Media;

namespace OpenRPA.Office.Activities
{
    public partial class ExportDocumentDesigner
    {
        public ExportDocumentDesigner()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            saveFileDialog1.Filter = "Word and Text files|*.doc;*.docx;*.txt|All files (*.*)|*.*";
            if (saveFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            ModelItem.Properties["Filename"].SetValue(
                new System.Activities.InArgument<string>()
                {
                    Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<string>("\"" + saveFileDialog1.FileName.ReplaceEnvironmentVariable() + "\"")
                });
        }
    }
}