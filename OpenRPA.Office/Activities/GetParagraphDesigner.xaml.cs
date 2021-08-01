using OpenRPA.Interfaces;
using System.Activities.Presentation.Model;
using System.Windows;
using System.Windows.Media;


namespace OpenRPA.Office.Activities
{
    public partial class GetParagraphDesigner
    {
        public GetParagraphDesigner()
        {
            InitializeComponent();
            Loaded += (sender, e) =>
            {
                var Variables = ModelItem.Properties[nameof(GetParagraph.Variables)].Collection;
                if (Variables != null && Variables.Count == 0)
                {
                    Variables.Add(new System.Activities.Variable<int>("Index", 0));
                    Variables.Add(new System.Activities.Variable<int>("Total", 0));
                }
            };
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            //openFileDialog1.Filter = "Binary Excel files (2.0-2003 format)|*.xls|OpenXml Excel files (2007 format)|*.xlsx|Comma-separated values (csv format)|*.csv|All files (*.*)|*.*";
            openFileDialog1.Filter = "Word and Text files|*.doc;*.docx;*.txt|All files (*.*)|*.*";
            if (openFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            ModelItem.Properties["Filename"].SetValue(
                new System.Activities.InArgument<string>()
                {
                    Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<string>("\"" + openFileDialog1.FileName.ReplaceEnvironmentVariable() + "\"")
                });
        }
    }
}