using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

namespace OpenRPA.Script.Activities
{
    public class Error
    {
        public ScriptSource source { get; set; }
        public string message { get; set; }
        public SourceSpan span { get; set; }
        public int errorCode { get; set; }
        public Severity severity { get; set; }
    }
    public class ErrorListener : Microsoft.Scripting.Hosting.ErrorListener
    {
        public List<Error> errors { get; set; } = new List<Error>();
        public override void ErrorReported(ScriptSource source, string message, SourceSpan span, int errorCode, Severity severity)
        {
            errors.Add(new Error { source = source, message = message, span = span, errorCode = errorCode, severity = severity });
        }
    }
}
