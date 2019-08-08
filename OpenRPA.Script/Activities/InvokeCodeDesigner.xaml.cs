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

namespace OpenRPA.Script.Activities
{
    public partial class InvokeCodeDesigner : INotifyPropertyChanged
    {
        public InvokeCodeDesigner()
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
            string code = ModelItem.GetValue<string>("Code");
            string language = ModelItem.GetValue<string>("Language");
            if (string.IsNullOrEmpty(language)) language = "VB";
            var f = new Editor(code, language);
            f.textEditor.SyntaxHighlighting = f.Languages.Where(x => x.Name == language).FirstOrDefault();
            f.ShowDialog();
            if (f.textEditor.Text != code)
            {
                ModelItem.Properties["Code"].SetValue(new InArgument<string>() { Expression = new Literal<string>(f.textEditor.Text) });
            }
            if (f.textEditor.SyntaxHighlighting.Name != language)
            {
                ModelItem.Properties["Language"].SetValue(new InArgument<string>() { Expression = new Literal<string>(f.textEditor.SyntaxHighlighting.Name) });
            }
        }
    }
}