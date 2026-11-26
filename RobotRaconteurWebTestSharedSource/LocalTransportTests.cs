using System;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    public class LocalTransportTests
    {
        // Regression test: LocalTransport node discovery listening can be disabled and enabled again
        public static void TestDiscoveryListeningReenable()
        {
            var node = new RobotRaconteurNode();
            var t = new LocalTransport(node);
            node.RegisterTransport(t);
            try
            {
                if (!t.IsLocalTransportSupported)
                    return;
                t.EnableNodeDiscoveryListening();
                t.DisableNodeDiscoveryListening();
                bool reenabled = false;
                try
                {
                    t.EnableNodeDiscoveryListening();
                    reenabled = true;
                }
                catch (InvalidOperationException) { }
                RRAssert.IsTrue(reenabled);
                t.DisableNodeDiscoveryListening();
            }
            finally
            {
                node.Shutdown();
            }
        }
    }
}
