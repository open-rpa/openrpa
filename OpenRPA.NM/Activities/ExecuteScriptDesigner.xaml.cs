using Microsoft.VisualBasic.Activities;
using OpenRPA.Interfaces;
using OpenRPA.NM.Activities;
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

namespace OpenRPA.NM
{
    public partial class ExecuteScriptDesigner : INotifyPropertyChanged
    {
        public ExecuteScriptDesigner()
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
            string Script = ModelItem.GetValue<string>("Script");
            if (Script == null) Script = ""; // so we don't overwrite if a variable has been used
            var f = new Editor(Script);
            f.Owner = GenericTools.MainWindow;
            f.ShowDialog();
            if (f.textEditor.Text != Script)
            {
                ModelItem.Properties["Script"].SetValue(new InArgument<string>() { Expression = new Literal<string>(f.textEditor.Text) });
            }
        }

    }
}