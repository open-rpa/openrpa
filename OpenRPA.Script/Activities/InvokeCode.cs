using System;
using System.Activities;
using OpenRPA.Interfaces;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Runtime.ExceptionServices;
using System.Reflection;
using System.CodeDom.Compiler;
using System.Management.Automation.Runspaces;
using System.Collections;
using System.Collections.ObjectModel;

namespace OpenRPA.Script.Activities
{
    [System.ComponentModel.Designer(typeof(InvokeCodeDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.comment.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class InvokeCode : CodeActivity
    {
        public InvokeCode()
        {

        }
        [RequiredArgument]
        public InArgument<string> Code { get; set; }
        [RequiredArgument]
        public InArgument<string> Language { get; set; }
        public OutArgument<Collection<System.Management.Automation.PSObject>> PipelineOutput { get; set; }

        public static RunspacePool pool { get; set; } = null;
        public static Runspace runspace = null;
        protected override void Execute(CodeActivityContext context)
        {
            var code = Code.Get(context);
            var language = Language.Get(context); 
            var variables = new Dictionary<string, Type>();
            var variablevalues = new Dictionary<string, object>();
            var vars = context.DataContext.GetProperties();
            foreach (dynamic v in vars)
            {
                Type rtype = v.PropertyType as Type;
                //var rtype = v.PropertyType.UnderlyingSystemType;
                var value = v.GetValue(context.DataContext);

                if (rtype == null && value != null) rtype = value.GetType();
                if (rtype == null) continue;
                variables.Add(v.DisplayName, rtype);
                variablevalues.Add(v.DisplayName, value);
            }
            string sourcecode = code;
            if(language=="VB") sourcecode = GetVBHeaderText(variables, "Expression") + code + GetVBFooterText();
            if (language == "C#") sourcecode = GetCSharpHeaderText(variables, "Expression") + code + GetCSharpFooterText();

            if(language == "PowerShell")
            {

                if (runspace == null)
                {
                    runspace = RunspaceFactory.CreateRunspace();
                    runspace.Open();
                }

                    using (var pipeline = runspace.CreatePipeline())
                {
                    Command cmd = new Command(sourcecode, true);
                    foreach (var parameter in variablevalues)
                    {
                        // cmd.Parameters.Add(parameter.Key, parameter.Value);
                        runspace.SessionStateProxy.SetVariable(parameter.Key, parameter.Value);
                    }
                    pipeline.Commands.Add(cmd);
                    var res = pipeline.Invoke();
                    foreach (var o in res)
                    {
                        Log.Output(o.ToString());
                    }
                    foreach (dynamic v in vars)
                    {
                        var value = runspace.SessionStateProxy.GetVariable(v.DisplayName);
                        var myVar = context.DataContext.GetProperties().Find(v.DisplayName, true);
                        if (myVar != null)
                        {
                            //var myValue = myVar.GetValue(context.DataContext);
                            myVar.SetValue(context.DataContext, value);
                        }
                    }
                    PipelineOutput.Set(context, res);
                }

                return;
            }


            var names = new List<string>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            var assemblyLocations = new List<string>();
            foreach (var asm in assemblies)
            {
                try
                {
                    //var a = Assembly.ReflectionOnlyLoad(asm.FullName);
                    //var a = Assembly.Load(asm.FullName);
                    //if(!assemblyLocations.Contains(a.Location)) assemblyLocations.Add(a.Location);
                    if (!asm.IsDynamic)
                    {
                        if (asm.Location.Contains("Microsoft.Office.Interop")) continue;
                        if (string.IsNullOrEmpty(asm.Location)) continue;
                        if(asm.Location.Contains("System.Numerics.Vectors"))
                        {
                            continue;
                        }
                        if (!assemblyLocations.Contains(asm.Location) && !names.Contains(asm.FullName))
                        {
                            names.Add(asm.FullName);
                            assemblyLocations.Add(asm.Location);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            CompileAndRun(language, sourcecode, assemblyLocations.ToArray(), variablevalues, context);
        }

        private static Dictionary<string, CompilerResults> cache = new Dictionary<string, CompilerResults>();
        public void CompileAndRun(string language, string code, string[] references, Dictionary<string, object> variablevalues, CodeActivityContext context)
        {
            CompilerResults compile = null;
            if (!cache.ContainsKey(code))
            {
                CompilerParameters CompilerParams = new CompilerParameters();
                string outputDirectory = System.IO.Directory.GetCurrentDirectory();

                //CompilerParams.GenerateInMemory = true;
                CompilerParams.TreatWarningsAsErrors = false;
                CompilerParams.GenerateExecutable = false;
                CompilerParams.CompilerOptions = "/optimize /d:DEBUG";
                CompilerParams.IncludeDebugInformation = true;
                CompilerParams.GenerateInMemory = false;
                CompilerParams.OutputAssembly = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString().Replace("-", "") + ".dll");

                CompilerParams.ReferencedAssemblies.AddRange(references);
                CodeDomProvider provider = null;
                if (language=="VB")
                {
                    provider = new Microsoft.VisualBasic.VBCodeProvider();
                }
                else
                {
                    provider = new Microsoft.CSharp.CSharpCodeProvider();

                }
                compile = provider.CompileAssemblyFromSource(CompilerParams, new[] { code });

                if (compile.Errors.HasErrors)
                {
                    string text = "Compile error: ";
                    foreach (CompilerError ce in compile.Errors)
                    {
                        text += "rn" + ce.ToString();
                    }
                    Log.Error(text);
                    throw new Exception(text);
                }
                cache.Add(code, compile);
            }
            else
            {
                compile = cache[code];
            }
            //ExpoloreAssembly(compile.CompiledAssembly);
            Module module = compile.CompiledAssembly.GetModules()[0];
            Type mt = null;
            MethodInfo methInfo = null;
            if (module != null)
            {
                mt = module.GetType("Expression");
            }
            if (module != null && mt == null)
            {
                mt = module.GetType("SomeNamespace.Expression");
            }
            if (mt != null)
            {
                methInfo = mt.GetMethod("ExpressionValue");
                foreach (var v in variablevalues)
                {
                    var p = mt.GetField(v.Key);
                    if (p != null)
                    {
                        p.SetValue(mt, v.Value);
                    }
                }
            }

            if (methInfo != null)
            {
                ExceptionDispatchInfo exceptionDispatchInfo = null;
                try
                {
                    methInfo.Invoke(null, new object[] { });
                }
                catch (Exception ex)
                {
                    exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
                }

                if (exceptionDispatchInfo != null) exceptionDispatchInfo.Throw();

                var vars = context.DataContext.GetProperties();
                foreach (dynamic v in vars)
                {

                    var p = mt.GetField(v.DisplayName);
                    if (p == null) continue;
                    var value = p.GetValue(mt);
                    v.SetValue(context.DataContext, value);
                }
            }
        }


        private List<string> Namespaces = new List<string>() { "System", "System.Collections", "System.Data" };
        public string GetCSharpHeaderText(Dictionary<string, Type> variables, string moduleName)
        {
            var headerText = new StringBuilder();
            foreach (var n in Namespaces)
            {
                headerText.AppendLine("using " + n + ";\r\n");
            }
            headerText.Append("\r\n namespace SomeNamespace { public class " + moduleName + " { \r\n");
            if (variables != null)
            {
                foreach (var var in variables)
                {
                    // Build a VB representation of the variable's type name
                    var variableTypeName = new StringBuilder();
                    AppendCSharpTypeName(variableTypeName, var.Value);

                    headerText.Append("public static " + variableTypeName + " " + var.Key + " = default(" + variableTypeName + ");");
                    headerText.AppendLine();
                }
            }
            headerText.AppendLine();
            headerText.AppendLine("public static void ExpressionValue() { ");
            return headerText.ToString();
        }
        public string GetCSharpFooterText()
        {
            return " } } }";
        }
        private static void AppendCSharpTypeName(StringBuilder typeName, Type type)
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
        public string GetVBHeaderText(Dictionary<string, Type> variables, string moduleName)
        {
            // Inject namespace imports
            //var headerText = new StringBuilder("Imports System\r\nImports System.Collections\r\nImports System.Collections.Generic\r\nImports System.Linq\r\n");
            var headerText = new StringBuilder();

            foreach (var n in Namespaces)
            {
                headerText.AppendLine("Imports " + n + "\r\n");
            }


            // NOTE: Automated IntelliPrompt will only show for namespaces and types that are within the imported namespaces...
            //       Add other namespace imports here if types from other namespaces should be accessible

            // Inject a Class and Sub wrapper
            headerText.Append("\r\nModule " + moduleName + "\r\n");
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
        public string GetVBFooterText()
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
    }
}