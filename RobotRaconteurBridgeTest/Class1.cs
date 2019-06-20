using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RobotRaconteurWeb;

namespace RobotRaconteurBridgeTest
{
    [RobotRaconteurWeb.RobotRaconteurServiceObjectInterface("blah")]
    public static class RegisterServiceTypes
    {
        public static void DoRegisterServiceTypes()
        {
            RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
            RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());
            RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService3.com__robotraconteur__testing__TestService3Factory());
        }
    }
}
