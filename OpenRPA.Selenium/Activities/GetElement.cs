using OpenRPA.Interfaces;
using OpenRPA.Selenium.Activities;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Activities.Statements;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace OpenRPA.Selenium
{
    [System.ComponentModel.Designer(typeof(GetElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetElement), "Resources.toolbox.default.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    [LocalizedToolboxTooltip("activity_getelement_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_getelement", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_getelement_helpurl", typeof(Resources.strings))]
    public class GetElement : BreakableLoop, System.Activities.Presentation.IActivityTemplateFactory
    {
        public GetElement()
        {
            BrowserDriver = new InArgument<IWebDriver>()
            {
                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IWebDriver>("BrowserDriver")
            };
            MaxResults = 1;
            MinResults = 1;
            FindBy = "XPath";
            Timeout = new TimeSpan();
        }
        [System.ComponentModel.Browsable(false)]
        public ActivityAction<IWebElement> Body { get; set; }
        public InArgument<int> MaxResults { get; set; }
        public InArgument<int> MinResults { get; set; }
        public InArgument<TimeSpan> Timeout { get; set; }

        [Editor(typeof(SelectByEditor), typeof(ExtendedPropertyValueEditor))]
        [RequiredArgument, Category("Input")]
        public InArgument<string> FindBy { get; set; }
        [RequiredArgument, Category("Input")]
        public InArgument<string> Value { get; set; }
        [RequiredArgument, Category("Input")]
        public InArgument<IWebDriver> BrowserDriver { get; set; }

        [Category("Output")]
        public OutArgument<IWebElement[]> Elements { get; set; }

        [System.ComponentModel.Browsable(false)]
        public System.Activities.Activity LoopAction { get; set; }
        private Variable<IEnumerator<IWebElement>> _elements = new Variable<IEnumerator<IWebElement>>("_elements");

        protected override void StartLoop(NativeActivityContext context)
        {
            var maxResults = MaxResults.Get(context);
            var minResults = MinResults.Get(context);

            var webDriver = BrowserDriver.Get(context);
            var findBy = FindBy.Get(context);
            var value = Value.Get(context);
            var timeout = Timeout.Get(context);

            try
            {
                var elements = FindElements(webDriver, findBy, value, timeout);
                if (elements == null || elements.Length < minResults)
                {
                    throw new ElementNotFoundException($"{DisplayName} couldn't find " + minResults + " item(s) element");
                }
                if (maxResults > 0)
                {
                    elements = elements.Take(maxResults).ToArray();
                }

                context.SetValue(Elements, elements);
                IEnumerator<IWebElement> enumerator = elements.ToList().GetEnumerator();
                if (enumerator.MoveNext())
                {
                    this._elements.Set(context, enumerator);
                    IncIndex(context);
                    SetTotal(context, elements.Count());
                    context.ScheduleAction(Body, enumerator.Current, OnBodyComplete);
                }
            }
            catch (WebDriverTimeoutException)
            {
                if(minResults > 0)
                {
                    throw new ElementNotFoundException($"{DisplayName} couldn't find element within the specified timeout");
                }
            }
            catch (Exception)
            {
                throw new Exception($"{DisplayName} couldn't find element");
            }
        }

        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<IWebElement> _enum = _elements.Get(context);
            if (_enum == null) return;
            bool more = _enum.MoveNext();
            if (more && !breakRequested)
            {
                IncIndex(context);
                context.ScheduleAction<IWebElement>(Body, _enum.Current, OnBodyComplete);
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
            System.Threading.Thread.Sleep(500);
            StartLoop(context);
        }

        public IWebElement[] FindElements(IWebDriver webDriver, string findBy, string value, TimeSpan timeout)
        {
            By by;
            if (findBy.Equals("Text"))
            {
                by = By.XPath($"//*[text()='{value}']");
            }
            else
            {
                var method = typeof(By).GetMethod(findBy, new Type[] { typeof(string) });
                by = (By)method.Invoke(null, new object[] { value });
            }

            if(timeout.TotalMilliseconds > 0)
            {
                var wait = new WebDriverWait(webDriver, timeout);
                return wait.Until(driver => 
                {
                    var elements = driver.FindElements(by);
                    return elements.Any() ? elements : null;
                })?.ToArray();
            }
            return webDriver.FindElements(by)?.ToArray();
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            metadata.AddImplementationVariable(_elements);
            base.CacheMetadata(metadata);
        }

        public System.Activities.Activity Create(System.Windows.DependencyObject target)
        {
            var fef = new GetElement();
            fef.Variables.Add(new Variable<int>("Index", 0));
            fef.Variables.Add(new Variable<int>("Total", 0));

            var aa = new ActivityAction<IWebElement>();
            var sequence = new Sequence();
            sequence.Activities.Add(new ElementActions());
            //aa.Handler = sequence;
            aa.Handler = new ElementActions();

            var da = new DelegateInArgument<IWebElement>();
            da.Name = "item";
            fef.Body = aa;
            aa.Argument = da;
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

        class SelectByEditor : CustomSelectEditor
        {
            public override DataTable options
            {

                get
                {
                    var lst = new DataTable();
                    lst.Columns.Add("ID", typeof(string));
                    lst.Columns.Add("TEXT", typeof(string));
                    lst.Rows.Add("TagName", "TagName");
                    lst.Rows.Add("Name", "Name");
                    lst.Rows.Add("Id", "Id");
                    lst.Rows.Add("ClassName", "ClassName");
                    lst.Rows.Add("CssSelector", "CssSelector");
                    lst.Rows.Add("XPath", "XPath");
                    lst.Rows.Add("LinkText", "LinkText");
                    lst.Rows.Add("Text", "Text");
                    return lst;
                }
            }
        }

    }
}