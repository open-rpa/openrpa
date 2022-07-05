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
    [Designer(typeof(MatchesDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.matches.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class Matches : BreakableLoop, System.Activities.Presentation.IActivityTemplateFactory
    {
        public ActivityAction<System.Text.RegularExpressions.Match> Body { get; set; }
        [RequiredArgument]
        public InArgument<string> Input { get; set; }
        [RequiredArgument]
        public InArgument<string> Pattern { get; set; }

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
        public OutArgument<System.Text.RegularExpressions.Match[]> Results { get; set; }
        private Variable<IEnumerator<System.Text.RegularExpressions.Match>> _elements = new Variable<IEnumerator<System.Text.RegularExpressions.Match>>("_elements");
        protected override void StartLoop(NativeActivityContext context)
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
            var result = new List<System.Text.RegularExpressions.Match>();
            var ma = Regex.Matches(Input.Get(context), Pattern.Get(context), options);
            foreach (System.Text.RegularExpressions.Match m in ma) result.Add(m);
            Results.Set(context, result.ToArray());
            IEnumerator<System.Text.RegularExpressions.Match> _enum = result.GetEnumerator();
            bool more = _enum.MoveNext();
            if (more)
            {
                context.SetValue(_elements, _enum);
                IncIndex(context);
                SetTotal(context, result.Count);
                context.ScheduleAction(Body, _enum.Current, OnBodyComplete);
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<System.Text.RegularExpressions.Match> _enum = _elements.Get(context);
            bool more = _enum.MoveNext();
            if (more && !breakRequested)
            {
                IncIndex(context);
                context.ScheduleAction(Body, _enum.Current, OnBodyComplete);
            }
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            Interfaces.Extensions.AddCacheArgument(metadata, "Input", Input);
            Interfaces.Extensions.AddCacheArgument(metadata, "Pattern", Pattern);

            metadata.AddImplementationVariable(_elements);
            base.CacheMetadata(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            var da = new DelegateInArgument<System.Text.RegularExpressions.Match>
            {
                Name = "item"
            };
            var wl = new System.Activities.Statements.WriteLine() { Text = new Microsoft.VisualBasic.Activities.VisualBasicValue<string>("item.Value") };
            var fef = new Matches();
            fef.Variables.Add(new Variable<int>("Index", 0));
            fef.Variables.Add(new Variable<int>("Total", 0));
            fef.Body = new ActivityAction<System.Text.RegularExpressions.Match>
            {
                Argument = da,
                Handler = (Activity)wl
            };
            return fef;
        }

    }
}