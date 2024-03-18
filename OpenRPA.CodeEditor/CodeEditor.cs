using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Search;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace OpenRPA.CodeEditor
{
    // https://www.strathweb.com/2018/12/using-roslyn-c-completion-service-programmatically/
    // https://github.com/microsoft/AppConsult-WinAppsModernizationWorkshop/blob/master/Samples/DotNetPad/DotNetPad.Presentation/Controls/CodeEditor.cs
    public class init
    {
        public async static Task Initialize()
        {
            try
            {
                CodeEditor ce = null;
                GenericTools.RunUI(() =>
                {
                    try
                    {
                        ce = new CodeEditor();
                        ce.document = ce.Initialize();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("CodeEditor.init-inner: " + ex.ToString());
                    }
                }, 10000);
                var completionService = CompletionService.GetService(ce.document);
                var completionList = await Task.Run(async () =>
                {
                    try
                    {
                        if (ce != null && ce.document != null)
                        {
                            return await completionService.GetCompletionsAsync(ce.document, 0);
                        }
                    }
                    catch (Exception)
                    {
                    }
                    return null;
                });
            }
            catch (Exception ex)
            {
                Log.Error("CodeEditor.init: " + ex.ToString());
            }
        }
    }
    public class CodeEditor : TextEditor, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private CompletionWindow completionWindow;
        private CancellationTokenSource completionCancellation;
        private AdhocWorkspace workspace;
        public string[] Namespaces;
        public Dictionary<string, Type> Variables;
        public CodeEditor()
        {
            SearchPanel.Install(TextArea);
            if(SyntaxHighlighting==null)
            {
                SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.HighlightingDefinitions.Where(x => x.Name == "VB").FirstOrDefault();
            }
            var dict = new ResourceDictionary();
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/OpenRPA.CodeEditor;component/Resources/ImageResources.xaml") });
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/OpenRPA.CodeEditor;component/Resources/CodeEditorResources.xaml") });

            completionCancellation = new CancellationTokenSource();
            TextArea.TextEntering += TextAreaTextEntering;
            TextArea.TextEntered += TextAreaTextEntered;
            IsVisibleChanged += IsVisibleChangedHandler;
        }
        private void TextAreaTextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && completionWindow != null)
            {
                if (!IsAllowedLanguageLetter(e.Text[0]))
                {
                    completionWindow.CompletionList.RequestInsertion(e);
                }
            }
        }
        private async void TextAreaTextEntered(object sender, TextCompositionEventArgs e)
        {
            var c = e.Text.FirstOrDefault();
            await ShowCompletionAsync(e.Text.FirstOrDefault());
        }
        private void IsVisibleChangedHandler(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible) { completionCancellation.Cancel(); }
        }
        private static bool IsAllowedLanguageLetter(char character)
        {
            return TextUtilities.GetCharacterClass(character) == CharacterClass.IdentifierPart;
        }
        private (int wordStart, string text) GetWord(int position)
        {
            var wordStart = TextUtilities.GetNextCaretPosition(TextArea.Document, position, LogicalDirection.Backward, CaretPositioningMode.WordStart);
            var text = TextArea.Document.GetText(wordStart, position - wordStart);
            return (wordStart, text);
        }
        private static async Task<ImmutableArray<TaggedText>> GetDescriptionAsync(CompletionService completionService, Document document, CompletionItem completionItem)
        {
            try
            {
                var desc = await completionService.GetDescriptionAsync(document, completionItem);
                var res = desc.TaggedParts;
                // var res = (await Task.Run(async () => )).TaggedParts;
                return res;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return default;
            }
        }
        private bool _initialized = false;
        private ProjectInfo projectinfo;
        private Project project;
        public Document document;
        private string currentLanguage = "";
        string header;
        string footer;
        private MetadataReference[] GetReferences()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            var mr = new List<MetadataReference>();
            foreach (var asm in assemblies)
            {
                try
                {
                    if (!asm.IsDynamic)
                    {
                        mr.Add(MetadataReference.CreateFromFile(asm.Location));
                    }
                }
                catch (Exception)
                {
                }
            }
            return mr.ToArray();
            // return new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
        }
        private Document InitializeCSharp()
        {
            _initialized = true;
            header = GetCSharpHeaderText(Variables, "Expression", Namespaces);
            footer = GetCSharpFooterText();

            var host = MefHostServices.Create(MefHostServices.DefaultAssemblies);
            workspace = new AdhocWorkspace(host);
            var compilationOptions = new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            projectinfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "Script", "Script", LanguageNames.CSharp)
                .WithMetadataReferences(GetReferences())
                .WithCompilationOptions(compilationOptions);

            project = workspace.AddProject(projectinfo);
            return project.AddDocument("TestDocument.cs", header + Text + footer);

            //var scriptProject = workspace.AddProject(projectinfo);
            //var scriptDocumentInfo = DocumentInfo.Create(
            //    DocumentId.CreateNewId(scriptProject.Id), "Script",
            //    sourceCodeKind: SourceCodeKind.Script,
            //    loader: TextLoader.From(TextAndVersion.Create(SourceText.From(header + Text + footer), VersionStamp.Create())));
            //return workspace.AddDocument(scriptDocumentInfo);
        }
        private Document InitializeVB()
        {

            _initialized = true;
            header = GetVBHeaderText(Variables, "Expression", Namespaces);
            footer = GetVBFooterText();
            var host = MefHostServices.Create(MefHostServices.DefaultAssemblies);
            workspace = new AdhocWorkspace(host);

            var compilationOptions = new Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            projectinfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "Script", "Script", LanguageNames.VisualBasic)
                .WithMetadataReferences(GetReferences())
                .WithCompilationOptions(compilationOptions);

            project = workspace.AddProject(projectinfo);
            
            return project.AddDocument("TestDocument.vb", header + Text + footer);

        }
        public Document Initialize()
        {
            Document document = this.document;
            GenericTools.RunUI(() =>
            {
                if (currentLanguage != SyntaxHighlighting.Name)
                {
                    currentLanguage = SyntaxHighlighting.Name;
                    _initialized = false;
                }
                if (!_initialized)
                {
                    if (currentLanguage == "C#")
                    {
                        document = InitializeCSharp();

                    }
                    else if (currentLanguage == "VB")
                    {
                        document = InitializeVB();
                    }
                    if (document == null) return;
                }
                else
                {
                    if (document == null) return;
                    if (currentLanguage == "C#")
                    {
                        document = document.WithText(SourceText.From(header + Text + footer));

                    }
                    else if (currentLanguage == "VB")
                    {
                        document = document.WithText(SourceText.From(header + Text + footer));
                    }
                }
            }, 10000);
            return document;
        }
        private async Task ShowCompletionAsync(char? triggerChar)
        {
            try
            {
                document = Initialize();
                
                if (document == null) return;
                currentLanguage = SyntaxHighlighting.Name;

                if (completionWindow == null && (triggerChar == null || triggerChar == '.' || IsAllowedLanguageLetter(triggerChar.Value)))
                {
                    completionCancellation.Cancel();
                    completionCancellation = new CancellationTokenSource();
                    var cancellationToken = completionCancellation.Token;

                    var word = GetWord(CaretOffset);
                    var completionService = CompletionService.GetService(document);

                    var position = CaretOffset + header.Length;
                    if (currentLanguage == "VB")
                    {
                        position = CaretOffset + header.Length;
                    }

                    //var completionList = await Task.Run(async () =>
                    //    await completionService.GetCompletionsAsync(document, position, cancellationToken: cancellationToken), cancellationToken);
                    var completionList = await Task.Run(async () =>
                        await completionService.GetCompletionsAsync(document, position), cancellationToken);
                    if (completionList == null) { return; }
                    if (completionCancellation.IsCancellationRequested) return;
                    //cancellationToken.ThrowIfCancellationRequested();

                    completionWindow = new CompletionWindow(TextArea)
                    {
                        WindowStyle = WindowStyle.None,
                        AllowsTransparency = true
                    };
                    completionWindow.MaxWidth = completionWindow.Width = 340;
                    completionWindow.MaxHeight = completionWindow.Height = 206;
                    foreach (var completionItem in completionList.Items)
                    {
                        completionWindow.CompletionList.CompletionData.Add(new CodeCompletionData(completionItem.DisplayText,
                            () => GetDescriptionAsync(completionService, document, completionItem), completionItem.Tags));
                    }

                    if (triggerChar == null || IsAllowedLanguageLetter(triggerChar.Value))
                    {
                        completionWindow.StartOffset = word.wordStart;
                        completionWindow.CompletionList.SelectItem(word.text);
                    }
                    completionWindow.Show();
                    completionWindow.Closed += (s2, e2) => completionWindow = null;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public static string GetVBVariablleText(Dictionary<string, Type> variables)
        {
            var headerText = new StringBuilder();
            if (variables != null)
            {
                foreach (var var in variables)
                {
                    // Build a VB representation of the variable's type name
                    var variableTypeName = new StringBuilder();
                    AppendVBTypeName(variableTypeName, var.Value);
                    headerText.Append("Dim ");
                    headerText.Append(var.Key);
                    headerText.Append(" As ");
                    headerText.Append(variableTypeName.Replace("[", "(").Replace("]", ")"));
                    headerText.AppendLine();
                }
            }
            headerText.AppendLine();
            return headerText.ToString();
        }
        public static string GetVBHeaderText(Dictionary<string, Type> variables, string moduleName, string[] namespaces)
        {
            // Inject namespace imports
            //var headerText = new StringBuilder("Imports System\r\nImports System.Collections\r\nImports System.Collections.Generic\r\nImports System.Linq\r\n");
            var headerText = new StringBuilder();
            if(namespaces!=null)
                foreach (var n in namespaces)
                {
                    headerText.AppendLine("Imports " + n);
                }


            // NOTE: Automated IntelliPrompt will only show for namespaces and types that are within the imported namespaces...
            //       Add other namespace imports here if types from other namespaces should be accessible

            // Inject a Class and Sub wrapper
            headerText.AppendLine("Module " + moduleName);
            //headerText.Append("\r\nClass Expression\r\nShared Sub ExpressionValue\r\n");


            if (variables != null)
            {
                foreach (var var in variables)
                {
                    // Build a VB representation of the variable's type name
                    var variableTypeName = new StringBuilder();
                    AppendVBTypeName(variableTypeName, var.Value);

                    headerText.Append("Public ");
                    headerText.Append(var.Key);
                    headerText.Append(" As ");
                    headerText.Append(variableTypeName.Replace("[", "(").Replace("]", ")"));
                    headerText.AppendLine();
                }
            }
            headerText.Append("Sub ExpressionValue\r\n");
            //// Since the document text is an expression, inject a Return statement start at the end of the header text
            //headerText.Append("\r\nReturn ");
            headerText.AppendLine();
            return headerText.ToString();
        }
        public static string GetVBFooterText()
        {
            // Close out the Sub and Class in the footer
            return "\r\nEnd Sub\r\nEnd Module";
        }
        private static void AppendVBTypeName(StringBuilder typeName, Type type)
        {
            var typeFullName = type.FullName;

            if (type.IsGenericType)
            {
                var tickIndex = typeFullName.IndexOf('`');
                if (tickIndex != -1)
                {
                    typeName.Append(typeFullName.Substring(0, tickIndex));
                    typeName.Append("(Of ");
                    var genericArgumentIndex = 0;
                    foreach (var genericArgument in type.GetGenericArguments())
                    {
                        if (genericArgumentIndex++ > 0)
                            typeName.Append(", ");

                        AppendVBTypeName(typeName, genericArgument);
                    }
                    typeName.Append(")");
                    return;
                }
            }

            typeName.Append(typeFullName);
        }
        public static string GetCSharpVariablleText(Dictionary<string, Type> variables)
        {
            var headerText = new StringBuilder();
            if (variables != null)
            {
                foreach (var var in variables)
                {
                    // Build a VB representation of the variable's type name
                    var variableTypeName = new StringBuilder();
                    AppendCSharpTypeName(variableTypeName, var.Value);

                    headerText.Append(variableTypeName + " " + var.Key + " = default(" + variableTypeName + ");");
                    headerText.AppendLine();
                }
            }
            return headerText.ToString();
        }
        public static string GetCSharpHeaderText(Dictionary<string, Type> variables, string moduleName, string[] namespaces)
        {
            var headerText = new StringBuilder();
            if(namespaces!=null)
            foreach (var n in namespaces)
            {
                headerText.AppendLine("using " + n + ";");
            }
            headerText.AppendLine("\r\n namespace SomeNamespace {\r\n public class " + moduleName + " {");
            headerText.AppendLine("public static void ExpressionValue() { ");

            if (variables != null)
            {
                foreach (var var in variables)
                {
                    // Build a VB representation of the variable's type name
                    var variableTypeName = new StringBuilder();
                    AppendCSharpTypeName(variableTypeName, var.Value);

                    headerText.AppendLine(variableTypeName + " " + var.Key + " = default(" + variableTypeName + ");");
                }
            }

            return headerText.ToString();
        }
        public static string GetCSharpFooterText()
        {
            return " } } }";
        }
        public static void AppendCSharpTypeName(StringBuilder typeName, Type type)
        {
            var typeFullName = type.FullName;

            if (type.IsGenericType)
            {
                var tickIndex = typeFullName.IndexOf('`');
                if (tickIndex != -1)
                {
                    typeName.Append(typeFullName.Substring(0, tickIndex));
                    typeName.Append("<");
                    var genericArgumentIndex = 0;
                    foreach (var genericArgument in type.GetGenericArguments())
                    {
                        if (genericArgumentIndex++ > 0)
                            typeName.Append(", ");

                        AppendCSharpTypeName(typeName, genericArgument);
                    }
                    typeName.Append(">");
                    return;
                }
            }

            typeName.Append(typeFullName);
        }

    }
}
