﻿// Copyright 2011-2019 Wason Technology, LLC
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
using System.Threading.Tasks;

namespace RobotRaconteurWeb
{
    public class AsyncMutex
    {
        Queue<TaskCompletionSource<int>> waiting_tasks=new Queue<TaskCompletionSource<int>>();
        Task current = null;


        public class LockHandle : IDisposable
        {
            Task task = null;
            AsyncMutex owner = null;
            internal LockHandle(AsyncMutex owner, Task task)
            {
                this.task = task;
                this.owner = owner;
            }
            
            public void Dispose()
            {
                owner.Exit(task);
            }

        }

        public async Task<IDisposable> Lock()
        {
            var t =Enter();
            await t;
            return new LockHandle(this, t);
        }

        public async Task<IDisposable> LockWithTimeout(int timeout)
        {
            Task t = null;            
            t = Enter();
            await Task.WhenAny(t, Task.Delay(timeout));
            if (t.IsCompleted || t.IsFaulted || t.IsCanceled)
            {
                await t;
                return new LockHandle(this, t);
            }

            Task noop=t.ContinueWith(delegate(Task x)
            {
                var e=x.Exception;
                try
                {
                    Exit(x);
                }
                catch { }
            });

            throw new TimeoutException("Timeout wating for mutex lock");
            
        }

        public Task Enter()
        {
            var t = new TaskCompletionSource<int>();
            lock (this)
            {
                if (current == null)
                {
                    t.TrySetResult(0);
                    current = t.Task;
                    return t.Task;
                }
                else
                {
                    waiting_tasks.Enqueue(t);
                    return t.Task;
                }

            }
        }

        public void Exit(Task t)
        {
            TaskCompletionSource<int> c=null;
            lock (this)
            {
                if (! Object.ReferenceEquals(t, current))
                {
                    throw new InvalidOperationException("Invalid task to release mutex");
                }
                if (waiting_tasks.Count > 0)
                {
                    c = waiting_tasks.Dequeue();
                    current = c.Task;
                }
                else
                {
                    current = null;
                }
            }
            if (c != null)
            {
                c.TrySetResult(0);
            }
        }

        public void CancelAll()
        {
            var c2= new Queue<TaskCompletionSource<int>>();
            lock (this)
            {
                while (waiting_tasks.Count > 0)
                {
                    c2.Enqueue(waiting_tasks.Dequeue());
                }
                
            }

            while (c2.Count > 0)
            {
                c2.Dequeue().TrySetCanceled();
            }
        }

    }
}
