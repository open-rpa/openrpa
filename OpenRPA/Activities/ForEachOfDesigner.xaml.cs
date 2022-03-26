using Microsoft.VisualBasic.Activities;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenRPA.Activities
{
    public partial class ForEachOfDesigner
    {
        public ForEachOfDesigner()
        {
            InitializeComponent();
            Loaded += (sender, e) =>
            {
                var Variables = ModelItem.Properties["Variables"].Collection;
                if (Variables != null && Variables.Count == 0)
                {
                    Variables.Add(new Variable<int>("Index", 0));
                    Variables.Add(new Variable<int>("Total", 0));
                }
            };

        }
        protected override void OnModelItemChanged(Object newItem)
        {
            base.OnModelItemChanged(newItem);
            GenericArgumentTypeUpdater.Attach(ModelItem);
        }
        private Type _listType;
        public Type ListType
        {
            get
            {
                if (null == _listType)
                {
                    var findActivity = ModelItem.GetCurrentValue();
                    var valuesProp = findActivity.GetType().GetProperty("Values");
                    var args = valuesProp.PropertyType.GenericTypeArguments;
                    _listType = args[0];
                }

                return _listType;
            }
        }

    }
}