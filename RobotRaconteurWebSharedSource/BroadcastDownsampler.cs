using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobotRaconteurWeb
{
    /**
    <summary>
    Downsampler to manage rate of packets sent to client
    </summary>
    <remarks>
    <para>
    PipeBroadcaster and WireBroadcaster by default sends packets to all clients when
    a pipe packet is sent or the wire value is changed. The updates typically happen
    within a sensor or control loop, with the rate set by the specific device producing
    the updates. Some clients may require less frequent data, and may run in to bandwidth
    or processing issues if the data is sent at the full update rate. The BroadcastDownsampler
    is used to implement broadcaster predicates that will drop packets.
    Clients specify how many packets they want dropped between each packet sent. For instance,
    a downsample of 0 means that no packets are dropped. A downsample of 1 will drop every
    other
    packet. A downsample of two will drop 2 packets between sending 1 packet, etc. The
    downsample level for each client is set using SetClientDownsample(). This should be
    made available to the client using a property member.
    </para>
    <para>
    PipeBroadcaster and WireBroadcaster must be added to the downsampler
    using AddPipeBroadcaster() and AddWireBroadcaster(), respectively.
    It is recommended that these functions be called within
    the RRServiceObjectInit(context,servicepath) function thit is called
    by the node when a service object is initialized.
    </para>
    <para>
    BeginStep() and EndStep() must be called for each iteration of the
    broadcasting loop. Use BroadcastDownsamplerStep for automatic
    management in the loop.
    </para>
    <para>See com.robotraconteur.isoch.IsochDevice for the standard use
    of downsampling.
    </para>
    </remarks>
    */
    [PublicApi]
    public class BroadcastDownsampler
    {
        protected internal ServerContext context;
        protected internal uint default_downsample;
        protected internal ulong step_count;
        protected internal Dictionary<uint, uint> client_downsamples = new Dictionary<uint, uint>();
        /**
        <summary>
        Construct a new BroadcastDownsampler
        </summary>
        <remarks>None</remarks>
        <param name="context">The service context</param>
        <param name="default_downsample">The default downsample. Usually set to 0</param>
        */
        [PublicApi]
        public BroadcastDownsampler(ServerContext context, uint default_downsample = 0)
        {
            this.context = context;
            this.default_downsample = default_downsample;
            context.ServerServiceListener += server_event;
        }
        /**
        <summary>
        Get the downsample for the specified client
        </summary>
        <remarks>None</remarks>
        <param name="ep">The endpoint ID of the client</param>
        <returns>The downsample</returns>
        */
        [PublicApi]
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
        /**
        <summary>
        Set the downsample for the specified client
        </summary>
        <remarks>None</remarks>
        <param name="ep">The endpoint ID of the client</param>
        <param name="downsample">The desired downsample</param>
        */
        [PublicApi]
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
        /**
        <summary>
        Begin the update loop step
        </summary>
        <remarks>
        Use BroadcastDownsamplerStep for automatic stepping
        </remarks>
        */
        [PublicApi]
        public void BeginStep()
        {
            lock(this)
            {
                ++step_count;
            }
        }
        /**
        <summary>
        End the update loop step
        </summary>
        <remarks>
        Use BroadcastDownsamplerStep for automatic stepping
        </remarks>
        */
        [PublicApi]
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
    /**
    <summary>
    Class for automatic broadcast downsampler stepping
    </summary>
    <remarks>
    Helper class to automate BroadcastDownsampler stepping.
    Calls BroadcastDownsampler.BeginStep() on construction,
    and BroadcastDownsampler.EndStep() when disposed.
    </remarks>
    */
    [PublicApi]
    public class BroadcasterDownsamplerStep : IDisposable
    {
        BroadcastDownsampler parent;
        /**
        <summary>
        Construct a BroadcastDownsampler
        </summary>
        <remarks>
        Calls BroadcastDownsampler.BeginStep() on downsampler.
        Calls BroadcastDownsampler.EndStep() on downsampler
        when destroyed.
        </remarks>
        <param name="downsampler">The downsampler to step</param>
        */
        [PublicApi]
        public BroadcasterDownsamplerStep(BroadcastDownsampler parent)
        {
            this.parent = parent;
            parent.BeginStep();
        }
        /// <summary>
        /// Release the step
        /// </summary> <remarks>
        /// None
        /// </remarks>
        [PublicApi]
        public void Dispose()
        {
            parent?.EndStep();
            parent = null;
        }
    }

}