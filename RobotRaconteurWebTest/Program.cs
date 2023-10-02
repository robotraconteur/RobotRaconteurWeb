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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService1;
using RobotRaconteurWeb;
using RobotRaconteurWebTest;

namespace RobotRaconteurTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string command = args[0];

            switch (command)
            {
                case "loopback":
                case "tcploopback":
                case "intraloopback":
                case "localloopback":
                case "client":
                    await ServiceTests.RunServiceTest(args);
                    break;
                case "server":
                    await TestServer.RunServer(args[2], uint.Parse(args[1]));
                    break;
                case "findservicebytype":
                case "findnodebyid":
                case "findnodebyname":
                    await DiscoveryTests.RunDiscoveryProgram(args);
                    break;
                case "robdeftest":
                    RobDefTest.RunRobDefTest(args);
                    break;
                case "subscribertest":
                case "subscriberurltest":
                case "subscriberfiltertest":
                    {
                        await SubscriberTestProgram.RunSubscriberTest(args);
                        break;
                    }
                case "serviceinfo2subscribertest":
                    {
                        await ServiceInfo2SubscriberTestProgram.RunServiceInfo2SubscriberTest(args);
                        break;
                    }
                case "subscriptioncitests":
                {
                        await RobotRaconteurSubTest.SubscriptionTests.RunTestSubscribeByType();
                        await RobotRaconteurSubTest.SubscriptionTests.RunTestSubscribeByUrl();
                        await RobotRaconteurSubTest.SubscriptionTests.RunTestSubscribeByUrlBadUrl();
                        await RobotRaconteurSubTest.SubscriptionTests.RunTestSubscribeServiceInfo2();


                        await RobotRaconteurSubTest.SubscriberFilterTests.TestSubscriberFilter();
                        await RobotRaconteurSubTest.SubscriberFilterTests.RunSubscriberAttributeFilter();

                        await RobotRaconteurSubTest.Pipes.SubscriptionPipeTests.TestPipeSubscription();
                        await RobotRaconteurSubTest.Wires.SubscriptionWireTests.TestWireSubscription();

                        Console.WriteLine("Done!");

                        break;
                }
                default:
                    throw new ArgumentException("Invalid test command " + command);
            }
        }
    }
}
