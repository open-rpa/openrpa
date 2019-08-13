using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Windows;
using System.Windows.Media;


namespace OpenRPA.Script.Activities
{
    public partial class ExecuteAHKScriptDesigner
    {
        public ExecuteAHKScriptDesigner()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string script = ModelItem.GetValue<string>("Script");
            var f = new Editor(script, "AutoHotkey", null);
            f.highlightingComboBox.Visibility = Visibility.Hidden;
            f.ShowDialog();
            if (f.textEditor.Text != script)
            {
                ModelItem.Properties["Script"].SetValue(new InArgument<string>() { Expression = new Literal<string>(f.textEditor.Text) });
            }

        }
    }
}