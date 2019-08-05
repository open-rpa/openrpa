using Forge.Forms.FormBuilding;
using Forge.Forms.FormBuilding.Defaults;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    public partial class Form : Window, INotifyPropertyChanged
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
        public Form(string form)
        {
            InitializeComponent();
            var dict = new ResourceDictionary();
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml") });
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml") });
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Colors.xaml") });
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml") });
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/Forge.Forms;component/Themes/Material.xaml") });

            xmlString = form;
            DataContext = this;
        }
        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            foreach(var rec in Application.Current.Resources.MergedDictionaries.ToList())
            {
                if (rec.Source == null) continue;
                if (rec.Source.AbsolutePath.Contains("Forge.Forms")) Application.Current.Resources.MergedDictionaries.Remove(rec);
                if (rec.Source.AbsolutePath.Contains("MahApps.Metro")) Application.Current.Resources.MergedDictionaries.Remove(rec);
                if (rec.Source.AbsolutePath.Contains("MaterialDesignThemes.Wpf")) Application.Current.Resources.MergedDictionaries.Remove(rec);
            }
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
                Top += (e.PreviousSize.Height - e.NewSize.Height) / 2;
            if (e.WidthChanged)
                Left += (e.PreviousSize.Width - e.NewSize.Width) / 2;
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
        private IDictionary<string, object> currentModel;
        public IDictionary<string, object> CurrentModel
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
        public Exception LastError { get; set; }
        private void BuildDefinition(object sender, RoutedEventArgs e)
        {
            try
            {
                LastError = null;
                CompiledDefinition = FormBuilder.Default.GetDefinition(xmlString);
                var t = CompiledDefinition.GetElements().Where(x => x is TitleElement).FirstOrDefault();
                if(t != null && t.Resources.ContainsKey("Content"))
                {
                    this.Title = t.Resources["Content"].GetStringValue(null).Value;
                } else
                {
                    var h = CompiledDefinition.GetElements().Where(x => x is HeadingElement).FirstOrDefault();
                    if (h != null && h.Resources.ContainsKey("Content"))
                    {
                        this.Title = h.Resources["Content"].GetStringValue(null).Value;
                    }
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(ex.ToString());
                DialogResult = false;
            }
        }
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
        public Forge.Forms.IActionContext actionContext { get; set; }
        public Dictionary<string, object> defaults { get; internal set; }
        private void DynamicForm_OnAction(object sender, Forge.Forms.ActionEventArgs e)
        {
            actionContext = e.ActionContext;
            var fields = CompiledDefinition.GetElements();
            var firstNameElement = (DataFormField)CompiledDefinition.GetElements().FirstOrDefault(x => x is DataFormField d && d.Key == "FirstName");
            if(actionContext.Action != null && actionContext.Action.ToString()!="reset")
            {
                DialogResult = true;
            }
        }
        public void setTimeOut(EventHandler doWork, int time)

        {
            System.Windows.Threading.DispatcherTimer myDispatcherTimer = new System.Windows.Threading.DispatcherTimer();

            myDispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, time); myDispatcherTimer.Tick += delegate (object s, EventArgs args) { myDispatcherTimer.Stop(); };
            myDispatcherTimer.Tick += doWork;

            myDispatcherTimer.Start();

        }
        private void Df_Loaded(object sender, RoutedEventArgs e)
        {
            if (defaults == null) return;
            if (CurrentModel == null)
            {
                setTimeOut(delegate (object s, EventArgs args)
                {
                    Df_Loaded(null, null);
                }, 1000);
                return;
            }
            GenericTools.RunUI(this, () =>
            {
                foreach (var p in defaults)
                {
                    //CompiledDefinition.UpdateDefaultValue(p.Key, p.Value);
                    CurrentModel[p.Key] = p.Value;
                }
            });
        }
    }
}
