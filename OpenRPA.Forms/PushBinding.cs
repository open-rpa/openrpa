using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace OpenRPA.Forms
{
    public class FreezableBinding : Freezable
    {
        #region Properties

        private Binding _binding;

        protected Binding Binding
        {
            get
            {
                if (_binding == null)
                {
                    _binding = new Binding();
                }

                return _binding;
            }
        }

        [DefaultValue(null)]
        public object AsyncState
        {
            get { return Binding.AsyncState; }
            set { Binding.AsyncState = value; }
        }

        [DefaultValue(false)]
        public bool BindsDirectlyToSource
        {
            get { return Binding.BindsDirectlyToSource; }
            set { Binding.BindsDirectlyToSource = value; }
        }

        [DefaultValue(null)]
        public IValueConverter Converter
        {
            get { return Binding.Converter; }
            set { Binding.Converter = value; }
        }

        [TypeConverter(typeof(CultureInfoIetfLanguageTagConverter)), DefaultValue(null)]
        public CultureInfo ConverterCulture
        {
            get { return Binding.ConverterCulture; }
            set { Binding.ConverterCulture = value; }
        }

        [DefaultValue(null)]

        public object ConverterParameter
        {
            get { return Binding.ConverterParameter; }
            set { Binding.ConverterParameter = value; }
        }

        [DefaultValue(null)]
        public string ElementName
        {
            get { return Binding.ElementName; }
            set { Binding.ElementName = value; }
        }

        [DefaultValue(null)]
        public object FallbackValue
        {
            get { return Binding.FallbackValue; }
            set { Binding.FallbackValue = value; }
        }

        [DefaultValue(false)]
        public bool IsAsync
        {
            get { return Binding.IsAsync; }
            set { Binding.IsAsync = value; }
        }

        [DefaultValue(BindingMode.Default)]
        public BindingMode Mode
        {
            get { return Binding.Mode; }
            set { Binding.Mode = value; }
        }

        [DefaultValue(false)]
        public bool NotifyOnSourceUpdated
        {
            get { return Binding.NotifyOnSourceUpdated; }
            set { Binding.NotifyOnSourceUpdated = value; }
        }

        [DefaultValue(false)]
        public bool NotifyOnTargetUpdated
        {
            get { return Binding.NotifyOnTargetUpdated; }
            set { Binding.NotifyOnTargetUpdated = value; }
        }

        [DefaultValue(false)]
        public bool NotifyOnValidationError
        {
            get { return Binding.NotifyOnValidationError; }
            set { Binding.NotifyOnValidationError = value; }
        }

        [DefaultValue(null)]
        public PropertyPath Path
        {
            get { return Binding.Path; }
            set { Binding.Path = value; }
        }

        [DefaultValue(null)]
        public RelativeSource RelativeSource
        {
            get { return Binding.RelativeSource; }
            set { Binding.RelativeSource = value; }
        }

        [DefaultValue(null)]
        public object Source
        {
            get { return Binding.Source; }
            set { Binding.Source = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UpdateSourceExceptionFilterCallback UpdateSourceExceptionFilter
        {
            get { return Binding.UpdateSourceExceptionFilter; }
            set { Binding.UpdateSourceExceptionFilter = value; }
        }

        [DefaultValue(UpdateSourceTrigger.PropertyChanged)]
        public UpdateSourceTrigger UpdateSourceTrigger
        {
            get { return Binding.UpdateSourceTrigger; }
            set { Binding.UpdateSourceTrigger = value; }
        }

        [DefaultValue(false)]
        public bool ValidatesOnDataErrors
        {
            get { return Binding.ValidatesOnDataErrors; }
            set { Binding.ValidatesOnDataErrors = value; }
        }

        [DefaultValue(false)]
        public bool ValidatesOnExceptions
        {
            get { return Binding.ValidatesOnExceptions; }
            set { Binding.ValidatesOnExceptions = value; }
        }

        [DefaultValue(null)]
        public string XPath
        {
            get { return Binding.XPath; }
            set { Binding.XPath = value; }
        }

        [DefaultValue(null)]
        public Collection<ValidationRule> ValidationRules
        {
            get { return Binding.ValidationRules; }
        }

        #endregion // Properties

        #region Freezable overrides

        protected override void CloneCore(Freezable sourceFreezable)
        {
            FreezableBinding freezableBindingClone = sourceFreezable as FreezableBinding;
            if (freezableBindingClone.ElementName != null)
            {
                ElementName = freezableBindingClone.ElementName;
            }
            else if (freezableBindingClone.RelativeSource != null)
            {
                RelativeSource = freezableBindingClone.RelativeSource;
            }
            else if (freezableBindingClone.Source != null)
            {
                Source = freezableBindingClone.Source;
            }

            AsyncState = freezableBindingClone.AsyncState;
            BindsDirectlyToSource = freezableBindingClone.BindsDirectlyToSource;
            Converter = freezableBindingClone.Converter;
            ConverterCulture = freezableBindingClone.ConverterCulture;
            ConverterParameter = freezableBindingClone.ConverterParameter;
            FallbackValue = freezableBindingClone.FallbackValue;
            IsAsync = freezableBindingClone.IsAsync;
            Mode = freezableBindingClone.Mode;
            NotifyOnSourceUpdated = freezableBindingClone.NotifyOnSourceUpdated;
            NotifyOnTargetUpdated = freezableBindingClone.NotifyOnTargetUpdated;
            NotifyOnValidationError = freezableBindingClone.NotifyOnValidationError;
            Path = freezableBindingClone.Path;
            UpdateSourceExceptionFilter = freezableBindingClone.UpdateSourceExceptionFilter;
            UpdateSourceTrigger = freezableBindingClone.UpdateSourceTrigger;
            ValidatesOnDataErrors = freezableBindingClone.ValidatesOnDataErrors;
            ValidatesOnExceptions = freezableBindingClone.ValidatesOnExceptions;
            XPath = XPath;
            foreach (ValidationRule validationRule in freezableBindingClone.ValidationRules)
            {
                ValidationRules.Add(validationRule);
            }

            base.CloneCore(sourceFreezable);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new FreezableBinding();
        }

        #endregion // Freezable overrides
    }

    public class PushBinding : FreezableBinding
    {
        #region Dependency Properties

        public static DependencyProperty TargetPropertyMirrorProperty =
            DependencyProperty.Register("TargetPropertyMirror",
                typeof(object),
                typeof(PushBinding));

        public static DependencyProperty TargetPropertyListenerProperty =
            DependencyProperty.Register("TargetPropertyListener",
                typeof(object),
                typeof(PushBinding),
                new UIPropertyMetadata(null, OnTargetPropertyListenerChanged));

        private static void OnTargetPropertyListenerChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            PushBinding pushBinding = sender as PushBinding;
            pushBinding.TargetPropertyValueChanged();
        }

        #endregion // Dependency Properties

        #region Constructor

        public PushBinding()
        {
            Mode = BindingMode.OneWayToSource;
        }

        #endregion // Constructor

        #region Properties

        public object TargetPropertyMirror
        {
            get { return GetValue(TargetPropertyMirrorProperty); }
            set { SetValue(TargetPropertyMirrorProperty, value); }
        }

        public object TargetPropertyListener
        {
            get { return GetValue(TargetPropertyListenerProperty); }
            set { SetValue(TargetPropertyListenerProperty, value); }
        }

        [DefaultValue(null)]
        public string TargetProperty { get; set; }

        [DefaultValue(null)]
        public DependencyProperty TargetDependencyProperty { get; set; }

        #endregion // Properties

        #region Public Methods

        public void SetupTargetBinding(DependencyObject targetObject)
        {
            if (targetObject == null)
            {
                return;
            }

            // Prevent the designer from reporting exceptions since
            // changes will be made of a Binding in use if it is set
            if (DesignerProperties.GetIsInDesignMode(this) == true)
                return;

            // Bind to the selected TargetProperty, e.g. ActualHeight and get
            // notified about changes in OnTargetPropertyListenerChanged
            Binding listenerBinding = new Binding
            {
                Source = targetObject,
                Mode = BindingMode.OneWay
            };
            if (TargetDependencyProperty != null)
            {
                listenerBinding.Path = new PropertyPath(TargetDependencyProperty);
            }
            else
            {
                listenerBinding.Path = new PropertyPath(TargetProperty);
            }

            BindingOperations.SetBinding(this, TargetPropertyListenerProperty, listenerBinding);

            // Set up a OneWayToSource Binding with the Binding declared in Xaml from
            // the Mirror property of this class. The mirror property will be updated
            // everytime the Listener property gets updated
            BindingOperations.SetBinding(this, TargetPropertyMirrorProperty, Binding);

            TargetPropertyValueChanged();
            if (targetObject is FrameworkElement)
            {
                ((FrameworkElement)targetObject).Loaded += delegate { TargetPropertyValueChanged(); };
            }
            else if (targetObject is FrameworkContentElement)
            {
                ((FrameworkContentElement)targetObject).Loaded += delegate { TargetPropertyValueChanged(); };
            }
        }

        #endregion // Public Methods

        #region Private Methods

        private void TargetPropertyValueChanged()
        {
            object targetPropertyValue = GetValue(TargetPropertyListenerProperty);
            this.SetValue(TargetPropertyMirrorProperty, targetPropertyValue);
        }

        #endregion // Private Methods

        #region Freezable overrides

        protected override void CloneCore(Freezable sourceFreezable)
        {
            PushBinding pushBinding = sourceFreezable as PushBinding;
            TargetProperty = pushBinding.TargetProperty;
            TargetDependencyProperty = pushBinding.TargetDependencyProperty;
            base.CloneCore(sourceFreezable);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new PushBinding();
        }

        #endregion // Freezable overrides
    }

    public class PushBindingCollection : FreezableCollection<PushBinding>
    {
        public PushBindingCollection()
        {
        }

        public PushBindingCollection(DependencyObject targetObject)
        {
            TargetObject = targetObject;
            ((INotifyCollectionChanged)this).CollectionChanged += CollectionChanged;
        }

        void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (PushBinding pushBinding in e.NewItems)
                {
                    pushBinding.SetupTargetBinding(TargetObject);
                }
            }
        }

        public DependencyObject TargetObject { get; private set; }
    }

    public class PushBindingManager
    {
        public static DependencyProperty PushBindingsProperty =
            DependencyProperty.RegisterAttached("PushBindingsInternal",
                typeof(PushBindingCollection),
                typeof(PushBindingManager),
                new UIPropertyMetadata(null));

        public static PushBindingCollection GetPushBindings(DependencyObject obj)
        {
            if (obj.GetValue(PushBindingsProperty) == null)
            {
                obj.SetValue(PushBindingsProperty, new PushBindingCollection(obj));
            }

            return (PushBindingCollection)obj.GetValue(PushBindingsProperty);
        }

        public static void SetPushBindings(DependencyObject obj, PushBindingCollection value)
        {
            obj.SetValue(PushBindingsProperty, value);
        }


        public static DependencyProperty StylePushBindingsProperty =
            DependencyProperty.RegisterAttached("StylePushBindings",
                typeof(PushBindingCollection),
                typeof(PushBindingManager),
                new UIPropertyMetadata(null, StylePushBindingsChanged));

        public static PushBindingCollection GetStylePushBindings(DependencyObject obj)
        {
            return (PushBindingCollection)obj.GetValue(StylePushBindingsProperty);
        }

        public static void SetStylePushBindings(DependencyObject obj, PushBindingCollection value)
        {
            obj.SetValue(StylePushBindingsProperty, value);
        }

        public static void StylePushBindingsChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target != null)
            {
                PushBindingCollection stylePushBindings = e.NewValue as PushBindingCollection;
                PushBindingCollection pushBindingCollection = GetPushBindings(target);
                foreach (PushBinding pushBinding in stylePushBindings)
                {
                    PushBinding pushBindingClone = pushBinding.Clone() as PushBinding;
                    pushBindingCollection.Add(pushBindingClone);
                }
            }
        }
    }

}
