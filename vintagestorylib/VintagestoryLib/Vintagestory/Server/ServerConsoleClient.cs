using System;

namespace Vintagestory.Server
{
	public class ServerConsoleClient : ConnectedClient
	{
		public override ServerPlayerData ServerData
		{
			get
			{
				return this.serverdata;
			}
		}

		public override bool IsPlayingClient
		{
			get
			{
				return false;
			}
		}

		public ServerConsoleClient(int clientId)
			: base(clientId)
		{
		}

		public override string ToString()
		{
			return string.Format("Server Console", Array.Empty<object>());
		}

		public ServerPlayerData serverdata;
	}
}
