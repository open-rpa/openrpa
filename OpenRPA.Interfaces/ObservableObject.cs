using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public class NotifyChange : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
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
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }
            object value;
            if (_backingFieldValues.TryGetValue(propertyName, out value))
            {
                //if(value is JArray)
                //{
                //    //JObject rItemValueJson = (JObject)rItem.Value;
                //    //Races rowsResult = item.Value<JObject>("races").ToObject<Races>();
                //    return ((JArray)value).ToList<T>();
                //}
                return (T)value;
            }
            return default(T);
        }
        /// <summary>
        /// Saves a property value to the internal backing field
        /// </summary>
        protected bool SetProperty<T>(T newValue, [CallerMemberName] string propertyName = null)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }
            if (IsEqual(GetProperty<T>(propertyName), newValue)) return false;
            _backingFieldValues[propertyName] = newValue;
            OnPropertyChanged(propertyName);
            return true;
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
