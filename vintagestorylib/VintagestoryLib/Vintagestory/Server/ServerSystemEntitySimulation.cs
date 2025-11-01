using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class ServerSystemEntitySimulation : ServerSystem
	{
		public ServerSystemEntitySimulation(ServerMain server)
			: base(server)
		{
			server.RegisterGameTickListener(new Action<float>(this.UpdateEvery1000ms), 1000, 0);
			server.EventManager.OnGameWorldBeingSaved += this.EventManager_OnGameWorldBeingSaved;
			server.RegisterGameTickListener(new Action<float>(this.UpdateEvery100ms), 100, 0);
			server.clientAwarenessEvents[EnumClientAwarenessEvent.ChunkTransition].Add(new Action<ClientStatistics>(this.OnPlayerLeaveChunk));
			server.PacketHandlers[17] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleEntityInteraction);
			server.PacketHandlers[12] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleSpecialKey);
			server.PacketHandlers[32] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleRuntimeSetting);
			server.EventManager.OnPlayerChat += this.EventManager_OnPlayerChat;
		}

		private void HandleRuntimeSetting(Packet_Client packet, ConnectedClient player)
		{
			player.Player.ImmersiveFpMode = packet.RuntimeSetting.ImmersiveFpMode > 0;
			player.Player.ItemCollectMode = packet.RuntimeSetting.ItemCollectMode;
		}

		private void EventManager_OnPlayerChat(IServerPlayer byPlayer, int channelId, ref string message, ref string data, BoolRef consumed)
		{
			if (byPlayer.Entitlements.Count <= 0)
			{
				message = string.Format("<strong>{0}:</strong> {1}", byPlayer.PlayerName, message);
				return;
			}
			Entitlement ent = byPlayer.Entitlements[0];
			double[] color;
			if (GlobalConstants.playerColorByEntitlement.TryGetValue(ent.Code, out color))
			{
				message = string.Format("<font color=\"" + VtmlUtil.toHexColor(color) + "\"><strong>{0}:</strong></font> {1}", byPlayer.PlayerName, message);
				return;
			}
			message = string.Format("<strong>{0}:</strong> {1}", byPlayer.PlayerName, message);
		}

		private void EventManager_OnGameWorldBeingSaved()
		{
			if (this.server.RunPhase != EnumServerRunPhase.Shutdown)
			{
				this.server.EventManager.defragLists();
			}
		}

		public override int GetUpdateInterval()
		{
			return 20;
		}

		public override void OnBeginModsAndConfigReady()
		{
			this.server.EventManager.OnPlayerRespawn += this.OnPlayerRespawn;
			new ShapeTesselatorManager(this.server).LoadEntityShapes(this.server.EntityTypes, this.server.api);
		}

		public override void OnPlayerJoin(ServerPlayer player)
		{
			this.physicsManager.ForceClientUpdateTick(player.client);
		}

		public override void OnPlayerDisconnect(ServerPlayer player)
		{
			this.physicsManager.ForceClientUpdateTick(player.client);
		}

		private void OnPlayerLeaveChunk(ClientStatistics clientStats)
		{
			this.physicsManager.ForceClientUpdateTick(clientStats.client);
		}

		public override void OnServerTick(float dt)
		{
			using (IEnumerator<ConnectedClient> enumerator = this.server.Clients.Values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ServerSystemEntitySimulation.<>c__DisplayClass11_0 CS$<>8__locals1 = new ServerSystemEntitySimulation.<>c__DisplayClass11_0();
					CS$<>8__locals1.client = enumerator.Current;
					if (CS$<>8__locals1.client.IsPlayingClient)
					{
						ServerSystemEntitySimulation.<>c__DisplayClass11_1 CS$<>8__locals2 = new ServerSystemEntitySimulation.<>c__DisplayClass11_1();
						CS$<>8__locals2.player = CS$<>8__locals1.client.Player;
						EntityPlayer entity = CS$<>8__locals2.player.Entity;
						BlockSelection blockSelection = CS$<>8__locals2.player.Entity.BlockSelection;
						entity.PreviousBlockSelection = ((blockSelection != null) ? blockSelection.Position.Copy() : null);
						this.server.RayTraceForSelection(CS$<>8__locals2.player, ref CS$<>8__locals2.player.Entity.BlockSelection, ref CS$<>8__locals2.player.Entity.EntitySelection, new BlockFilter(CS$<>8__locals1.<OnServerTick>g__bFilter|0), new EntityFilter(CS$<>8__locals2.<OnServerTick>g__eFilter|1));
						if (CS$<>8__locals2.player.Entity.BlockSelection != null)
						{
							bool firstTick = CS$<>8__locals2.player.Entity.PreviousBlockSelection == null || CS$<>8__locals2.player.Entity.BlockSelection.Position != CS$<>8__locals2.player.Entity.PreviousBlockSelection;
							this.server.BlockAccessor.GetBlock(CS$<>8__locals2.player.Entity.BlockSelection.Position).OnBeingLookedAt(CS$<>8__locals2.player, CS$<>8__locals2.player.Entity.BlockSelection, firstTick);
						}
					}
				}
			}
			this.TickEntities(dt);
			this.SendPlayerEntityDeaths();
		}

		private void UpdateEvery100ms(float t1)
		{
			this.SendEntityDespawns();
			int count = this.server.DelayedSpawnQueue.Count;
			if (count > 0)
			{
				ServerMain.FrameProfiler.Enter("spawningentities");
				int maxCount = MagicNum.MaxEntitySpawnsPerTick;
				if (count > maxCount * 3)
				{
					maxCount = count / 2;
				}
				count = Math.Min(count, maxCount);
				Entity entity;
				while (count-- > 0 && this.server.DelayedSpawnQueue.TryDequeue(out entity))
				{
					try
					{
						this.server.SpawnEntity(entity);
						ServerMain.FrameProfiler.Mark("spawning:", entity.Code);
					}
					catch (Exception e)
					{
						ServerMain.Logger.Error(e);
					}
				}
				ServerMain.FrameProfiler.Leave();
			}
		}

		private void UpdateEvery1000ms(float dt)
		{
			foreach (Entity entity in this.server.LoadedEntities.Values)
			{
				long chunkindex3d = this.server.WorldMap.ChunkIndex3D(entity.ServerPos);
				if (entity.InChunkIndex3d != chunkindex3d)
				{
					ServerChunk oldChunk = this.server.WorldMap.GetServerChunk(entity.InChunkIndex3d);
					ServerChunk newChunk = this.server.WorldMap.GetServerChunk(chunkindex3d);
					if (newChunk != null)
					{
						if (oldChunk != null)
						{
							oldChunk.RemoveEntity(entity.EntityId);
						}
						newChunk.AddEntity(entity);
						entity.InChunkIndex3d = chunkindex3d;
					}
				}
			}
		}

		private void TickEntities(float dt)
		{
			List<KeyValuePair<Entity, EntityDespawnData>> despawned = new List<KeyValuePair<Entity, EntityDespawnData>>();
			ServerMain.FrameProfiler.Enter("tickentities");
			foreach (Entity entity in this.server.LoadedEntities.Values)
			{
				if (!Dimensions.ShouldNotTick(entity.ServerPos, entity.Api))
				{
					entity.OnGameTick(dt);
				}
				if (entity.ShouldDespawn)
				{
					despawned.Add(new KeyValuePair<Entity, EntityDespawnData>(entity, entity.DespawnReason));
				}
			}
			ServerMain.FrameProfiler.Enter("despawning");
			foreach (KeyValuePair<Entity, EntityDespawnData> val in despawned)
			{
				this.server.DespawnEntity(val.Key, val.Value);
				ServerMain.FrameProfiler.Mark("despawned-", val.Key.Code.Path);
			}
			ServerMain.FrameProfiler.Leave();
			ServerMain.FrameProfiler.Leave();
		}

		private void HandleEntityInteraction(Packet_Client packet, ConnectedClient client)
		{
			ServerPlayer player = client.Player;
			if (player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
			{
				return;
			}
			Packet_EntityInteraction p = packet.EntityInteraction;
			Entity[] entitiesAround = this.server.GetEntitiesAround(player.Entity.ServerPos.XYZ, player.WorldData.PickingRange + 10f, player.WorldData.PickingRange + 10f, (Entity e) => e.EntityId == p.EntityId);
			if (entitiesAround == null || entitiesAround.Length == 0)
			{
				ServerMain.Logger.Debug("HandleEntityInteraction received from client " + client.PlayerName + " but no such entity found in his range!");
				return;
			}
			Entity entity = entitiesAround[0];
			Cuboidd cuboidd = entity.SelectionBox.ToDouble().Translate(entity.SidedPos.X, entity.SidedPos.Y, entity.SidedPos.Z);
			EntityPos sidedPos = client.Entityplayer.SidedPos;
			IPlayerInventoryManager inventoryManager = client.Player.InventoryManager;
			ItemStack itemStack2;
			if (inventoryManager == null)
			{
				itemStack2 = null;
			}
			else
			{
				ItemSlot activeHotbarSlot = inventoryManager.ActiveHotbarSlot;
				itemStack2 = ((activeHotbarSlot != null) ? activeHotbarSlot.Itemstack : null);
			}
			ItemStack itemStack = itemStack2;
			float range = ((itemStack != null) ? itemStack.Collectible.GetAttackRange(itemStack) : GlobalConstants.DefaultAttackRange);
			if ((cuboidd.ShortestDistanceFrom(sidedPos.X + client.Entityplayer.LocalEyePos.X, sidedPos.Y + client.Entityplayer.LocalEyePos.Y, sidedPos.Z + client.Entityplayer.LocalEyePos.Z) > (double)(range * 2f) && p.MouseButton == 0) || (p.MouseButton == 0 && (((!this.server.Config.AllowPvP || !player.HasPrivilege("attackplayers")) && entity is EntityPlayer) || (!player.HasPrivilege("attackcreatures") && entity is EntityAgent))))
			{
				return;
			}
			EntityPlayer entityPlayer = entity as EntityPlayer;
			if (entityPlayer != null)
			{
				IServerPlayer obj = entityPlayer.Player as IServerPlayer;
				if (obj == null || obj.ConnectionState != EnumClientState.Playing)
				{
					return;
				}
			}
			Vec3d hitPosition = new Vec3d(CollectibleNet.DeserializeDouble(p.HitX), CollectibleNet.DeserializeDouble(p.HitY), CollectibleNet.DeserializeDouble(p.HitZ));
			long entityId = p.EntityId;
			EntitySelection currentEntitySelection = player.CurrentEntitySelection;
			long? num;
			if (currentEntitySelection == null)
			{
				num = null;
			}
			else
			{
				Entity entity2 = currentEntitySelection.Entity;
				num = ((entity2 != null) ? new long?(entity2.EntityId) : null);
			}
			long? num2 = num;
			if (!((entityId == num2.GetValueOrDefault()) & (num2 != null)))
			{
				player.Entity.EntitySelection = new EntitySelection
				{
					Entity = entity,
					SelectionBoxIndex = p.SelectionBoxIndex,
					Position = hitPosition
				};
			}
			else
			{
				player.CurrentEntitySelection.Position = hitPosition;
				player.CurrentEntitySelection.SelectionBoxIndex = p.SelectionBoxIndex;
			}
			EnumHandling handling = EnumHandling.PassThrough;
			this.server.EventManager.TriggerPlayerInteractEntity(entity, player, player.inventoryMgr.ActiveHotbarSlot, hitPosition, p.MouseButton, ref handling);
			if (handling == EnumHandling.PassThrough)
			{
				entity.OnInteract(player.Entity, player.InventoryManager.ActiveHotbarSlot, hitPosition, (p.MouseButton != 0) ? EnumInteractMode.Interact : EnumInteractMode.Attack);
			}
		}

		private void HandleSpecialKey(Packet_Client packet, ConnectedClient client)
		{
			SaveGame saveGameData = this.server.SaveGameData;
			int lives = ((saveGameData != null) ? saveGameData.WorldConfiguration.GetString("playerlives", "-1").ToInt(-1) : (-1));
			if (lives < 0 || lives > client.WorldData.Deaths)
			{
				ServerMain.Logger.VerboseDebug("Received respawn request from {0}", new object[] { client.PlayerName });
				this.server.EventManager.TriggerPlayerRespawn(client.Player);
				return;
			}
			client.Player.SendMessage(GlobalConstants.CurrentChatGroup, "Cannot revive! All lives used up.", EnumChatType.CommandError, null);
		}

		private void OnPlayerRespawn(IServerPlayer player)
		{
			if (player.Entity == null || player.Entity.Alive)
			{
				ServerMain.Logger.VerboseDebug("Respawn key received but ignored. Cause: {0} || {1}", new object[]
				{
					player.Entity == null,
					player.Entity.Alive
				});
				return;
			}
			FuzzyEntityPos pos = player.GetSpawnPosition(true);
			ConnectedClient client = this.server.Clients[player.ClientId];
			if (pos.UsesLeft >= 0)
			{
				if (pos.UsesLeft == 99)
				{
					player.SendLocalisedMessage(GlobalConstants.GeneralChatGroup, "playerrespawn-nocustomspawnset", Array.Empty<object>());
				}
				else if (pos.UsesLeft > 0)
				{
					player.SendLocalisedMessage(GlobalConstants.GeneralChatGroup, "You have re-emerged at your returning point. It will vanish after {0} more uses", new object[] { pos.UsesLeft });
				}
				else if (pos.UsesLeft == 0)
				{
					player.SendLocalisedMessage(GlobalConstants.GeneralChatGroup, "You have re-emerged at your returning point, which has now vanished.", Array.Empty<object>());
				}
			}
			if (pos.Radius > 0f)
			{
				this.server.LocateRandomPosition(pos.XYZ, pos.Radius, 10, (BlockPos spawnpos) => ServerSystemSupplyChunks.AdjustForSaveSpawnSpot(this.server, spawnpos, player, this.server.rand.Value), delegate(BlockPos foundpos)
				{
					if (foundpos != null)
					{
						EntityPos targetPos = pos.Copy();
						targetPos.X = (double)foundpos.X;
						targetPos.Y = (double)foundpos.Y;
						targetPos.Z = (double)foundpos.Z;
						this.teleport(client, targetPos);
						return;
					}
					this.teleport(client, pos);
				});
			}
			else
			{
				this.teleport(client, pos);
			}
			ServerMain.Logger.VerboseDebug("Respawn key received. Teleporting player to spawn and reviving once chunks have loaded.");
		}

		private void teleport(ConnectedClient client, EntityPos targetPos)
		{
			EntityPlayer eplr = client.Player.Entity;
			eplr.TeleportTo(targetPos, delegate
			{
				eplr.Revive();
				this.server.ServerUdpNetwork.physicsManager.UpdateTrackedEntitiesStates(client);
			});
		}

		private void GenDeathMessagesCache()
		{
			this.deathMessagesCache = new Dictionary<string, List<string>>();
			foreach (KeyValuePair<string, string> val in Lang.AvailableLanguages["en"].GetAllEntries())
			{
				AssetLocation loc = new AssetLocation(val.Key);
				if (loc.PathStartsWith("deathmsg"))
				{
					List<string> parts = new List<string>(loc.Path.Split('-', StringSplitOptions.None));
					parts.RemoveAt(parts.Count - 1);
					string key = string.Join("-", parts);
					List<string> elems;
					if (this.deathMessagesCache.ContainsKey(key))
					{
						elems = this.deathMessagesCache[key];
					}
					else
					{
						elems = new List<string>();
						this.deathMessagesCache[key] = elems;
					}
					elems.Add(loc.Path);
				}
			}
		}

		private void SendPlayerEntityDeaths()
		{
			if (this.deathMessagesCache == null)
			{
				this.GenDeathMessagesCache();
			}
			List<ConnectedClient> deadClients = this.server.Clients.Values.Where((ConnectedClient client) => (client.State == EnumClientState.Connected || client.State == EnumClientState.Playing) && !client.Entityplayer.Alive && client.Entityplayer.DeadNotify).ToList<ConnectedClient>();
			if (deadClients.Count == 0)
			{
				return;
			}
			SaveGame saveGameData = this.server.SaveGameData;
			int lives = ((saveGameData != null) ? saveGameData.WorldConfiguration.GetString("playerlives", "-1").ToInt(-1) : (-1));
			foreach (ConnectedClient client2 in deadClients)
			{
				client2.Entityplayer.DeadNotify = false;
				client2.WorldData.Deaths++;
				this.server.EventManager.TriggerPlayerDeath(client2.Player, client2.Entityplayer.DeathReason);
				this.server.BroadcastPacket(new Packet_Server
				{
					Id = 45,
					PlayerDeath = new Packet_PlayerDeath
					{
						ClientId = client2.Id,
						LivesLeft = ((lives < 0) ? (-1) : Math.Max(0, lives - client2.WorldData.Deaths))
					}
				}, Array.Empty<IServerPlayer>());
				DamageSource src = client2.Entityplayer.DeathReason;
				bool flag = !this.server.api.World.Config.GetBool("disableDeathMessages", false);
				string deathMessage = "";
				if (flag)
				{
					deathMessage = this.GetDeathMessage(client2, src);
					this.server.SendMessageToGeneral(deathMessage, EnumChatType.Notification, null, null);
				}
				EntityPlayer otherPlayer = ((src != null) ? src.GetCauseEntity() : null) as EntityPlayer;
				if (otherPlayer != null)
				{
					string creatureName = this.server.PlayerByUid(otherPlayer.PlayerUID).PlayerName;
					LoggerBase logger = ServerMain.Logger;
					string text = "{0} killed {1}, with item (if any): {2}";
					object[] array = new object[3];
					array[0] = creatureName;
					array[1] = client2.PlayerName;
					int num = 2;
					ItemSlot rightHandItemSlot = otherPlayer.RightHandItemSlot;
					object obj;
					if (rightHandItemSlot == null)
					{
						obj = null;
					}
					else
					{
						ItemStack itemstack = rightHandItemSlot.Itemstack;
						obj = ((itemstack != null) ? itemstack.Collectible.Code : null);
					}
					array[num] = obj;
					logger.Audit(Lang.Get(text, array));
				}
				else
				{
					ServerMain.Logger.Audit(Lang.Get("{0} died. Death message: {1}", new object[] { client2.PlayerName, deathMessage }));
				}
			}
		}

		private string GetDeathMessage(ConnectedClient client, DamageSource src)
		{
			if (src == null)
			{
				Lang.Get("Player {0} died.", new object[] { client.PlayerName });
			}
			Entity causeEntity = ((src != null) ? src.GetCauseEntity() : null);
			if (causeEntity == null)
			{
				string code = null;
				if (src.Source == EnumDamageSource.Explosion)
				{
					code = "deathmsg-explosion";
				}
				else if (src.Type == EnumDamageType.Hunger)
				{
					code = "deathmsg-hunger";
				}
				else if (src.Type == EnumDamageType.Fire)
				{
					code = "deathmsg-fire-block";
				}
				else if (src.Type == EnumDamageType.Electricity)
				{
					code = "deathmsg-electricity-block";
				}
				else if (src.Source == EnumDamageSource.Fall)
				{
					code = "deathmsg-fall";
				}
				if (code != null)
				{
					List<string> messages;
					this.deathMessagesCache.TryGetValue(code, out messages);
					if (messages != null && messages.Count > 0)
					{
						int variant = this.server.rand.Value.Next(messages.Count);
						return Lang.Get(messages[variant], new object[] { client.PlayerName });
					}
				}
				return Lang.Get("Player {0} died.", new object[] { client.PlayerName });
			}
			string ecode = "deathmsg-" + causeEntity.Code.Path.Replace("-", "");
			List<string> messages2;
			this.deathMessagesCache.TryGetValue(ecode, out messages2);
			if (messages2 != null && messages2.Count > 0)
			{
				return Lang.Get(messages2[this.server.rand.Value.Next(messages2.Count)], new object[] { client.PlayerName });
			}
			string creatureName = causeEntity.GetPrefixAndCreatureName(null);
			return Lang.Get("Player {0} got killed by {1}", new object[] { client.PlayerName, creatureName });
		}

		private void SendEntityDespawns()
		{
			if (this.server.EntityDespawnSendQueue.Count == 0)
			{
				return;
			}
			Packet_EntityDespawn p = new Packet_EntityDespawn();
			Packet_Server packet = new Packet_Server
			{
				Id = 36,
				EntityDespawn = p
			};
			List<long> entityIds = new List<long>();
			List<int> despawnReasons = new List<int>();
			List<int> damageSource = new List<int>();
			foreach (ConnectedClient client in this.server.Clients.Values)
			{
				entityIds.Clear();
				despawnReasons.Clear();
				damageSource.Clear();
				foreach (KeyValuePair<Entity, EntityDespawnData> val in this.server.EntityDespawnSendQueue)
				{
					if (client.TrackedEntities.Contains(val.Key.EntityId))
					{
						entityIds.Add(val.Key.EntityId);
						List<int> list = despawnReasons;
						EntityDespawnData value = val.Value;
						list.Add((int)((value != null) ? value.Reason : EnumDespawnReason.Death));
						List<int> list2 = damageSource;
						EntityDespawnData value2 = val.Value;
						list2.Add((int)((((value2 != null) ? value2.DamageSourceForDeath : null) == null) ? EnumDamageSource.Unknown : val.Value.DamageSourceForDeath.Source));
					}
				}
				p.SetEntityId(entityIds.ToArray());
				p.SetDeathDamageSource(damageSource.ToArray());
				p.SetDespawnReason(despawnReasons.ToArray());
				this.server.SendPacket(client.Id, packet);
			}
			this.server.EntityDespawnSendQueue.Clear();
		}

		internal int trackingRangeSq = MagicNum.DefaultEntityTrackingRange * MagicNum.ServerChunkSize * MagicNum.DefaultEntityTrackingRange * MagicNum.ServerChunkSize;

		internal PhysicsManager physicsManager;

		private Dictionary<string, List<string>> deathMessagesCache;
	}
}
