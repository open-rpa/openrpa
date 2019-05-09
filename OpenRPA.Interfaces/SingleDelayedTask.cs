using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    using System;
    using System.Timers;
    public class SingleDelayedTask
    {
        private readonly Timer _timer = new Timer();
        private ElapsedEventHandler _currentHandler;

        public SingleDelayedTask()
        {
            _timer.AutoReset = false;
        }

        /// <summary>
        /// Enqeue <paramref name="callback"/> to be executed after the specified
        /// <paramref name="delay"/>. The currently enqueued callback is removed
        /// from the queue and won't be executed.
        /// </summary>
        public void Post(TimeSpan delay, Action callback)
        {
            Cancel();

            _currentHandler = (obj, args) => {
                callback();
            };
            _timer.Elapsed += _currentHandler;
            _timer.Interval = delay.TotalMilliseconds;
            _timer.Start();
        }

        /// <summary>
        /// Cancels the currently enqueued delayed task if there is one.
        /// </summary>
        public void Cancel()
        {
            _timer.Stop();
            if (_currentHandler != null)
            {
                _timer.Elapsed -= _currentHandler;
                _currentHandler = null;
            }
        }
    }
}
