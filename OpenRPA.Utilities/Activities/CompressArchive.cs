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

namespace OpenRPA.Utilities
{
    [Designer(typeof(CompressArchiveDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.compressarchive.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class CompressArchive : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Path { get; set; }
        [RequiredArgument]
        public InArgument<string> Filename { get; set; }
        [Editor(typeof(CompressionLevelOptionsEditor), typeof(ExtendedPropertyValueEditor))]
        public InArgument<int> CompressionLevel { get; set; }
        public InArgument<bool> IncludeBaseDirectory { get; set; }
        public InArgument<bool> Overwrite { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var includeBaseDirectory = IncludeBaseDirectory.Get(context);
            var compressionLevel = CompressionLevel.Get(context);
            var filename = Filename.Get(context);
            filename = Environment.ExpandEnvironmentVariables(filename);
            var path = Path.Get(context);
            path = Environment.ExpandEnvironmentVariables(path);
            if(!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(filename))
            {
                if(System.IO.File.Exists(filename)) {
                    if (Overwrite.Get(context))
                    {
                        System.IO.File.Delete(filename);
                    } else
                    {
                        throw new System.IO.IOException("The file '" + filename + "' already exists.");
                    }
                }
                System.IO.Compression.ZipFile.CreateFromDirectory(path, filename, (System.IO.Compression.CompressionLevel)compressionLevel, includeBaseDirectory);
            }
        }
    }
    class CompressionLevelOptionsEditor : CustomSelectEditor
    {
        public override DataTable options
        {
            get
            {
                DataTable lst = new DataTable();
                lst.Columns.Add("ID", typeof(int));
                lst.Columns.Add("TEXT", typeof(string));
                lst.Rows.Add("0", "Optimal");
                lst.Rows.Add("1", "Fastest");
                lst.Rows.Add("2", "NoCompression");
                return lst;
            }
        }
    }

}