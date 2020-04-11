using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.IE
{
    [System.ComponentModel.Designer(typeof(GetElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetElement), "Resources.toolbox.gethtmlelement.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    [LocalizedToolboxTooltip("activity_getelement_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_getelement", typeof(Resources.strings))]
    public class GetElement : NativeActivity, System.Activities.Presentation.IActivityTemplateFactory
    {
        //[RequiredArgument]
        //public InArgument<string> XPath { get; set; }

        [Browsable(false)]
        public ActivityAction<IEElement> Body { get; set; }
        public InArgument<TimeSpan> Timeout { get; set; }
        public InArgument<int> MaxResults { get; set; }
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }
        public InArgument<IEElement> From { get; set; }
        public OutArgument<IEElement[]> Elements { get; set; }
        [LocalizedDisplayName("activity_waitforready", typeof(Resources.strings)), LocalizedDescription("activity_waitforready_help", typeof(Resources.strings))]
        public InArgument<bool> WaitForReady { get; set; }
        [Browsable(false)]
        public string Image { get; set; }
        private Variable<IEnumerator<IEElement>> _elements = new Variable<IEnumerator<IEElement>>("_elements");
        public Activity LoopAction { get; set; }
        public GetElement()
        {
            MaxResults = 1;
            Timeout = new InArgument<TimeSpan>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<TimeSpan>("TimeSpan.FromSeconds(3)")
            };
        }
        protected override void Execute(NativeActivityContext context)
        {
            var selector = Selector.Get(context);
            selector = OpenRPA.Interfaces.Selector.Selector.ReplaceVariables(selector, context.DataContext);
            var sel = new IESelector(selector);
            var timeout = Timeout.Get(context);
            var from = From.Get(context);
            var maxresults = MaxResults.Get(context);
            if (maxresults < 1) maxresults = 1;
            IEElement[] elements = { };

            if (WaitForReady.Get(context))
            {
                var browser = Browser.GetBrowser();
                MSHTML.HTMLDocument doc = browser.Document;
                var sw2 = new Stopwatch();
                sw2.Start();
                while (sw2.Elapsed < timeout && doc.readyState != "complete" && doc.readyState != "interactive")
                {
                    // Log.Debug("pending complete, readyState: " + doc.readyState);
                    System.Threading.Thread.Sleep(100);
                }

            }

            var sw = new Stopwatch();
            sw.Start();
            do
            {
                elements = IESelector.GetElementsWithuiSelector(sel, from);
            } while (elements .Count() == 0 && sw.Elapsed < timeout);
            if (elements.Count() > maxresults) elements = elements.Take(maxresults).ToArray();  
            context.SetValue(Elements, elements);
            IEnumerator<IEElement> _enum = elements.ToList().GetEnumerator();
            context.SetValue(_elements, _enum);
            bool more = _enum.MoveNext();
            if (more)
            {
                context.ScheduleAction(Body, _enum.Current, OnBodyComplete);
            }
            else
            {
                throw new Interfaces.ElementNotFoundException("Failed locating item");
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<IEElement> _enum = _elements.Get(context);
            bool more = _enum.MoveNext();
            if (more)
            {
                context.ScheduleAction<IEElement>(Body, _enum.Current, OnBodyComplete);
            }
            else
            {
                if (LoopAction != null)
                {
                    context.ScheduleActivity(LoopAction, LoopActionComplete);
                }
            }
        }
        private void LoopActionComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            Execute(context);
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            Interfaces.Extensions.AddCacheArgument(metadata, "Selector", Selector);
            Interfaces.Extensions.AddCacheArgument(metadata, "From", From);
            Interfaces.Extensions.AddCacheArgument(metadata, "Elements", Elements);
            Interfaces.Extensions.AddCacheArgument(metadata, "MaxResults", MaxResults);
            metadata.AddImplementationVariable(_elements);
            base.CacheMetadata(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            var fef = new GetElement();
            var aa = new ActivityAction<IEElement>();
            var da = new DelegateInArgument<IEElement>();
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