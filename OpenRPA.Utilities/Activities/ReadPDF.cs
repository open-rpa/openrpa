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

namespace OpenRPA.Utilities
{
    [Designer(typeof(ReadPDFDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.readpdf.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class ReadPDF : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Filename { get; set; }
        public OutArgument<iTextSharp.text.pdf.PdfReader> Result { get; set; }
        public OutArgument<string> AllText { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var filename = Filename.Get(context);
            filename = Environment.ExpandEnvironmentVariables(filename);
            iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(filename);
            context.SetValue(AllText, reader);
            var result = GetTextFromAllPages(reader);
            context.SetValue(AllText, result);
        }
        public static string GetTextFromAllPages(iTextSharp.text.pdf.PdfReader reader)
        {
            var output = new System.IO.StringWriter();
            for (int i = 1; i <= reader.NumberOfPages; i++)
                output.WriteLine(iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(reader, i, new iTextSharp.text.pdf.parser.SimpleTextExtractionStrategy()));
            return output.ToString();
        }
    }
}