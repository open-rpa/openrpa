using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace OpenRPA.Script.Activities
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Editor : Window
    {
        public void LoadFromResource(string resourceName, Type type)
        {
            // string[] names = typeof(Extensions).Assembly.GetManifestResourceNames();
            string[] names = type.Assembly.GetManifestResourceNames();
            foreach (var name in names)
            {
                if (name.EndsWith(resourceName))
                {
                    using (Stream s = type.Assembly.GetManifestResourceStream(name))
                    {
                        using (System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(s))
                        {
                            Languages.Add(HighlightingLoader.Load(reader, HighlightingManager.Instance));
                        }
                    }
                }
            }
        }

        public Editor(string code, string language)
        {
            InitializeComponent();
            DataContext = this;
            //string[] names = typeof(ICSharpCode.AvalonEdit.AvalonEditCommands).Assembly.GetManifestResourceNames();
            foreach (var hi in HighlightingManager.Instance.HighlightingDefinitions)
            {
                if (hi.Name == "VB" || hi.Name == "C#" || hi.Name == "PowerShell" || hi.Name == "Python") Languages.Add(hi);
            }
            LoadFromResource("ICSharpCode.PythonBinding.Resources.Python.xshd", typeof(Editor));
            LoadFromResource("Autohotkey.xshd", typeof(Editor));
            textEditor.Text = code;
            textEditor.SyntaxHighlighting = Languages.Where(x => x.Name == language).FirstOrDefault();
            if(language == "Python") textEditor.ShowLineNumbers = true;

            Button_Click(null, null);
        }
        public System.Collections.ObjectModel.ObservableCollection<IHighlightingDefinition> Languages { get; set; } = new System.Collections.ObjectModel.ObservableCollection<IHighlightingDefinition> ();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.errors.Text = "";
            var language = textEditor.SyntaxHighlighting.Name;
            var code = textEditor.Text;
            if(language=="Python")
            {
                var engine = IronPython.Hosting.Python.CreateEngine();
                var source = engine.CreateScriptSourceFromString(code, Microsoft.Scripting.SourceCodeKind.Statements);
                var errors = new ErrorListener();
                var command = source.Compile(errors);
                if (command == null)
                {
                    foreach (var _e in errors.errors.Take(5))
                    {
                        this.errors.Text += _e.source.ToString() + "(" + _e.span.Start + "): " + _e.message + Environment.NewLine;
                    }
                }

            }

            this.errors.Visibility = (string.IsNullOrEmpty(this.errors.Text) ? Visibility.Hidden : Visibility.Visible);

        }
    }
}
