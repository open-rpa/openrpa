using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Activities.Hosting;

namespace OpenRPA.Interfaces
{
    public abstract class AsyncTaskNativeActivity : AsyncNativeActivity
    {
        protected sealed override IAsyncResult BeginExecute(NativeActivityContext context, AsyncCallback callback, object state)
        {
            var task = ExecuteAsync(context);
            var tcs = new TaskCompletionSource<object>(state);
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(t.Result);
                callback?.Invoke(tcs.Task);
            });
            return tcs.Task;
        }

        protected sealed override void EndExecute(NativeActivityContext context, IAsyncResult result)
        {
            var task = (Task<object>)result;
            try
            {
                AfterExecute(context, task.Result);
                return;
            }
            catch (AggregateException ex)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }
        protected abstract Task<object> ExecuteAsync(NativeActivityContext context);
        protected abstract void AfterExecute(NativeActivityContext context, object result);
    }



//}




//using System;
//using System.Activities;
//using System.Activities.Hosting;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace OpenRPA.Interfaces
//{
    public class BookmarkResumptionHelper : IWorkflowInstanceExtension

    {

        private WorkflowInstanceProxy instance;



        public void ResumeBookmark(Bookmark bookmark, object value)

        {

            this.instance.EndResumeBookmark(

                this.instance.BeginResumeBookmark(bookmark, value, null, null));

        }



        IEnumerable<object> IWorkflowInstanceExtension.GetAdditionalExtensions()

        {

            yield break;

        }



        void IWorkflowInstanceExtension.SetInstance(WorkflowInstanceProxy instance)

        {

            this.instance = instance;

        }

    }



    public abstract class AsyncNativeActivity : NativeActivity

    {

        private Variable<NoPersistHandle> NoPersistHandle { get; set; }

        private Variable<Bookmark> Bookmark { get; set; }



        protected override bool CanInduceIdle

        {

            get

            {

                return true; // we create bookmarks

            }

        }



        protected abstract IAsyncResult BeginExecute(

            NativeActivityContext context,

            AsyncCallback callback, object state);



        protected abstract void EndExecute(

            NativeActivityContext context,

            IAsyncResult result);



        protected override void Execute(NativeActivityContext context)

        {

            var noPersistHandle = NoPersistHandle.Get(context);

            noPersistHandle.Enter(context);



            var bookmark = context.CreateBookmark(BookmarkResumptionCallback);

            this.Bookmark.Set(context, bookmark);



            BookmarkResumptionHelper helper = context.GetExtension<BookmarkResumptionHelper>();

            Action<IAsyncResult> resumeBookmarkAction = (result) =>

            {

                helper.ResumeBookmark(bookmark, result);

            };



            IAsyncResult asyncResult = this.BeginExecute(context, AsyncCompletionCallback, resumeBookmarkAction);



            if (asyncResult.CompletedSynchronously)

            {

                noPersistHandle.Exit(context);

                context.RemoveBookmark(bookmark);

                EndExecute(context, asyncResult);

            }

        }



        private void AsyncCompletionCallback(IAsyncResult asyncResult)

        {

            if (!asyncResult.CompletedSynchronously)

            {

                Action<IAsyncResult> resumeBookmark = asyncResult.AsyncState as Action<IAsyncResult>;

                resumeBookmark.Invoke(asyncResult);

            }

        }



        private void BookmarkResumptionCallback(NativeActivityContext context, Bookmark bookmark, object value)

        {

            var noPersistHandle = NoPersistHandle.Get(context);

            noPersistHandle.Exit(context);

            // unnecessary since it's not multiple resume:

            // context.RemoveBookmark(bookmark);



            IAsyncResult asyncResult = value as IAsyncResult;

            this.EndExecute(context, asyncResult);

        }



        protected override void CacheMetadata(NativeActivityMetadata metadata)

        {

            this.NoPersistHandle = new Variable<NoPersistHandle>();

            this.Bookmark = new Variable<Bookmark>();

            metadata.AddImplementationVariable(this.NoPersistHandle);

            metadata.AddImplementationVariable(this.Bookmark);

            metadata.RequireExtension<BookmarkResumptionHelper>();

            metadata.AddDefaultExtensionProvider<BookmarkResumptionHelper>(() => new BookmarkResumptionHelper());

        }

    }

}
