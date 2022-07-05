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
using System.Windows.Threading;
using System.Reflection;
using FlaUI.Core.Input;

namespace OpenRPA.Windows
{
    [System.ComponentModel.Designer(typeof(GetElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetElement), "Resources.toolbox.getelement.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    [LocalizedToolboxTooltip("activity_getelement_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_getelement", typeof(Resources.strings))]
    public class GetElement : BreakableLoop, System.Activities.Presentation.IActivityTemplateFactory
    {
        public GetElement()
        {
            MaxResults = 1;
            MinResults = 1;
        }
        [Browsable(false)]
        public ActivityAction<UIElement> Body { get; set; }
        public InArgument<TimeSpan> Timeout { get; set; }
        public InArgument<int> MaxResults { get; set; }
        public InArgument<int> MinResults { get; set; }
        public InArgument<string> Selector { get; set; }
        public InArgument<IElement> From { get; set; }
        public InArgument<bool> ClearCache { get; set; }
        public InArgument<bool> Interactive { get; set; }
        public OutArgument<UIElement[]> Elements { get; set; }
        [Browsable(false)]
        public string Image { get; set; }
        private Variable<IEnumerator<UIElement>> _elements = new Variable<IEnumerator<UIElement>>("_elements");
        private Variable<UIElement[]> _lastelements = new Variable<UIElement[]>("_lastelements");
        private Variable<Stopwatch> _sw = new Variable<Stopwatch>("_sw");
        [System.ComponentModel.Browsable(false)]
        public Activity LoopAction { get; set; }
        protected override void StartLoop(NativeActivityContext context)
        {
            WindowsCacheExtension ext = context.GetExtension<WindowsCacheExtension>();
            var sw = new Stopwatch();
            sw.Start();
            Log.Selector(string.Format("Windows.GetElement::begin {0:mm\\:ss\\.fff}", sw.Elapsed));

            UIElement[] elements = null;
            var selector = Selector.Get(context);
            selector = OpenRPA.Interfaces.Selector.Selector.ReplaceVariables(selector, context.DataContext);
            var sel = new WindowsSelector(selector);
            var timeout = Timeout.Get(context);
            var maxresults = MaxResults.Get(context);
            var minresults = MinResults.Get(context);
            if (maxresults < 1) maxresults = 1;
            var interactive = Interactive.Get(context);
            var from = From.Get(context);
            int failcounter = 0;
            if (timeout.Minutes > 5 || timeout.Hours > 1)
            {
                Activity _Activity = null;
                try
                {
                    var strProperty = context.GetType().GetProperty("Activity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var strGetter = strProperty.GetGetMethod(nonPublic: true);
                    _Activity = (Activity)strGetter.Invoke(context, null);
                }
                catch (Exception)
                {
                }
                if (_Activity != null)
                {
                    Log.Warning("Timeout for Activity " + _Activity.Id + " is above 5 minutes, was this the intention ? calculated value " + timeout.ToString());
                }
                else
                {
                    Log.Warning("Timeout for on of your Windows.GetElements is above 5 minutes, was this the intention ? calculated value " + timeout.ToString());
                }
            }
            do
            {
                if (ClearCache != null && ClearCache.Get(context))
                {
                    Log.Selector(string.Format("Windows.GetElement::Clearing windows element cache {0:mm\\:ss\\.fff}", sw.Elapsed));
                    WindowsSelectorItem.ClearCache();
                }
                if (PluginConfig.get_elements_in_different_thread)
                {
                    elements = OpenRPA.AutomationHelper.RunSTAThread<UIElement[]>(() =>
                    {
                        try
                        {

                            Log.Selector(string.Format("Windows.GetElement::GetElementsWithuiSelector in non UI thread {0:mm\\:ss\\.fff}", sw.Elapsed));
                            return WindowsSelector.GetElementsWithuiSelector(sel, from, maxresults, ext);
                        }
                        catch (System.Threading.ThreadAbortException)
                        {
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                        return new UIElement[] { };
                    }, PluginConfig.search_timeout).Result;
                }
                else
                {
                    Log.Selector(string.Format("Windows.GetElement::GetElementsWithuiSelector using UI thread {0:mm\\:ss\\.fff}", sw.Elapsed));
                    elements = WindowsSelector.GetElementsWithuiSelector(sel, from, maxresults, ext);
                    if (elements == null || elements.Length == 0)
                    {
                        elements = WindowsSelector.GetElementsWithuiSelector(sel, from, maxresults, ext);
                    }
                }
                //elements = WindowsSelector.GetElementsWithuiSelector(sel, from, maxresults);
                if (elements == null)
                {
                    elements = new UIElement[] { };
                }
                if (elements.Length == 0)
                {
                    Log.Selector(string.Format("Windows.GetElement::Found no elements {0:mm\\:ss\\.fff}", sw.Elapsed));
                    failcounter++;
                }
                if (failcounter > 2)
                {
                    WindowsSelectorItem.ClearCache();
                }
            } while (elements != null && elements.Length == 0 && sw.Elapsed < timeout);
            if (PluginConfig.get_elements_in_different_thread && elements.Length > 0)
            {
                // Get them again, we need the COM objects to be loaded in the UI thread
                elements = WindowsSelector.GetElementsWithuiSelector(sel, from, maxresults, ext);
            }
            context.SetValue(Elements, elements);

            var lastelements = context.GetValue(_lastelements);
            if (lastelements == null) lastelements = new UIElement[] { };
            context.SetValue(_lastelements, elements);
            if ((elements.Length + lastelements.Length) < minresults)
            {
                Log.Selector(string.Format("Windows.GetElement::Failed locating " + minresults + " item(s) {0:mm\\:ss\\.fff}", sw.Elapsed));
                throw new ElementNotFoundException("Failed locating " + minresults + " item(s)");
            }
            IEnumerator<UIElement> _enum = elements.ToList().GetEnumerator();
            bool more = _enum.MoveNext();
            if (lastelements.Length == elements.Length && lastelements.Length > 0)
            {
                more = !System.Collections.StructuralComparisons.StructuralEqualityComparer.Equals(lastelements, elements);
            }
            if (more)
            {
                if (interactive)
                {
                    var testelement = _enum.Current;
                    Wait.UntilResponsive(testelement.RawElement, PluginConfig.search_timeout);
                }
                context.SetValue(_elements, _enum);
                context.SetValue(_sw, sw);
                Log.Selector(string.Format("Windows.GetElement::end:: call ScheduleAction: {0:mm\\:ss\\.fff}", sw.Elapsed));
                IncIndex(context);
                SetTotal(context, elements.Length);
                context.ScheduleAction<UIElement>(Body, _enum.Current, OnBodyComplete);
            }
            else
            {
                Log.Selector(string.Format("Windows.GetElement:end {0:mm\\:ss\\.fff}", sw.Elapsed));
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<UIElement> _enum = _elements.Get(context);
            Stopwatch sw = _sw.Get(context);
            Log.Selector(string.Format("Windows.GetElement:OnBodyComplete::begin {0:mm\\:ss\\.fff}", sw.Elapsed));
            bool more = _enum.MoveNext();
            if (more && !breakRequested)
            {
                IncIndex(context);
                Log.Selector(string.Format("Windows.GetElement:ScheduleAction {0:mm\\:ss\\.fff}", sw.Elapsed));
                context.ScheduleAction<UIElement>(Body, _enum.Current, OnBodyComplete);
            }
            else
            {
                if (LoopAction != null && !breakRequested)
                {
                    context.ScheduleActivity(LoopAction, LoopActionComplete);
                }
            }
            Log.Selector(string.Format("Windows.GetElement:end {0:mm\\:ss\\.fff}", sw.Elapsed));
        }
        private void LoopActionComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            if (!breakRequested) StartLoop(context);
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            Interfaces.Extensions.AddCacheArgument(metadata, "Selector", Selector);
            Interfaces.Extensions.AddCacheArgument(metadata, "From", From);
            Interfaces.Extensions.AddCacheArgument(metadata, "Elements", Elements);
            Interfaces.Extensions.AddCacheArgument(metadata, "MaxResults", MaxResults);

            metadata.AddImplementationVariable(_elements);
            metadata.AddImplementationVariable(_lastelements);
            metadata.AddImplementationVariable(_sw);
            base.CacheMetadata(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            var da = new DelegateInArgument<UIElement>
            {
                Name = "item"
            };
            Type t = Type.GetType("OpenRPA.Activities.ClickElement, OpenRPA");
            var instance = Activator.CreateInstance(t);
            var fef = new GetElement();
            fef.Variables.Add(new Variable<int>("Index", 0));
            fef.Variables.Add(new Variable<int>("Total", 0));
            fef.Body = new ActivityAction<UIElement>
            {
                Argument = da,
                Handler = (Activity)instance
            };
            return fef;
        }
        [LocalizedDisplayName("activity_displayname", typeof(Resources.strings)), LocalizedDescription("activity_displayname_help", typeof(Resources.strings))]
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