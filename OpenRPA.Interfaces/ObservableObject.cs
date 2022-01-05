using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public class NotifyChange : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                PropertyChanged += value;
            }

            remove
            {
                PropertyChanged -= value;
            }
        }

        //public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }

    public abstract class ObservableObject : NotifyChange, System.ComponentModel.INotifyPropertyChanged
    {
        [JsonIgnore]
        public Dictionary<string, object> _backingFieldValues = new Dictionary<string, object>();
        public void SetBackingFieldValues(Dictionary<string, object> values)
        {
            _backingFieldValues = values;
        }
        /// <summary>
        /// Gets a property value from the internal backing field
        /// </summary>
        protected T GetProperty<T>([CallerMemberName] string propertyName = null)
        {
            try
            {
                if (propertyName == null)
                {
                    throw new ArgumentNullException(nameof(propertyName));
                }
                object value;
                if (_backingFieldValues.TryGetValue(propertyName, out value))
                {
                    return (T)value;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                // throw;
            }
            return default(T);
        }
        private static string[] isDirtyIgnored = { "isDirty", "isLocalOnly", "IsExpanded", "IsExpanded", "IsSelected", "_type", "_id" };
        /// <summary>
        /// Saves a property value to the internal backing field
        /// </summary>
        protected bool SetProperty<T>(T newValue, [CallerMemberName] string propertyName = null)
        {
            bool _disabledirty = false;
            if (_backingFieldValues.ContainsKey("_disabledirty"))
            {
                if (_backingFieldValues.ContainsKey("_disabledirty") == true) _disabledirty = true;
            }
            try
            {
                if (propertyName == null)
                {
                    throw new ArgumentNullException(nameof(propertyName));
                }
                if (!isDirtyIgnored.Contains(propertyName))
                {
                    string modulename = null;
                    string modulename2 = null;
                    try
                    {
                        if (!_disabledirty)
                        {
                            var stack = (new System.Diagnostics.StackTrace());
                            modulename = stack.GetFrame(1).GetMethod().Module.ScopeName;
                            if (stack.FrameCount > 3) modulename2 = stack.GetFrame(3).GetMethod().Module.ScopeName;
                            if (modulename2 == "Newtonsoft.Json.dll")
                            {
                            }
                            else if (modulename2 == "LiteDB.dll")
                            {
                            }
                            else if (modulename2 == "CommonLanguageRuntimeLibrary")
                            {
                                _backingFieldValues["isDirty"] = true;
                            }
                            else if (modulename == "OpenRPA.exe" && modulename2 == "System.Activities.dll")
                            {
                                _backingFieldValues["isDirty"] = true;
                            }
                            else if (modulename == "OpenRPA.exe" && modulename2 == "OpenRPA.exe")
                            {
                                _backingFieldValues["isDirty"] = true;
                            }
                            else if (modulename == "OpenRPA.exe" && modulename2 == "LiteDB.dll")
                            {
                                _backingFieldValues["isDirty"] = true;
                            }
                            else if (modulename == "OpenRPA.exe" && modulename2 == "OpenRPA.Interfaces.dll")
                            {
                                _backingFieldValues["isDirty"] = true;
                            }
                            else if (modulename == "OpenRPA.exe" && modulename2 == "RefEmit_InMemoryManifestModule")
                            {
                                _backingFieldValues["isDirty"] = true;
                            }
                            else
                            {
#if (DEBUG)
                                // Log.Output(modulename + " " + modulename2);
#else
#endif
                                _backingFieldValues["isDirty"] = true;
                            }

                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                if (IsEqual(GetProperty<T>(propertyName), newValue)) return false;
                _backingFieldValues[propertyName] = newValue;
                OnPropertyChanged(propertyName);
                Type typeParameterType = typeof(T);
                if (typeParameterType.Name.ToLower().Contains("readonly"))
                {
                    return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
        }
        /// <summary>
        /// Sets a property value to the backing field
        /// </summary>
        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (IsEqual(field, newValue)) return false;
            field = newValue;
            OnPropertyChanged(propertyName);
            return true;
        }
        protected virtual void OnPropertyChanged<T>(Expression<Func<T>> selectorExpression)
        {
            NotifyPropertyChanged(GetNameFromExpression(selectorExpression));
        }
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            NotifyPropertyChanged(propertyName);
        }
        private bool IsEqual<T>(T field, T newValue)
        {
            // Alternative: EqualityComparer<T>.Default.Equals(field, newValue);
            return Equals(field, newValue);
        }
        private string GetNameFromExpression<T>(Expression<Func<T>> selectorExpression)
        {
            var body = (MemberExpression)selectorExpression.Body;
            var propertyName = body.Member.Name;
            return propertyName;
        }
    }
}
