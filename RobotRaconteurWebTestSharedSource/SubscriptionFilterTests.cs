using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using experimental.testing.subtestfilter;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurSubTest
{
    public class sub_testroot_impl_base
    {
        double d1;
        public Task<double> get_d1(CancellationToken cancel = default)
        {
            return Task.FromResult(d1);
        }

        public Task set_d1(double value, CancellationToken cancel = default)
        {
            d1 = value;
            return Task.CompletedTask;
        }
    }

    public class sub_testroot1_impl : sub_testroot_impl_base, sub_testroot
    {

    }

    public class sub_testroot1_impl2 : sub_testroot_impl_base, sub_testroot2
    {

    }

    public class SubscriberFilterTests
    {
        private static RobotRaconteurNode InitNode(string nodeName, string serviceName, Dictionary<string, object> attributes, uint service_type = 0)
        {
            var node = new RobotRaconteurNode();
            node.NodeName = nodeName;

            var intraTransport = new IntraTransport(node);
            node.RegisterTransport(intraTransport);
            intraTransport.StartServer();

            // Assuming you have a method or mechanism to load the service definition similar to _robdef
            node.RegisterServiceType(new experimental__testing__subtestfilterFactory());

            object serviceInstance;
            if (service_type == 1)
            {
                serviceInstance = new sub_testroot1_impl(); 
            }
            else
            {
                serviceInstance = new sub_testroot1_impl2(); 
            }
            var serviceContext = node.RegisterService(serviceName, "com.robotraconteur.testing.subtestfilter", serviceInstance);
            serviceContext.Attributes = (attributes);

            return node;
        }

#if !ROBOTRACONTEUR_H5
        static private void RegisterServiceAuth(RobotRaconteurNode node, string serviceName, uint service_type)
        {
            string authData = "testuser1 0b91dec4fe98266a03b136b59219d0d6 objectlock\ntestuser2 841c4221c2e7e0cefbc0392a35222512 objectlock\ntestsuperuser 503ed776c50169f681ad7bbc14198b68 objectlock,objectlockoverride";
            UserAuthenticator passwordAuthenticator = new PasswordFileUserAuthenticator(authData);

            var policies = new Dictionary<string, string>
            {
                {"requirevaliduser", "true"},
                {"allowobjectlock", "true"}
            };

            var securityPolicy = new ServiceSecurityPolicy(passwordAuthenticator, policies);

            object serviceInstance;
            if (service_type == 1)
            {
                serviceInstance = new sub_testroot1_impl();
            }
            else
            {
                serviceInstance = new sub_testroot1_impl2();
            }

            node.RegisterService(serviceName, "com.robotraconteur.testing.subtestfilter", serviceInstance, securityPolicy);
        }
#endif

        static private RobotRaconteurNode InitClientNode()
        {
            var node1 = new RobotRaconteurNode();

            var t1 = new IntraTransport(node1);
            node1.RegisterTransport(t1);
            t1.StartClient();

            node1.SetLogLevelFromString("WARNING");

            return node1;
        }

        private async Task AssertConnectedClients(ServiceSubscription c, int count)
        {
            int tryCount = 0;

            if (count == 0)
            {
                await Task.Delay(500);
            }

            while (true)
            {
                await Task.Delay(100);

                try
                {
                    if (c.GetConnectedClients().Count == count)
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    if (tryCount > 50)
                    {
                        var clients = c.GetConnectedClients();
                        foreach (var client in clients.Keys)
                        {
                            Console.WriteLine(client.NodeID.ToString());
                            Console.WriteLine(client.ServiceName);
                        }
                        Console.WriteLine(clients.Count);
                        throw;
                    }

                    tryCount += 1;
                }
            }
        }

        public async Task RunAttributesFilterTest(RobotRaconteurNode clientNode, Dictionary<string, ServiceSubscriptionFilterAttributeGroup> attributesGroups, int expectedCount)
        {
            var filter1 = new ServiceSubscriptionFilter();

            foreach (var kvp in attributesGroups)
            {
                filter1.Attributes[kvp.Key] = kvp.Value;
            }

            var sub2 = clientNode.SubscribeServiceByType(
                new string[] { "com.robotraconteur.testing.subtestfilter.sub_testroot" }, filter1);

            await AssertConnectedClients(sub2, expectedCount);
            sub2.Close();
        }

        public async Task RunFilterTest(RobotRaconteurNode clientNode, ServiceSubscriptionFilter filter, int expectedCount, string servicetypeSuffix = "")
        {
            var sub2 = clientNode.SubscribeServiceByType(
                new string[] { $"com.robotraconteur.testing.subtestfilter.sub_testroot{servicetypeSuffix}" }, filter);

            await AssertConnectedClients(sub2, expectedCount);
            sub2.Close();
        }

        public async Task TestSubscriberAttributeFilter()
        {
            // Use IntraTransport for tests so everything is local to the test

            // Create server nodes
            var node1Attrs = new Dictionary<string, object>
            {
                { "a1", "test_attr_val1" },
                { "a2", "test_attr_val2" },
                { "a4", "test_attr_val4,test_attr_val4_1" }
            };

                    var node2Attrs = new Dictionary<string, object>
            {
                { "a2", "test_attr_val2" },
                { "a3", "test_attr_val3,test_attr_val3_1" }
            };

                    var node3Attrs = new Dictionary<string, object>
            {
                { "a1", "test_attr_val3" },
                { "a2", "test_attr_val5" },
                { "a4", "test_attr_val4,test_attr_val4_2" }
            };

            var node1 = InitNode("test_node1", "service1", node1Attrs);
            var node2 = InitNode("test_node2", "service2", node2Attrs);
            var node3 = InitNode("test_node7", "service3", node3Attrs);

            await Task.Delay(500);

            // Create client node
            var clientNode = InitClientNode();

            // Connect and disconnect to make sure everything is working
            var c1 = await clientNode.ConnectService("rr+intra:///?nodename=test_node1&service=service1");
            var c2 = await clientNode.ConnectService("rr+intra:///?nodename=test_node2&service=service2");
            var c3 = await clientNode.ConnectService("rr+intra:///?nodename=test_node7&service=service3");

            await clientNode.DisconnectService(c1);
            await clientNode.DisconnectService(c2);
            await clientNode.DisconnectService(c3);

            var sub1 = clientNode.SubscribeServiceByType(new[] { "com.robotraconteur.testing.subtestfilter.sub_testroot" });
            await AssertConnectedClients(sub1, 3);
            sub1.Close();

            var attrGrp1 = new ServiceSubscriptionFilterAttributeGroup();
            attrGrp1.Attributes.Add(new ServiceSubscriptionFilterAttribute("test_attr_val1"));
            var attrGrps1 = new Dictionary<string, ServiceSubscriptionFilterAttributeGroup> { { "a1", attrGrp1 } };
            await RunAttributesFilterTest(clientNode, attrGrps1, 1);

            var attrGrp2 = new ServiceSubscriptionFilterAttributeGroup();
            attrGrp2.Attributes.Add(new ServiceSubscriptionFilterAttribute("test_attr_val4"));
            attrGrp2.Attributes.Add(new ServiceSubscriptionFilterAttribute("test_attr_val4_2"));
            var attrGrps2 = new Dictionary<string, ServiceSubscriptionFilterAttributeGroup> { { "a4", attrGrp2 } };
            await RunAttributesFilterTest(clientNode, attrGrps2, 2);

            var attrGrp4 = new ServiceSubscriptionFilterAttributeGroup();
            attrGrp4.Attributes.Add(new ServiceSubscriptionFilterAttribute("test_attr_val4"));
            attrGrp4.Attributes.Add(new ServiceSubscriptionFilterAttribute("test_attr_val4_2"));
            attrGrp4.Operation = ServiceSubscriptionFilterAttributeGroupOperation.AND;
            var attrGrps4 = new Dictionary<string, ServiceSubscriptionFilterAttributeGroup> { { "a4", attrGrp4 } };
            await RunAttributesFilterTest(clientNode, attrGrps4, 1);

            var attrGrp3 = new ServiceSubscriptionFilterAttributeGroup();
            attrGrp3.Attributes.Add(ServiceSubscriptionFilterAttributeFactory.CreateServiceSubscriptionFilterAttributeRegex(".*_attr_val1"));
            var attrGrps3 = new Dictionary<string, ServiceSubscriptionFilterAttributeGroup> { { "a1", attrGrp3 } };
            await RunAttributesFilterTest(clientNode, attrGrps3, 1);

            var attrGrp5 = new ServiceSubscriptionFilterAttributeGroup();
            attrGrp5.Attributes.Add(ServiceSubscriptionFilterAttributeFactory.CreateServiceSubscriptionFilterAttributeRegex(".*_attr_val1"));
            var attrGrps5 = new Dictionary<string, ServiceSubscriptionFilterAttributeGroup> { { "a1", attrGrp5 } };
            var attrGrp6 = new ServiceSubscriptionFilterAttributeGroup();
            attrGrp6.Attributes.Add(new ServiceSubscriptionFilterAttribute("test_attr_val_not_there"));
            attrGrps5["a4"] = attrGrp6;

            var filter2 = new ServiceSubscriptionFilter();
            filter2.Attributes = attrGrps5;
            filter2.AttributesMatchOperation = ServiceSubscriptionFilterAttributeGroupOperation.OR;
            await RunFilterTest(clientNode, filter2, 1);

            node1.Shutdown();
            node2.Shutdown();
            node3.Shutdown();
        }

        public async Task TestSubscriberFilter()
        {
            var node1 = InitNode("test_node3", "service1", new Dictionary<string, object>(), 2);
            var node2 = InitNode("test_node4", "service1", new Dictionary<string, object>(), 2);
            var node3 = InitNode("test_node5", "service3", new Dictionary<string, object>(), 2);
            var node4 = InitNode("test_node6", "service2", new Dictionary<string, object>(), 2);
#if !ROBOTRACONTEUR_H5
            RegisterServiceAuth(node3, "service1", 2);
#endif

            await Task.Delay(500);

            // Create client node
            var clientNode = InitClientNode();

            // Connect and disconnect to make sure everything is working
            var c1 = await clientNode.ConnectService("rr+intra:///?nodename=test_node3&service=service1");
            var c2 = await clientNode.ConnectService("rr+intra:///?nodename=test_node4&service=service1");
            var cred1 = new Dictionary<string, object> { { "password", "testpass1" } };
            var c3 = await clientNode.ConnectService("rr+intra:///?nodename=test_node5&service=service1", "testuser1", cred1);
            var c4 = await clientNode.ConnectService("rr+intra:///?nodename=test_node6&service=service2");

            await clientNode.DisconnectService(c1);
            await clientNode.DisconnectService(c2);
            await clientNode.DisconnectService(c3);
            await clientNode.DisconnectService(c4);

            var sub1 = clientNode.SubscribeServiceByType(new[] { "com.robotraconteur.testing.subtestfilter.sub_testroot2" });
            await AssertConnectedClients(sub1, 5);
            sub1.Close();

            var filter1 = new ServiceSubscriptionFilter();
            filter1.ServiceNames = new string[] { "service1" };
            await RunFilterTest(clientNode, filter1, 3, "2");

            var filter1Node = new ServiceSubscriptionFilterNode
            {
                NodeName = "test_node3"
            };
            filter1.Nodes = new ServiceSubscriptionFilterNode[] { filter1Node };
            await RunFilterTest(clientNode, filter1, 1, "2");
#if !ROBOTRACONTEUR_H5
            var filter2 = new ServiceSubscriptionFilter();
            var filter2Node = new ServiceSubscriptionFilterNode
            {
                NodeName = "test_node5",
                Username = "testuser1",
                Credentials = new Dictionary<string, object> { { "password", "testpass1" } }
            };
            filter2.ServiceNames = new string[] { "service1" };
            filter2.Nodes = new ServiceSubscriptionFilterNode[] { filter2Node };
            await RunFilterTest(clientNode, filter2, 1, "2");
#endif
            node1.Shutdown();
            node2.Shutdown();
            node3.Shutdown();
            node4.Shutdown();
        }






    }
}
