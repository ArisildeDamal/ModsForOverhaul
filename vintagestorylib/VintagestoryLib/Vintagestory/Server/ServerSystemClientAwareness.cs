using System;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.Server
{
	internal class ServerSystemClientAwareness : ServerSystem
	{
		public ServerSystemClientAwareness(ServerMain server)
			: base(server)
		{
			server.clientAwarenessEvents = new Dictionary<EnumClientAwarenessEvent, List<Action<ClientStatistics>>>();
			server.clientAwarenessEvents[EnumClientAwarenessEvent.ChunkTransition] = new List<Action<ClientStatistics>>();
		}

		public override int GetUpdateInterval()
		{
			return 100;
		}

		public override void OnServerTick(float dt)
		{
			foreach (ClientStatistics clientStats in this.clients.Values)
			{
				EnumClientAwarenessEvent? clientEvent = clientStats.DetectChanges();
				if (clientEvent != null)
				{
					foreach (Action<ClientStatistics> action in this.server.clientAwarenessEvents[clientEvent.Value])
					{
						action(clientStats);
					}
				}
			}
		}

		public void TriggerEvent(EnumClientAwarenessEvent clientEvent, int clientId)
		{
			ClientStatistics clientStats;
			List<Action<ClientStatistics>> actions;
			if (this.clients.TryGetValue(clientId, out clientStats) && this.server.clientAwarenessEvents.TryGetValue(clientEvent, out actions))
			{
				foreach (Action<ClientStatistics> action in actions)
				{
					action(clientStats);
				}
			}
		}

		public override void OnPlayerJoin(ServerPlayer player)
		{
			EntityPos playerPos = player.Entity.ServerPos;
			this.clients[player.ClientId] = new ClientStatistics
			{
				client = player.client,
				lastChunkX = (int)playerPos.X / 32,
				lastChunkY = (int)playerPos.Y / 32,
				lastChunkZ = (int)playerPos.Z / 32
			};
		}

		public override void OnPlayerDisconnect(ServerPlayer player)
		{
			this.clients.Remove(player.ClientId);
		}

		private Dictionary<int, ClientStatistics> clients = new Dictionary<int, ClientStatistics>();
	}
}
