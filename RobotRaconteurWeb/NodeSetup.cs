using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace RobotRaconteurWeb
{
	[Flags]
	public enum RobotRaconteurNodeSetupFlags
	{
		
		None = 0x0,
		EnableNodeDiscoveryListening = 0x1,
		EnableNodeAnnounce = 0x2,
		EnableLocalTransport = 0x4,
		EnableTcpTransport = 0x8,		
		LocalTransportStartServer = 0x20,
		LocalTransportStartClient = 0x40,
		TcpTransportStartServer = 0x80,
		
		EnableAllTransports = 
			EnableLocalTransport 
			| EnableTcpTransport,

		ClientDefault = 
			EnableAllTransports 
			| EnableNodeDiscoveryListening 
			| LocalTransportStartClient,

		ServerDefault = 
			EnableAllTransports 
			| LocalTransportStartServer 
			| TcpTransportStartServer 
			| EnableNodeAnnounce 
			| EnableNodeDiscoveryListening

	};

	public class RobotRaconteurNodeSetup : IDisposable
	{
		public RobotRaconteurNode Node { get; }
		public TcpTransport TcpTransport { get; }

		public LocalTransport LocalTransport { get; }
		
		public RobotRaconteurNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, bool scan_assembly_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags)
		{
			Node = node;

			if (flags.HasFlag(RobotRaconteurNodeSetupFlags.EnableLocalTransport))
			{
				LocalTransport = new LocalTransport(node);
				if (flags.HasFlag(RobotRaconteurNodeSetupFlags.LocalTransportStartServer))
				{
					LocalTransport.StartServerAsNodeName(nodename);
				}
				else if (flags.HasFlag(RobotRaconteurNodeSetupFlags.LocalTransportStartClient) && ! string.IsNullOrEmpty(nodename))
				{
					LocalTransport.StartClientAsNodeName(nodename);
				}

				if (flags.HasFlag(RobotRaconteurNodeSetupFlags.EnableNodeDiscoveryListening))
				{
					LocalTransport.EnableNodeDiscoveryListening();
				}

				node.RegisterTransport(LocalTransport);
			}

			if (flags.HasFlag(RobotRaconteurNodeSetupFlags.EnableTcpTransport))
			{
				TcpTransport = new TcpTransport(node);
				if (flags.HasFlag(RobotRaconteurNodeSetupFlags.TcpTransportStartServer))
				{
					TcpTransport.StartServer(tcp_port);
				}

				if (flags.HasFlag(RobotRaconteurNodeSetupFlags.EnableNodeDiscoveryListening))
				{
					TcpTransport.EnableNodeDiscoveryListening();
				}

				if (flags.HasFlag(RobotRaconteurNodeSetupFlags.EnableNodeAnnounce))
				{
					TcpTransport.EnableNodeAnnounce();
				}

				node.RegisterTransport(TcpTransport);
			}

			if (service_types != null)
			{
				foreach (var t in service_types)
				{
					node.RegisterServiceType(t);
				}
			}

			if (scan_assembly_types)
			{
				try
				{
					var scanned_types = ScanAssembliesForServiceTypes();
					foreach (var t in scanned_types)
					{
						node.RegisterServiceType(t);
					}
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("warning: assembly scanning failed: " + e.Message);
				}
			}

		}

		public void Dispose()
		{
			Node?.Shutdown();
		}

		static List<ServiceFactory> ScanAssembliesForServiceTypes()
		{
			// https://stackoverflow.com/questions/13493416/scan-assembly-for-classes-that-implement-certain-interface-and-add-them-to-a-con

			var o = new List<ServiceFactory>();

			var assignableType = typeof(ServiceFactory);

			var scanners = AppDomain.CurrentDomain.GetAssemblies().ToList()
				.SelectMany(x => x.GetTypes())
				.Where(t => assignableType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract).ToList();

			foreach (Type type in scanners)
			{
				if (type == typeof(RobotRaconteurServiceIndex.RobotRaconteurServiceIndexFactory))
                {
					continue;
                }
				var service_factory = Activator.CreateInstance(type) as ServiceFactory;
				if (service_factory != null)
				{
					o.Add(service_factory);
				}
			}

			return o;
		}
	}

	public class ClientNodeSetup : RobotRaconteurNodeSetup
	{
		public ClientNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault)
			: base(node, service_types, false,nodename, 0 , flags)
		{

		}

		public ClientNodeSetup(ServiceFactory[] service_types, string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault)
			: base(RobotRaconteurNode.s, service_types, false, nodename, 0, flags)
		{

		}

		public ClientNodeSetup(RobotRaconteurNode node, string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault)
			: base(node, null, true, nodename, 0, flags)
		{

		}

		public ClientNodeSetup(string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault)
			: base(RobotRaconteurNode.s, null, true, nodename, 0, flags)
		{

		}
	}

	public class ServerNodeSetup : RobotRaconteurNodeSetup
	{
		public ServerNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault)
			: base(node, service_types, false, nodename, tcp_port, flags)
		{

		}

		public ServerNodeSetup(ServiceFactory[] service_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault)
			: base(RobotRaconteurNode.s, service_types, false, nodename, tcp_port, flags)
		{

		}

		public ServerNodeSetup(RobotRaconteurNode node, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault)
			: base(node, null, true, nodename, tcp_port, flags)
		{

		}

		public ServerNodeSetup(string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault)
			: base(RobotRaconteurNode.s, null, true, nodename, tcp_port, flags)
		{

		}
	}



}
