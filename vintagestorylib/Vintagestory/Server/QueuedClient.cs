using System;

namespace Vintagestory.Server
{
	public class QueuedClient
	{
		public QueuedClient(ConnectedClient client, Packet_ClientIdentification identification, string entitlements)
		{
			this.Client = client;
			this.Identification = identification;
			this.Entitlements = entitlements;
		}

		public ConnectedClient Client;

		public Packet_ClientIdentification Identification;

		public string Entitlements;
	}
}
