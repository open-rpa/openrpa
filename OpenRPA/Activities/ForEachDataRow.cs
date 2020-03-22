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

namespace OpenRPA.Activities
{
    [System.ComponentModel.Designer(typeof(ForEachDataRowDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(ResFinder), "Resources.toolbox.foreach.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class ForEachDataRow : NativeActivity, System.Activities.Presentation.IActivityTemplateFactory
    {
        [RequiredArgument,Category("Input"),OverloadGroup("DataTable"), LocalizedDisplayName("activity_datatable", typeof(Resources.strings)), LocalizedDescription("activity_datatable_help", typeof(Resources.strings))s]
        public InArgument<System.Data.DataTable> DataTable { get; set; }
        [RequiredArgument, Category("Input"), OverloadGroup("DataView"), LocalizedDisplayName("activity_dataview", typeof(Resources.strings)), LocalizedDescription("activity_dataview_help", typeof(Resources.strings))]
        public InArgument<System.Data.DataView> DataView { get; set; }
        [Browsable(false)]
        public ActivityAction<System.Data.DataRowView> Body { get; set; }
        private Variable<IEnumerator<System.Data.DataRowView>> _elements = new Variable<IEnumerator<System.Data.DataRowView>>("_elements");
        protected override void Execute(NativeActivityContext context)
        {
            System.Data.DataView dv;
            System.Data.DataTable dt = DataTable.Get(context);
            if(dt!=null)
            {
                dv = dt.DefaultView;
            } else
            {
                dv = DataView.Get(context);
            }
             
            var elements = (from row in dv.Cast<System.Data.DataRowView>() select row).ToList();
            IEnumerator<System.Data.DataRowView> _enum = elements.GetEnumerator();
            context.SetValue(_elements, _enum);
            bool more = _enum.MoveNext();
            if (more)
            {
                context.ScheduleAction(Body, _enum.Current, OnBodyComplete);
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<System.Data.DataRowView> _enum = _elements.Get(context);
            bool more = _enum.MoveNext();
            if (more)
            {
                context.ScheduleAction<System.Data.DataRowView>(Body, _enum.Current, OnBodyComplete);
            }
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            Interfaces.Extensions.AddCacheArgument(metadata, "DataTable", DataTable);
            Interfaces.Extensions.AddCacheArgument(metadata, "DataView", DataView);
            metadata.AddImplementationVariable(_elements);
            base.CacheMetadata(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            var fef = new ForEachDataRow();
            var aa = new ActivityAction<System.Data.DataRowView>();
            var da = new DelegateInArgument<System.Data.DataRowView>();
            da.Name = "row";
            fef.Body = aa;
            aa.Argument = da;
            return fef;
        }
        [LocalizedDisplayName("activity_displayname", typeof(Resources.strings)), LocalizedDescription("activity_displayname_help", typeof(Resources.strings))]
        public new string DisplayName
        {
            get
            {
                return base.DisplayName;
            }
            set
            {
                base.DisplayName = value;
            }
        }
    }
}