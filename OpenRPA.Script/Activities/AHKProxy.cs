using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Script.Activities
{
    public class AHKProxy : MarshalByRefObject
    {
        public static void New_AHKSession(bool NewInstance = false)
        {
            if (sharpAHK.ahkGlobal.ahkdll == null || NewInstance == true) { sharpAHK.ahkGlobal.ahkdll = new AutoHotkey.Interop.AutoHotkeyEngine(); }

            else { sharpAHK.ahkGlobal.ahkdll = null; }  // option to start new AHK session (resets variables and previously loaded functions)

            sharpAHK.ahkGlobal.LoadedAHK = new List<string>(); // reset loaded ahk list
        }
        public AHKProxy()
        {
            New_AHKSession(false);
        }
        public Assembly GetAssembly(string assemblyPath)
        {
            try
            {
                return Assembly.LoadFile(assemblyPath);
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            //This handler is called only when the common language runtime tries to bind to the assembly and fails.

            //Retrieve the list of referenced assemblies in an array of AssemblyName.
            Assembly MyAssembly, objExecutingAssemblies;
            string strTempAssmbPath = "";

            var asmBase = System.IO.Directory.GetCurrentDirectory();

            objExecutingAssemblies = Assembly.GetExecutingAssembly();
            AssemblyName[] arrReferencedAssmbNames = objExecutingAssemblies.GetReferencedAssemblies();

            //Loop through the array of referenced assembly names.
            foreach (AssemblyName strAssmbName in arrReferencedAssmbNames)
            {
                //Check for the assembly names that have raised the "AssemblyResolve" event.
                if (strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")) == args.Name.Substring(0, args.Name.IndexOf(",")))
                {
                    //Build the path of the assembly from where it has to be loaded.
                    //The following line is probably the only line of code in this method you may need to modify:
                    strTempAssmbPath = asmBase;
                    if (strTempAssmbPath.EndsWith("\\")) strTempAssmbPath += "\\";
                    strTempAssmbPath += args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll";
                    break;
                }

            }
            //Load the assembly from the specified path.
            MyAssembly = Assembly.LoadFrom(strTempAssmbPath);

            //Return the loaded assembly.
            return MyAssembly;
        }
        internal void SetVar(string key, string v)
        {
            sharpAHK.ahkGlobal.ahkdll.SetVar(key, v);
        }
        internal void ExecRaw(string code)
        {
            sharpAHK.ahkGlobal.ahkdll.ExecRaw(code);
        }
        internal string GetVar(string displayName)
        {
            return sharpAHK.ahkGlobal.ahkdll.GetVar(displayName);
        }
    }
}
