using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.DynamicUpdate;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(ForEachOfDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.foreach.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    [System.Activities.Presentation.DefaultTypeArgument(typeof(string))]
    [LocalizedToolboxTooltip("activity_breakableforeachof_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_breakableforeachof", typeof(Resources.strings))]
    public class ForEachOf<T> : BreakableLoop
    {
        public ForEachOf()
        {
            valueEnumerator = new Variable<IEnumerator<T>>();
            Body = new ActivityAction<T>
            {
                Argument = new DelegateInArgument<T>("Item")
            };
        }
        //[DefaultValue((string)null)]
        //public Type TargetType { get; set; }
        [RequiredArgument]
        public InArgument<IEnumerable<T>> Values { get; set; }

        [Browsable(false)]
        public ActivityAction<T> Body { get; set; }
        private Variable<IEnumerator<T>> valueEnumerator;

        protected override void StartLoop(NativeActivityContext context)
        {
            IEnumerable<T> enumerable = this.Values.Get(context);
            IEnumerator<T> enumerator = enumerable.GetEnumerator();
            this.valueEnumerator.Set(context, enumerator);
            if ((this.Body != null) && (this.Body.Handler != null))
            {
                SetTotal(context, enumerable.Count());
                this.InternalExecute(context, null, enumerator);
            }
            else
            {
                while (enumerator.MoveNext())
                {
                }
                enumerator.Dispose();
            }
        }
        private void InternalExecute(NativeActivityContext context, ActivityInstance completedInstance, IEnumerator<T> valueEnumerator)
        {
            if (!valueEnumerator.MoveNext())
            {
                if ((completedInstance != null) && ((completedInstance.State == ActivityInstanceState.Canceled) || (context.IsCancellationRequested && (completedInstance.State == ActivityInstanceState.Faulted))))
                {
                    try
                    {
                        context.MarkCanceled();
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex.ToString());
                    }
                }
                valueEnumerator.Dispose();
            }
            else if (!context.IsCancellationRequested)
            {
                IncIndex(context);
                context.ScheduleAction<T>(this.Body, valueEnumerator.Current, this.OnChildComplete, null);
            }
            else
            {
                try
                {
                    context.MarkCanceled();
                }
                catch (Exception ex)
                {
                    Log.Debug(ex.ToString());
                }
                valueEnumerator.Dispose();
            }
        }
        private CompletionCallback onChildComplete;
        private CompletionCallback OnChildComplete
        {
            get
            {
                this.onChildComplete = new CompletionCallback(this.GetStateAndExecute);
                return this.onChildComplete;
            }
        }
        private void GetStateAndExecute(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<T> valueEnumerator = this.valueEnumerator.Get(context);
            if (!breakRequested ) this.InternalExecute(context, completedInstance, valueEnumerator);
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            RuntimeArgument argument = new RuntimeArgument("Values", typeof(IEnumerable<T>), ArgumentDirection.In, true);
            metadata.Bind(Values, argument);
            metadata.AddArgument(argument);
            metadata.AddDelegate(Body);
            metadata.AddImplementationVariable(valueEnumerator);
            base.CacheMetadata(metadata);
            //metadata.AddChild(Body);
            //metadata.AddChild(Condition);
            //metadata.AddImplementationVariable(_elements);
            //base.CacheMetadata(metadata);
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