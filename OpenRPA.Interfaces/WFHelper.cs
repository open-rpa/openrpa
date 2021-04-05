using System;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public static class WFHelper
    {
        //public static System.Activities.ActivityWithResult TryCreateLiteral(Type type, string expressionText)
        //{
        //    var ActivityDesignerAsm = typeof(System.Activities.Presentation.ActivityDesigner).Assembly;
        //    var types = ActivityDesignerAsm.GetTypes();
        //    var ExpressionHelper = types.Where(x => x.Name == "ExpressionHelper").FirstOrDefault();
        //    var ParserContext = types.Where(x => x.Name == "ParserContext").FirstOrDefault();

        //    object context = Activator.CreateInstance(ParserContext);

        //    var TryCreateLiteralMethod = ExpressionHelper.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(x => x.Name == "TryCreateLiteral").FirstOrDefault();
        //    var o = TryCreateLiteralMethod.Invoke(ExpressionHelper, new object[] { type, expressionText, context });
        //    return o as System.Activities.ActivityWithResult;
        //}
        public static void AddVBNamespaceSettings(System.Activities.ActivityBuilder rootObject, string[] namespaces, params Type[] types)
        {
            var rootElement = rootObject.Implementation;
            var vbsettings = Microsoft.VisualBasic.Activities.VisualBasic.GetSettings(rootObject);
            if (vbsettings == null)
            {
                vbsettings = new Microsoft.VisualBasic.Activities.VisualBasicSettings();
            }
            foreach (var name in namespaces)
            {
                var import = new Microsoft.VisualBasic.Activities.VisualBasicImportReference() { Import = name };
                vbsettings.ImportReferences.Add(import);
            }
            foreach (Type t in types)
            {
                vbsettings.ImportReferences.Add(
                    new Microsoft.VisualBasic.Activities.VisualBasicImportReference
                    {
                        Assembly = t.Assembly.GetName().Name,
                        Import = t.Namespace
                    });
            }
            Microsoft.VisualBasic.Activities.VisualBasic.SetSettings(rootObject, vbsettings);
        }
        public static void AddVBNamespaceSettings(System.Activities.Presentation.WorkflowDesigner WorkflowDesigner, params Type[] types)
        {
            var namespaces = WorkflowDesigner.Context.Items.GetValue<System.Activities.Presentation.Hosting.ImportedNamespaceContextItem>().ImportedNamespaces.ToArray();
            var acci = WorkflowDesigner.Context.Items.GetValue<System.Activities.Presentation.Hosting.AssemblyContextControlItem>() ?? new System.Activities.Presentation.Hosting.AssemblyContextControlItem();
            foreach(var t in types)
            {
                DynamicAssemblyMonitor(WorkflowDesigner, t.Assembly.GetName().Name, t.Assembly, true);
            }
        }
        public static void DynamicAssemblyMonitor(System.Activities.Presentation.WorkflowDesigner WorkflowDesigner, string fullname, Assembly asm, bool toadd)
        {
            //var modelService = WorkflowDesigner.Context.Services.GetService<System.Activities.Presentation.Services.ModelService>();
            //using (ModelEditingScope editingScope = modelService.Root.BeginEdit("Implementation"))
            //{
            //    var acci = WorkflowDesigner.Context.Items.GetValue<System.Activities.Presentation.Hosting.AssemblyContextControlItem>() ?? new System.Activities.Presentation.Hosting.AssemblyContextControlItem();
            //    if (acci.ReferencedAssemblyNames == null)
            //        acci.ReferencedAssemblyNames = new List<AssemblyName>();
            //    if (toadd)
            //        AddDyanamicAssembly(WorkflowDesigner, acci, asm);
            //    else
            //        RemoveDynamicAssembly(WorkflowDesigner, acci, asm);
            //    editingScope.Complete();
            //}
            var modelService = WorkflowDesigner.Context.Services.GetService<System.Activities.Presentation.Services.ModelService>();
            using (ModelEditingScope editingScope = modelService.Root.BeginEdit("Implementation"))
            {
                var acci = WorkflowDesigner.Context.Items.GetValue<System.Activities.Presentation.Hosting.AssemblyContextControlItem>() ?? new System.Activities.Presentation.Hosting.AssemblyContextControlItem();
                if (acci.ReferencedAssemblyNames == null)
                {
                    var root = GetRootElement(WorkflowDesigner);
                    var vbsettings = Microsoft.VisualBasic.Activities.VisualBasic.GetSettings(root) ?? new Microsoft.VisualBasic.Activities.VisualBasicSettings();

                    var namespaces = WorkflowDesigner.Context.Items.GetValue<System.Activities.Presentation.Hosting.ImportedNamespaceContextItem>().ImportedNamespaces.ToArray();
                    var orgs = acci.AllAssemblyNamesInContext.ToList();
                    var orgs2 = new List<AssemblyName>();
                    foreach (var a in orgs) orgs2.Add(new AssemblyName(a));

                    IEnumerable<Assembly> appAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                    var result = new List<AssemblyName>();
                    foreach (var a in appAssemblies)
                    {
                        if (a.IsDynamic) continue;
                        var _namespaces = a.ExportedTypes.Select(x => x.Namespace).ToArray();
                        var exists = _namespaces.Where(x => namespaces.Contains(x));
                        if (exists.Count() > 0)
                        {
                            try
                            {
                                var asmname = a.GetName().Name;
                                result.Add(a.GetName());
                                foreach (var name in _namespaces)
                                {
                                    if (string.IsNullOrEmpty(name)) continue;
                                    var import = new Microsoft.VisualBasic.Activities.VisualBasicImportReference() { Assembly = asmname, Import = name };
                                    vbsettings.ImportReferences.Add(import);
                                }

                            }
                            catch (Exception)
                            {
                                Console.WriteLine(a.FullName);
                            }

                        }
                    }
                    acci.ReferencedAssemblyNames = result;
                    Microsoft.VisualBasic.Activities.VisualBasic.SetSettings(root, vbsettings);
                }
                if (acci.ReferencedAssemblyNames == null)
                    acci.ReferencedAssemblyNames = new List<AssemblyName>();
                if (toadd)
                    AddDyanamicAssembly(WorkflowDesigner, acci, asm);
                else
                    RemoveDynamicAssembly(WorkflowDesigner, acci, asm);
                editingScope.Complete();
            }
        }
        public static void DynamicAssemblyMonitor2(System.Activities.Presentation.WorkflowDesigner WorkflowDesigner, string fullname, Assembly asm, bool toadd)
        {
            var acci = WorkflowDesigner.Context.Items.GetValue<System.Activities.Presentation.Hosting.AssemblyContextControlItem>() ?? new System.Activities.Presentation.Hosting.AssemblyContextControlItem();
            if (acci.ReferencedAssemblyNames == null)
            {
                var namespaces = WorkflowDesigner.Context.Items.GetValue<System.Activities.Presentation.Hosting.ImportedNamespaceContextItem>().ImportedNamespaces.ToArray();

                var orgs = acci.AllAssemblyNamesInContext.ToList();
                var orgs2 = new List<AssemblyName>();
                foreach (var a in orgs) orgs2.Add(new AssemblyName(a));
                IEnumerable<Assembly> appAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                var result = new List<AssemblyName>();
                foreach (var a in appAssemblies)
                {
                    if (a.IsDynamic) continue;
                    var _namespaces = a.ExportedTypes.Select(x => x.Namespace).ToArray();
                    var exists = _namespaces.Where(x => namespaces.Contains(x));
                    if (exists.Count() > 0)
                    {
                        try
                        {
                            // AddDyanamicAssembly(WorkflowDesigner, acci, a);
                            result.Add(a.GetName());
                        }
                        catch (Exception)
                        {
                            Console.WriteLine(a.FullName);
                        }

                    }
                }
                acci.ReferencedAssemblyNames = result;
                var rootObject = GetRootElement(WorkflowDesigner) as System.Activities.ActivityBuilder;
                var vbsettings = Microsoft.VisualBasic.Activities.VisualBasic.GetSettings(rootObject) ?? new Microsoft.VisualBasic.Activities.VisualBasicSettings();

                foreach (var name in namespaces)
                {
                    var import = new Microsoft.VisualBasic.Activities.VisualBasicImportReference() { Import = name };
                    vbsettings.ImportReferences.Add(import);
                }
                Microsoft.VisualBasic.Activities.VisualBasic.SetSettings(rootObject, vbsettings);
                WorkflowDesigner.Context.Items.SetValue(acci);
            }
            if (toadd)
                AddDyanamicAssembly(WorkflowDesigner, acci, asm);
            else
                RemoveDynamicAssembly(WorkflowDesigner, acci, asm);
        }
        public static void RemoveDynamicAssembly(System.Activities.Presentation.WorkflowDesigner WorkflowDesigner, System.Activities.Presentation.Hosting.AssemblyContextControlItem acci, Assembly asm)
        {
            if (acci.ReferencedAssemblyNames.Contains(asm.GetName()))
            {
                acci.ReferencedAssemblyNames.Remove(asm.GetName());
                WorkflowDesigner.Context.Items.SetValue(acci);
            }
            var root = GetRootElement(WorkflowDesigner);
            if (null == root) return;
            var vbs = Microsoft.VisualBasic.Activities.VisualBasic.GetSettings(root) ?? new Microsoft.VisualBasic.Activities.VisualBasicSettings();

            var namespaces = (from type in asm.GetTypes() select type.Namespace).Distinct();
            var fullname = asm.FullName;
            foreach (var name in namespaces)
            {
                var theimport = (from importname in vbs.ImportReferences where importname.Assembly == fullname where importname.Import == name select importname).FirstOrDefault();
                if (theimport != null)
                    vbs.ImportReferences.Remove(theimport);
            }
            Microsoft.VisualBasic.Activities.VisualBasic.SetSettings(root, vbs);

        }
        public static void AddDyanamicAssembly(System.Activities.Presentation.WorkflowDesigner WorkflowDesigner, System.Activities.Presentation.Hosting.AssemblyContextControlItem acci, Assembly asm)
        {
            if (!acci.ReferencedAssemblyNames.Select(x=> x.FullName).Contains(asm.GetName().FullName))
            {
                acci.ReferencedAssemblyNames.Add(asm.GetName());
                WorkflowDesigner.Context.Items.SetValue(acci);
            }
            var root = GetRootElement(WorkflowDesigner);
            var fullname = asm.FullName;
            var asmname = asm.GetName().Name;
            if (null == root) return;
            var vbs = Microsoft.VisualBasic.Activities.VisualBasic.GetSettings(root) ?? new Microsoft.VisualBasic.Activities.VisualBasicSettings();

            var namespaces = (from type in asm.GetTypes() select type.Namespace).Distinct();
            foreach (var name in namespaces)
            {
                if (string.IsNullOrEmpty(name)) continue;
                var import = new Microsoft.VisualBasic.Activities.VisualBasicImportReference() { Assembly = asmname, Import = name };
                vbs.ImportReferences.Add(import);
            }
            Microsoft.VisualBasic.Activities.VisualBasic.SetSettings(root, vbs);
        }
        public static object GetRootElement(System.Activities.Presentation.WorkflowDesigner WorkflowDesigner)
        {
            var modelservice = WorkflowDesigner.Context.Services.GetService<System.Activities.Presentation.Services.ModelService>();
            if (modelservice == null) return null;
            var rootmodel = modelservice.Root.GetCurrentValue();
            return rootmodel;
        }

    }
}
