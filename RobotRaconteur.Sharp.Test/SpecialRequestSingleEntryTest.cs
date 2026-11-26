using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurSharp;

namespace RobotRaconteurSharp.Test
{
    // A special request must be the only entry in its message (object protocol, "Special Requests"). A message
    // with more entries is a protocol violation: SpecialRequest throws and the transport closes the connection.
    // Each entry used to be answered, so one message could make the node build a reply of entries x service
    // definition size
    public static class SpecialRequestSingleEntryTest
    {
        static Message NewSpecialRequestMessage(int entry_count)
        {
            var m = new Message();
            m.header = new MessageHeader();
            m.header.ReceiverNodeID = NodeID.Any;
            m.header.SenderNodeID = NodeID.NewUniqueID();
            m.header.SenderEndpoint = 1234;
            for (int i = 0; i < entry_count; i++)
            {
                var e = new MessageEntry(MessageEntryType.GetServiceDesc, "");
                e.AddElement("ServiceType", "com.robotraconteur.testing.TestService1");
                e.AddElement("clientversion", "0.9.2");
                m.entries.Add(e);
            }
            return m;
        }

        public static async Task RunTestSpecialRequestSingleEntry()
        {
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                var ret = await nodes.node.SpecialRequest(NewSpecialRequestMessage(1), uint.MaxValue);
                RRAssert.IsTrue(ret != null);
                RRAssert.AreEqual(ret.entries.Count, 1);
                RRAssert.AreEqual(ret.entries[0].EntryType, MessageEntryType.GetServiceDescRet);
                RRAssert.AreEqual(ret.entries[0].Error, MessageErrorType.None);

                foreach (int n in new int[] { 2, 0 })
                {
                    Exception err = null;
                    try
                    {
                        await nodes.node.SpecialRequest(NewSpecialRequestMessage(n), uint.MaxValue);
                    }
                    catch (Exception e)
                    {
                        err = e;
                    }
                    RRAssert.IsTrue(err is ProtocolException);
                }
            }
        }
    }
}
