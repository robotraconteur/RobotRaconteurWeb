using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobotRaconteurWeb
{
    public class BroadcastDownsampler
    {
        protected internal ServerContext context;
        protected internal uint default_downsample;
        protected internal ulong step_count;
        protected internal Dictionary<uint, uint> client_downsamples = new Dictionary<uint, uint>();
        public BroadcastDownsampler(ServerContext context, uint default_downsample = 0)
        {
            this.context = context;
            this.default_downsample = default_downsample;
            context.ServerServiceListener += server_event;
        }

        public uint GetClientDownsample(uint ep)
        {
            lock (this)
            {
                if (client_downsamples.TryGetValue(ep, out var downsample))
                {
                    return downsample;
                }
                else
                {
                    return default_downsample;
                }
            }
        }

        public void SetClientDownsample(uint ep, uint downsample)
            {
                lock (this)
                {
                    if (downsample == 0)
                    {
                        client_downsamples.Remove(ep);
                    }
                    else
                    {
                        client_downsamples[ep] = downsample;
                    }
                }
            }

        public void BeginStep()
        {
            lock(this)
            {
                ++step_count;
            }
        }

        public void EndStep()
        {

        }

        public void AddPipeBroadcaster<T>(PipeBroadcaster<T> broadcaster)
        {
            broadcaster.Predicate = pipe_predicate;
        }

        public void AddWireBroadcaster<T>(WireBroadcaster<T> broadcaster)
        {
            broadcaster.Predicate = wire_predicate;
        }

        bool wire_predicate(object wire, uint ep)
        {
            lock(this)
            {
                var downsample = default_downsample + 1;
                if (client_downsamples.TryGetValue(ep, out uint e))
                {
                    downsample = e + 1;
                }

                var step_count = this.step_count;
                bool drop = (step_count % downsample == 0);
                return drop;
            }
        }

        bool pipe_predicate(object pipe, uint ep, int index)
        {
            lock (this)
            {
                var downsample = default_downsample + 1;
                if (client_downsamples.TryGetValue(ep, out uint e))
                {
                    downsample = e + 1;
                }

                var step_count = this.step_count;
                bool drop = (step_count % downsample == 0);
                return drop;
            }
        }

        void server_event(ServerContext ctx, ServerServiceListenerEventType evt, object param) 
        {
            if (evt != ServerServiceListenerEventType.ClientDisconnected)
                return;

            lock(this)
            {
                client_downsamples.Remove((uint)param);
            }
        }
    }

    public class BroadcasterDownsamplerStep : IDisposable
    {
        BroadcastDownsampler parent;

        public BroadcasterDownsamplerStep(BroadcastDownsampler parent)
        {
            this.parent = parent;
            parent.BeginStep();
        }
        public void Dispose()
        {
            parent?.EndStep();
            parent = null;
        }
    }

}