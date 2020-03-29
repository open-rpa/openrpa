using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Word;
using OpenRPA.Interfaces;

namespace OpenRPA.Office.Activities
{
    [System.ComponentModel.Designer(typeof(GetParagraphDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder2), "Resources.toolbox.readexcel.png")]
    [LocalizedToolboxTooltip("activity_getparagraph_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_getparagraph", typeof(Resources.strings))]
    public class GetParagraph : NativeActivity, System.Activities.Presentation.IActivityTemplateFactory
    {
        [System.ComponentModel.Category("Input")]
        public InArgument<string> Filename { get; set; }
        [System.ComponentModel.Category("Input")]
        public InArgument<int> Index { get; set; }
        [System.ComponentModel.Category("Output")]
        public OutArgument<string> Text { get; set; }
        [System.ComponentModel.Category("Output")]
        public OutArgument<int> Count { get; set; }
        public InArgument<int> MaxResults { get; set; }
        public ActivityAction<string> Body { get; set; }
        private readonly Variable<IEnumerator<string>> _elements = new Variable<IEnumerator<string>>("_elements");
        protected override void Execute(NativeActivityContext context)
        {
            var filename = Filename.Get(context);
            var maxresults = MaxResults.Get(context);
            filename = Environment.ExpandEnvironmentVariables(filename);
            Application activeObject = null;
            object missing = Type.Missing;
            Document document = null;
            try
            {
                activeObject = (Application)Marshal.GetActiveObject("Word.Application");
                foreach (Document current in activeObject.Documents)
                {
                    if (current.FullName == filename)
                    {
                        document = current;
                        break;
                    }
                }
            }
            catch
            {
                activeObject = null;
            }
            finally
            {
                if(activeObject==null) activeObject = (Application)Activator.CreateInstance(Marshal.GetTypeFromCLSID(new Guid("000209FF-0000-0000-C000-000000000046")));
                activeObject.Visible = true;
            }
            object ofilename = filename;
            if(document==null) document = activeObject.Documents.Open(ref ofilename, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing);
            var p = document.Content.Paragraphs;
            string text = "";
            int index = Index.Get(context);
            Count.Set(context, p.Count);
            var result = new List<string>();
            if(Index.Expression == null)
            {
                for(var i = 0; i < p.Count; i++)
                {
                    result.Add(p[i + 1].Range.Text);
                    text += p[i + 1].Range.Text;
                    if (maxresults > 0 && maxresults == result.Count) break;
                }
            } 
            else
            {
                if (p.Count < index) throw new Exception("filename only contains " + p.Count + " Paragraphs");
                for (var i = (index-1); i < p.Count; i++)
                {
                    result.Add(p[i + 1].Range.Text);
                    text += p[i + 1].Range.Text;
                    if (maxresults > 0 && maxresults == result.Count) break;
                }

                //text = p[index].Range.Text;
            }
            Text.Set(context, text);
            IEnumerator<string> _enum = result.ToList().GetEnumerator();
            _elements.Set(context, _enum);
            bool more = _enum.MoveNext();
            if (more)
            {
                if(Body!=null) context.ScheduleAction(Body, _enum.Current, OnBodyComplete);
            }

        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<string> _enum = _elements.Get(context);
            bool more = _enum.MoveNext();
            if (more)
            {
                context.ScheduleAction<string>(Body, _enum.Current, OnBodyComplete);
            }
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            Interfaces.Extensions.AddCacheArgument(metadata, "Filename", Filename);
            Interfaces.Extensions.AddCacheArgument(metadata, "Index", Index);
            Interfaces.Extensions.AddCacheArgument(metadata, "MaxResults", MaxResults);
            Interfaces.Extensions.AddCacheArgument(metadata, "Text", Text);
            Interfaces.Extensions.AddCacheArgument(metadata, "Count", Count);

            metadata.AddImplementationVariable(_elements);
            base.CacheMetadata(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            var fef = new GetParagraph();
            var aa = new ActivityAction<string>();
            var da = new DelegateInArgument<string>();
            da.Name = "item";
            fef.Body = aa;
            aa.Argument = da;
            return fef;
        }
        public new string DisplayName
        {
            get
            {
                var displayName = base.DisplayName;
                if (displayName == this.GetType().Name)
                {
                    var displayNameAttribute = this.GetType().GetCustomAttributes(typeof(DisplayNameAttribute), true).FirstOrDefault() as DisplayNameAttribute;
                    if (displayNameAttribute != null) displayName = displayNameAttribute.DisplayName;
                }
                return displayName;
            }
            set
            {
                base.DisplayName = value;
            }
        }
    }
}
