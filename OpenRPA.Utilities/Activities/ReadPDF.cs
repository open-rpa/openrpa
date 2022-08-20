using iTextSharp.text.pdf;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace OpenRPA.Utilities
{
    [Designer(typeof(ReadPDFDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.readpdf.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class ReadPDF : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Filename { get; set; }
        public InArgument<string> Password { get; set; }
        public OutArgument<iTextSharp.text.pdf.PdfReader> Result { get; set; }
        public OutArgument<string> AllText { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var filename = Filename.Get(context);
            filename = Environment.ExpandEnvironmentVariables(filename);
            var password = Password.Get(context);
            iTextSharp.text.pdf.PdfReader reader;
            if(!string.IsNullOrEmpty(password))
            {
                PdfReader.unethicalreading = true;
                reader = new iTextSharp.text.pdf.PdfReader(filename, System.Text.ASCIIEncoding.ASCII.GetBytes(password));
            }
            else
            {
                reader = new iTextSharp.text.pdf.PdfReader(filename);
            }
            context.SetValue(Result, reader);
            var result = GetTextFromAllPages(reader);
            context.SetValue(AllText, result);
            if (Result == null || Result.GetIsEmpty())
            {
                reader.Close();
            }
        }
        public static string GetTextFromAllPages(iTextSharp.text.pdf.PdfReader reader)
        {
            // ITextExtractionStrategy strategy = new LocationTextExtractionStrategy();
            var strategy = new iTextSharp.text.pdf.parser.LocationTextExtractionStrategy();
            var output = new System.IO.StringWriter();
            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                var text = iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(reader, i, strategy);
                text = Convert(text);
                output.WriteLine(text);
            }
            return output.ToString();
        }
        private static string Convert(string source)
        {
            string arabicWord = string.Empty;
            StringBuilder sbDestination = new StringBuilder();

            foreach (var ch in source)
            {
                if (IsArabic(ch))
                    arabicWord += ch;
                else
                {
                    if (arabicWord != string.Empty)
                        sbDestination.Append(Reverse(arabicWord));

                    sbDestination.Append(ch);
                    arabicWord = string.Empty;
                }
            }

            // if the last word was arabic    
            if (arabicWord != string.Empty)
                sbDestination.Append(Reverse(arabicWord));

            return sbDestination.ToString();
        }
        private static bool IsArabic(char character)
        {
            if (character >= 0x600 && character <= 0x6ff)
                return true;

            if (character >= 0x750 && character <= 0x77f)
                return true;

            if (character >= 0xfb50 && character <= 0xfc3f)
                return true;

            if (character >= 0xfe70 && character <= 0xfefc)
                return true;

            return false;
        }

        // Reverse the characters of string
        private static string Reverse(string source)
        {
            return new string(source.ToCharArray().Reverse().ToArray());
        }
    }
}