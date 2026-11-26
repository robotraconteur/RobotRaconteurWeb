using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService1;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    // A hand-written skel whose object type name contains "_skel"
    class arm_skeleton_skel : ServiceSkel
    {
        public arm_skeleton_skel(string p, object o, ServerContext c) : base(p, o, c) { }
        public override Task<MessageEntry> CallGetProperty(MessageEntry m) { throw new NotImplementedException(); }
        public override Task<MessageEntry> CallSetProperty(MessageEntry m) { throw new NotImplementedException(); }
        public override Task<MessageEntry> CallFunction(MessageEntry m) { throw new NotImplementedException(); }
        public override Task<object> GetSubObj(string name, string ind) { throw new NotImplementedException(); }
        public override void ReleaseCastObject() { }
    }

    static class SkelObjectTypeTest
    {
        static public async Task TestSkelObjectTypeName()
        {
            // Only the "_skel" suffix of the class name may be removed
            var skel = (ServiceSkel)RuntimeHelpers.GetUninitializedObject(typeof(arm_skeleton_skel));
            RRAssert.AreEqual(skel.GetObjectType(), "RobotRaconteurWebTest.arm_skeleton");

            // Generated skels report the object type of the service definition
            using (var nodes = new TestNodeConfig("testprog", false, false, true))
            {
                var ctx = nodes.node.GetService("RobotRaconteurTestService");
                RRAssert.AreEqual(ctx.RootObjectType, "com.robotraconteur.testing.TestService1.testroot");
                var c = (ServiceStub)await nodes.client_node.ConnectService(nodes.GetServiceUrl("RobotRaconteurTestService"));
                await ((testroot)c).get_o1();
                await nodes.client_node.DisconnectService(c);
            }
            Console.WriteLine("SkelObjectTypeTest.TestSkelObjectTypeName passed");
        }
    }
}
