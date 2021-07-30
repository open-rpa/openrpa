using System;
using System.Activities;
using System.Collections.Generic;
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
            //var Value = Index.Get(context);
            //Value++;
            //context.SetValue(Index, Value);
            if (Variables.Count < 1) return;
            var input = context.DataContext.GetProperties()[Variables[0].Name];
            if (input != null)
            {
                var _index = 0;
                var value = input.GetValue(context.DataContext);
                if (value != null) _index = (int)value;
                _index++;
                input.SetValue(context.DataContext, _index);
            }
        }
        public void SetIndex(NativeActivityContext context, int Value)
        {
            //context.SetValue(Index, Value);
            if (Variables.Count < 1) return;
            var input = context.DataContext.GetProperties()[Variables[0].Name];
            if (input != null) input.SetValue(context.DataContext, Value);
        }
        public void SetTotal(NativeActivityContext context, int Value)
        {
            //context.SetValue(Total, Value);
            if (Variables.Count < 2) return;
            var input = context.DataContext.GetProperties()[Variables[1].Name];
            if (input != null) input.SetValue(context.DataContext, Value);
        }
        private System.Collections.ObjectModel.Collection<Variable> variables;
        [System.ComponentModel.Browsable(false)]
        public System.Collections.ObjectModel.Collection<Variable> Variables
        {
            get
            {
                if (variables == null)
                {
                    variables = new System.Collections.ObjectModel.Collection<Variable>();
                }
                return variables;
            }
        }
        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }
    }
}
