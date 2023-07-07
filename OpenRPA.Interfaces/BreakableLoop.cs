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
        public void IncIndex(NativeActivityContext context)
        {
            var input = GetTotalDescriptor(context);
            if (input == null) return;
            if (input != null)
            {
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
        }
        public Variable GetTotalVariable(NativeActivityContext context)
        {
            if (Variables.Count < 1) return null;
            for (var i = 0; i < Variables.Count; i++)
            {
                var variable = Variables[i];
                if (variable.Name == "Total" && variable.Type == typeof(int))
                {
                    return variable;
                }
            }
            return null;
        }
        public PropertyDescriptor GetTotalDescriptor(NativeActivityContext context)
        {
            if (Variables.Count < 1) return null;
            for (var i = 0; i < Variables.Count; i++)
            {
                var variable = Variables[i];
                if (variable.Name == "Total" && variable.Type == typeof(int))
                {
                    return context.DataContext.GetProperties()[variable.Name];
                }
            }
            return null;
        }
        public Variable GetIndexVariable(NativeActivityContext context)
        {
            if (Variables.Count < 1) return null;
            for (var i = 0; i < Variables.Count; i++)
            {
                var variable = Variables[i];
                if (variable.Name == "Index" && variable.Type == typeof(int))
                {
                    return variable;
                }
            }
            return null;
        }
        public PropertyDescriptor GetIndexDescriptor(NativeActivityContext context)
        {
            if (Variables.Count < 1) return null;
            for (var i = 0; i < Variables.Count; i++)
            {
                var variable = Variables[i];
                if (variable.Name == "Index" && variable.Type == typeof(int))
                {
                    return context.DataContext.GetProperties()[variable.Name];
                }
            }
            return null;
        }
        public void SetTotal(NativeActivityContext context, int Value)
        {
            var input = GetTotalDescriptor(context);
            if (input == null) return;
            input.SetValue(context.DataContext, Value);
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
