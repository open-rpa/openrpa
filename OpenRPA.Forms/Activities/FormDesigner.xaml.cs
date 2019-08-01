using Forge.Forms.FormBuilding;
using Newtonsoft.Json;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenRPA.Forms.Activities
{
    /// <summary>
    /// Interaction logic for FormDesigner.xaml
    /// </summary>
    public partial class FormDesigner : Window, INotifyPropertyChanged
    {
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                PropertyChanged += value;
            }

            remove
            {
                PropertyChanged -= value;
            }
        }

        public FormDesigner(string form)
        {
            InitializeComponent();
            xmlString = form;
            DataContext = this;
            BuildDefinition(null, null);
        }
        private IFormDefinition compiledDefinition;
        public IFormDefinition CompiledDefinition
        {
            get => compiledDefinition;
            private set
            {
                if (Equals(compiledDefinition, value))
                {
                    return;
                }

                compiledDefinition = value;
                NotifyPropertyChanged("CompiledDefinition");
            }
        }
        private string xmlString;
        public string XmlString
        {
            get => xmlString;
            set
            {
                if (Equals(xmlString, value))
                {
                    return;
                }

                xmlString = value;
                NotifyPropertyChanged("XmlString");
            }
        }
        private object currentModel;
        public object CurrentModel
        {
            get => currentModel;
            set
            {
                currentModel = value;
                NotifyPropertyChanged("CurrentModel");
            }
        }
        public string json
        {
            get
            {
                var json = JsonConvert.SerializeObject(CurrentModel, Formatting.Indented);
                return json;                
            }
        }
        private void BuildDefinition(object sender, RoutedEventArgs e)
        {
            try
            {
                CompiledDefinition = FormBuilder.Default.GetDefinition(xmlString);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
