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
using RobotRaconteurWeb.Extensions;

namespace RobotRaconteurWeb
{
    /**
    <summary>
    A timer to invoke a callback
    </summary>
    <remarks>
    <para>
    Timers invoke a callback at a specified rate. The timer
    can either be one-short, or repeating.
    </para>
    <para> Use RobotRaconteurNode.CreateTimer() to create timers.
    </para>
    </remarks>
    */

        [PublicApi]
    public interface ITimer
    {
        /**
        <summary>
        Start the timer
        </summary>
        <remarks>
        Must be called after RobotRaconteurNode.CreateTimer()
        </remarks>
        */
        [PublicApi] 
        void Start();
        /**
        <summary>
        Stop the timer
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi] 
        void Stop();
        /**
        <summary>
        Get/Set the period of the timer in milliseconds
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi] 
        int Period {get; set;}
        /**
        <summary>
        Get if the timer is running
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi] 
        bool IsRunning { get; }        
    }

    /**
    <summary>
    Rate to stabilize a loop
    </summary>
    <remarks>
    Rate is used to stabilize the period of a loop. Use
    RobotRaconteur.CreateRate() to create rates.
    </remarks>
    */

        [PublicApi]
    public interface IRate
    {
        /**
        <summary>
        Sleep the calling thread until the current loop period expires
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        Task Sleep();
    }
    /**
    <summary>
    Timer event structure
    </summary>
    <remarks>
    Contains information about the state of the timer. Passed to the
    callback on invocation.
    </remarks>
    */

        [PublicApi]
    public struct TimerEvent
    {
        /**
        <summary>
        True if timer has been stopped
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public bool stopped;
        /**
        <summary>
        The last expected callback invocation time
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public DateTime last_expected;
        /**
        <summary>
        The last real callback invocation time
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public DateTime last_real;
        /**
        <summary>
        The current expected callback invocation time
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public DateTime current_expected;
        /**
        <summary>
        The current real callback invocation time
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public DateTime current_real;
    }

#pragma warning disable 1591
    public class WallTimer : ITimer
    {

        protected DateTime start_time;
        protected DateTime actual_last_time;
        protected DateTime last_time;
        protected bool oneshot;
        
        protected RobotRaconteurNode node;
        protected Action<TimerEvent> handler;
        protected CancellationTokenSource cancel;

        public WallTimer(int period, Action<TimerEvent> handler, bool oneshot, RobotRaconteurNode node = null)
        {
            if (node == null)
            {
                this.node = RobotRaconteurNode.s;
            }
            else
            {
                this.node = node;
            }

            this.Period = period;
            this.handler = handler;
            this.oneshot = oneshot;
        }

        public void Start()
        {
            lock (this)
            {
                if (IsRunning) throw new InvalidOperationException("Already running");
                start_time = node.UtcNow;
                last_time = start_time;
                actual_last_time = last_time;

                cancel = new CancellationTokenSource();
                if (!oneshot)
                {
                    var noop = PeriodicTask.Run(periodic_handler, TimeSpan.FromMilliseconds(Period), cancel.Token);
                }
                else
                {                    
                    RunOne().IgnoreResult();
                }
                
            }
        }

        private async Task RunOne()
        {
            await Task.Delay(Period).ConfigureAwait(false);
            
            if (!cancel.IsCancellationRequested)
            {
                lock (this)
                {
                    cancel.Cancel();
                    cancel = null;
                    var e = new TimerEvent
                    {
                        stopped = IsRunning,
                        last_expected = this.last_time,
                        last_real = this.actual_last_time,
                        current_expected = last_time + TimeSpan.FromMilliseconds(Period),
                        current_real = node.UtcNow
                    };
                    handler(e);
                }
            }
        }

        private void periodic_handler()
        {
            TimerEvent e;
            lock (this)
            {
                e = new TimerEvent
                {
                    stopped = IsRunning,
                    last_expected = this.last_time,
                    last_real = this.actual_last_time,
                    current_expected = last_time + TimeSpan.FromMilliseconds(Period),
                    current_real = node.UtcNow
                };

                this.last_time = e.current_expected;
                this.actual_last_time = e.last_real;
            }
            handler(e);
        }

        public void Stop()
        {
            lock (this)
            {
                if (!IsRunning) throw new InvalidOperationException("Not running");
                if (cancel != null) cancel.Cancel();
                cancel = null;
            }            
        }

        public int Period {get; set;}        

        public bool IsRunning
        {
            get
            {
                lock (this)
                {
                    if (cancel == null) return false;
                    return !cancel.IsCancellationRequested;
                }
            }
        }

#if !ROBOTRACONTEUR_H5
        ~WallTimer()
        {
            try
            {
                var c = cancel;
                if (c != null)
                    c.Cancel();
            }
            catch { }
        }
#endif
    }

    public class WallRate : IRate
    {
        protected RobotRaconteurNode node;
        protected double period; 
        protected DateTime start_time;
        protected DateTime last_time;

        public WallRate(double frequency, RobotRaconteurNode node=null)
        {
            if (node == null)
            {
                this.node = RobotRaconteurNode.s;
            }
            else
            {
                this.node = node;
            }

            this.period = (double)(1000.0 / frequency);
            last_time = node.UtcNow;
            start_time = node.UtcNow;
        }

        public async Task Sleep()
        {
            var p2 = last_time + TimeSpan.FromMilliseconds(period);
            try
            {
                await Task.Delay((int)(p2 - node.UtcNow).TotalMilliseconds).ConfigureAwait(false);
            }
            catch { }
            last_time = p2;            
        }
    }
#pragma warning restore 1591

}
