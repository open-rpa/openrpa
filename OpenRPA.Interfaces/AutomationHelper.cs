using FlaUI.Core.AutomationElements;
using FlaUI.Core.AutomationElements.Infrastructure;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public class AutomationHelper
    {
        public static System.Threading.SynchronizationContext syncContext { get; set; }
        public static TResult Send<TResult>(Func<TResult> func)
        {
            TResult retval = default(TResult);
            syncContext.Send(new System.Threading.SendOrPostCallback((x) =>
            {
                retval = func();
            })
            , null);
            return retval;
        }
        //public static TResult Send<TResult>(Func<TResult> func)
        //{
        //    TResult retval = default(TResult);
        //    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
        //    {
        //        retval = func();
        //    });
        //    return retval;
        //}
        public static Task<T> RunSTAThread<T>(Func<T> action, TimeSpan Timeout)
        {
            var tcs = new TaskCompletionSource<T>();
            System.Threading.Thread runThread = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
            {
                try
                {
                    var result = action();
                    tcs.TrySetResult(result);
                }
                catch (System.Threading.ThreadAbortException)
                {
                    // Do NOT set it here, or applicatin will crash with an UnhandledException
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    tcs.TrySetException(ex);
                }
            }));
            runThread.Name = "STAThread";
            runThread.SetApartmentState(System.Threading.ApartmentState.STA);
            runThread.Start();
            if (!runThread.Join(Timeout))
            {
                runThread.Abort();
                tcs.TrySetResult(default(T));
            }
            return tcs.Task;
        }
        public static UIElement GetFromPoint(int X, int Y)
        {
            try
            {
                using (var automation = AutomationUtil.getAutomation())
                {
                    try
                    {
                        if (automation.FromPoint(new System.Drawing.Point(X, Y)) is AutomationElement rawElement)
                        {
                            return new UIElement(rawElement);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                    try
                    {
                        var task = Task.Run(() =>
                        {
                            if (automation.FromPoint(new System.Drawing.Point(X, Y)) is AutomationElement rawElement)
                            {
                                return new UIElement(rawElement);
                            }
                            return null;
                        });
                        if (task.Wait(TimeSpan.FromMilliseconds(200)))
                            return task.Result;
                        else
                            return null;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return null;
        }
        public static UIElement GetFromFocusedElement()
        {
            using (var automation = AutomationUtil.getAutomation())
            {
                if (automation.FocusedElement() is AutomationElement rawElement)
                {
                    return new UIElement(rawElement);
                }
            }
            return null;
        }
        public static void init()
        {
            AutomationElement[] elements = { };
        }
    }
}
