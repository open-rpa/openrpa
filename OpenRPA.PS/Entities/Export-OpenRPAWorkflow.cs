using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRPA.Interfaces;
using System.Management.Automation;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Activities.Presentation;
using System.Activities.Core.Presentation;
using System.Activities.Presentation.Services;
using System.Activities.Presentation.Model;
using System.Activities;
using System.Activities.Statements;

namespace OpenRPA.PS
{
    [Cmdlet("Export", "OpenRPAWorkflow")]
    public class ExportOpenRPAWorkflow : OpenRPACmdlet
    {
        [Parameter(ValueFromPipeline = true, Position = 0, Mandatory = true, ParameterSetName = "withObject")] public List<object> Objects { get; set; }
        [Parameter(ValueFromPipeline = false, Position = 1, Mandatory = false, ParameterSetName = "withObject")] public string Folder { get; set; }
        [Parameter()] public SwitchParameter Force { get; set; }
        protected override async Task ProcessRecordAsync()
        {
            if (string.IsNullOrEmpty(Folder)) Folder = GetVariableValue("pwd").ToString();
            if (!System.IO.Directory.Exists(Folder))
            {
                WriteError(new ErrorRecord(new System.IO.DirectoryNotFoundException(Folder + " not found"), "", ErrorCategory.NotSpecified, null));
                return;
            }
            try
            {
                foreach (PSObject obj in Objects)
                {
                    if (obj.Properties["Xaml"] != null && obj.Properties["Xaml"].Value != null)
                    {
                        string Xaml = obj.Properties["Xaml"].Value.ToString();
                        string Filename = obj.Properties["Filename"].Value.ToString();
                        string xaml = await LoadImages(Xaml);
                        string filepath = System.IO.Path.Combine(Folder, Filename);
                        if (!System.IO.Directory.Exists(filepath) && !Force.IsPresent)
                        {
                            WriteError(new ErrorRecord(new System.IO.IOException(filepath + " allready exists"), "", ErrorCategory.NotSpecified, null));
                            return;
                        }
                        System.IO.File.WriteAllText(filepath, xaml);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "", ErrorCategory.NotSpecified, null));
            }
        }
        public async Task<string> LoadImages(string xaml)
        {
            var doc = new System.Xml.XmlDocument(); bool foundone = false;
            doc.LoadXml(xaml);
            var nodes = doc.SelectNodes("//*[@Image]");
            foreach (System.Xml.XmlNode n in nodes)
            {
                var image = n.Attributes["Image"].Value;
                Console.WriteLine("Image: " + image);
                if (System.Text.RegularExpressions.Regex.Match(image, "[a-f0-9]{24}").Success)
                {
                    WriteVerbose("Loading image id " + image);
                    using (var b = await Interfaces.Image.Util.LoadBitmap(image))
                    {
                        image = Interfaces.Image.Util.Bitmap2Base64(b);
                    }
                    foundone = true;
                    n.Attributes["Image"].Value = image;
                }
            }
            if (foundone) return doc.OuterXml;
            return xaml;
        }
    }
}
