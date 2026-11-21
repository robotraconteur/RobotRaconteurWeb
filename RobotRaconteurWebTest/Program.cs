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

// cSpell: ignore subscriptioncitests, citests

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
                    await TestServer.RunServer(args[2], args[1]);
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
                        await RobotRaconteurSubTest.SubscriptionTests.RunTestSubscribeMultiServiceSameNode();

                        await RobotRaconteurSubTest.SubscriptionTests.RunTestServiceInfo2ServiceRemoved();

                        await RobotRaconteurSubTest.SubscriptionTests.RunTestIntraServiceRegisteredLater();

                        await RobotRaconteurSubTest.ConnectServiceTests.TestConnectServiceUrlsWithListener();

                        await RobotRaconteurSubTest.ConnectFailureTests.TestFailedConnectDoesNotLeakEndpoint();


                        await RobotRaconteurSubTest.SubscriberFilterTests.TestSubscriberFilter();
                        await RobotRaconteurSubTest.SubscriberFilterTests.RunSubscriberAttributeFilter();
                        RobotRaconteurSubTest.SubscriberFilterTests.TestSubscriptionAttributeIdentifier();
                        RobotRaconteurSubTest.SubscriberFilterTests.TestAttributeGroupSplitStringDelimiter();

                        await RobotRaconteurSubTest.Pipes.SubscriptionPipeTests.TestPipeSubscription();
                        await RobotRaconteurSubTest.Pipes.PipeClientTests.TestPipeReconnectAfterClose();
                        await RobotRaconteurSubTest.Wires.SubscriptionWireTests.TestWireSubscription();
                        await RobotRaconteurSubTest.Wires.WireMemberTests.TestWaitOutValueValid();

                        await RobotRaconteurSubTest.Wires.WireSubscriptionServicePathTests.TestWildcardServicePathMultipleServiceNames();

                        await RobotRaconteurSubTest.Wires.WireServerCloseTests.TestServiceSideWireClose();
                        await RobotRaconteurSubTest.Manager.SubscriptionManagerTests.RunTestSubManagerSubscribeByType();
                        await RobotRaconteurSubTest.Manager.SubscriptionManagerTests.RunTestSubManagerSubscribeByUrl();

                        Console.WriteLine("Done!");

                        break;
                    }
                case "citests":
                    {
                        MessageSerializationTest.RunMessageTests();
                        ErrorMappingTests.TestArgumentOutOfRangeMapping();

                        await ServiceTests.RunIntraLoopback();
                        await ServiceTests.RunTcpLoopback();
                        await ServiceTests.RunLocalLoopback();

                        await AttributesTest.RunAttributesTest();
                        await ServiceLockAsyncTests.RunTestLockAfterAsyncGetter();
                        await MemoryReplyTest.TestMemoryWriteReplyType();

                        await TimerTest.TestWallRateDefaultNode();

                        MultiDimArrayTest.TestCopyRangeOverflow();

                        MultiDimArrayTest.TestInvalidPosLength();


                        ServicePathTest.RunServicePathTest();

                        await TcpTransportTests.TestWebSocketHandshakeHeaderLineLimit();

                        await WebSocketTests.TestWebSocketOversizedControlFrame();

                        await AsyncStreamTransportTests.TestWirePacketBurstDoesNotStall();

                        LocalTransportTests.TestDiscoveryListeningReenable();

                        await LocalTransportDiscoveryTests.TestGetDetectedNodesNodeName();

                        await StreamHeaderTests.TestOversizedMessageLengthRejected();

                        RobDefTest.TestConstantInt64();

                        RobDefTest.TestConstantSignedRange();

                        RobDefTest.TestConstantSignedZero();

                        MessageSerializationTest.TestVarValueComplexBool();

                        RobDefTest.TestTypeDefinitionEquality();

                        RobDefTest.TestTypeDefinitionMultiDimSingleToString();

                        await WebSocketFrameTests.TestWebSocketZeroLengthFrames();

                        await RobotRaconteurSubTest.SubscriptionTests.RunTestSubscribeByType();
                        await RobotRaconteurSubTest.SubscriptionTests.RunTestSubscribeByUrl();
                        await RobotRaconteurSubTest.SubscriptionTests.RunTestSubscribeByUrlBadUrl();
                        await RobotRaconteurSubTest.SubscriptionTests.RunTestSubscribeServiceInfo2();
                        await RobotRaconteurSubTest.SubscriptionTests.RunTestSubscribeMultiServiceSameNode();

                        await RobotRaconteurSubTest.SubscriptionTests.RunTestServiceInfo2ServiceRemoved();

                        await RobotRaconteurSubTest.SubscriptionTests.RunTestIntraServiceRegisteredLater();

                        await RobotRaconteurSubTest.ConnectServiceTests.TestConnectServiceUrlsWithListener();

                        await RobotRaconteurSubTest.ConnectFailureTests.TestFailedConnectDoesNotLeakEndpoint();


                        await RobotRaconteurSubTest.SubscriberFilterTests.TestSubscriberFilter();
                        await RobotRaconteurSubTest.SubscriberFilterTests.RunSubscriberAttributeFilter();
                        RobotRaconteurSubTest.SubscriberFilterTests.TestSubscriptionAttributeIdentifier();
                        RobotRaconteurSubTest.SubscriberFilterTests.TestAttributeGroupSplitStringDelimiter();

                        await RobotRaconteurSubTest.Pipes.SubscriptionPipeTests.TestPipeSubscription();
                        await RobotRaconteurSubTest.Pipes.PipeClientTests.TestPipeReconnectAfterClose();
                        await RobotRaconteurSubTest.Wires.SubscriptionWireTests.TestWireSubscription();
                        await RobotRaconteurSubTest.Wires.WireMemberTests.TestWaitOutValueValid();

                        await RobotRaconteurSubTest.Wires.WireSubscriptionServicePathTests.TestWildcardServicePathMultipleServiceNames();

                        await RobotRaconteurSubTest.Wires.WireServerCloseTests.TestServiceSideWireClose();
                        await RobotRaconteurSubTest.SubObject.SubObjectSubscriptionTests.RunTestSubscribeSubObject();
                        await RobotRaconteurSubTest.Manager.SubscriptionManagerTests.RunTestSubManagerSubscribeByType();
                        await RobotRaconteurSubTest.Manager.SubscriptionManagerTests.RunTestSubManagerSubscribeByUrl();
                        Console.WriteLine("Done!");
                        break;
                    }
                case "multidimarray":
                    {
                        MultiDimArrayTest.TestInvalidPosLength();
                        break;
                    }
                case "multidimarray":
                    {
                        MultiDimArrayTest.TestCopyRangeOverflow();
                        break;
                    }

                case "filterdelimiter":
                    {
                        RobotRaconteurSubTest.SubscriberFilterTests.TestAttributeGroupSplitStringDelimiter();
                        break;
                    }
                case "timer":
                    {
                        await TimerTest.TestWallRateDefaultNode();
                        break;
                    }
                case "memoryreply":
                    {
                        await MemoryReplyTest.TestMemoryWriteReplyType();
                        break;
                    }
                case "message":
                    {
                        MessageSerializationTest.RunMessageTests();
                        break;
                    }
                case "servicepath":
                    {
                        ServicePathTest.RunServicePathTest();
                        break;
                    }
                case "httpserver":
                    {
                        await HttpServerTest.RunHttpServer();
                        break;
                    }
                default:
                    throw new ArgumentException("Invalid test command " + command);
            }
        }
    }
}
