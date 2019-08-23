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
using sharpAHK;
using Python.Runtime;

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
        public static void New_AHKSession(bool NewInstance = false)
        {
            if (ahkGlobal.ahkdll == null || NewInstance == true) { ahkGlobal.ahkdll = new AutoHotkey.Interop.AutoHotkeyEngine(); }

            else { ahkGlobal.ahkdll = null; }  // option to start new AHK session (resets variables and previously loaded functions)

            ahkGlobal.LoadedAHK = new List<string>(); // reset loaded ahk list
        }
        public static RunspacePool pool { get; set; } = null;
        public static Runspace runspace = null;
        //private static ScriptEngine _engine;
        //private static ScriptScope _scope;

        // private static AssemblyLoader assemblyLoader = new AssemblyLoader();
        private static bool firstRun = true;

        public static void ExecuteNewAppDomain(Action action)
        {
            AppDomain domain = null;

            try
            {
                domain = AppDomain.CreateDomain("New App Domain: " + Guid.NewGuid());

                var domainDelegate = (AppDomainDelegate)domain.CreateInstanceAndUnwrap(
                    typeof(AppDomainDelegate).Assembly.FullName,
                    typeof(AppDomainDelegate).FullName);

                domainDelegate.Execute(action);
            }
            finally
            {
                if (domain != null)
                    AppDomain.Unload(domain);
            }
        }
        public static void ExecuteNewAppDomain(string code, Action<string> action)
        {
            AppDomain domain = null;

            try
            {
                domain = AppDomain.CreateDomain("New App Domain: " + Guid.NewGuid());

                var domainDelegate = (AppDomainDelegate)domain.CreateInstanceAndUnwrap(
                    typeof(AppDomainDelegate).Assembly.FullName,
                    typeof(AppDomainDelegate).FullName);

                domainDelegate.Execute<string>(code, action);
            }
            finally
            {
                if (domain != null)
                    AppDomain.Unload(domain);
            }
        }
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
            if (language == "VB") sourcecode = GetVBHeaderText(variables, "Expression") + code + GetVBFooterText();
            if (language == "C#") sourcecode = GetCSharpHeaderText(variables, "Expression") + code + GetCSharpFooterText();

            if (language == "PowerShell")
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
                        if (o != null) Log.Output(o.ToString());
                    }
                    foreach (dynamic v in vars)
                    {
                        var value = runspace.SessionStateProxy.GetVariable(v.DisplayName);
                        var myVar = context.DataContext.GetProperties().Find(v.DisplayName, true);
                        if (myVar != null && value != null && value != "")
                        {
                            //var myValue = myVar.GetValue(context.DataContext);
                            myVar.SetValue(context.DataContext, value);
                        }
                    }
                    PipelineOutput.Set(context, res);
                }

                return;
            }

            if (language == "AutoHotkey")
            {
                if (sharpAHK.ahkGlobal.ahkdll == null) { New_AHKSession(true); }
                foreach (var parameter in variablevalues)
                {
                    if (parameter.Value == null) continue;
                    sharpAHK.ahkGlobal.ahkdll.SetVar(parameter.Key, parameter.Value.ToString());
                }
                sharpAHK.ahkGlobal.ahkdll.ExecRaw(code);
                foreach (dynamic v in vars)
                {
                    var value = sharpAHK.ahkGlobal.ahkdll.GetVar(v.DisplayName);
                    PropertyDescriptor myVar = context.DataContext.GetProperties().Find(v.DisplayName, true);
                    if (myVar != null && value != null && value != "")
                    {
                        if (myVar.PropertyType == typeof(string))
                            myVar.SetValue(context.DataContext, value);
                        else if (myVar.PropertyType == typeof(int)) myVar.SetValue(context.DataContext, int.Parse(value.ToString()));
                        else if (myVar.PropertyType == typeof(bool)) myVar.SetValue(context.DataContext, bool.Parse(value.ToString()));
                        else Log.Information("Ignorering variable " + v.DisplayName + " of type " + myVar.PropertyType.FullName);
                        //var myValue = myVar.GetValue(context.DataContext);

                    }
                }
                sharpAHK.ahkGlobal.ahkdll.Terminate();
                return;
            }
            if (language == "Python")
            {
                try
                {
                    GenericTools.RunUI(() =>
                    {
                        IntPtr lck = IntPtr.Zero;
                        try
                        {
                            lck = PythonEngine.AcquireLock();
                            using (var scope = Py.CreateScope())
                            {
                                foreach (var parameter in variablevalues)
                                {
                                    PyObject pyobj = parameter.Value.ToPython();
                                    scope.Set(parameter.Key, pyobj);
                                }
                                try
                                {
                                    PythonEngine.RunSimpleString(@"
import sys
from System import Console
class output(object):
    def write(self, msg):
        Console.Out.Write(msg)
    def writelines(self, msgs):
        for msg in msgs:
            Console.Out.Write(msg)
    def flush(self):
        pass
    def close(self):
        pass
sys.stdout = sys.stderr = output()
");

                                }
                                catch (Exception ex)
                                {
                                    Log.Debug(ex.ToString());
                                }
                                scope.Exec(code);
                                foreach (var parameter in variablevalues)
                                {
                                    PyObject pyobj = scope.Get(parameter.Key);
                                    if (pyobj == null) continue;
                                    PropertyDescriptor myVar = context.DataContext.GetProperties().Find(parameter.Key, true);
                                    if (myVar.PropertyType == typeof(string))
                                        myVar.SetValue(context.DataContext, pyobj.ToString());
                                    else if (myVar.PropertyType == typeof(int)) myVar.SetValue(context.DataContext, int.Parse(pyobj.ToString()));
                                    else if (myVar.PropertyType == typeof(bool)) myVar.SetValue(context.DataContext, bool.Parse(pyobj.ToString()));
                                    else Log.Information("Ignorering variable " + parameter.Key + " of type " + myVar.PropertyType.FullName);
                                }
                            }

                            //lck = PythonEngine.AcquireLock();
                            //PythonEngine.Exec(code);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                        finally
                        {
                            PythonEngine.ReleaseLock(lck);
                        }
                    });
                    //using (Python.Runtime.Py.GIL())
                    //{
                    //    IntPtr lck = Python.Runtime.PythonEngine.AcquireLock();
                    //    Python.Runtime.PythonEngine.Exec(code);
                    //    Python.Runtime.PythonEngine.ReleaseLock(lck);
                    //    //// create a Python scope
                    //    //using (var scope = Python.Runtime.Py.CreateScope())
                    //    //{
                    //    //    //// convert the Person object to a PyObject
                    //    //    //PyObject pyPerson = person.ToPython();

                    //    //    // create a Python variable "person"
                    //    //    // scope.Set("person", pyPerson);

                    //    //    // the person object may now be used in Python
                    //    //    // string code = "fullName = person.FirstName + ' ' + person.LastName";
                    //    //    scope.Exec(code);
                    //    //}
                    //}
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    try
                    {
                        // Python.Runtime.PythonEngine.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
                return;
            }
            if (language == "Python2")
            {
                // http://putridparrot.com/blog/hosting-ironpython-in-a-c-application/
                // http://jonsblogat.blogspot.com/2011/06/adding-scripting-support-to-wpf.html
                //var ipy = IronPython.Hosting.Python.CreateRuntime();
                //var engine = IronPython.Hosting.Python.CreateEngine();
                //var paths = Environment.GetEnvironmentVariable("path").Split(';');
                //var libs = new List<string>();
                //foreach (var p in paths.Where(x => x.ToLower().Contains("python")))
                //{
                //    if (System.IO.Directory.Exists(p) && p.ToLower().Contains("ironpython"))
                //    {
                //        libs.Add(p);
                //        if (System.IO.Directory.Exists(System.IO.Path.Combine(p, "Lib"))) libs.Add(System.IO.Path.Combine(p, "Lib"));
                //        if (System.IO.Directory.Exists(System.IO.Path.Combine(p, "lib\\site-packages"))) libs.Add(System.IO.Path.Combine(p, "lib\\site-packages"));
                //        if (System.IO.Directory.Exists(System.IO.Path.Combine(p, "DLLs"))) libs.Add(System.IO.Path.Combine(p, "DLLs"));
                //    }

                //}
                //engine.SetSearchPaths(libs);
                //var scope = engine.CreateScope();
                //foreach (var parameter in variablevalues)
                //{
                //    // if (parameter.Value == null) continue;
                //    scope.SetVariable(parameter.Key, parameter.Value);
                //}
                //var source = engine.CreateScriptSourceFromString(code, Microsoft.Scripting.SourceCodeKind.Statements);
                //var errors = new ErrorListener();
                //var command = source.Compile(errors);
                //if (command == null)
                //{
                //    foreach (var e in errors.errors)
                //    {
                //        Console.WriteLine(e.source.ToString() + "(" + e.span.Start + "): " + e.message);
                //    }
                //}
                //var result = source.Execute(scope);
                //foreach (string name in scope.GetVariableNames())
                //{
                //    try
                //    {
                //        var myVar = context.DataContext.GetProperties().Find(name, true);
                //        if (myVar != null)
                //        {
                //            myVar.SetValue(context.DataContext, scope.GetVariable(name));
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        Log.Error(ex.ToString());
                //    }
                //}
                return;
            }

            var assemblyLocations = GetAssemblyLocations();
            CompileAndRun(language, sourcecode, assemblyLocations.ToArray(), variablevalues, context);
        }
        public static string[] GetAssemblyLocations()
        {
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
                        // if (asm.Location.Contains("Microsoft.Office.Interop")) continue;
                        if (string.IsNullOrEmpty(asm.Location)) continue;
                        if (asm.Location.Contains("System.Numerics.Vectors"))
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
            return assemblyLocations.ToArray();
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
                // CompilerParams.ReferencedAssemblies.Add(@"C:\code\openrpa\bin\Microsoft.Office.Tools.Excel.dll");
                CodeDomProvider provider = null;
                if (language == "VB")
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
        private static List<string> Namespaces = new List<string>() { "System", "System.Collections", "System.Data" };
        public static string GetCSharpHeaderText(Dictionary<string, Type> variables, string moduleName)
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
        public static string GetCSharpFooterText()
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
        public static string GetVBHeaderText(Dictionary<string, Type> variables, string moduleName)
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
    }

    public class AssemblyLoader
    {
        private Dictionary<string, Assembly> loadedAssemblies;

        public AssemblyLoader()
        {
            loadedAssemblies = new Dictionary<string, Assembly>();

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string shortName = args.Name.Split(',')[0];
                string resourceName = $"{shortName}.dll";

                if (loadedAssemblies.ContainsKey(resourceName))
                {
                    return loadedAssemblies[resourceName];
                }

            // looks for the assembly from the resources and load it
            using (System.IO.Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        var assemblyData = new byte[stream.Length];
                        stream.Read(assemblyData, 0, assemblyData.Length);
                        Assembly assembly = Assembly.Load(assemblyData);
                        loadedAssemblies[resourceName] = assembly;
                        return assembly;
                    }
                }
                return null;
            };
        }

    }
    //public class AppDomainDelegate : MarshalByRefObject
    //{
    //    public void Execute(Action action)
    //    {
    //        action();
    //    }
    //}
    public class AppDomainDelegate : MarshalByRefObject
    {
        public void Execute(Action action) { action(); }

        public void Execute<T>(T parameter, Action<T> action)
        {
            action(parameter);
        }
    }
}