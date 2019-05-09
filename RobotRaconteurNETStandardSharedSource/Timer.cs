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
using RobotRaconteur.Extensions;

namespace RobotRaconteur
{
    public interface ITimer
    {
        void Start();
        void Stop();
        int Period {get; set;}
        bool IsRunning { get; }        
    }

    public interface IRate
    {
        Task Sleep();
    }

    public struct TimerEvent
    {
        public bool stopped;
        public DateTime last_expected;
        public DateTime last_real;
        public DateTime current_expected;
        public DateTime current_real;
    }

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
            await Task.Delay(Period);
            
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

#if !ROBOTRACONTEUR_BRIDGE
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
                await Task.Delay((int)(p2 - node.UtcNow).TotalMilliseconds);
            }
            catch { }
            last_time = p2;            
        }
    }

}
