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

namespace OpenRPA.SAP
{
    [System.ComponentModel.Designer(typeof(GetElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetElement), "Resources.toolbox.SAP_logo_small2.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    [LocalizedToolboxTooltip("activity_getelement_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_getelement", typeof(Resources.strings))]
    public class GetElement : BreakableLoop, System.Activities.Presentation.IActivityTemplateFactory
    {
        public GetElement()
        {
            MaxResults = 1;
            MinResults = 1;
            Timeout = new InArgument<TimeSpan>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<TimeSpan>("TimeSpan.FromSeconds(3)")
            };
        }
        [System.ComponentModel.Browsable(false)]
        public ActivityAction<SAPElement> Body { get; set; }
        public InArgument<int> MaxResults { get; set; }
        public InArgument<int> MinResults { get; set; }
        public InArgument<TimeSpan> Timeout { get; set; }
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }
        [Browsable(false)]
        public InArgument<SAPElement> From { get; set; }
        public InArgument<bool> FlatternGuiTree { get; set; }
        public OutArgument<SAPElement[]> Elements { get; set; }
        [Browsable(false)]
        public string Image { get; set; }
        private Variable<IEnumerator<SAPElement>> _elements = new Variable<IEnumerator<SAPElement>>("_elements");
        public Activity LoopAction { get; set; }
        protected override void StartLoop(NativeActivityContext context)
        {
            var SelectorString = Selector.Get(context);
            SelectorString = OpenRPA.Interfaces.Selector.Selector.ReplaceVariables(SelectorString, context.DataContext);
            var sel = new SAPSelector(SelectorString);
            var timeout = Timeout.Get(context);
            var maxresults = MaxResults.Get(context);
            var minresults = MinResults.Get(context);
            var flatternguitree = FlatternGuiTree.Get(context);
            //var from = From.Get(context);
            SAPElement from = null;
            if (maxresults < 1) maxresults = 1;

            SAPElement[] elements = { };
            var sw = new Stopwatch();
            sw.Start();
            do
            {
                var selector = new SAPSelector(SelectorString);
                elements = SAPSelector.GetElementsWithuiSelector(selector, from, 0, maxresults, flatternguitree);

            } while (elements.Count() == 0 && sw.Elapsed < timeout);
            Log.Debug(string.Format("OpenRPA.SAP::GetElement::found {1} elements in {0:mm\\:ss\\.fff}", sw.Elapsed, elements.Count()));
            if (elements.Count() > maxresults) elements = elements.Take(maxresults).ToArray();
            if (elements.Count() < minresults)
            {
                Log.Selector(string.Format("Windows.GetElement::Failed locating " + minresults + " item(s) {0:mm\\:ss\\.fff}", sw.Elapsed));
                throw new ElementNotFoundException("Failed locating " + minresults + " item(s)");
            }
            context.SetValue(Elements, elements);
            IEnumerator<SAPElement> _enum = elements.ToList().GetEnumerator();
            context.SetValue(_elements, _enum);
            bool more = _enum.MoveNext();
            if (more)
            {
                IncIndex(context);
                SetTotal(context, elements.Length);
                context.ScheduleAction(Body, _enum.Current, OnBodyComplete);
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<SAPElement> _enum = _elements.Get(context);
            bool more = _enum.MoveNext();
            if (more && !breakRequested)
            {
                IncIndex(context);
                context.ScheduleAction<SAPElement>(Body, _enum.Current, OnBodyComplete);
            }
            else
            {
                if (LoopAction != null && !breakRequested)
                {
                    context.ScheduleActivity(LoopAction, LoopActionComplete);
                }
            }
        }
        private void LoopActionComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            if (!breakRequested) StartLoop(context);
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            Interfaces.Extensions.AddCacheArgument(metadata, "Selector", Selector);
            //Interfaces.Extensions.AddCacheArgument(metadata, "From", From);
            Interfaces.Extensions.AddCacheArgument(metadata, "FlatternGuiTree", FlatternGuiTree);
            Interfaces.Extensions.AddCacheArgument(metadata, "Elements", Elements);
            Interfaces.Extensions.AddCacheArgument(metadata, "MaxResults", MaxResults);
            metadata.AddImplementationVariable(_elements);
            base.CacheMetadata(metadata);
            AddIndexTotal(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            var fef = new GetElement();
            var aa = new ActivityAction<SAPElement>();
            var da = new DelegateInArgument<SAPElement>();
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