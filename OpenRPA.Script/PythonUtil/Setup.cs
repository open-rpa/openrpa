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
        private static string pipCommand = "pip";
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
            // cd C:\Users\Allan\AppData\Local\python-3.7.3-embed-amd64\Lib && curl https://bootstrap.pypa.io/get-pip.py -o get-pip.py

        }
        public static void AddToPath(string path)
        {
            var name = "PATH";
            var scope = EnvironmentVariableTarget.Process;
            var Value = Environment.GetEnvironmentVariable(name, scope);
            if(!Value.Contains(path))
            {
                Value += ";" + path;
                Environment.SetEnvironmentVariable(name, Value, scope);
            }
        }
        public static void Run(string[] modules = null)
        {
            if (modules==null) modules = new string[] { "numpy" };
            int pyversion = CheckPythonVer();
            if (pyversion == 0)
                throw new Exception("Python 3.6 not found! Please download and install from https://www.python.org/downloads/release/python-368/");

            if (pyversion == 36 || pyversion == 37)
            {
                foreach (var item in modules)
                {
                    InstallModule(item);
                }
            }
            else
            {
                throw new Exception("Version not supported: " + pyversion);
            }
        }
        public static void InstallModule(string name)
        {
            Log.Debug("using existing python, check if '" + name + "' is installed");
            if (CheckModule(name) == null)
            {
                Log.Information("Installing python module '" + name + "'");
                Console.WriteLine("Installing {0}.....", name);
                string result = RunCommand(null, pipCommand, string.Format("install {0}", name));
                Console.Write("Done!");
            }
        }
        public static ModuleInfo CheckModule(string name)
        {
            int pyversion = CheckPythonVer();
            if (pyversion == 0)
                throw new Exception("Python 3.6 not found");
            ModuleInfo result = null;
            if (pyversion == 36 || pyversion == 37)
            {
                string info = RunCommand(null, pipCommand, string.Format("show {0}", name));
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
                throw new Exception("Version not supported: " + pyversion);
            }
            return result;
        }
        private static int CheckPythonVer()
        {
            try
            {
                string result = RunCommand(null, "python", "--version");
                string[] versionSplit = result.Replace("Python", "").Trim().Split('.');

                var filepath = CommandLinePathResolver.TryGetFullPathForCommand("python.exe");
                var path = System.IO.Path.GetDirectoryName(filepath);
                SetPythonPath(path);

                return Convert.ToInt32(versionSplit[0] + versionSplit[1]);
            }
            catch
            {
                try
                {
                    string result = RunCommand(null, "python3", "--version");
                    string[] versionSplit = result.Replace("Python", "").Trim().Split('.');

                    var filepath = CommandLinePathResolver.TryGetFullPathForCommand("python3.exe");
                    var path = System.IO.Path.GetDirectoryName(filepath);
                    SetPythonPath(path);

                    //pythonCommand = "python3";
                    pipCommand = "pip3";
                    return Convert.ToInt32(versionSplit[0] + versionSplit[1]);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }
        public static string RunCommand(string path, string exec, string arguments)
        {
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                if(PluginConfig.py_create_no_window)
                {
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.RedirectStandardOutput = true;
                    startInfo.RedirectStandardError = true;
                    startInfo.CreateNoWindow = PluginConfig.py_create_no_window;
                }
                startInfo.FileName = exec;
                startInfo.Arguments = arguments;
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
                } else
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
        public static void SetPythonPath(string path)
        {
            Environment.SetEnvironmentVariable("PYTHON_PATH", path);
            Environment.SetEnvironmentVariable("PYTHON_HOME", path);
            PythonUtil.Setup.AddToPath(path);
            PythonUtil.Setup.AddToPath(System.IO.Path.Combine(path, "Scripts"));
            Python.Runtime.PythonEngine.PythonHome = path;
            Python.Runtime.PythonEngine.Initialize();
        }
    }
}
