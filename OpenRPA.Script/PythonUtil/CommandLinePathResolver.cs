using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Script.PythonUtil
{
    internal static class CommandLinePathResolver
    {
        private const int MAX_PATH = 260;
        private static Lazy<Dictionary<string, string>> appPaths = new Lazy<Dictionary<string, string>>(LoadAppPaths);
        private static Lazy<string[]> executableExtensions = new Lazy<string[]>(LoadExecutableExtensions);
        public static string TryGetFullPathForCommand(string command)
        {
            if (Path.HasExtension(command))
                return TryGetFullPathForFileName(command);

            return TryGetFullPathByProbingExtensions(command);
        }
        private static string[] LoadExecutableExtensions() => Environment.GetEnvironmentVariable("PATHEXT").Split(';');
        private static Dictionary<string, string> LoadAppPaths()
        {
            var appPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths"))
            foreach (var subkeyName in key.GetSubKeyNames())
            {
                    using (var subkey = key.OpenSubKey(subkeyName))
                        appPaths.Add(subkeyName, subkey.GetValue(string.Empty)?.ToString());
            }
            return appPaths;
        }
        private static string TryGetFullPathByProbingExtensions(string command)
        {
            foreach (var extension in executableExtensions.Value)
            {
                var result = TryGetFullPathForFileName(command + extension);
                if (result != null)
                    return result;
            }

            return null;
        }
        private static string TryGetFullPathForFileName(string fileName) =>
            TryGetFullPathFromPathEnvironmentVariable(fileName) ?? TryGetFullPathFromAppPaths(fileName);
        private static string TryGetFullPathFromAppPaths(string fileName) =>
            appPaths.Value.TryGetValue(fileName, out var path) ? path : null;
        private static string TryGetFullPathFromPathEnvironmentVariable(string fileName)
        {
            if (fileName.Length >= MAX_PATH)
                throw new ArgumentException($"The executable name '{fileName}' must have less than {MAX_PATH} characters.", nameof(fileName));

            var sb = new StringBuilder(fileName, MAX_PATH);
            return PathFindOnPath(sb, null) ? sb.ToString() : null;
        }
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        private static extern bool PathFindOnPath([In, Out] StringBuilder pszFile, [In] string[] ppszOtherDirs);
    }
}
