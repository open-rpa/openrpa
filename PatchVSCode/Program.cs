using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
namespace PatchVSCode
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }
        private static async Task MainAsync()
        {
            await DownloadAndCopyFramework4_0();
            await DownloadAndCopyFramework4_5();
            await DownloadAndCopyFramework4_6();
            //await DownloadAndCopyFramework4_62();
        }

        public static async Task DownloadAndCopyFramework4_62()
        {
            await DownloadAndCopyFrameworkGeneric("net462", "v4.6.2", "1.0.2");
        }
        public static async Task DownloadAndCopyFramework4_6()
        {
            await DownloadAndCopyFrameworkGeneric("net46", "v4.6", "1.0.2");
        }
        public static async Task DownloadAndCopyFramework4_5()
        {
            await DownloadAndCopyFrameworkGeneric("net45", "v4.5", "1.0.2");
        }

        public static async Task DownloadAndCopyFramework4_0()
        {
            await DownloadAndCopyFrameworkGeneric("net40", "v4.0", "1.0.2");
        }

        public static async Task DownloadAndCopyFrameworkGeneric(string netVersion, string folder, string nugetVersion)
        {
            Console.WriteLine("Downloading " + netVersion);
            var name = netVersion + "-" + DateTimeToFileString(DateTime.Now);
            var fileName = $"{name}.zip";
            var url = $"https://www.nuget.org/api/v2/package/Microsoft.NETFramework.ReferenceAssemblies.{netVersion}/{nugetVersion}";
            await DownloadFile(fileName, url);
            ZipFile.ExtractToDirectory(fileName, name);
            var from = System.IO.Path.Combine(name, @"build\.NETFramework\" + folder);
            var to = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\" + folder;
            // var to = @"C:\temp\" + folder;
            Console.WriteLine("Copy " + netVersion + " to " + to);
            FileSystem.CopyDirectory(from, to, UIOption.AllDialogs);
        }

        private static string DateTimeToFileString(DateTime d)
        {
            return d.ToString("yyyy-dd-M--HH-mm-ss");
        }

        private static async Task DownloadFile(string fileName, string url)
        {
            var uri = new Uri(url);
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(uri);
            using (var fs = new System.IO.FileStream(
                fileName,
                System.IO.FileMode.CreateNew))
            {
                await response.Content.CopyToAsync(fs);
            }
        }

    }
}
