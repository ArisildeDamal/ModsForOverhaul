using System;

namespace Vintagestory.Server
{
	public abstract class ServerAPIComponentBase
	{
		public ServerAPIComponentBase(ServerMain server)
		{
			this.server = server;
		}

		internal ServerMain server;
	}
}
