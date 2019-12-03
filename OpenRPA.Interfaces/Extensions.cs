using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public static class Extensions
    {
        public static string MyVideos
        {
            get
            {
                var dir = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                if (!System.IO.Directory.Exists(System.IO.Path.Combine(dir)))
                    System.IO.Directory.CreateDirectory(dir);
                return dir;
            }
        }
        public static string MyDocuments
        {
            get
            {
                var dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (!System.IO.Directory.Exists(System.IO.Path.Combine(dir)))
                    System.IO.Directory.CreateDirectory(dir);
                return dir;
            }
        }
        public static string MyPictures
        {
            get
            {
                var dir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                if (!System.IO.Directory.Exists(System.IO.Path.Combine(dir)))
                    System.IO.Directory.CreateDirectory(dir);
                return dir;
            }
        }
        public static string UserDirectory
        {
            get
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenRPA");
                if (!System.IO.Directory.Exists(System.IO.Path.Combine(dir)))
                    System.IO.Directory.CreateDirectory(dir);
                return dir;
            }
        }
        private static string _ProjectsDirectory = null;
        public static string ProjectsDirectory
        {
            get
            {
                if(string.IsNullOrEmpty(_ProjectsDirectory))
                {
                    var asm = System.Reflection.Assembly.GetEntryAssembly();
                    var filepath = asm.CodeBase.Replace("file:///", "");
                    var path = System.IO.Path.GetDirectoryName(filepath);
                    // if (path.ToLower().Contains("program")) path = System.IO.Path.Combine(MyDocuments, "OpenRPA");
                    path = System.IO.Path.Combine(MyDocuments, "OpenRPA");
                    if (!System.IO.Directory.Exists(System.IO.Path.Combine(path))) System.IO.Directory.CreateDirectory(path);
                    _ProjectsDirectory = path;

                }
                return _ProjectsDirectory;
            }
            set
            {
                _ProjectsDirectory = value;
            }
        }
        public static string PluginsDirectory
        {
            get
            {
                var asm = System.Reflection.Assembly.GetEntryAssembly();
                var filepath = asm.CodeBase.Replace("file:///", "");
                var path = System.IO.Path.GetDirectoryName(filepath);
                return path;
            }
        }

        public static string DataDirectory
        {
            get
            {
                var asm = System.Reflection.Assembly.GetEntryAssembly();
                var filepath = asm.CodeBase.Replace("file:///", "");
                var path = System.IO.Path.GetDirectoryName(filepath);
                // if (path.ToLower().Contains("program")) path = UserDirectory;
                path = UserDirectory;
                return path;
            }
        }

        static public string ResourceAsString(this Type type, string resourceName)
        {
            // string[] names = typeof(Extensions).Assembly.GetManifestResourceNames();
            string[] names = type.Assembly.GetManifestResourceNames();
            foreach (var name in names)
            {
                if (name.EndsWith(resourceName))
                {
                    using (var s = type.Assembly.GetManifestResourceStream(name))
                    {
                        using (var reader = new System.IO.StreamReader(s))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
                else
                {
                    try
                    {
                        var set = new System.Resources.ResourceSet(type.Assembly.GetManifestResourceStream(names[0]));
                        foreach (System.Collections.DictionaryEntry resource in set)
                        {
                            // Log.Information("\n[{0}] \t{1}", resource.Key, resource.Value);
                            if (((string)resource.Key).EndsWith(resourceName.ToLower()))
                            {
                                using (var reader = new System.IO.StreamReader(resource.Value as System.IO.Stream))
                                {
                                    return reader.ReadToEnd();
                                }

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex.ToString());
                    }
                }
            }
            return null;
        }
        static public string ResourceAsString(string resourceName)
        {
            return ResourceAsString(typeof(Extensions), resourceName);
        }
        public static System.Windows.Media.Imaging.BitmapFrame GetImageSourceFromResource(string resourceName)
        {
            string[] names = typeof(Extensions).Assembly.GetManifestResourceNames();
            foreach (var name in names)
            {
                if (name.EndsWith(resourceName))
                {
                    return System.Windows.Media.Imaging.BitmapFrame.Create(typeof(Extensions).Assembly.GetManifestResourceStream(name));
                }
            }
            return null;
        }
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source) action(item);
        }
        public static string JParse(this string o)
        {
            if (o == null) return null;
            string result = Newtonsoft.Json.JsonConvert.ToString(o);
            result = result.Substring(1, result.Length - 2);
            return result;
        }
        public static void AddCacheArgument(System.Activities.NativeActivityMetadata metadata, string name, System.Activities.Argument argument)
        {
            try
            {
                if (argument == null) return;
                Type ttype = argument.GetType().GetGenericArguments()[0];
                System.Activities.ArgumentDirection direction = System.Activities.ArgumentDirection.In;
                if (argument is System.Activities.InArgument) direction = System.Activities.ArgumentDirection.In;
                if (argument is System.Activities.InOutArgument) direction = System.Activities.ArgumentDirection.InOut;
                if (argument is System.Activities.OutArgument) direction = System.Activities.ArgumentDirection.Out;
                var ra = new System.Activities.RuntimeArgument(name, ttype, direction);
                metadata.Bind(argument, ra);
                metadata.AddArgument(ra);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static string replaceEnvironmentVariable(this string filename)
        {
            var USERPROFILE = Environment.GetEnvironmentVariable("USERPROFILE");
            var windir = Environment.GetEnvironmentVariable("windir");
            var SystemRoot = Environment.GetEnvironmentVariable("SystemRoot");
            var PUBLIC = Environment.GetEnvironmentVariable("PUBLIC");

            if (!string.IsNullOrEmpty(USERPROFILE)) filename = filename.Replace(USERPROFILE, "%USERPROFILE%");
            if (!string.IsNullOrEmpty(windir)) filename = filename.Replace(windir, "%windir%");
            if (!string.IsNullOrEmpty(SystemRoot)) filename = filename.Replace(SystemRoot, "%SystemRoot%");
            if (!string.IsNullOrEmpty(PUBLIC)) filename = filename.Replace(PUBLIC, "%PUBLIC%");

            var ProgramData = Environment.GetEnvironmentVariable("ProgramData");
            if (!string.IsNullOrEmpty(ProgramData)) filename = filename.Replace(ProgramData, "%ProgramData%");
            var ProgramFilesx86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            if (!string.IsNullOrEmpty(ProgramFilesx86)) filename = filename.Replace(ProgramFilesx86, "%ProgramFiles(x86)%");
            var ProgramFiles = Environment.GetEnvironmentVariable("ProgramFiles");
            if (!string.IsNullOrEmpty(ProgramFiles)) filename = filename.Replace(ProgramFiles, "%ProgramFiles%");
            var LOCALAPPDATA = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrEmpty(LOCALAPPDATA)) filename = filename.Replace(LOCALAPPDATA, "%LOCALAPPDATA%");
            var APPDATA = Environment.GetEnvironmentVariable("APPDATA");
            if (!string.IsNullOrEmpty(APPDATA)) filename = filename.Replace(APPDATA, "%APPDATA%");


            //var = Environment.GetEnvironmentVariable("");
            //if (!string.IsNullOrEmpty()) filename = filename.Replace(, "%%");

            return filename;
        }
        public static Task WaitOneAsync(this System.Threading.WaitHandle waitHandle)
        {
            if (waitHandle == null) throw new ArgumentNullException("waitHandle");
            var tcs = new TaskCompletionSource<bool>();
            var rwh = System.Threading.ThreadPool.RegisterWaitForSingleObject(waitHandle, delegate { tcs.TrySetResult(true); }, null, -1, true);
            var t = tcs.Task;
            t.ContinueWith((antecedent) => rwh.Unregister(null));
            return t;
        }
        public static bool TryCast<T>(this object obj, out T result)
        {
            if (obj is T)
            {
                result = (T)obj;
                return true;
            }
            if (obj is System.Activities.Expressions.Literal<T>)
            {
                result = (T)((System.Activities.Expressions.Literal<T>)obj).Value;
                return true;
            }

            result = default(T);
            return false;
        }
        public static T TryCast<T>(this object obj)
        {
            T result = default(T);
            if (TryCast<T>(obj, out result))
                return result;
            return result;
        }
        public static T GetValue<T>(this System.Activities.Presentation.Model.ModelItem model, string name)
        {
            T result = default(T);
            if (model.Properties[name] != null)
            {
                if (model.Properties[name].Value == null) return result;
                if (model.Properties[name].Value.Properties["Expression"] != null)
                {
                    result = model.Properties[name].Value.Properties["Expression"].ComputedValue.TryCast<T>();
                    return result;
                }
                result = model.Properties[name].ComputedValue.TryCast<T>();
                return result;
            }
            return result;
        }
        public static ProcessInfo GetProcessInfo(this FlaUI.Core.AutomationElements.Infrastructure.AutomationElement element)
        {
            if (!element.Properties.ProcessId.IsSupported) return null;
            ProcessInfo result = new ProcessInfo();
            int processId = -1;
            IntPtr handle = IntPtr.Zero;
            try
            {
                processId = element.Properties.ProcessId.Value;
                var p = System.Diagnostics.Process.GetProcessById(processId);
                handle = p.Handle;
                result.processname = p.ProcessName;
                result.filename = p.MainModule.FileName.replaceEnvironmentVariable();
            }
            catch (Exception)
            {
            }

            bool _isImmersiveProcess = false;
            try
            {
                if (handle != IntPtr.Zero) _isImmersiveProcess = IsImmersiveProcess(handle);
            }
            catch (Exception)
            {
            }
            string ApplicationUserModelId = null;
            if (_isImmersiveProcess)
            {
                var automation = AutomationUtil.getAutomation();
                var pc = new FlaUI.Core.Conditions.PropertyCondition(automation.PropertyLibrary.Element.ClassName, "Windows.UI.Core.CoreWindow");
                var _el = element.FindFirstChild(pc);
                if (_el != null)
                {
                    processId = _el.Properties.ProcessId.Value;

                    IntPtr ptrProcess = OpenProcess(QueryLimitedInformation, false, processId);
                    if (IntPtr.Zero != ptrProcess)
                    {
                        uint cchLen = 130; // Currently APPLICATION_USER_MODEL_ID_MAX_LENGTH = 130
                        StringBuilder sbName = new StringBuilder((int)cchLen);
                        Int32 lResult = GetApplicationUserModelId(ptrProcess, ref cchLen, sbName);
                        if (APPMODEL_ERROR_NO_APPLICATION == lResult)
                        {
                            _isImmersiveProcess = false;
                        }
                        else if (ERROR_SUCCESS == lResult)
                        {
                            ApplicationUserModelId = sbName.ToString();
                        }
                        else if (ERROR_INSUFFICIENT_BUFFER == lResult)
                        {
                            sbName = new StringBuilder((int)cchLen);
                            if (ERROR_SUCCESS == GetApplicationUserModelId(ptrProcess, ref cchLen, sbName))
                            {
                                ApplicationUserModelId = sbName.ToString();
                            }
                        }
                        CloseHandle(ptrProcess);
                    }
                }
                else { _isImmersiveProcess = false; }


            }
            try
            {
                var arguments = GetCommandLine(processId);
                var arr = parseCommandLine(arguments);

                if (arr.Length == 0)
                {

                }
                else if (arguments.Contains("\"" + arr[0] + "\""))
                {
                    result.arguments = arguments.Replace("\"" + arr[0] + "\"", "");
                }
                else
                {
                    result.arguments = arguments.Replace(arr[0], "");
                }
                if (result.arguments != null) { result.arguments = result.arguments.replaceEnvironmentVariable(); }
                //if (arr.Length > 0)
                //{
                //    var resultarr = new string[arr.Length - 1];
                //    Array.Copy(arr, 1, resultarr, 0, arr.Length - 1);
                //    result.arguments = string.Join(" ", resultarr).replaceEnvironmentVariable();
                //}

            }
            catch (Exception)
            {
            }
            result.applicationUserModelId = ApplicationUserModelId;
            result.isImmersiveProcess = _isImmersiveProcess;
            return result;
        }
        public static string GetCommandLine(int processId)
        {
            string result = null;
            try
            {
                var thread = new System.Threading.Thread(() =>
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + processId))
                    using (ManagementObjectCollection objects = searcher.Get())
                    {
                        result = objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
                    }
                });
                thread.Start();
                thread.Join(); //wait for the thread to finish

            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                return null;
            }
            return result;
        }
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsImmersiveProcess(IntPtr hWnd);
        public const int QueryLimitedInformation = 0x1000;
        public const int ERROR_INSUFFICIENT_BUFFER = 0x7a;
        public const int ERROR_SUCCESS = 0x0;
        public const int APPMODEL_ERROR_NO_APPLICATION = 15703;
        public static String[] parseCommandLine(String commandLine)
        {
            List<String> arguments = new List<String>();

            Boolean stringIsQuoted = false;
            String argString = "";
            for (int c = 0; c < commandLine.Length; c++)  //process string one character at a tie
            {
                if (commandLine.Substring(c, 1) == "\"")
                {
                    if (stringIsQuoted)  //end quote so populate next element of list with constructed argument
                    {
                        arguments.Add(argString);
                        argString = "";
                    }
                    else
                    {
                        stringIsQuoted = true; //beginning quote so flag and scip
                    }
                }
                else if (commandLine.Substring(c, 1) == "".PadRight(1))
                {
                    if (stringIsQuoted)
                    {
                        argString += commandLine.Substring(c, 1); //blank is embedded in quotes, so preserve it
                    }
                    else if (argString.Length > 0)
                    {
                        arguments.Add(argString);  //non-quoted blank so add to list if the first consecutive blank
                    }
                }
                else
                {
                    argString += commandLine.Substring(c, 1);  //non-blan character:  add it to the element being constructed
                }
            }

            return arguments.ToArray();

        }
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hHandle);
        [DllImport("kernel32.dll")]
        public static extern Int32 GetApplicationUserModelId(
            IntPtr hProcess,
            ref UInt32 AppModelIDLength,
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder sbAppUserModelID);
    }
    public class ProcessInfo
    {
        public string filename { get; set; }
        public string processname { get; set; }
        public string arguments { get; set; }
        public string applicationUserModelId { get; set; }
        public bool isImmersiveProcess { get; set; }
    }

}
