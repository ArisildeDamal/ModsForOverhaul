using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class ClientSystemEntities : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "sce";
			}
		}

		public ClientSystemEntities(ClientMain game)
			: base(game)
		{
			game.PacketHandlers[40] = new ServerPacketHandler<Packet_Server>(this.HandleEntitiesPacket);
			game.PacketHandlers[34] = new ServerPacketHandler<Packet_Server>(this.HandleEntitySpawnPacket);
			game.PacketHandlers[36] = new ServerPacketHandler<Packet_Server>(this.HandleEntityDespawnPacket);
			game.PacketHandlers[37] = new ServerPacketHandler<Packet_Server>(this.HandleEntityAttributesPacket);
			game.PacketHandlers[38] = new ServerPacketHandler<Packet_Server>(this.HandleEntityAttributeUpdatePacket);
			game.PacketHandlers[67] = new ServerPacketHandler<Packet_Server>(this.HandleEntityPacket);
			game.PacketHandlers[33] = new ServerPacketHandler<Packet_Server>(this.HandleEntityLoadedPacket);
			game.PacketHandlers[60] = new ServerPacketHandler<Packet_Server>(this.HandleEntityBulkAttributesPacket);
			game.PacketHandlers[62] = new ServerPacketHandler<Packet_Server>(this.HandleEntityBulkDebugAttributesPacket);
			game.RegisterGameTickListener(new Action<float>(this.OnGameTick), 20, 0);
			game.RegisterGameTickListener(new Action<float>(this.UpdateEvery1000ms), 1000, 0);
			game.eventManager.OnEntitySpawn.Add(delegate(Entity e)
			{
				this.OnEntitySpawnOrLoaded(e, false);
			});
			game.eventManager.OnEntityLoaded.Add(delegate(Entity e)
			{
				this.OnEntitySpawnOrLoaded(e, true);
			});
			game.eventManager.OnEntityDespawn.Add(new EntityDespawnDelegate(this.OnEntityDespawn));
			game.eventManager.OnReloadShapes += this.EventManager_OnReloadShapes;
		}

		private void EventManager_OnReloadShapes()
		{
			AnimationCache.ClearCache(this.game.api);
			this.game.TesselatorManager.LoadEntityShapesAsync(this.game.EntityTypes, this.game.api);
			List<EntityProperties> entityTypes = this.game.EntityTypes;
			using (Dictionary<long, Entity>.Enumerator enumerator = this.game.LoadedEntities.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<long, Entity> val = enumerator.Current;
					Entity value = val.Value;
					bool flag;
					if (value == null)
					{
						flag = null != null;
					}
					else
					{
						EntityProperties properties = value.Properties;
						flag = ((properties != null) ? properties.Client : null) != null;
					}
					if (flag)
					{
						EntityClientProperties client = val.Value.Properties.Client;
						EntityProperties entityProperties = entityTypes.FirstOrDefault((EntityProperties et) => et.Code.Equals(val.Value.Properties.Code));
						Shape shape;
						if (entityProperties == null)
						{
							shape = null;
						}
						else
						{
							EntityClientProperties client2 = entityProperties.Client;
							shape = ((client2 != null) ? client2.LoadedShape : null);
						}
						client.LoadedShapeForEntity = shape;
						if (val.Value.AnimManager != null)
						{
							val.Value.AnimManager.Dispose();
							IAnimationManager animManager = val.Value.AnimManager;
							ICoreAPI api = this.game.api;
							Entity value2 = val.Value;
							Shape loadedShapeForEntity = val.Value.Properties.Client.LoadedShapeForEntity;
							IAnimator animator = val.Value.AnimManager.Animator;
							animManager.LoadAnimator(api, value2, loadedShapeForEntity, (animator != null) ? animator.Animations : null, val.Value.requirePosesOnServer, new string[] { "head" });
						}
					}
				}
			}
		}

		private void OnGameTick(float dt)
		{
			if (this.game.IsPaused)
			{
				return;
			}
			object entityLoadQueueLock = this.game.EntityLoadQueueLock;
			lock (entityLoadQueueLock)
			{
				while (this.game.EntityLoadQueue.Count > 0)
				{
					Entity entity = this.game.EntityLoadQueue.Pop();
					if (!this.game.LoadedEntities.ContainsKey(entity.EntityId))
					{
						this.game.LoadedEntities[entity.EntityId] = entity;
						ClientEventManager eventManager = this.game.eventManager;
						if (eventManager != null)
						{
							eventManager.TriggerEntityLoaded(entity);
						}
					}
				}
			}
			this.game.api.World.FrameProfiler.Mark("loadedEntityQueue-lockcontention");
			foreach (Entity entity2 in ((IEnumerable<Entity>)this.game.LoadedEntities.Values))
			{
				entity2.OnGameTick(dt);
			}
		}

		private void UpdateEvery1000ms(float dt)
		{
			foreach (Entity entity in this.game.LoadedEntities.Values)
			{
				entity.NearestPlayerDistance = (float)entity.ServerPos.DistanceTo(this.game.EntityPlayer.Pos);
				long chunkindex3d = this.game.WorldMap.ChunkIndex3D(entity.Pos);
				if (entity.InChunkIndex3d != chunkindex3d)
				{
					this.game.UpdateEntityChunk(entity, chunkindex3d);
				}
			}
		}

		private void OnEntitySpawnOrLoaded(Entity entity, bool loaded)
		{
			if (this.game.EntityRenderers.ContainsKey(entity.EntityId))
			{
				return;
			}
			if (ClientMain.ClassRegistry.EntityRendererClassNameToTypeMapping.ContainsKey(entity.Properties.Client.RendererName))
			{
				try
				{
					entity.Properties.Client.Renderer = ClientMain.ClassRegistry.CreateEntityRenderer(entity.Properties.Client.RendererName, new object[]
					{
						entity,
						this.game.api
					});
					this.game.EntityRenderers.Add(entity.EntityId, entity.Properties.Client.Renderer);
					goto IL_0172;
				}
				catch (Exception e)
				{
					ILogger logger = this.game.Platform.Logger;
					string text = "Exception while loading entity ";
					Type type = entity.GetType();
					logger.Error(text + ((type != null) ? type.ToString() : null) + " and creating renderer, entity will be invisible!");
					EntityItem ei = entity as EntityItem;
					if (ei != null)
					{
						this.game.Platform.Logger.Error("Was EntityItem with slot:" + ((ei.Slot == null) ? "null" : ei.Slot.ToString()));
					}
					this.game.Platform.Logger.Error(e);
					goto IL_0172;
				}
			}
			ILogger logger2 = this.game.Platform.Logger;
			string text2 = "Couldn't find renderer for entity ";
			Type type2 = entity.GetType();
			logger2.Error(text2 + ((type2 != null) ? type2.ToString() : null) + ", entity will be invisible!");
			IL_0172:
			if (loaded)
			{
				entity.OnEntityLoaded();
			}
			else
			{
				entity.OnEntitySpawn();
			}
			if (entity is EntityPlayer)
			{
				entity.InChunkIndex3d = 0L;
				ClientPlayer clientPlayer = this.game.PlayerByUid((entity as EntityPlayer).PlayerUID) as ClientPlayer;
				if (clientPlayer != null)
				{
					clientPlayer.WarnIfEntityChanged(entity.EntityId, "spawn/loaded");
					clientPlayer.worlddata.EntityPlayer = entity as EntityPlayer;
					this.game.api.eventapi.TriggerPlayerEntitySpawn(clientPlayer);
				}
			}
		}

		private void OnEntityDespawn(Entity entity, EntityDespawnData despawnReason)
		{
			entity.OnEntityDespawn(despawnReason);
			this.game.RemoveEntityRenderer(entity);
			entity.Properties.Client.Renderer = null;
			if (entity is EntityPlayer)
			{
				IPlayer plr = this.game.PlayerByUid((entity as EntityPlayer).PlayerUID);
				if (plr != null)
				{
					this.game.api.eventapi.TriggerPlayerEntityDespawn(plr as IClientPlayer);
					(plr as ClientPlayer).worlddata.EntityPlayer = null;
				}
			}
		}

		private void HandleEntityLoadedPacket(Packet_Server serverpacket)
		{
			Packet_Entity packet = serverpacket.Entity;
			if (packet == null)
			{
				return;
			}
			this.game.EnqueueMainThreadTask(delegate
			{
				Entity entity = ClientSystemEntities.createOrUpdateEntityFromPacket(packet, this.game, false);
				this.game.LoadedEntities[entity.EntityId] = entity;
				ClientEventManager eventManager = this.game.eventManager;
				if (eventManager == null)
				{
					return;
				}
				eventManager.TriggerEntityLoaded(entity);
			}, "entityloadedpacket");
		}

		private void HandleEntitiesPacket(Packet_Server serverpacket)
		{
			Packet_Entity[] packet = serverpacket.Entities.Entities;
			if (this.game.ClassRegistryInt.entityClassNameToTypeMapping.Count == 0)
			{
				this.game.Logger.Error(string.Format("Server sent me one or emore entity packets, but I cannot instantiate/update it, I don't have the entity class to type mapping (yet). Maybe server sent a packet too early? Will ignore.", Array.Empty<object>()));
				return;
			}
			for (int i = 0; i < packet.Length; i++)
			{
				if (packet[i] == null)
				{
					return;
				}
				Entity entity = ClientSystemEntities.createOrUpdateEntityFromPacket(packet[i], this.game, false);
				if (entity == null)
				{
					throw new InvalidOperationException(string.Format("Server sent me an entity packet for entity {0}, but I cannot instantiate/update it, entity is null", packet[i].EntityType));
				}
				this.game.LoadedEntities[entity.EntityId] = entity;
				ClientEventManager eventManager = this.game.eventManager;
				if (eventManager != null)
				{
					eventManager.TriggerEntityLoaded(entity);
				}
			}
		}

		private void HandleEntitySpawnPacket(Packet_Server serverpacket)
		{
			Packet_EntitySpawn packet = serverpacket.EntitySpawn;
			for (int i = 0; i < packet.EntityCount; i++)
			{
				Entity entity;
				this.game.LoadedEntities.TryGetValue(packet.Entity[i].EntityId, out entity);
				if (entity == null)
				{
					entity = ClientSystemEntities.entityFromPacket(packet.Entity[i], this.game);
					if (entity != null)
					{
						this.game.LoadedEntities[entity.EntityId] = entity;
						ClientEventManager eventManager = this.game.eventManager;
						if (eventManager != null)
						{
							eventManager.TriggerEntitySpawn(entity);
						}
					}
				}
				else
				{
					ClientSystemEntities.updateEntityFromPacket(packet.Entity[i], entity);
				}
			}
		}

		private void HandleEntityDespawnPacket(Packet_Server serverpacket)
		{
			for (int i = 0; i < serverpacket.EntityDespawn.EntityIdCount; i++)
			{
				long entityId = serverpacket.EntityDespawn.EntityId[i];
				Entity entity;
				if (this.game.LoadedEntities.TryGetValue(entityId, out entity))
				{
					EntityDespawnData despawnReason = new EntityDespawnData
					{
						Reason = (EnumDespawnReason)serverpacket.EntityDespawn.DespawnReason[i],
						DamageSourceForDeath = new DamageSource
						{
							Source = (EnumDamageSource)serverpacket.EntityDespawn.DeathDamageSource[i]
						}
					};
					ClientEventManager eventManager = this.game.eventManager;
					if (eventManager != null)
					{
						eventManager.TriggerEntityDespawn(entity, despawnReason);
					}
					ClientChunk chunk = this.game.WorldMap.GetClientChunk(entity.InChunkIndex3d);
					if (chunk != null)
					{
						chunk.RemoveEntity(entityId);
					}
					this.game.RemoveEntityRenderer(entity);
					entity.OnEntityDespawn(despawnReason);
					this.game.LoadedEntities.Remove(entityId);
				}
			}
		}

		private void HandleEntityBulkAttributesPacket(Packet_Server packet)
		{
			Packet_BulkEntityAttributes p = packet.BulkEntityAttributes;
			for (int i = 0; i < p.FullUpdatesCount; i++)
			{
				this.HandleEntityAttributesPacket(p.FullUpdates[i]);
			}
			for (int j = 0; j < p.PartialUpdatesCount; j++)
			{
				this.HandleEntityAttributeUpdatePacket(p.PartialUpdates[j]);
			}
		}

		private void HandleEntityBulkDebugAttributesPacket(Packet_Server packet)
		{
			Packet_BulkEntityDebugAttributes p = packet.BulkEntityDebugAttributes;
			for (int i = 0; i < p.FullUpdatesCount; i++)
			{
				Entity entity;
				this.game.LoadedEntities.TryGetValue(p.FullUpdates[i].EntityId, out entity);
				if (entity != null)
				{
					BinaryReader reader = new BinaryReader(new MemoryStream(p.FullUpdates[i].Data));
					entity.DebugAttributes.FromBytes(reader);
					entity.DebugAttributes.MarkAllDirty();
				}
			}
		}

		private void HandleEntityAttributesPacket(Packet_Server serverpacket)
		{
			this.HandleEntityAttributesPacket(serverpacket.EntityAttributes);
		}

		private void HandleEntityAttributesPacket(Packet_EntityAttributes packet)
		{
			Entity entity;
			this.game.LoadedEntities.TryGetValue(packet.EntityId, out entity);
			if (entity != null)
			{
				BinaryReader reader = new BinaryReader(new MemoryStream(packet.Data));
				entity.FromBytes(reader, true);
			}
		}

		private void HandleEntityAttributeUpdatePacket(Packet_Server serverpacket)
		{
			this.HandleEntityAttributeUpdatePacket(serverpacket.EntityAttributeUpdate);
		}

		private void HandleEntityAttributeUpdatePacket(Packet_EntityAttributeUpdate p)
		{
			Entity entity;
			this.game.LoadedEntities.TryGetValue(p.EntityId, out entity);
			if (entity != null)
			{
				for (int i = 0; i < p.AttributesCount; i++)
				{
					Packet_PartialAttribute pkt = p.Attributes[i];
					entity.WatchedAttributes.PartialUpdate(pkt.Path, pkt.Data);
				}
			}
		}

		private void HandleEntityPacket(Packet_Server serverpacket)
		{
			Packet_EntityPacket p = serverpacket.EntityPacket;
			Entity entity;
			this.game.LoadedEntities.TryGetValue(p.EntityId, out entity);
			if (entity != null)
			{
				entity.OnReceivedServerPacket(p.Packetid, p.Data);
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		public static Entity createOrUpdateEntityFromPacket(Packet_Entity entitypacket, ClientMain game, bool addToLoadQueue = false)
		{
			Entity entity;
			game.LoadedEntities.TryGetValue(entitypacket.EntityId, out entity);
			if (entity == null)
			{
				entity = ClientSystemEntities.entityFromPacket(entitypacket, game);
				if (entity == null || !addToLoadQueue)
				{
					return entity;
				}
				object entityLoadQueueLock = game.EntityLoadQueueLock;
				lock (entityLoadQueueLock)
				{
					game.EntityLoadQueue.Push(entity);
					return entity;
				}
			}
			ClientSystemEntities.updateEntityFromPacket(entitypacket, entity);
			return entity;
		}

		private static void updateEntityFromPacket(Packet_Entity entitypacket, Entity entity)
		{
			BinaryReader reader = new BinaryReader(new MemoryStream(entitypacket.Data));
			entity.FromBytes(reader, true);
		}

		private static Entity entityFromPacket(Packet_Entity entitypacket, ClientMain game)
		{
			EntityProperties entityType = game.GetEntityType(new AssetLocation(entitypacket.EntityType));
			if (entityType == null)
			{
				game.Logger.Error("Server sent a create entity packet for entity code '{0}', but no such entity exists?. Ignoring", new object[] { entitypacket.EntityType });
				return null;
			}
			Entity entity = game.Api.ClassRegistry.CreateEntity(entityType);
			entity.SimulationRange = entitypacket.SimulationRange;
			entity.Api = game.Api;
			ClientSystemEntities.updateEntityFromPacket(entitypacket, entity);
			long index3d = game.WorldMap.ChunkIndex3D(entity.Pos);
			entity.Initialize(entityType.Clone(), game.api, index3d);
			entity.AfterInitialized(false);
			ClientChunk chunk = game.WorldMap.GetClientChunk(index3d);
			if (chunk != null)
			{
				chunk.AddEntity(entity);
			}
			return entity;
		}

		public static EntityPos entityPosFromPacket(Packet_EntityPosition packet)
		{
			return new EntityPos
			{
				X = CollectibleNet.DeserializeDoublePrecise(packet.X),
				Y = CollectibleNet.DeserializeDoublePrecise(packet.Y),
				Z = CollectibleNet.DeserializeDoublePrecise(packet.Z),
				Yaw = CollectibleNet.DeserializeFloatPrecise(packet.Yaw),
				Pitch = CollectibleNet.DeserializeFloatPrecise(packet.Pitch),
				Roll = CollectibleNet.DeserializeFloatPrecise(packet.Roll),
				HeadYaw = CollectibleNet.DeserializeFloatPrecise(packet.HeadYaw),
				HeadPitch = CollectibleNet.DeserializeFloatPrecise(packet.HeadPitch)
			};
		}

		internal static BlockEntity createBlockEntityFromPacket(Packet_BlockEntity packet, ClientMain game)
		{
			BlockEntity blockEntity = ClientMain.ClassRegistry.CreateBlockEntity(packet.Classname);
			ClientSystemEntities.UpdateBlockEntityData(blockEntity, packet.Data, game, true);
			return blockEntity;
		}

		internal static void UpdateBlockEntityData(BlockEntity entity, byte[] data, ClientMain game, bool isNew)
		{
			BinaryReader reader = new BinaryReader(new MemoryStream(data));
			TreeAttribute tree = new TreeAttribute();
			tree.FromBytes(reader);
			if (isNew)
			{
				Block block = game.BlockAccessor.GetBlockRaw(tree.GetInt("posx", 0), tree.GetInt("posy", 0), tree.GetInt("posz", 0), 1);
				entity.CreateBehaviors(block, game);
			}
			entity.FromTreeAttributes(tree, game);
		}
	}
}
