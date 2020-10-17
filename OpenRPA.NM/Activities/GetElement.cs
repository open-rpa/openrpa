using FlaUI.Core.AutomationElements;
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

namespace OpenRPA.NM
{
    [System.ComponentModel.Designer(typeof(GetElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetElement), "Resources.toolbox.gethtmlelement.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    [LocalizedToolboxTooltip("activity_getelement_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_getelement", typeof(Resources.strings))]
    public class GetElement : AsyncTaskNativeActivity, System.Activities.Presentation.IActivityTemplateFactory
    {
        [System.ComponentModel.Browsable(false)]
        public ActivityAction<NMElement> Body { get; set; }
        public InArgument<int> MaxResults { get; set; }
        public InArgument<int> MinResults { get; set; }
        public InArgument<TimeSpan> Timeout { get; set; }
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }
        public InArgument<NMElement> From { get; set; }
        public OutArgument<NMElement[]> Elements { get; set; }
        [LocalizedDisplayName("activity_waitforready", typeof(Resources.strings)), LocalizedDescription("activity_waitforready_help", typeof(Resources.strings))]
        public InArgument<bool> WaitForReady { get; set; }
        [Browsable(false)]
        public string Image { get; set; }
        private readonly Variable<IEnumerator<NMElement>> _elements = new Variable<IEnumerator<NMElement>>("_elements");
        private Variable<NMElement[]> _allelements = new Variable<NMElement[]>("_allelements");
        [System.ComponentModel.Browsable(false)]
        public Activity LoopAction { get; set; }
        public GetElement()
        {
            MaxResults = 1;
            MinResults = 1;
            Timeout = new InArgument<TimeSpan>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<TimeSpan>("TimeSpan.FromSeconds(3)")
            };
        }
        private void DoWaitForReady(string browser)
        {
            if (!string.IsNullOrEmpty(browser))
            {
                NMHook.enumtabs();
                if (browser == "chrome" && NMHook.CurrentChromeTab != null)
                {
                    NMHook.WaitForTab(NMHook.CurrentChromeTab.id, browser, TimeSpan.FromSeconds(10));
                }
                if (browser == "ff" && NMHook.CurrentFFTab != null)
                {
                    NMHook.WaitForTab(NMHook.CurrentFFTab.id, browser, TimeSpan.FromSeconds(10));
                }
            }

        }
        private bool IsCancel = false;
        protected override async Task<object> ExecuteAsync(NativeActivityContext context)
        {
            IsCancel = false;
            var selector = Selector.Get(context);
            selector = OpenRPA.Interfaces.Selector.Selector.ReplaceVariables(selector, context.DataContext);
            var sel = new NMSelector(selector);
            var timeout = Timeout.Get(context);
            var from = From?.Get(context);
            var maxresults = MaxResults.Get(context);
            var minresults = MinResults.Get(context);
            if (maxresults < 1) maxresults = 1;
            NMElement[] elements = { };
            string browser = sel.browser;
            if (WaitForReady != null && WaitForReady.Get(context))
            {
                if (from != null) browser = from.browser;
                DoWaitForReady(browser);
            }
            var allelements = context.GetValue(_allelements);
            if (allelements == null) allelements = new NMElement[] { };

            var s = new NMSelectorItem(sel[0]);
            if (!string.IsNullOrEmpty(s.url))
            {
                var tab = NMHook.tabs.Where(x => x.browser == browser && x.url.ToLower().StartsWith(s.url.ToLower())).FirstOrDefault();
                if (tab != null)
                {
                    if (!tab.highlighted || !tab.selected)
                    {
                        var _tab = NMHook.selecttab(browser, tab.id);
                    }
                }
            }
            await Task.Run(() =>
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                do
                {
                    elements = NMSelector.GetElementsWithuiSelector(sel, from, maxresults);
                    Log.Selector("BEGIN:: I have " + elements.Count() + " elements, and " + allelements.Count() + " in all elements");
                    if (allelements.Length > 0)
                    {
                        var newelements = new List<NMElement>();
                        for (var i = elements.Length - 1; i >= 0; i--)
                        {
                            var element = elements[i];
                            if (!allelements.Contains(element)) newelements.Insert(0, element);
                        }
                        elements = newelements.ToArray();
                    }

                } while (elements.Count() == 0 && sw.Elapsed < timeout && !IsCancel);
                if (IsCancel)
                {
                    Console.WriteLine("was Canceled: true! DisplayName" + DisplayName);
                }
                if (sw.Elapsed >= timeout)
                {
                    Console.WriteLine("Timeout !");
                }
            });
            return elements;
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<NMElement> _enum = _elements.Get(context);
            if (_enum == null) return;
            bool more = _enum.MoveNext();
            if (more)
            {
                context.ScheduleAction<NMElement>(Body, _enum.Current, OnBodyComplete);
            }
            else
            {
                if (LoopAction != null)
                {
                    var allelements = context.GetValue(_allelements);
                    context.ScheduleActivity(LoopAction, LoopActionComplete);
                }
            }
        }
        private void LoopActionComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            var allelements = context.GetValue(_allelements);
            var selector = Selector.Get(context);
            selector = OpenRPA.Interfaces.Selector.Selector.ReplaceVariables(selector, context.DataContext);
            var sel = new NMSelector(selector);
            var from = From?.Get(context);
            string browser = sel.browser;
            if (from != null) browser = from.browser;
            System.Threading.Thread.Sleep(500);
            DoWaitForReady(browser);
            Execute(context);

        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            metadata.AddChild(LoopAction);
            Interfaces.Extensions.AddCacheArgument(metadata, "MaxResults", MaxResults);
            Interfaces.Extensions.AddCacheArgument(metadata, "MinResults", MinResults);
            Interfaces.Extensions.AddCacheArgument(metadata, "Timeout", Timeout);

            Interfaces.Extensions.AddCacheArgument(metadata, "Selector", Selector);
            Interfaces.Extensions.AddCacheArgument(metadata, "From", From);
            Interfaces.Extensions.AddCacheArgument(metadata, "Elements", Elements);
            Interfaces.Extensions.AddCacheArgument(metadata, "WaitForReady", WaitForReady);

            metadata.AddImplementationVariable(_elements);
            metadata.AddImplementationVariable(_allelements);
            base.CacheMetadata(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            var fef = new GetElement();
            var aa = new ActivityAction<NMElement>();
            var da = new DelegateInArgument<NMElement>();
            da.Name = "item";
            fef.Body = aa;
            aa.Argument = da;
            return fef;
        }
        protected override void AfterExecute(NativeActivityContext context, object result)
        {
            var allelements = context.GetValue(_allelements);
            if (allelements == null) allelements = new NMElement[] { };
            var maxresults = MaxResults.Get(context);
            var minresults = MinResults.Get(context);
            NMElement[] elements = result as NMElement[];
            if (elements.Count() > maxresults) elements = elements.Take(maxresults).ToArray();

            if ((elements.Length + allelements.Length) < minresults)
            {
                Log.Selector(string.Format("Windows.GetElement::Failed locating " + minresults + " item(s)"));
                throw new ElementNotFoundException("Failed locating " + minresults + " item(s)");
            }
            IEnumerator<NMElement> _enum = elements.ToList().GetEnumerator();
            bool more = _enum.MoveNext();
            if (more)
            {
                allelements = allelements.Concat(elements).ToArray();
                var eq = new Activities.NMEqualityComparer();
                allelements = allelements.Distinct(eq).ToArray();
            }
            context.SetValue(_allelements, allelements);
            context.SetValue(Elements, allelements);
            Log.Selector("END:: I have " + elements.Count() + " elements, and " + allelements.Count() + " in all elements");
            if (more)
            {
                context.SetValue(_elements, _enum);
                context.ScheduleAction(Body, _enum.Current, OnBodyComplete);
            }

        }
        protected override void Cancel(NativeActivityContext context)
        {
            IsCancel = true;
            Console.WriteLine("Cancel " + DisplayName);
            base.Cancel(context);
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