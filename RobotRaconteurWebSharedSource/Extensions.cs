// Copyright 2011-2019 Wason Technology, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobotRaconteurWeb.Extensions
{
    public static class Extensions
    {
        public static async Task AwaitWithTimeout(this Task task, int timeout)
        {
            if (timeout < 0)
            {
                await task;
                return;
            }

            var c = new CancellationTokenSource();
            Task timeout_task = Task.Delay(timeout,c.Token);

            var r1 = await Task.WhenAny(task, timeout_task);
            if (task.IsCompleted || task.IsFaulted || task.IsCanceled)
            {

                var noop = timeout_task.IgnoreResult();
                c.Cancel();

                await task;
                return;
            }
            else
            {
                var noop = task.IgnoreResult();
                throw new TimeoutException("Operation timed out");
            }           
        }

        public static async Task<T> AwaitWithTimeout<T>(this Task<T> task, int timeout)
        {
            if (timeout < 0)
            {
                return await task;
            }

            var c = new CancellationTokenSource();
            Task timeout_task = Task.Delay(timeout, c.Token);

            var r1 = await Task.WhenAny(task, timeout_task);
            if (task.IsCompleted || task.IsFaulted || task.IsCanceled)
            {
                var noop = timeout_task.IgnoreResult();
                c.Cancel();

                return await task;
            }
            else
            {
                var noop = task.IgnoreResult();
                throw new TimeoutException("Operation timed out");
            }
        }

        public static void AttachCancellationToken<T>(this TaskCompletionSource<T> source, CancellationToken cancel, Exception e=null)
        {
            cancel.Register(delegate()
            {
                if (e == null)
                {
                    source.TrySetCanceled();
                }
                else
                {
                    source.TrySetException(e);
                }
            });
        }

        public static Task IgnoreResult(this Task t)
        {
            return t.ContinueWith(delegate(Task t2)
            {
                try
                {
                    var e = t2.Exception;
                }
                catch (Exception) { }
            });
        }

        public static Task IgnoreResult<T>(this Task<T> t)
        {
            return t.ContinueWith(delegate(Task<T> t2)
            {
                try
                {
                    var e = t2.Result;
                }
                catch (Exception) { }
            });
        }

        public static IAsyncResult AsApm<T>(this Task<T> task,
                                    AsyncCallback callback,
                                    object state)
        {
            if (task == null)
                throw new ArgumentNullException("task");

            var tcs = new TaskCompletionSource<T>(state);
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(t.Result);

                if (callback != null)
                    callback(tcs.Task);
            });
            return tcs.Task;
        }

        public static IAsyncResult AsApm(this Task task,
                                    AsyncCallback callback,
                                    object state)
        {
            if (task == null)
                throw new ArgumentNullException("task");

            var tcs = new TaskCompletionSource<int>(state);
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(0);

                if (callback != null)
                    callback(tcs.Task);
            });
            return tcs.Task;
        }

        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
    }
}
