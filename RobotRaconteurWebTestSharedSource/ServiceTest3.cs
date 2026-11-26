using System;
using System.Threading;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService5;
using RobotRaconteurWeb;

namespace RobotRaconteurTest
{
    // Port of Core's test/cpp/service/ServiceTest3.cpp ("RobotRaconteurTestService3")
    public class RobotRaconteurTestServiceSupport3
    {
        public asynctestroot_impl testservice3;

        public RobotRaconteurNode node;
        public RobotRaconteurTestServiceSupport3(RobotRaconteurNode node = null)
        {
            this.node = node;
            if (node == null)
            {
                this.node = RobotRaconteurNode.s;
            }
        }

        public void RegisterServices()
        {
            testservice3 = new asynctestroot_impl();
            node.RegisterService("RobotRaconteurTestService3", "com.robotraconteur.testing.TestService5", testservice3);
        }

        public void UnregisterServices()
        {
            node.CloseService("RobotRaconteurTestService3");
        }
    }

    // Members not overridden here throw NotImplementedException, as in Core
    public class asynctestroot_impl : asynctestroot_default_impl
    {
        public override Task<double> get_d1(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(8.5515);
        }

        public override Task set_d1(double value, CancellationToken cancel = default(CancellationToken))
        {
            if (value != 3.0819) throw new ArgumentException("");
            return Task.FromResult(0);
        }

        public override Task<double> get_err(CancellationToken cancel = default(CancellationToken))
        {
            throw new ArgumentException("Test message 1");
        }

        public override Task set_err(double value, CancellationToken cancel = default(CancellationToken))
        {
            throw new InvalidOperationException("Test message 2");
        }

        public override Task f1(CancellationToken rr_cancel = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public override Task f2(int a, CancellationToken rr_cancel = default(CancellationToken))
        {
            if (a != 247) throw new ArgumentException("");
            return Task.FromResult(0);
        }

        public override Task<int> f3(double a, double b, CancellationToken rr_cancel = default(CancellationToken))
        {
            return Task.FromResult(checked((int)(a + b)));
        }

        public override Task err_func(CancellationToken rr_cancel = default(CancellationToken))
        {
            throw new InvalidOperationException("");
        }

        public override Task<double> err_func2(CancellationToken rr_cancel = default(CancellationToken))
        {
            throw new asynctestexp("");
        }
    }
}
