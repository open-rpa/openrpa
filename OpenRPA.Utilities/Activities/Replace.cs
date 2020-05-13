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
using Microsoft.VisualBasic.FileIO;
using System.Text.RegularExpressions;
using System.Windows;

namespace OpenRPA.Utilities
{
    [Designer(typeof(ReplaceDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.regex.png")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class Replace : CodeActivity
    {
        [RequiredArgument]
        public InArgument<string> Input { get; set; }
        [RequiredArgument]
        public InArgument<string> Pattern { get; set; }
        [RequiredArgument]
        public InArgument<string> Replacement { get; set; }

        [Category("RegexOptions"), Description("Specifies that the regular expression is compiled to an assembly. This yields faster execution but increases startup time.")]
        public bool Compiled { get; set; } = true;
        [Category("RegexOptions"), Description("Specifies that cultural differences in language is ignored. ")]
        public bool CultureInvariant { get; set; }
        [Category("RegexOptions"), Description("Enables ECMAScript-compliant behavior for the expression. This value can be used only in conjunction with the IgnoreCase, Multiline, and Compiled values.")]
        public bool ECMAScript { get; set; }
        [Category("RegexOptions"), Description("Specifies that the only valid captures are explicitly named or numbered groups of the form (?<name>...). This allows unnamed parentheses to act as noncapturing groups without the syntactic clumsiness of the expression (?:...).")]
        public bool ExplicitCapture { get; set; }
        [Category("RegexOptions"), Description("Specifies case-insensitive matching. ")]
        public bool IgnoreCase { get; set; } = true;
        [Category("RegexOptions"), Description("Eliminates unescaped white space from the pattern and enables comments marked with #. However, this value does not affect or eliminate white space in character classes, numeric quantifiers, or tokens that mark the beginning of individual regular expression language elements.")]
        public bool IgnorePatternWhitespace { get; set; }
        [Category("RegexOptions"), Description("Multiline mode. Changes the meaning of ^ and $ so they match at the beginning and end, respectively, of any line, and not just the beginning and end of the entire string.")]
        public bool Multiline { get; set; }
        [Category("RegexOptions"), Description("Specifies that the search will be from right to left instead of from left to right. ")]
        public bool RightToLeft { get; set; }
        [Category("RegexOptions"), Description("Specifies single-line mode. Changes the meaning of the dot (.) so it matches every character (instead of every character except \n).")]
        public bool Singleline { get; set; }
        public OutArgument<string> Result { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            var options = RegexOptions.None;
            if (Compiled) options = options | RegexOptions.Compiled;
            if (CultureInvariant) options = options | RegexOptions.CultureInvariant;
            if (ECMAScript) options = options | RegexOptions.ECMAScript;
            if (ExplicitCapture) options = options | RegexOptions.ExplicitCapture;
            if (IgnoreCase) options = options | RegexOptions.IgnoreCase;
            if (IgnorePatternWhitespace) options = options | RegexOptions.IgnorePatternWhitespace;
            if (Multiline) options = options | RegexOptions.Multiline;
            if (RightToLeft) options = options | RegexOptions.RightToLeft;
            if (Singleline) options = options | RegexOptions.Singleline;
            var result = Regex.Replace(Input.Get(context), Pattern.Get(context), Replacement.Get(context), options);
            Result.Set(context, result);
        }

    }


}