using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRPA
{
    public class TimeoutAction
    {
        private Thread ActionThread { get; set; }
        private Thread TimeoutThread { get; set; }
        private AutoResetEvent ThreadSynchronizer { get; set; }
        private bool _success;
        private bool _timout;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="waitLimit">in ms</param>
        /// <param name="action">delegate action</param>
        public TimeoutAction(int waitLimit, Action action, ApartmentState state)
        {
            ThreadSynchronizer = new AutoResetEvent(false);
            ActionThread = new Thread(new ThreadStart(delegate
            {
                action.Invoke();
                if (_timout) return;
                _timout = true;
                _success = true;
                ThreadSynchronizer.Set();
            }));
            ActionThread.Name = "ActionThread";
            ActionThread.SetApartmentState(state);

            TimeoutThread = new Thread(new ThreadStart(delegate
            {
                Thread.Sleep(waitLimit);
                if (_success) return;
                _timout = true;
                _success = false;
                ThreadSynchronizer.Set();
            }));
            TimeoutThread.Name = "TimeoutThread";
        }

        /// <summary>
        /// If the action takes longer than the wait limit, this will throw a TimeoutException
        /// </summary>
        public bool Start()
        {
            ActionThread.Start();
            TimeoutThread.Start();

            ThreadSynchronizer.WaitOne();

            if (!_success)
            {
                // throw new TimeoutException();
                ActionThread.Abort();
            }
            ThreadSynchronizer.Close();
            return _success;
        }
    }
    public class TaskWithTimeoutWrapper
    {
        protected volatile bool taskFinished = false;

        public async Task<T> RunWithCustomTimeoutAsync<T>(int millisecondsToTimeout, Func<Task<T>> taskFunc, CancellationTokenSource cancellationTokenSource = null)
        {
            this.taskFinished = false;

            var results = await Task.WhenAll<T>(new List<Task<T>>
        {
            this.RunTaskFuncWrappedAsync<T>(taskFunc),
            this.DelayToTimeoutAsync<T>(millisecondsToTimeout, cancellationTokenSource)
        });

            return results[0];
        }

        public async Task RunWithCustomTimeoutAsync(int millisecondsToTimeout, Func<Task> taskFunc, CancellationTokenSource cancellationTokenSource = null)
        {
            this.taskFinished = false;

            await Task.WhenAll(new List<Task>
        {
            this.RunTaskFuncWrappedAsync(taskFunc),
            this.DelayToTimeoutAsync(millisecondsToTimeout, cancellationTokenSource)
        });
        }

        protected async Task DelayToTimeoutAsync(int millisecondsToTimeout, CancellationTokenSource cancellationTokenSource)
        {
            await Task.Delay(millisecondsToTimeout);

            this.ActionOnTimeout(cancellationTokenSource);
        }

        protected async Task<T> DelayToTimeoutAsync<T>(int millisecondsToTimeout, CancellationTokenSource cancellationTokenSource)
        {
            await this.DelayToTimeoutAsync(millisecondsToTimeout, cancellationTokenSource);

            return default(T);
        }

        protected virtual void ActionOnTimeout(CancellationTokenSource cancellationTokenSource)
        {
            if (!this.taskFinished)
            {
                cancellationTokenSource?.Cancel();
                throw new TimeoutException("Timeout");
            }
        }

        protected async Task RunTaskFuncWrappedAsync(Func<Task> taskFunc)
        {
            await taskFunc.Invoke();

            this.taskFinished = true;
        }

        protected async Task<T> RunTaskFuncWrappedAsync<T>(Func<Task<T>> taskFunc)
        {
            var result = await taskFunc.Invoke();

            this.taskFinished = true;

            return result;
        }
    }
}
