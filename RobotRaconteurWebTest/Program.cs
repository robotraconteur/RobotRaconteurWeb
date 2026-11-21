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
                        await ServicePathAliasTests.RunTestServicePathAliasLock();
                        await LockReleaseOnLogoutTests.RunTestLockReleaseOnLogout();
                        await AllowObjectLockPolicyTests.RunTestAllowObjectLockPolicy();
                        await PreAuthMessageLimitTests.RunTestPreAuthMessageLimit();
                        await SkelObjectTypeTest.TestSkelObjectTypeName();

                        await RobotRaconteurSubTest.DefaultClientTest.TestDefaultClientSkipsConnecting();

                        await RobotRaconteurSubTest.ServiceInfo2RefreshTest.TestServiceInfo2RefreshSameUrls();

                        await RobotRaconteurSubTest.Pipes.PipeSubscriptionBacklogTest.TestPipeSubscriptionMaxBacklog();

                        await RobotRaconteurSubTest.Manager.SubscriptionManagerEnableTest.TestEnableDisabledByType();

                        RobotRaconteurSubTest.IdentifierFullMatchTest.TestIdentifierFullMatch();

                        await RobotRaconteurSubTest.Wires.SubscriptionUpdateFilterTest.TestUpdateServiceByTypeDisconnectsFiltered();

                        MessageSerializationTest.TestPackEnumContainers();

                        PasswordFileNullCredentialTests.RunTestNullCredentials();
                        PasswordFileParseTests.RunTestPasswordFileWhitespace();
                        MessageSerializationTest.TestVarValueNestedContainers();

                        TimeSpecCompareTests.RunTestTimeSpecCompare();
                        await WallTimerEventTests.RunTestWallTimerEvents();
                        await WallRateSleepTests.RunTestWallRateLateSleep();
                        await PulledServiceTypesTest.TestPulledServiceTypesDefinitionText();

                        ScanAssembliesTests.RunTestScanWithPreregisteredType();
                        await PullServiceDefsCycleTest.TestPullServiceDefinitionImportCycle();

                        CommandLineConfigParserTests.RunTestCommandLineOptions();
                        await TransportListenerLeakTest.TestTransportListenerRemoved();

                        NodeDirectoriesNodeIdStoreTests.RunTestOversizedNameFileReplaced();
                        NodeDirectoriesNameFdTests.RunTestNameFileClosedAfterStart();
                        await GeneratorCleanupTest.TestGeneratorCleanup();

                        NodeDirectoriesLockTests.RunTestNodeNameLockInUse();
                        NodeDirectoriesPrefixTests.RunTestSystemPrefix();
                        LocalStartFailTests.RunTestLocalStartFailureCleanup();
                        await SubObjectCurrentPathTest.TestFirstSubObjectRequestCurrentPath();

                        await LocalListenCloseTests.RunTestLocalCloseRemovesSocket();
                        await LocalDiscoveryWatchTests.RunTestLocalDiscoveryDetectsNewServer();
                        await RobotRaconteurSubTest.IntraDetectedNodesTests.RunTestIntraGetDetectedNodes();
                        await ReplyEntryTypeTest.TestUnhandledReplyEntryType();

                        ServiceStateNonceTests.RunTestAnnounceNonce(); await ServiceStateNonceTests.RunTestLocalInfoFileNonce();
                        await CheckServiceCapabilityTest.TestCheckServiceCapability();

                        await RobotRaconteurWebTest.FindServiceByTypeIndexTests.RunTestFindServiceByTypeSkippedNode();
                        await RobotRaconteurWebTest.FindNodeUrlTests.RunTestFindNodeUrls();
                        await ServicePathReleasedTest.TestServicePathReleasedSeparator();

                        await ReleaseServicePathTest.TestReleaseServicePathSeparator();

                        await RobotRaconteurSubTest.Wires.WireWaitValidTest.TestWaitValueValidAlreadyValid();

                        await PipeWaiterTest.TestPipeReceiveWaitReleasesWaiter();

                        await RobotRaconteurSubTest.Wires.WireRepeatConnectTest.TestWireRepeatConnect();

                        await MemoryMaxTransferTest.TestMemoryReadCountLimit();

                        TmpUserRunDirTests.RunTestTmpUserRunDirSecure();
                        await TcpSilentConnectionTests.RunTestSilentConnectionClosed();
                        DiscoveryAnnounceValidationTests.RunTestAnnounceValidation();
                        DiscoveryAnnounceValidationTests.RunTestAnnounceLoopbackSender();
                        await MemoryMaxTransferSizeTests.RunTestMaxTransferSizeTooSmall();
                        await ServiceLockSessionTests.RunTestReleaseClientLockOtherSession();
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

                        await WebSocketFrameTests.TestWebSocketSplitFrameHeader();

                        RobDefTest.TestObjRefArrayContainer();

                        RobDefTest.TestServiceDefinitionFirstLine();

                        RobDefTest.TestUsingAsWhitespace();

                        RobDefTest.TestServiceDefinitionDuplicateStdver();

                        RobDefTest.TestNumberParseException();

                        RobDefTest.TestConstantIntegerValues();

                        RobDefTest.TestConstantStringUnicodeEscape();

                        RobDefTest.TestRecursiveImplementsAndImportsNotThroughRoot();

                        RobDefTest.TestGenSkelKeywordObjectName();

                        RobDefTest.TestGenSingleConstants();

                        RobDefTest.TestGenKeywordWireNames();

                        RobDefTest.TestGenAsyncDelegateReturnType();

                        await AsyncStreamSendQueueCancelTest.TestCancelledQueuedSendDoesNotStall();

                        MultiDimArrayTest.TestZeroCountCopiesNothing();

                        await AsyncStreamStreamOpCancelTest.TestCancelledQueuedStreamOpDoesNotStall();

                        RobDefTest.TestParseErrorLineNumbers();

                        await AsyncStreamConnectFailCloseTest.TestFailedConnectClosesStream();

                        await AsyncStreamStreamOpReplyTest.TestStreamOpReplyForOtherCommandRejected();

                        RobDefTest.TestMemberParentheses();

                        RobDefTest.TestEntryConstantsToString();

                        await AsyncStreamSingleEndpointTest.TestSecondConnectClientClosesConnection();

                        await AsyncStreamStartTlsTimeoutTest.TestSilentServerStartTlsTimesOut();

                        RobDefTest.TestVerifyWithoutStdver();

                        await AsyncStreamStartTlsErrorTest.TestStartTlsErrorReplyReported();

                        RobDefTest.TestImplementsEventMismatch();

                        RobDefTest.TestEnumFirstValueRequired();

                        RobDefTest.TestFloatConstantLeadingDecimalPoint();

                        await TcpDiscoveryListenRestartTest.TestDiscoveryListeningRestart();

                        await TcpWebSocketTargetNodeTest.TestWebSocketConnectChecksNodeID();

                        await TcpWebSocketHeaderCaseTest.TestWebSocketHandshakeHeaderCase();

                        await AsyncStreamStalledWriteTest.TestStalledWriteReceiveTimeout();

                        await ServiceTests.TestPipeDisconnectReplyType();

                        AttributesTest.TestPasswordHashUtf8();

                        AttributesTest.TestCloseServiceUpdatesNonce();

                        AttributesTest.TestSetAttributesUpdatesNonce();

                        await WebSocketFrameTests.TestWebSocketStrictServer();

                        await RobotRaconteurSubTest.SubscriptionTests.RunTestSubscribeByType();
                        await RobotRaconteurSubTest.SubscriptionTests.RunTestSubscribeByUrl();
                        await RobotRaconteurSubTest.SubscriptionTests.RunTestSubscribeByUrlBadUrl();
                        await RobotRaconteurSubTest.SubscriptionTests.RunTestSubscribeServiceInfo2();
                        await RobotRaconteurSubTest.SubscriptionTests.RunTestSubscribeMultiServiceSameNode();

                        await RobotRaconteurSubTest.SubscriptionTests.RunTestServiceInfo2ServiceRemoved();

                        await RobotRaconteurSubTest.SubscriptionTests.RunTestIntraServiceRegisteredLater();

                        await RobotRaconteurSubTest.ConnectServiceTests.TestConnectServiceUrlsWithListener();

                        await RobotRaconteurSubTest.ConnectFailureTests.TestFailedConnectDoesNotLeakEndpoint();

                        await RobotRaconteurSubTest.FindServiceByTypeDisconnectTests.RunTestFindServiceByTypeDisconnects();


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
                case "memorymaxtransfer":
                    {
                        await MemoryMaxTransferTest.TestMemoryReadCountLimit();
                        break;
                    }
                case "wirerepeatconnect":
                    {
                        await RobotRaconteurSubTest.Wires.WireRepeatConnectTest.TestWireRepeatConnect();
                        break;
                    }
                case "pipewaiter":
                    {
                        await PipeWaiterTest.TestPipeReceiveWaitReleasesWaiter();
                        break;
                    }
                case "wirewaitvalid":
                    {
                        await RobotRaconteurSubTest.Wires.WireWaitValidTest.TestWaitValueValidAlreadyValid();
                        break;
                    }
                case "releaseservicepath":
                    {
                        await ReleaseServicePathTest.TestReleaseServicePathSeparator();
                        break;
                    }
                case "servicepathreleased":
                    {
                        await ServicePathReleasedTest.TestServicePathReleasedSeparator();
                        break;
                    }
                case "checkcapability":
                    {
                        await CheckServiceCapabilityTest.TestCheckServiceCapability();
                        break;
                    }
                case "replyentrytype":
                    {
                        await ReplyEntryTypeTest.TestUnhandledReplyEntryType();
                        break;
                    }
                case "subobjcurrentpath":
                    {
                        await SubObjectCurrentPathTest.TestFirstSubObjectRequestCurrentPath();
                        break;
                    }
                case "generatorcleanup":
                    {
                        await GeneratorCleanupTest.TestGeneratorCleanup();
                        break;
                    }
                case "transportlistenerleak":
                    {
                        await TransportListenerLeakTest.TestTransportListenerRemoved();
                        break;
                    }
                case "pulldefscycle":
                    {
                        await PullServiceDefsCycleTest.TestPullServiceDefinitionImportCycle();
                        break;
                    }
                case "pulledservicetypes":
                    {
                        await PulledServiceTypesTest.TestPulledServiceTypesDefinitionText();
                        break;
                    }
                case "varvaluenested":
                    {
                        MessageSerializationTest.TestVarValueNestedContainers();
                        break;
                    }
                case "packenum":
                    {
                        MessageSerializationTest.TestPackEnumContainers();
                        break;
                    }
                case "subupdatefilter":
                    {
                        await RobotRaconteurSubTest.Wires.SubscriptionUpdateFilterTest.TestUpdateServiceByTypeDisconnectsFiltered();
                        break;
                    }
                case "identifierfullmatch":
                    {
                        RobotRaconteurSubTest.IdentifierFullMatchTest.TestIdentifierFullMatch();
                        break;
                    }
                case "submanagerenable":
                    {
                        await RobotRaconteurSubTest.Manager.SubscriptionManagerEnableTest.TestEnableDisabledByType();
                        break;
                    }
                case "pipesubbacklog":
                    {
                        await RobotRaconteurSubTest.Pipes.PipeSubscriptionBacklogTest.TestPipeSubscriptionMaxBacklog();
                        break;
                    }
                case "serviceinfo2refresh":
                    {
                        await RobotRaconteurSubTest.ServiceInfo2RefreshTest.TestServiceInfo2RefreshSameUrls();
                        break;
                    }
                case "defaultclient":
                    {
                        await RobotRaconteurSubTest.DefaultClientTest.TestDefaultClientSkipsConnecting();
                        break;
                    }
                case "skelobjecttype":
                    {
                        await SkelObjectTypeTest.TestSkelObjectTypeName();
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
