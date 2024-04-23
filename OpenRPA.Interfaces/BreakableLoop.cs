using Newtonsoft.Json.Linq;
using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public abstract class BreakableLoop : NativeActivity
    {
        protected abstract void StartLoop(NativeActivityContext context);
        public bool breakRequested = false;
        protected override void Execute(NativeActivityContext context)
        {
            this.breakRequested = false;
            Bookmark onbreak = context.CreateBookmark(new BookmarkCallback(this.OnBreak), BookmarkOptions.NonBlocking);
            context.Properties.Add("BreakBookmark", onbreak);
            Bookmark oncontinue = context.CreateBookmark(new BookmarkCallback(this.OnContinue), BookmarkOptions.NonBlocking | BookmarkOptions.MultipleResume);
            context.Properties.Add("ContinueBookmark", oncontinue);
            this.StartLoop(context);
        }
        private void OnBreak(NativeActivityContext context, Bookmark bookmark, object value)
        {
            context.CancelChildren();
            breakRequested = true;
            Bookmark _bookmark = value as Bookmark;
            if (_bookmark != null)
            {
                context.ResumeBookmark(_bookmark, value);
            }
        }
        private void OnContinue(NativeActivityContext context, Bookmark bookmark, object value)
        {
            context.CancelChildren();
            Bookmark _bookmark = value as Bookmark;
            if (_bookmark != null)
            {
                context.ResumeBookmark(_bookmark, value);
            }
        }
        public void SetTotal(NativeActivityContext context, int Value)
        {
            var input = GetDescriptor<int>(context, "Total");
            if (input == null) return;
            input.SetValue(context.DataContext, Value);
        }
        public void IncIndex(NativeActivityContext context)
        {
            var input = GetDescriptor<int>(context, "Index");
            if (input == null) return;
            try
            {
                var _index = 0;
                var value = input.GetValue(context.DataContext);
                if (value != null)
                {
                    if (value is int wfview)
                    {
                        _index = (int)value;
                    }
                    else if(int.TryParse(value.ToString(), out int current))
                    {
                        _index = current;
                    }                        
                }
                _index++;
                input.SetValue(context.DataContext, _index);
            }
            catch (Exception ex)
            {
                Log.Warning("Updating loop Index failed with " + ex.ToString());
            }
        }
        public Variable GetVariable<T>(NativeActivityContext context, string Name)
        {
            if (Variables.Count < 1) return null;
            if(context == null) return null;
            if (string.IsNullOrEmpty(Name)) return null;
            for (var i = 0; i < Variables.Count; i++)
            {
                var variable = Variables[i];
                if (variable.Name.ToLower() == Name.ToLower() && variable.Type == typeof(T))
                {
                    return variable;
                }
            }
            return null;
        }
        public PropertyDescriptor GetDescriptor<T>(NativeActivityContext context, string Name)
        {
            var v = GetVariable<T>(context, Name);
            if(v == null) return null;
            return context.DataContext.GetProperties()[v.Name];
        }
        [System.ComponentModel.Browsable(false)]
        public System.Collections.ObjectModel.Collection<Variable> Variables { get; set; } = new System.Collections.ObjectModel.Collection<Variable>();
        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }
    }
}
