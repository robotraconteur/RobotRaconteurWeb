using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{

    static class AttributesTest
    {
        static public async Task RunAttributesTest()
        {
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                var urls = nodes.GetServiceUrl("RobotRaconteurTestService");

                var c = (com.robotraconteur.testing.TestService1.testroot)await nodes.client_node.ConnectService(urls[0]);
                var attr = nodes.client_node.GetServiceAttributes(c);

                RRAssert.IsTrue(attr != null);
                RRAssert.Equals(attr.Count, 2);
                RRAssert.Equals((string)attr["test"], "This is a test attribute");
                RRAssert.Equals((int[])attr["test2"], new int[] { 42 });
            }
        }
    }
}
