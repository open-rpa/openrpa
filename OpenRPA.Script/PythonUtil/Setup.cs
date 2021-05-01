// Inspired by Keras.NET 
// https://github.com/SciSharp/Keras.NET/blob/master/Keras/Setup.cs
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Script.PythonUtil
{
    public static class Setup
    {
        //private static string pythonCommand = "python";
        // private static string pipCommand = "pip";
        public static void InstallPip(string path)
        {
            var libpath = System.IO.Path.Combine(path, "Lib");
            if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
            if (!System.IO.Directory.Exists(libpath)) System.IO.Directory.CreateDirectory(libpath);
            using (var webClient = new System.Net.WebClient())
            {
                webClient.DownloadFile("https://bootstrap.pypa.io/get-pip.py", System.IO.Path.Combine(libpath, "get-pip.py"));
            }
            string result = RunCommand(path, System.IO.Path.Combine(path, "python.exe"), "Lib\\get-pip.py");

        }
        public static void AddToPath(string path)
        {
            var Value = GetEnv("PATH");
            if (!Value.Contains(path))
            {
                Value += (path + ";");
                SetEnv("PATH", Value);
            }
        }
        public static void SetEnv(string Name, string Value)
        {
            var scope = EnvironmentVariableTarget.Process;
            Environment.SetEnvironmentVariable(Name, Value, scope);
        }
        public static string GetEnv(string Name)
        {
            var scope = EnvironmentVariableTarget.Process;
            var Value = Environment.GetEnvironmentVariable(Name, scope);
            return Value;
        }
        public static void Run(string[] modules = null)
        {
            // if (modules == null) modules = new string[] { "numpy" };
            if (modules == null) modules = new string[] { };
            int pyversion = CheckPythonVer();
            if (pyversion == 0)
                throw new Exception("Python 3.7 not found! Please download and install from https://www.python.org/downloads/release/python-370/");
            if (pyversion == 37 || pyversion == 37 || pyversion == 38 || pyversion == 39)
            {
                foreach (var item in modules)
                {
                    InstallModule(item);
                }
            }
            else
            {
                throw new Exception("Python version not supported: " + pyversion);
            }
        }
        public static void InstallModule(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            Log.Debug("using existing python, check if '" + name + "' is installed");
            if (CheckModule(name) == null)
            {
                Log.Information("Installing python module '" + name + "'");
                Console.WriteLine("Installing {0}.....", name);
                // string result = RunCommand(null, "python", string.Format("-m pip install {0}", name));
                string result = RunCommand(null, "pip", string.Format("install {0}", name));
                Console.Write("Done!");
            }
        }
        public static ModuleInfo CheckModule(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            int pyversion = CheckPythonVer();
            if (pyversion == 0)
                throw new Exception("Python 3.7 not found");
            ModuleInfo result = null;
            if (pyversion == 37 || pyversion == 37 || pyversion == 38 || pyversion == 39)
            {
                string info = RunCommand(null, "python", string.Format("-m pip show {0}", name));
                if (!string.IsNullOrWhiteSpace(info))
                {
                    string[] lines = info.Split('\n');
                    result = new ModuleInfo();
                    foreach (var item in lines)
                    {
                        if (item.Contains("Name: "))
                        {
                            result.Name = item.Replace("Name: ", "").Trim();
                        }

                        if (item.Contains("Version: "))
                        {
                            result.Version = item.Replace("Version: ", "").Trim();
                        }

                        if (item.Contains("Summary: "))
                        {
                            result.Summary = item.Replace("Summary: ", "").Trim();
                        }

                        if (item.Contains("Author: "))
                        {
                            result.Author = item.Replace("Author: ", "").Trim();
                        }

                        if (item.Contains("Author-email: "))
                        {
                            result.AuthorEmail = item.Replace("Author-email: ", "").Trim();
                        }

                        if (item.Contains("License: "))
                        {
                            result.License = item.Replace("License: ", "").Trim();
                        }

                        if (item.Contains("Location: "))
                        {
                            result.Location = item.Replace("Location: ", "").Trim();
                        }

                        if (item.Contains("Requires: "))
                        {
                            result.Requires = item.Replace("Requires: ", "").Trim();
                        }

                        if (item.Contains("Required-by: "))
                        {
                            result.RequiredBy = item.Replace("Required-by: ", "").Trim();
                        }

                        if (item.Contains("Home-page: "))
                        {
                            result.HomePage = item.Replace("Home-page: ", "").Trim();
                        }
                    }
                }
            }
            else
            {
                throw new Exception("Python version not supported: " + pyversion);
            }
            return result;
        }
        private static int CheckPythonVer()
        {
            try
            {
                string result = RunCommand(null, "python", "--version");
                string[] versionSplit = result.Replace("Python", "").Trim().Split('.');

                var PYTHON_PATH = PythonUtil.Setup.GetEnv("PYTHON_PATH");
                if (string.IsNullOrEmpty(PYTHON_PATH))
                {
                    var filepath = CommandLinePathResolver.TryGetFullPathForCommand("python");
                    var path = System.IO.Path.GetDirectoryName(filepath);
                    SetPythonPath(path, true);
                }

                return Convert.ToInt32(versionSplit[0] + versionSplit[1]);
            }
            catch
            {
                try
                {
                    string result = RunCommand(null, "python3", "--version");
                    string[] versionSplit = result.Replace("Python", "").Trim().Split('.');

                    var PYTHON_PATH = PythonUtil.Setup.GetEnv("PYTHON_PATH");
                    if (string.IsNullOrEmpty(PYTHON_PATH))
                    {
                        var filepath = CommandLinePathResolver.TryGetFullPathForCommand("python3");
                        var path = System.IO.Path.GetDirectoryName(filepath);
                        SetPythonPath(path, true);
                    }


                    //pythonCommand = "python3";
                    //pipCommand = "pip3";
                    return Convert.ToInt32(versionSplit[0] + versionSplit[1]);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }
        public static string GetExePath(string exec)
        {
            var path = "";
            var PYTHON_PATH = GetEnv("PYTHON_PATH");
            var PYTHON_HOME = GetEnv("PYTHON_HOME");
            if (!exec.EndsWith(".exe") && !exec.EndsWith(".bat")) exec = exec + ".exe";
            if (!string.IsNullOrEmpty(PluginConfig.python_exe_path) && System.IO.File.Exists(System.IO.Path.Combine(PluginConfig.python_exe_path, exec)))
            {
                path = PluginConfig.python_exe_path;
            }
            if (!string.IsNullOrEmpty(PluginConfig.python_exe_path) && System.IO.File.Exists(System.IO.Path.Combine(PluginConfig.python_exe_path, "Scripts", exec)))
            {
                path = System.IO.Path.Combine(PluginConfig.python_exe_path, "Scripts");
            }
            else if (!string.IsNullOrEmpty(PYTHON_PATH) && System.IO.File.Exists(System.IO.Path.Combine(PYTHON_PATH, exec)))
            {
                path = PYTHON_PATH;
            }
            else if (!string.IsNullOrEmpty(PYTHON_PATH) && System.IO.File.Exists(System.IO.Path.Combine(PYTHON_PATH, "Scripts", exec)))
            {
                path = System.IO.Path.Combine(PYTHON_PATH, "Scripts");
            }
            else if (!string.IsNullOrEmpty(PYTHON_HOME) && System.IO.File.Exists(System.IO.Path.Combine(PYTHON_HOME, exec)))
            {
                path = PYTHON_HOME;
            }
            else if (!string.IsNullOrEmpty(PYTHON_HOME) && System.IO.File.Exists(System.IO.Path.Combine(PYTHON_HOME, "Scripts", exec)))
            {
                path = System.IO.Path.Combine(PYTHON_HOME, "Scripts");
            }
            else
            {
                var temppath = CommandLinePathResolver.TryGetFullPathForCommand(exec);
                if (!string.IsNullOrEmpty(temppath)) path = System.IO.Path.GetDirectoryName(temppath);
            }
            return path;
        }
        public static string RunCommand(string path, string exec, string arguments)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    string _temppath = GetExePath(exec);
                    if (!string.IsNullOrEmpty(_temppath)) path = _temppath;
                }
                var filepath = exec;
                if (!string.IsNullOrEmpty(path)) filepath = System.IO.Path.Combine(path, exec);
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                if (PluginConfig.py_create_no_window)
                {
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.RedirectStandardOutput = true;
                    startInfo.RedirectStandardError = true;
                    startInfo.CreateNoWindow = PluginConfig.py_create_no_window;
                }
                startInfo.FileName = filepath;
                startInfo.Arguments = arguments;
                startInfo.UseShellExecute = false;
                startInfo.WorkingDirectory = path;
                process.StartInfo = startInfo;
                process.Start();
                if (PluginConfig.py_create_no_window)
                {
                    string error = process.StandardError.ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(error) && !error.Contains("WARNING"))
                        throw new Exception(error);

                    return process.StandardOutput.ReadToEnd();
                }
                else
                {
                    process.WaitForExit();
                    return "";
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex.ToString());
                throw;
            }
        }
        public static string RunCommand2(string path, string command)
        {
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                if (PluginConfig.py_create_no_window)
                {
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.RedirectStandardOutput = true;
                    startInfo.RedirectStandardError = true;
                    startInfo.CreateNoWindow = PluginConfig.py_create_no_window;
                }
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/c " + command + " 2>&1";
                startInfo.UseShellExecute = false;
                startInfo.WorkingDirectory = path;
                process.StartInfo = startInfo;
                process.Start();
                if (PluginConfig.py_create_no_window)
                {
                    string error = process.StandardError.ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(error))
                        throw new Exception(error);

                    return process.StandardOutput.ReadToEnd();
                }
                else
                {
                    process.WaitForExit();
                    return "";
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex.ToString());
                throw;
            }
        }
        private static string originalPath = null;
        public static void SetPythonPath(string path, bool init)
        {
            SetEnv("PYTHON_PATH", path);
            SetEnv("PYTHON_HOME", path);
            if (string.IsNullOrEmpty(originalPath))
            {
                originalPath = GetEnv("PATH");
            }
            else { SetEnv("PATH", originalPath); }
            AddToPath(path);
            AddToPath(System.IO.Path.Combine(path, "Scripts"));
            try
            {
                Python.Runtime.PythonEngine.PythonHome = path;
                if (init) Python.Runtime.PythonEngine.Initialize();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}
