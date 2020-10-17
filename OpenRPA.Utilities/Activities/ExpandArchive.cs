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
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using ExcelDataReader;
using System.Data;
using System.IO.Compression;

namespace OpenRPA.Utilities
{
    [Designer(typeof(ExpandArchiveDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.expandarchive.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class ExpandArchive : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Path { get; set; }
        [RequiredArgument]
        public InArgument<string> Filename { get; set; }
        public InArgument<bool> Overwrite { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var overwrite = Overwrite.Get(context);
            var filename = Filename.Get(context);
            filename = Environment.ExpandEnvironmentVariables(filename);
            var path = Path.Get(context);
            path = Environment.ExpandEnvironmentVariables(path);
            if(!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(filename))
            {
                var archive = ZipFile.OpenRead(filename);
                archive.ExtractToDirectory(path, overwrite);
            }
        }

    }

    public static class ZipArchiveExtensions
    {
        public static void ExtractToDirectory(this ZipArchive archive, string destinationDirectoryName, bool overwrite)
        {
            if (!overwrite)
            {
                archive.ExtractToDirectory(destinationDirectoryName);
                return;
            }

            var di = System.IO.Directory.CreateDirectory(destinationDirectoryName);
            string destinationDirectoryFullPath = di.FullName;

            foreach (ZipArchiveEntry file in archive.Entries)
            {
                string completeFileName = System.IO.Path.GetFullPath(System.IO.Path.Combine(destinationDirectoryFullPath, file.FullName));

                if (!completeFileName.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new System.IO.IOException("Trying to extract file outside of destination directory. See this link for more info: https://snyk.io/research/zip-slip-vulnerability");
                }

                if (file.Name == "")
                {// Assuming Empty for Directory
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(completeFileName));
                    continue;
                }
                file.ExtractToFile(completeFileName, true);
            }
        }
    }
}