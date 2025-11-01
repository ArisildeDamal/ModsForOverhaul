using System;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class ServerInformation
	{
		public ServerInformation()
		{
			this.ServerName = "";
			this.connectdata = new ServerConnectData();
			this.ServerPing = new Ping();
		}

		internal string ServerName;

		internal ServerConnectData connectdata;

		internal Ping ServerPing;

		internal string Playstyle;

		internal string PlayListCode;

		internal int Seed;

		internal string SavegameIdentifier;

		internal bool RequiresRemappings;
	}
}
