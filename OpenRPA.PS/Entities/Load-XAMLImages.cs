using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRPA.Interfaces;
using System.Management.Automation;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Dynamic;
using Newtonsoft.Json;

using System.Activities;
using System.Activities.Core.Presentation;
using System.Activities.Debugger;
using System.Activities.Presentation;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.Services;
using System.Activities.Presentation.Toolbox;
using System.Activities.Presentation.View;
using System.Activities.Statements;

namespace OpenRPA.PS
{
    [Cmdlet("Load", "XAMLImages")]
    public class LoadXAMLImages : OpenRPACmdlet
    {
        [Parameter(ValueFromPipeline = true, Position = 0, Mandatory = true, ParameterSetName = "withObject")] public List<object> Objects { get; set; }
        protected override async Task ProcessRecordAsync()
        {
            var results = new List<PSObject>();
            try
            {
                foreach (PSObject obj in Objects)
                {
                    if (obj.Properties["Xaml"] != null && obj.Properties["Xaml"].Value != null)
                    {
                        string Xaml = obj.Properties["Xaml"].Value.ToString();
                        string Filename = obj.Properties["Filename"].Value.ToString();
                        string Collection = obj.Properties["__pscollection"].Value.ToString();
                        string xaml = await LoadImages(Xaml);
                        var json = obj.toJson();
                        var entity = JObject.Parse(json);
                        entity["Xaml"] = xaml;
                        results.Add(entity.toPSObjectWithTypeName(Collection));
                    }
                }
                WriteObject(results, false);
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
