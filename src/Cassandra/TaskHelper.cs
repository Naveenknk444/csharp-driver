﻿using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Cassandra
{
    internal static class TaskHelper
    {
        private static readonly Action<Exception> PreserveStackHandler = (Action<Exception>)Delegate.CreateDelegate(
            typeof(Action<Exception>),
            typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic));

        /// <summary>
        /// Returns an AsyncResult according to the .net async programming model (Begin)
        /// </summary>
        public static Task<TResult> ToApm<TResult>(this Task<TResult> task, AsyncCallback callback, object state)
        {
            if (task.AsyncState == state)
            {
                if (callback != null)
                {
                    task.ContinueWith((t) => callback(t), TaskContinuationOptions.ExecuteSynchronously);
                }
                return task;
            }

            var tcs = new TaskCompletionSource<TResult>(state);
            task.ContinueWith(delegate
            {
                if (task.IsFaulted)
                {
                    tcs.TrySetException(task.Exception.InnerExceptions);
                }
                else if (task.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(task.Result);
                }

                if (callback != null)
                {
                    callback(tcs.Task);
                }

            }, TaskContinuationOptions.ExecuteSynchronously);
            return tcs.Task;
        }

        /// <summary>
        /// Returns a faulted task with the provided exception
        /// </summary>
        public static Task<TResult> FromException<TResult>(Exception exception)
        {
            var tcs = new TaskCompletionSource<TResult>();
            tcs.SetException(exception);
            return tcs.Task;
        }

        /// <summary>
        /// Waits the task to transition to RanToComplete.
        /// It throws the inner exception of the AggregateException in case there is a single exception.
        /// It throws the Aggregate exception when there is more than 1 inner exception.
        /// It throws a TimeoutException when the task didn't complete in the expected time.
        /// </summary>
        /// <param name="task">the task to wait upon</param>
        /// <param name="timeout">timeout in milliseconds</param>
        /// <exception cref="TimeoutException" />
        /// <exception cref="AggregateException" />
        public static T WaitToComplete<T>(Task<T> task, int timeout = System.Threading.Timeout.Infinite)
        {
            //It should wait and throw any exception
            try
            {
                task.Wait(timeout);
            }
            catch (AggregateException ex)
            {
                ex = ex.Flatten();
                //throw the actual exception when there was a single exception
                if (ex.InnerExceptions.Count == 1)
                {
                    throw PreserveStackTrace(ex.InnerExceptions[0]);
                }
                else
                {
                    throw;
                }
            }
            if (task.Status != TaskStatus.RanToCompletion)
            {
                throw new TimeoutException("The task didn't complete before timeout.");
            }
            return task.Result;
        }

        /// <summary>
        /// Attempts to transition the underlying Task to RanToCompletion or Faulted state.
        /// </summary>
        public static void TrySet<T>(this TaskCompletionSource<T> tcs, Exception ex, T result)
        {
            if (ex != null)
            {
                tcs.TrySetException(ex);
            }
            else
            {
                tcs.TrySetResult(result);
            }
        }

        /// <summary>
        /// Required when retrowing exceptions to mantain the stack trace of the original exception
        /// </summary>
        private static Exception PreserveStackTrace(Exception ex)
        {
            PreserveStackHandler(ex);
            return ex;
        }
    }
}