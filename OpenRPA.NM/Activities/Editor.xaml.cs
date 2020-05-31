using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace OpenRPA.NM.Activities
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
        public Editor(string code)
        {
            InitializeComponent();
            using (var stream = System.Reflection.Assembly.GetAssembly(typeof(ICSharpCode.AvalonEdit.TextEditor)).GetManifestResourceStream("ICSharpCode.AvalonEdit.Highlighting.Resources.JavaScript-Mode.xshd"))
            {
                using (var reader = new System.Xml.XmlTextReader(stream))
                {
                    textEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }

            textEditor.Text = code;
        }
        public System.Collections.ObjectModel.ObservableCollection<IHighlightingDefinition> Languages { get; set; } = new System.Collections.ObjectModel.ObservableCollection<IHighlightingDefinition> ();
    }
}
