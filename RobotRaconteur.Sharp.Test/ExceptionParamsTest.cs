using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService3;
using RobotRaconteurSharp;
using RobotRaconteurTest;

namespace RobotRaconteurSharp.Test
{
    // Service object that throws exceptions carrying an error sub-name and error parameter,
    // matching what Core's ServiceTest2.cpp sends for test_exception_params1/2
    class exception_params_testroot3_impl : testroot3_impl
    {
        public override Task test_exception_params1(CancellationToken rr_cancel = default(CancellationToken))
        {
            var param = new Dictionary<string, object>();
            param.Add("param1", new int[] { 10 });
            param.Add("param2", "20");
            // As Core's test service (InvalidOperationException with sub-name and param); a plain .NET exception
            // carries them in Exception.Data, as in the RobotRaconteurNET wrapper
            var e = new InvalidOperationException("test error");
            e.Data["ErrorSubName"] = "my_error";
            e.Data["ErrorParam"] = param;
            throw e;
        }

        public override Task test_exception_params2(CancellationToken rr_cancel = default(CancellationToken))
        {
            var param = new Dictionary<string, object>();
            param.Add("param1", new int[] { 30 });
            param.Add("param2", "40");
            throw new test_exception4("test error2", "my_error2", param);
        }
    }

    static class ExceptionParamsTest
    {
        static void CheckParam(object param, int param1, string param2)
        {
            var param_map = param as Dictionary<string, object>;
            RRAssert.IsTrue(param_map != null);
            RRAssert.AreEqual(param_map.Count, 2);
            RRAssert.AreEqual(((int[])param_map["param1"])[0], param1);
            RRAssert.AreEqual((string)param_map["param2"], param2);
        }

        static public async Task RunExceptionParamsTest()
        {
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                nodes.node.RegisterService("ExceptionParamsTestService", "com.robotraconteur.testing.TestService3",
                                           new exception_params_testroot3_impl());
                var urls = nodes.GetServiceUrl("ExceptionParamsTestService");

                var c = (testroot3)await nodes.client_node.ConnectService(urls[0]);

                bool exp1_caught = false;
                try
                {
                    await c.test_exception_params1();
                }
                catch (InvalidOperationException e)
                {
                    exp1_caught = true;
                    RRAssert.AreEqual(e.Message, "test error");
                    RRAssert.AreEqual(e.Data["ErrorSubName"] as string, "my_error");
                    CheckParam(e.Data["ErrorParam"], 10, "20");
                }
                RRAssert.IsTrue(exp1_caught);

                bool exp2_caught = false;
                try
                {
                    await c.test_exception_params2();
                }
                catch (test_exception4 e)
                {
                    exp2_caught = true;
                    RRAssert.AreEqual(e.Error, "com.robotraconteur.testing.TestService3.test_exception4");
                    RRAssert.AreEqual(e.Message, "test error2");
                    RRAssert.AreEqual(e.ErrorSubName, "my_error2");
                    CheckParam(e.ErrorParam, 30, "40");
                }
                RRAssert.IsTrue(exp2_caught);

                await nodes.client_node.DisconnectService(c);
            }
        }
    }
}
