using System;
using System.Linq;
using RobotRaconteurTest;
using RobotRaconteurSharp;

namespace RobotRaconteurSharp.Test
{
    // Default WebSocket origins: pages loaded from local files ("null", "file://") and https robotraconteur.com,
    // but not every browser extension or plain http origins an on-path attacker can produce
    public static class WebSocketDefaultOriginsTests
    {
        public static void RunTestDefaultOrigins()
        {
            var node = new RobotRaconteurNode();
            try
            {
                var t = new TcpTransport(node);
                var o = t.GetWebSocketAllowedOrigins();
                RRAssert.IsTrue(o.Contains("null"));
                RRAssert.IsTrue(o.Contains("file://"));
                RRAssert.IsFalse(o.Contains("chrome-extension://"));
                RRAssert.IsFalse(o.Any(x => x.StartsWith("http://")));
                RRAssert.IsTrue(o.Contains("https://robotraconteur.com"));

                // "null" can be removed and added back (the origin regex used to reject it)
                t.RemoveWebSocketAllowedOrigin("null");
                RRAssert.IsFalse(t.GetWebSocketAllowedOrigins().Contains("null"));
                t.AddWebSocketAllowedOrigin("null");
                RRAssert.IsTrue(t.GetWebSocketAllowedOrigins().Contains("null"));
            }
            finally
            {
                node.Shutdown();
            }
        }
    }
}
