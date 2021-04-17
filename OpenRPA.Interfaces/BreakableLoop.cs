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
        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }
    }
}
