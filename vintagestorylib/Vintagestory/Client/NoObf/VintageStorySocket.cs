using System;
using System.Net.Sockets;

namespace Vintagestory.Client.NoObf
{
	public class VintageStorySocket : Socket
	{
		public VintageStorySocket(SocketType socketType, ProtocolType protocolType)
			: base(socketType, protocolType)
		{
		}

		public VintageStorySocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
			: base(addressFamily, socketType, protocolType)
		{
		}

		public bool Disposed { get; private set; }

		protected override void Dispose(bool disposing)
		{
			this.Disposed = true;
			base.Dispose(disposing);
		}
	}
}
