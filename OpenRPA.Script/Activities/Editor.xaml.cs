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
        private List<System.Activities.Presentation.Model.ModelItem> Variables;
        public Editor(string code, string language, List<System.Activities.Presentation.Model.ModelItem> Variables, string[] namespaces)
        {
            InitializeComponent();


            var variables = new Dictionary<string, Type>();
            foreach (var variableModel in Variables)
            {
                var variable = variableModel.GetCurrentValue() as System.Activities.LocationReference;
                if(variables.ContainsKey(variable.Name))
                {
                    System.Windows.MessageBox.Show("Editor: Doublecate variable  " + variable.Name + " found. Try and avoid that if possible");
                } 
                else
                {
                    variables.Add(variable.Name, variable.Type);
                }                
            }

            if (!variables.ContainsKey("instance")) variables.Add("instance", typeof(IWorkflowInstance));

            textEditor.Namespaces = namespaces;
            this.Variables = Variables;
            DataContext = this;
            //string[] names = typeof(ICSharpCode.AvalonEdit.AvalonEditCommands).Assembly.GetManifestResourceNames();
            foreach (var hi in HighlightingManager.Instance.HighlightingDefinitions)
            {
                if (hi.Name == "VB" || hi.Name == "C#" || hi.Name == "PowerShell" || hi.Name == "Python") Languages.Add(hi);
            }
            // LoadFromResource("ICSharpCode.PythonBinding.Resources.Python.xshd", typeof(Editor));
            LoadFromResource("Autohotkey.xshd", typeof(Editor));
            textEditor.Text = code;


            if (language == "VB")
            {
                
                var header = CodeEditor.CodeEditor.GetVBHeaderText(variables, "Expression", namespaces);
                int numLines = header.Split('\n').Length;
                Log.Information("Header (add to line numbers): " + numLines);
                Title = "Editor (Add " + numLines + " to line numbers)";
            }
            if (language == "C#")
            {
                var header = CodeEditor.CodeEditor.GetCSharpHeaderText(variables, "Expression", namespaces);
                int numLines = header.Split('\n').Length;
                Log.Information("Header (add to line numbers): " + numLines);
                Title = "Editor (Add " + numLines + " to line numbers)";
            }
            textEditor.Variables = variables;
            textEditor.SyntaxHighlighting = Languages.Where(x => x.Name == language).FirstOrDefault();
            // if (language == "C#") textEditor.Text = CodeEditor.CodeEditor.GetCSharpHeaderText(variables, "Expression", textEditor.Namespaces) + code + CodeEditor.CodeEditor.GetCSharpFooterText();
            Button_Click(null, null);
        }
        public System.Collections.ObjectModel.ObservableCollection<IHighlightingDefinition> Languages { get; set; } = new System.Collections.ObjectModel.ObservableCollection<IHighlightingDefinition> ();
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (textEditor == null)
            {
                Log.Error("textEditor is null?");
                return;
            }
            this.errors.Text = "";
            var language = textEditor.SyntaxHighlighting.Name;
            var code = textEditor.Text;
            string error = "";
            try
            {
                textEditor.ShowLineNumbers = true;
                //if (language == "VB" || language == "C#")
                //{
                //    textEditor.ShowLineNumbers = false;
                //}
            }
            catch (Exception ex)
            {
                Log.Error("set ShowLineNumbers: " + ex.ToString());
            }
            var newtitle = "Editor";
            Task.Run(() => {
                if (language == "VB" || language == "C#")
                {
                    var variables = new Dictionary<string, Type>();
                    foreach (var variableModel in Variables)
                    {
                        var variable = variableModel.GetCurrentValue() as System.Activities.LocationReference;
                        variables.Add(variable.Name, variable.Type);
                    }
                    if (!variables.ContainsKey("instance")) variables.Add("instance", typeof(IWorkflowInstance));
                    if (language == "VB")
                    {

                        var header = CodeEditor.CodeEditor.GetVBHeaderText(variables, "Expression", textEditor.Namespaces);
                        int numLines = header.Split('\n').Length;
                        Log.Information("Header (add to line numbers): " + numLines);
                        newtitle = "Editor (Add " + numLines + " to line numbers)";
                    }
                    else if (language == "C#")
                    {
                        var header = CodeEditor.CodeEditor.GetCSharpHeaderText(variables, "Expression", textEditor.Namespaces);
                        int numLines = header.Split('\n').Length;
                        Log.Information("Header (add to line numbers): " + numLines);
                        newtitle = "Editor (Add " + numLines + " to line numbers)";
                    }

                    string sourcecode = code;
                    textEditor.Variables = variables;
                    if (language == "VB") sourcecode = CodeEditor.CodeEditor.GetVBHeaderText(variables, "Expression", textEditor.Namespaces) + code + CodeEditor.CodeEditor.GetVBFooterText();
                    if (language == "C#") sourcecode = CodeEditor.CodeEditor.GetCSharpHeaderText(variables, "Expression", textEditor.Namespaces) + code + CodeEditor.CodeEditor.GetCSharpFooterText();
                    var references = InvokeCode.GetAssemblyLocations();
                    var CompilerParams = new System.CodeDom.Compiler.CompilerParameters();
                    //CompilerParams.GenerateInMemory = true;
                    CompilerParams.TreatWarningsAsErrors = false;
                    CompilerParams.GenerateExecutable = false;
                    CompilerParams.CompilerOptions = "/optimize /d:DEBUG";
                    CompilerParams.IncludeDebugInformation = true;
                    CompilerParams.GenerateInMemory = false;
                    CompilerParams.OutputAssembly = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString().Replace("-", "") + ".dll");

                    CompilerParams.ReferencedAssemblies.AddRange(references);
                    // CompilerParams.ReferencedAssemblies.Add(@"C:\code\openrpa\bin\Microsoft.Office.Tools.Excel.dll");
                    System.CodeDom.Compiler.CodeDomProvider provider = null;
                    if (language == "VB")
                    {
                        provider = new Microsoft.VisualBasic.VBCodeProvider();
                    }
                    else
                    {
                        provider = new Microsoft.CSharp.CSharpCodeProvider();

                    }
                    System.CodeDom.Compiler.CompilerResults compile = provider.CompileAssemblyFromSource(CompilerParams, new[] { sourcecode });

                    if (compile.Errors.HasErrors)
                    {
                        string text = "";
                        foreach (System.CodeDom.Compiler.CompilerError ce in compile.Errors)
                        {
                            if(!ce.IsWarning) text += ce.ToString();
                            Log.Error(ce.ToString());
                        }
                        error = text;
                    }
                }
                if (language == "PowerShell")
                {
                    Collection<System.Management.Automation.PSParseError> errors;
                    System.Management.Automation.PSParser.Tokenize(code, out errors);
                    if (errors != null && errors.Count > 0)
                    {
                        foreach (var _e in errors.Take(5))
                        {
                            error += "(" + _e.Token.StartLine + ":" + _e.Token.StartColumn + ") " + _e.Message + Environment.NewLine;
                        }
                    }
                }
                if (language == "AutoHotkey")
                {
                    //if (sharpAHK.ahkGlobal.ahkdll == null) { InvokeCode.New_AHKSession(true); }
                    //sharpAHK.ahkGlobal.ahkdll.Reset();
                    //try
                    //{
                    //    //sharpAHK.ahkGlobal.ahkdll.LoadScript(code);
                    //}
                    //catch (Exception ex)
                    //{
                    //    error = ex.ToString();
                    //}
                }
                if (language == "Python")
                {
                    //var engine = IronPython.Hosting.Python.CreateEngine();
                    //var source = engine.CreateScriptSourceFromString(code, Microsoft.Scripting.SourceCodeKind.Statements);
                    //var errors = new ErrorListener();
                    //var command = source.Compile(errors);
                    //if (command == null)
                    //{
                    //    foreach (var _e in errors.errors.Take(5))
                    //    {
                    //        error += _e.source.ToString() + "(" + _e.span.Start + "): " + _e.message + Environment.NewLine;
                    //    }
                    //}
                }
                GenericTools.RunUI(() =>
                {
                    Title = newtitle;
                    this.errors.Text = error;
                    this.errors.Visibility = (string.IsNullOrEmpty(this.errors.Text) ? Visibility.Hidden : Visibility.Visible);
                });
            });

        }
        private void HighlightingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var language = textEditor.SyntaxHighlighting.Name;
            btnValidate.Visibility = (language == "AutoHotkey" ? Visibility.Hidden : Visibility.Visible);
            Button_Click(null, null);
        }
    }
}
