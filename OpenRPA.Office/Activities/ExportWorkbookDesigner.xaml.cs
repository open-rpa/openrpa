using OpenRPA.Interfaces;
using System.Activities.Presentation.Model;
using System.Windows;
using System.Windows.Media;

namespace OpenRPA.Office.Activities
{
    public partial class ExportWorkbookDesigner
    {
        public ExportWorkbookDesigner()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog1.Filter = "All files (*.*)|*.*";
            if (saveFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            ModelItem.Properties["Filename"].SetValue(
                new System.Activities.InArgument<string>()
                {
                    Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<string>("\"" + saveFileDialog1.FileName.ReplaceEnvironmentVariable() + "\"")
                });
        }
    }
}