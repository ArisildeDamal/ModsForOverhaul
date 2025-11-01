using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Network.Packets;
using Vintagestory.Server.Systems;

namespace Vintagestory.Server
{
	public class PhysicsManager : LoadBalancedTask
	{
		public PhysicsManager(ICoreServerAPI sapi, ServerUdpNetwork udpNetwork)
		{
			this.sapi = sapi;
			this.udpNetwork = udpNetwork;
			this.AnimationsAndTagsChannel = sapi.Network.RegisterChannel("EntityAnims");
			this.AnimationsAndTagsChannel.RegisterMessageType<AnimationPacket>().RegisterMessageType<BulkAnimationPacket>().RegisterMessageType<EntityTagPacket>();
			this.maxPhysicsThreads = Math.Clamp(MagicNum.MaxPhysicsThreads, 1, 8);
			if (sapi.Server.ReducedServerThreads)
			{
				this.maxPhysicsThreads = 1;
			}
			this.entitiesFullUpdate = new Dictionary<long, Packet_EntityAttributes>[this.maxPhysicsThreads];
			this.entitiesPartialUpdate = new Dictionary<long, Packet_EntityAttributeUpdate>[this.maxPhysicsThreads];
			this.entitiesDebugUpdate = new Dictionary<long, Packet_EntityAttributes>[this.maxPhysicsThreads];
			this.entitiesPositionPackets = new Dictionary<long, Packet_EntityPosition>[this.maxPhysicsThreads];
			this.entitiesAnimPackets = new Dictionary<long, AnimationPacket>[this.maxPhysicsThreads];
			this.entitiesTagPackets = new ConcurrentDictionary<long, EntityTagPacket>();
			this.entitiesUpdateReusableBuffers = new FastMemoryStream[this.maxPhysicsThreads];
			for (int i = 0; i < this.entitiesFullUpdate.Length; i++)
			{
				this.entitiesFullUpdate[i] = new Dictionary<long, Packet_EntityAttributes>();
			}
			for (int j = 0; j < this.entitiesPartialUpdate.Length; j++)
			{
				this.entitiesPartialUpdate[j] = new Dictionary<long, Packet_EntityAttributeUpdate>();
			}
			for (int k = 0; k < this.entitiesDebugUpdate.Length; k++)
			{
				this.entitiesDebugUpdate[k] = new Dictionary<long, Packet_EntityAttributes>();
			}
			for (int l = 0; l < this.entitiesPositionPackets.Length; l++)
			{
				this.entitiesPositionPackets[l] = new Dictionary<long, Packet_EntityPosition>();
			}
			for (int m = 0; m < this.entitiesAnimPackets.Length; m++)
			{
				this.entitiesAnimPackets[m] = new Dictionary<long, AnimationPacket>();
			}
			for (int n = 0; n < this.entitiesUpdateReusableBuffers.Length; n++)
			{
				this.entitiesUpdateReusableBuffers[n] = new FastMemoryStream();
			}
			this.server = sapi.World as ServerMain;
			this.loadBalancer = new LoadBalancer(this, ServerMain.Logger);
			this.loadBalancer.CreateDedicatedThreads(this.maxPhysicsThreads, "physicsManager", this.server.Serverthreads);
			this.offthreadProcess = new PhysicsManager.PhysicsOffthreadTasks(this);
			if (!this.server.ReducedServerThreads)
			{
				Thread thread = TyronThreadPool.CreateDedicatedThread(new ThreadStart(this.offthreadProcess.Start), "physicsManagerHelper");
				thread.IsBackground = true;
				thread.Priority = Thread.CurrentThread.Priority;
				this.server.Serverthreads.Add(thread);
			}
			this.listener = this.server.RegisterGameTickListener(new Action<float>(this.ServerTick), 1, 0);
			PhysicsManager.rateModifier = 1f;
			PhysicsBehaviorBase.InitServerMT(sapi);
		}

		public void Init()
		{
			this.es = this.server.Systems.First((ServerSystem s) => s is ServerSystemEntitySimulation) as ServerSystemEntitySimulation;
			this.es.physicsManager = this;
		}

		public void ForceClientUpdateTick(ConnectedClient client)
		{
			this.attrUpdateAccum = 0.2f;
		}

		public void ServerTick(float dt)
		{
			ServerMain.FrameProfiler.Enter("physicsmanager-servertick");
			Entity[] spawnsToSend;
			while (!this.toAdd.IsEmpty)
			{
				IPhysicsTickable addable;
				if (!this.toAdd.TryDequeue(out addable))
				{
					IL_00B5:
					IPhysicsTickable removable;
					while (!this.toRemove.IsEmpty && this.toRemove.TryDequeue(out removable))
					{
						if (removable != null)
						{
							this.tickables.Remove(removable);
						}
					}
					this.physicsTickAccum += dt;
					this.deltaT = dt;
					if (this.physicsTickAccum > 0.4f)
					{
						int skippedTicks = (int)((this.physicsTickAccum - 0.4f) / 0.033333335f);
						if (ServerMain.FrameProfiler.Enabled)
						{
							ServerMain.Logger.Warning("Over 400ms tick. Skipping {0} physics ticks.", new object[] { skippedTicks });
						}
						this.physicsTickAccum %= 0.4f;
					}
					ServerMain.FrameProfiler.Mark("physicsmanager-preparation");
					this.ticksToDo = (int)(this.physicsTickAccum / 0.033333335f);
					this.physicsTickAccum -= (float)this.ticksToDo * 0.033333335f;
					long timeout = (long)(Environment.TickCount + 1000);
					while (this.offthreadProcess.Busy() && (long)Environment.TickCount < timeout)
					{
						Thread.Sleep(0);
					}
					ServerMain.FrameProfiler.Mark("physicsmanager-waitingOnPreviousTick");
					this.BuildClientList(this.server.Clients.Values);
					this.attrUpdateAccum += dt;
					if (this.attrUpdateAccum >= 0.2f)
					{
						this.attrUpdateAccum = 0f;
					}
					ServerMain.FrameProfiler.Mark("physicsmanager-buildclientlist");
					int threadsWorking = 1;
					if (this.ticksToDo > 0)
					{
						this.stateChanges.Clear();
						if (this.tickables.Count > 800 && this.maxPhysicsThreads > 1)
						{
							threadsWorking = this.maxPhysicsThreads;
							this.loadBalancer.SynchroniseWorkToMainThread(this);
							this.loadBalancer.AwaitCompletionOnAllThreads(threadsWorking);
							ServerMain.FrameProfiler.Mark("physicsmanager-waitingForSlowestThread");
							if (this.attrUpdateAccum == 0f)
							{
								this.GatherUpdatePacketsFromAllThreads();
								ServerMain.FrameProfiler.Mark("physicsmanager-mergeThreadPackets");
							}
						}
						else
						{
							this.DoWork(0);
						}
						foreach (Entity entity in this.stateChanges)
						{
							this.ActiveStateChanged(entity);
						}
						this.stateChanges.Clear();
						this.currentTick += this.ticksToDo;
						float adjustedRate = (float)this.ticksToDo * 0.033333335f * PhysicsManager.rateModifier;
						foreach (IPhysicsTickable tickable in this.tickables)
						{
							try
							{
								tickable.AfterPhysicsTick(adjustedRate);
							}
							catch (Exception e)
							{
								ServerMain.Logger.Error(e);
							}
						}
						ServerMain.FrameProfiler.Mark("physicsmanager-afterphysicstick");
					}
					this.SendPositionsForNonTickableEntities(this.attrUpdateAccum == 0f);
					ServerMain.FrameProfiler.Mark("physicsmanager-nontickables");
					if (this.attrUpdateAccum == 0f)
					{
						foreach (ConnectedClient client in this.ClientList)
						{
							this.UpdateTrackedEntityLists(client, threadsWorking);
						}
						ServerMain.FrameProfiler.Mark("physicsmanager-updatetrackedentitylists");
						this.SendTrackedEntitiesStateChanges();
						this.offthreadProcess.QueueAsyncTask(delegate
						{
							this.SendAttributesViaTCP(this.ClientList);
						});
					}
					spawnsToSend = null;
					int spawnsCount = 0;
					List<Entity> entitySpawnSendQueue = this.server.EntitySpawnSendQueue;
					lock (entitySpawnSendQueue)
					{
						List<Entity> SendQueue = this.server.EntitySpawnSendQueue;
						spawnsCount = SendQueue.Count;
						if (spawnsCount > 0)
						{
							spawnsToSend = new Entity[spawnsCount];
							for (int i = 0; i < spawnsToSend.Length; i++)
							{
								Entity entity2 = SendQueue[i];
								if (entity2.Alive)
								{
									spawnsToSend[i] = entity2;
								}
								else
								{
									spawnsCount--;
								}
							}
							SendQueue.Clear();
						}
					}
					ServerMain.FrameProfiler.Mark("physicsmanager-sendspawnlockwaiting");
					if (spawnsCount > 0)
					{
						if (this.PrepareEntitySpawns(spawnsToSend, this.ClientList) > 0)
						{
							Dictionary<long, Packet_EntityPosition> entityPositionPackets = this.entitiesPositionPackets[0];
							entityPositionPackets.Clear();
							this.DoFirstPhysicsTicks(spawnsToSend, entityPositionPackets);
							this.offthreadProcess.QueueAsyncTask(delegate
							{
								this.SendEntitySpawns(spawnsToSend, this.ClientList, entityPositionPackets);
							});
						}
						ServerMain.FrameProfiler.Mark("physicsmanager-sendspawns");
					}
					ServerMain.FrameProfiler.Leave();
					return;
				}
				if (addable != null && addable.Entity != null)
				{
					if (addable.Entity.ServerBehaviorsThreadsafe == null)
					{
						ServerMain.Logger.Warning("An entity " + addable.Entity.Code.ToShortString() + " failed to complete initialisation, will not be physics ticked.");
					}
					else
					{
						this.tickables.Add(addable);
					}
				}
			}
			goto IL_00B5;
		}

		private void BuildClientList(ICollection<ConnectedClient> values)
		{
			List<ConnectedClient> clients = (this.ClientList = new List<ConnectedClient>());
			foreach (ConnectedClient client in values)
			{
				if ((client.State == EnumClientState.Connected || client.State == EnumClientState.Playing) && client.Entityplayer != null)
				{
					clients.Add(client);
				}
			}
			if (this.positions.Length < clients.Count * 3)
			{
				this.positions = new double[clients.Count * 3];
			}
			double[] positions = this.positions;
			int i = 0;
			foreach (ConnectedClient client2 in clients)
			{
				EntityPos pos = client2.Position;
				positions[i] = pos.X;
				positions[i + 1] = pos.Y;
				positions[i + 2] = pos.Z;
				i += 3;
				if (client2.threadedTrackedEntities == null)
				{
					List<Entity>[] threadedTracked = (client2.threadedTrackedEntities = new List<Entity>[this.maxPhysicsThreads]);
					for (int j = 0; j < this.maxPhysicsThreads; j++)
					{
						threadedTracked[j] = new List<Entity>();
					}
				}
				else
				{
					List<Entity>[] threadedTracked2 = client2.threadedTrackedEntities;
					for (int k = 0; k < this.maxPhysicsThreads; k++)
					{
						threadedTracked2[k].Clear();
					}
				}
			}
		}

		public void UpdateTrackedEntitiesStates(ConnectedClient client)
		{
			List<ConnectedClient> clients = new List<ConnectedClient> { client };
			EntityPos pos = client.Position;
			this.positions[0] = pos.X;
			this.positions[1] = pos.Y;
			this.positions[2] = pos.Z;
			foreach (Entity entity in this.server.LoadedEntities.Values)
			{
				if (this.UpdateTrackedEntityState(entity, clients, 0))
				{
					this.ActiveStateChanged(entity);
				}
			}
			this.UpdateTrackedEntityLists(client, 1);
		}

		private bool UpdateTrackedEntityState(Entity entity, List<ConnectedClient> clients, int zeroBasedThreadNum)
		{
			EntityPos serverPos = entity.ServerPos;
			double x = serverPos.X;
			double y = serverPos.Y;
			double z = serverPos.Z;
			double minrangeSq = double.MaxValue;
			double simRangeSq = (double)(entity.SimulationRange * entity.SimulationRange);
			double trackRange = Math.Max((double)this.es.trackingRangeSq, simRangeSq);
			long entityId = entity.EntityId;
			long entityChunkIndex = entity.InChunkIndex3d;
			bool doTrackOutsideLoadedRange = entity.AllowOutsideLoadedRange;
			double[] positions = this.positions;
			int i = 0;
			foreach (ConnectedClient client in clients)
			{
				double num = x - positions[i];
				double dy = y - positions[i + 1];
				double dz = z - positions[i + 2];
				i += 3;
				double rangeSq = num * num + dz * dz + dy * dy;
				if (rangeSq < minrangeSq)
				{
					minrangeSq = rangeSq;
				}
				if (rangeSq < trackRange && (client.DidSendChunk(entityChunkIndex) || entityId == client.Player.Entity.EntityId || doTrackOutsideLoadedRange))
				{
					client.threadedTrackedEntities[zeroBasedThreadNum].Add(entity);
				}
			}
			if (minrangeSq < trackRange)
			{
				entity.IsTracked = ((minrangeSq >= 2500.0) ? 1 : 2);
			}
			else
			{
				entity.IsTracked = 0;
				if (!(entity is EntityPlayer))
				{
					this.CompletePositionUpdate(entity);
				}
			}
			entity.NearestPlayerDistance = (float)Math.Sqrt(minrangeSq);
			if (!entity.AlwaysActive)
			{
				bool active = minrangeSq < simRangeSq;
				if (active != (entity.State == EnumEntityState.Active))
				{
					entity.State = (active ? EnumEntityState.Active : EnumEntityState.Inactive);
					return true;
				}
			}
			return false;
		}

		private void ActiveStateChanged(Entity entity)
		{
			entity.OnStateChanged(entity.State ^ EnumEntityState.Inactive);
		}

		private void CompletePositionUpdate(Entity entity)
		{
			entity.PreviousServerPos.SetFrom(entity.ServerPos);
			entity.IsTeleport = false;
		}

		private void UpdateTrackedEntityLists(ConnectedClient client, int threadCount)
		{
			List<Entity>[] threadedTracked = client.threadedTrackedEntities;
			List<Entity> threadedTracked2 = threadedTracked[0];
			HashSet<long> cte = client.TrackedEntities;
			List<long> alreadyTracked = this.alreadyTracked;
			alreadyTracked.EnsureCapacity(cte.Count);
			List<Entity> newlyTracked = this.newlyTracked;
			foreach (Entity e in threadedTracked2)
			{
				long id;
				if (cte.Remove(id = e.EntityId))
				{
					alreadyTracked.Add(id);
				}
				else
				{
					newlyTracked.Add(e);
				}
			}
			for (int i = 1; i < threadCount; i++)
			{
				foreach (Entity e2 in threadedTracked[i])
				{
					long id;
					if (cte.Remove(id = e2.EntityId))
					{
						alreadyTracked.Add(id);
					}
					else
					{
						newlyTracked.Add(e2);
					}
					threadedTracked2.Add(e2);
				}
				threadedTracked[i].Clear();
			}
			foreach (long untrackedId in cte)
			{
				client.entitiesNowOutOfRange.Add(new EntityDespawn
				{
					ForClientId = client.Id,
					DespawnData = this.outofRangeDespawnData,
					EntityId = untrackedId
				});
			}
			cte.Clear();
			cte.AddRange(alreadyTracked);
			alreadyTracked.Clear();
			foreach (Entity entity in newlyTracked)
			{
				if (cte.Count >= MagicNum.TrackedEntitiesPerClient)
				{
					break;
				}
				cte.Add(entity.EntityId);
				client.entitiesNowInRange.Add(new EntityInRange
				{
					ForClientId = client.Id,
					Entity = entity
				});
			}
			newlyTracked.Clear();
		}

		public void SendTrackedEntitiesStateChanges()
		{
			List<AnimationPacket> entityAnimPackets = new List<AnimationPacket>();
			FastMemoryStream ms = null;
			try
			{
				foreach (ConnectedClient client in this.ClientList)
				{
					if (client.entitiesNowInRange.Count > 0)
					{
						entityAnimPackets.Clear();
						if (ms == null)
						{
							ms = new FastMemoryStream();
						}
						foreach (EntityInRange nowInRange in client.entitiesNowInRange)
						{
							Entity entity = nowInRange.Entity;
							EntityPlayer entityPlayer = entity as EntityPlayer;
							if (entityPlayer != null)
							{
								ServerPlayer value;
								this.server.PlayersByUid.TryGetValue(entityPlayer.PlayerUID, out value);
								if (value != null)
								{
									this.server.SendPacket(nowInRange.ForClientId, ((ServerWorldPlayerData)value.WorldData).ToPacketForOtherPlayers(value));
								}
							}
							ms.Reset();
							BinaryWriter writer = new BinaryWriter(ms);
							this.server.SendPacket(nowInRange.ForClientId, ServerPackets.GetFullEntityPacket(entity, ms, writer));
							if (entity.AnimManager != null)
							{
								entityAnimPackets.Add(new AnimationPacket(entity));
							}
						}
						BulkAnimationPacket bulkAnimationPacket = new BulkAnimationPacket
						{
							Packets = entityAnimPackets.ToArray()
						};
						this.AnimationsAndTagsChannel.SendPacket<BulkAnimationPacket>(bulkAnimationPacket, new IServerPlayer[] { client.Player });
						client.entitiesNowInRange.Clear();
					}
					if (client.entitiesNowOutOfRange.Count > 0)
					{
						this.server.SendPacket(client.Id, ServerPackets.GetEntityDespawnPacket(client.entitiesNowOutOfRange));
						client.entitiesNowOutOfRange.Clear();
					}
				}
			}
			finally
			{
				if (ms != null)
				{
					ms.Dispose();
				}
			}
			ServerMain.FrameProfiler.Mark("physicsmanager-sendstatechanged");
		}

		public void GatherUpdatePacketsFromAllThreads()
		{
			Dictionary<long, Packet_EntityAttributes> eFullUpdate = this.entitiesFullUpdate[0];
			Dictionary<long, Packet_EntityAttributeUpdate> ePartialUpdate = this.entitiesPartialUpdate[0];
			Dictionary<long, Packet_EntityAttributes> eDebugUpdate = this.entitiesDebugUpdate[0];
			for (int i = 1; i < this.maxPhysicsThreads; i++)
			{
				eFullUpdate.AddRange(this.entitiesFullUpdate[i]);
				ePartialUpdate.AddRange(this.entitiesPartialUpdate[i]);
				eDebugUpdate.AddRange(this.entitiesDebugUpdate[i]);
				this.entitiesFullUpdate[i].Clear();
				this.entitiesPartialUpdate[i].Clear();
				this.entitiesDebugUpdate[i].Clear();
			}
		}

		public void SendAttributesViaTCP(List<ConnectedClient> clientList)
		{
			Dictionary<long, Packet_EntityAttributes> eFullUpdate = this.entitiesFullUpdate[0];
			Dictionary<long, Packet_EntityAttributeUpdate> ePartialUpdate = this.entitiesPartialUpdate[0];
			Dictionary<long, Packet_EntityAttributes> eDebugUpdate = this.entitiesDebugUpdate[0];
			List<Packet_EntityAttributes> cliententitiesFullUpdate = this.cliententitiesFullUpdate;
			List<Packet_EntityAttributeUpdate> cliententitiesPartialUpdate = this.cliententitiesPartialUpdate;
			List<Packet_EntityAttributes> cliententitiesDebugUpdate = this.cliententitiesDebugUpdate;
			bool debugMode = this.server.Config.EntityDebugMode;
			foreach (ConnectedClient client in clientList)
			{
				cliententitiesFullUpdate.Clear();
				cliententitiesPartialUpdate.Clear();
				cliententitiesDebugUpdate.Clear();
				try
				{
					try
					{
						foreach (Entity entity in client.threadedTrackedEntities[0])
						{
							long entityId = entity.EntityId;
							Packet_EntityAttributes pf;
							if (eFullUpdate.TryGetValue(entityId, out pf))
							{
								cliententitiesFullUpdate.Add(pf);
							}
							Packet_EntityAttributeUpdate pp;
							if (ePartialUpdate.TryGetValue(entityId, out pp))
							{
								cliententitiesPartialUpdate.Add(pp);
							}
							Packet_EntityAttributes pd;
							if (debugMode && eDebugUpdate.TryGetValue(entityId, out pd))
							{
								cliententitiesDebugUpdate.Add(pd);
							}
						}
					}
					catch (InvalidOperationException)
					{
					}
					if (cliententitiesFullUpdate.Count > 0 || cliententitiesPartialUpdate.Count > 0)
					{
						this.server.SendPacket(client.Id, ServerPackets.GetBulkEntityAttributesPacket(cliententitiesFullUpdate, cliententitiesPartialUpdate));
					}
					if (cliententitiesDebugUpdate.Count > 0)
					{
						this.server.SendPacket(client.Id, ServerPackets.GetBulkEntityDebugAttributesPacket(cliententitiesDebugUpdate));
					}
				}
				finally
				{
					cliententitiesFullUpdate.Clear();
					cliententitiesPartialUpdate.Clear();
					cliententitiesDebugUpdate.Clear();
					client.threadedTrackedEntities[0].Clear();
				}
			}
			eFullUpdate.Clear();
			ePartialUpdate.Clear();
			eDebugUpdate.Clear();
		}

		public void BuildPositionPacket(Entity entity, bool forceUpdate, Dictionary<long, Packet_EntityPosition> entityPositionPackets, Dictionary<long, AnimationPacket> entityAnimPackets)
		{
			if (entity is EntityPlayer)
			{
				return;
			}
			EntityAgent entityAgent = entity as EntityAgent;
			if (entity.AnimManager != null && (entity.AnimManager.AnimationsDirty || entity.IsTeleport))
			{
				entityAnimPackets[entity.EntityId] = new AnimationPacket(entity);
				entity.AnimManager.AnimationsDirty = false;
			}
			if (forceUpdate || !entity.ServerPos.BasicallySameAs(entity.PreviousServerPos, 0.0001) || (entityAgent != null && entityAgent.Controls.Dirty) || entity.tagsDirty)
			{
				int tick = entity.Attributes.GetIntAndIncrement("tick", 0);
				entityPositionPackets[entity.EntityId] = ServerPackets.getEntityPositionPacket(entity.ServerPos, entity, tick);
				if (entityAgent != null)
				{
					entityAgent.Controls.Dirty = false;
				}
				entity.tagsDirty = false;
			}
			this.CompletePositionUpdate(entity);
		}

		private EntityTagPacket BuildAttributesPackets(Entity entity, FastMemoryStream ms, bool debugMode, Dictionary<long, Packet_EntityAttributes> eFullUpdate, Dictionary<long, Packet_EntityAttributeUpdate> ePartialUpdate, Dictionary<long, Packet_EntityAttributes> eDebugUpdate)
		{
			EntityTagPacket result = null;
			SyncedTreeAttribute WatchedAttributes = entity.WatchedAttributes;
			if (WatchedAttributes.AllDirty)
			{
				ms.Reset();
				eFullUpdate[entity.EntityId] = ServerPackets.GetEntityPacket(ms, entity);
				entity.tagsDirty = false;
			}
			else
			{
				if (WatchedAttributes.PartialDirty)
				{
					ms.Reset();
					ePartialUpdate[entity.EntityId] = ServerPackets.GetEntityPartialAttributePacket(ms, entity);
				}
				if (entity.tagsDirty)
				{
					result = ServerPackets.GetEntityTagPacket(entity);
					entity.tagsDirty = false;
				}
			}
			if (debugMode && (entity.DebugAttributes.AllDirty || entity.DebugAttributes.PartialDirty))
			{
				ms.Reset();
				eDebugUpdate[entity.EntityId] = ServerPackets.GetEntityDebugAttributePacket(ms, entity);
			}
			WatchedAttributes.MarkClean();
			return result;
		}

		public void SendPositionsAndAnimations(Dictionary<long, Packet_EntityPosition> entityPositionPackets, Dictionary<long, AnimationPacket> entityAnimPackets, int zeroBasedThreadNum, bool stateUpdateTick)
		{
			List<Packet_EntityPosition> positionUpdate = new List<Packet_EntityPosition>();
			List<AnimationPacket> animationUpdate = new List<AnimationPacket>();
			List<EntityTagPacket> tagUpdate = new List<EntityTagPacket>();
			ConcurrentDictionary<long, EntityTagPacket> entityTagPackets = this.entitiesTagPackets;
			foreach (ConnectedClient client in this.ClientList)
			{
				positionUpdate.Clear();
				animationUpdate.Clear();
				tagUpdate.Clear();
				if (stateUpdateTick)
				{
					List<Entity> TrackedEntities = client.threadedTrackedEntities[zeroBasedThreadNum];
					foreach (Entity entity in TrackedEntities)
					{
						long id = entity.EntityId;
						Packet_EntityPosition pu;
						if (entityPositionPackets.TryGetValue(id, out pu))
						{
							positionUpdate.Add(pu);
						}
						AnimationPacket au;
						if (entityAnimPackets.TryGetValue(id, out au))
						{
							animationUpdate.Add(au);
						}
						EntityTagPacket tu;
						if (entityTagPackets.TryGetValue(id, out tu))
						{
							tagUpdate.Add(tu);
						}
					}
					AnimationPacket auOwn;
					if (entityAnimPackets.TryGetValue(client.Entityplayer.EntityId, out auOwn) && !TrackedEntities.Contains(client.Entityplayer))
					{
						animationUpdate.Add(auOwn);
					}
				}
				else
				{
					foreach (long id2 in client.TrackedEntities)
					{
						Packet_EntityPosition pu2;
						if (entityPositionPackets.TryGetValue(id2, out pu2))
						{
							positionUpdate.Add(pu2);
						}
						AnimationPacket au2;
						if (entityAnimPackets.TryGetValue(id2, out au2))
						{
							animationUpdate.Add(au2);
						}
						EntityTagPacket tu2;
						if (entityTagPackets.TryGetValue(id2, out tu2))
						{
							tagUpdate.Add(tu2);
						}
					}
				}
				int positionsCount = positionUpdate.Count;
				if (positionsCount > 8 && !client.IsSinglePlayerClient && !client.FallBackToTcp)
				{
					for (int i = 0; i < positionsCount; i += 8)
					{
						Packet_EntityPosition[] chunk = new Packet_EntityPosition[Math.Min(8, positionsCount - i)];
						for (int j = 0; j < chunk.Length; j++)
						{
							chunk[j] = positionUpdate[i + j];
						}
						Packet_BulkEntityPosition bulkPositionPacket = new Packet_BulkEntityPosition();
						bulkPositionPacket.SetEntityPositions(chunk);
						this.udpNetwork.SendPacket_Threadsafe(client, bulkPositionPacket);
					}
				}
				else if (positionsCount > 0)
				{
					Packet_BulkEntityPosition bulkPositionPacket2 = new Packet_BulkEntityPosition();
					bulkPositionPacket2.SetEntityPositions(positionUpdate.ToArray());
					this.udpNetwork.SendPacket_Threadsafe(client, bulkPositionPacket2);
				}
				if (animationUpdate.Count > 0)
				{
					BulkAnimationPacket bulkAnimationPacket = new BulkAnimationPacket
					{
						Packets = animationUpdate.ToArray()
					};
					this.AnimationsAndTagsChannel.SendPacket<BulkAnimationPacket>(bulkAnimationPacket, new IServerPlayer[] { client.Player });
				}
				if (tagUpdate.Count > 0)
				{
					foreach (EntityTagPacket tagPacket in tagUpdate)
					{
						this.AnimationsAndTagsChannel.SendPacket<EntityTagPacket>(tagPacket, new IServerPlayer[] { client.Player });
					}
				}
			}
		}

		private int PrepareEntitySpawns(Entity[] spawnsToSend, List<ConnectedClient> clientList)
		{
			int squareDistance = MagicNum.DefaultEntityTrackingRange * MagicNum.ServerChunkSize * MagicNum.DefaultEntityTrackingRange * MagicNum.ServerChunkSize;
			double[] positions = this.positions;
			int clientCount = clientList.Count;
			foreach (ConnectedClient connectedClient in clientList)
			{
				connectedClient.EntitySpawnsToSend.Clear();
			}
			int numberInRange = 0;
			for (int i = 0; i < spawnsToSend.Length; i++)
			{
				Entity entity = spawnsToSend[i];
				if (entity != null)
				{
					EntityPos spawnedEntityPos = entity.ServerPos;
					long spawnedEntityId = entity.EntityId;
					bool inRange = false;
					int j = 0;
					while (j < positions.Length && j < clientCount)
					{
						if (spawnedEntityPos.InRangeOf(positions[j], positions[j + 1], positions[j + 2], (float)squareDistance))
						{
							ConnectedClient connectedClient2 = clientList[j / 3];
							connectedClient2.TrackedEntities.Add(spawnedEntityId);
							connectedClient2.EntitySpawnsToSend.Add(entity);
							inRange = true;
						}
						j += 3;
					}
					if (inRange)
					{
						numberInRange++;
					}
					else
					{
						spawnsToSend[i] = null;
					}
				}
			}
			return numberInRange;
		}

		public void SendEntitySpawns(Entity[] spawnsToSend, List<ConnectedClient> clientList, Dictionary<long, Packet_EntityPosition> entityPositionPackets)
		{
			FastMemoryStream ms = this.offthreadProcess.buffer;
			ms.Reset();
			try
			{
				foreach (ConnectedClient client in clientList)
				{
					if (client.EntitySpawnsToSend.Count > 0)
					{
						this.server.SendPacket(client.Id, ServerPackets.GetEntitySpawnPacket(client.EntitySpawnsToSend, ms));
						foreach (Entity entity in client.EntitySpawnsToSend)
						{
							Packet_EntityPosition posPacket;
							if (entityPositionPackets.TryGetValue(entity.EntityId, out posPacket))
							{
								Packet_Server packet = new Packet_Server
								{
									Id = 80,
									EntityPosition = posPacket
								};
								this.server.SendPacket(client.Id, packet);
								entity.ServerPos.SetFromPacket(posPacket, entity);
								entity.Attributes.SetInt("tick", 2);
							}
						}
					}
				}
				foreach (Entity entity2 in spawnsToSend)
				{
					if (entity2 != null)
					{
						entity2.packet = null;
					}
				}
			}
			catch (Exception e)
			{
				ServerMain.Logger.Error(e);
			}
			entityPositionPackets.Clear();
		}

		private void DoFirstPhysicsTicks(Entity[] spawnsToSend, Dictionary<long, Packet_EntityPosition> entityPositionPackets)
		{
			float adjustedRate = 0.033333335f * PhysicsManager.rateModifier;
			EntityPos tmpPos = new EntityPos();
			foreach (Entity entity in spawnsToSend)
			{
				if (entity != null && !(entity is EntityPlayer))
				{
					tmpPos.SetFrom(entity.ServerPos);
					entityPositionPackets[entity.EntityId] = PhysicsManager.DoFirstPhysicsTick(entity, adjustedRate);
					entity.ServerPos.SetFrom(tmpPos);
				}
			}
		}

		private static Packet_EntityPosition DoFirstPhysicsTick(Entity entity, float adjustedRate)
		{
			foreach (EntityBehavior entityBehavior in entity.SidedProperties.Behaviors)
			{
				IPhysicsTickable tickable = entityBehavior as IPhysicsTickable;
				if (tickable != null)
				{
					tickable.Ticking = true;
					tickable.OnPhysicsTick(adjustedRate);
					tickable.OnPhysicsTick(adjustedRate);
					tickable.AfterPhysicsTick(adjustedRate);
					break;
				}
			}
			return ServerPackets.getEntityPositionPacket(entity.ServerPos, entity, 1);
		}

		internal void SendPrioritySpawn(Entity entity, ICollection<ConnectedClient> clientList)
		{
			Packet_Server spawnPacket = ServerPackets.GetEntitySpawnPacket(new List<Entity>(1) { entity });
			Packet_EntityPosition posPacket = PhysicsManager.DoFirstPhysicsTick(entity, 0.06666667f);
			Packet_Server packet2 = new Packet_Server
			{
				Id = 80,
				EntityPosition = posPacket
			};
			entity.Attributes.SetInt("tick", 2);
			int squareDistance = MagicNum.DefaultEntityTrackingRange * MagicNum.ServerChunkSize * MagicNum.DefaultEntityTrackingRange * MagicNum.ServerChunkSize;
			EntityPos entityPos = entity.ServerPos;
			foreach (ConnectedClient client in clientList)
			{
				if ((client.State == EnumClientState.Connected || client.State == EnumClientState.Playing) && client.Entityplayer != null)
				{
					EntityPos clientPos = client.Entityplayer.ServerPos;
					if (entityPos.InRangeOf(clientPos, squareDistance))
					{
						client.TrackedEntities.Add(entity.EntityId);
						this.server.SendPacket(client.Id, spawnPacket);
						this.server.SendPacket(client.Id, packet2);
					}
				}
			}
		}

		public void DoWork(int threadNumber)
		{
			float adjustedRate = 0.033333335f * PhysicsManager.rateModifier;
			List<IPhysicsTickable> tickables = this.tickables;
			int count = tickables.Count;
			int activePhysicsThreadsCount = this.maxPhysicsThreads;
			if (threadNumber == 0)
			{
				threadNumber = 1;
				activePhysicsThreadsCount = 1;
			}
			int countoffset = 480;
			int startpos = countoffset + (count - countoffset) * (threadNumber - 1) / activePhysicsThreadsCount;
			int endpos = countoffset + (count - countoffset) * threadNumber / activePhysicsThreadsCount;
			if (threadNumber == 1)
			{
				startpos = 0;
			}
			Dictionary<long, Packet_EntityPosition> entityPositionPackets = this.entitiesPositionPackets[threadNumber - 1];
			Dictionary<long, AnimationPacket> entityAnimPackets = this.entitiesAnimPackets[threadNumber - 1];
			entityPositionPackets.Clear();
			entityAnimPackets.Clear();
			if (this.attrUpdateAccum == 0f)
			{
				List<ConnectedClient> ClientList = this.ClientList;
				for (int i = startpos; i < endpos; i++)
				{
					IPhysicsTickable tickable = tickables[i];
					if (this.UpdateTrackedEntityState(tickable.Entity, ClientList, threadNumber - 1))
					{
						if (threadNumber == 1)
						{
							this.ActiveStateChanged(tickable.Entity);
						}
						else
						{
							this.stateChanges.Add(tickable.Entity);
						}
					}
				}
			}
			FrameProfilerUtil frameProfiler = null;
			if (threadNumber == 1)
			{
				frameProfiler = ServerMain.FrameProfiler;
				if (frameProfiler == null)
				{
					throw new Exception("FrameProfiler on main thread was null - this should be impossible!");
				}
				if (!frameProfiler.Enabled)
				{
					frameProfiler = null;
				}
			}
			if (activePhysicsThreadsCount == 1)
			{
				if (frameProfiler != null)
				{
					frameProfiler.Enter(string.Concat(new string[]
					{
						"entityphysics-mainthread (",
						count.ToString(),
						" entities, single-threaded) (",
						this.ticksToDo.ToString(),
						" physics ticks to do)"
					}));
				}
			}
			else if (frameProfiler != null)
			{
				frameProfiler.Enter(string.Concat(new string[]
				{
					"entityphysics-mainthread (",
					count.ToString(),
					" entities across ",
					activePhysicsThreadsCount.ToString(),
					" threads) (",
					this.ticksToDo.ToString(),
					" physics ticks to do)"
				}));
			}
			FastMemoryStream ms = this.entitiesUpdateReusableBuffers[threadNumber - 1];
			ms.Reset();
			bool debugMode = this.server.Config.EntityDebugMode;
			Dictionary<long, Packet_EntityAttributes> efu = null;
			Dictionary<long, Packet_EntityAttributeUpdate> epu = null;
			Dictionary<long, Packet_EntityAttributes> edu = null;
			if (this.attrUpdateAccum == 0f)
			{
				efu = this.entitiesFullUpdate[threadNumber - 1];
				epu = this.entitiesPartialUpdate[threadNumber - 1];
				edu = this.entitiesDebugUpdate[threadNumber - 1];
				efu.Clear();
				epu.Clear();
				edu.Clear();
			}
			try
			{
				int physicsTicknum = -1;
				float dt = this.deltaT;
				bool attrUpdateTick = this.attrUpdateAccum == 0f;
				while (++physicsTicknum < this.ticksToDo)
				{
					int doubleTick = (this.ticksToDo - physicsTicknum + 1) % 2;
					physicsTicknum += doubleTick;
					bool finalTick = physicsTicknum == this.ticksToDo - 1;
					bool firstTick = physicsTicknum == doubleTick;
					bool forceUpdate = (physicsTicknum + this.currentTick) % 30 <= doubleTick;
					for (int j = startpos; j < endpos; j++)
					{
						IPhysicsTickable tickable2 = tickables[j];
						Entity entity = tickable2.Entity;
						if (firstTick)
						{
							EntityBehavior[] serverBehaviors = entity.ServerBehaviorsThreadsafe;
							for (int k = 0; k < serverBehaviors.Length; k++)
							{
								serverBehaviors[k].OnGameTick(dt);
								if (frameProfiler != null)
								{
									frameProfiler.Mark(serverBehaviors[k].ProfilerName);
								}
							}
						}
						if (entity.IsTracked == 0)
						{
							if (finalTick)
							{
								entity.PositionTicked = true;
							}
						}
						else
						{
							PhysicsBehaviorBase physicsBehaviorBase = tickable2 as PhysicsBehaviorBase;
							object obj;
							if (physicsBehaviorBase == null)
							{
								obj = null;
							}
							else
							{
								IMountable mountableSupplier = physicsBehaviorBase.mountableSupplier;
								obj = ((mountableSupplier != null) ? mountableSupplier.Controller : null);
							}
							EntityPlayer p = obj as EntityPlayer;
							if (p == null || !p.Alive)
							{
								tickable2.OnPhysicsTick(adjustedRate);
								if (frameProfiler == null)
								{
									if (doubleTick != 0)
									{
										tickable2.OnPhysicsTick(adjustedRate);
									}
									this.BuildPositionPacket(entity, forceUpdate, entityPositionPackets, entityAnimPackets);
								}
								else
								{
									frameProfiler.Mark("physicstick-oneentity");
									if (doubleTick != 0)
									{
										tickable2.OnPhysicsTick(adjustedRate);
										frameProfiler.Mark("physicstick-oneentity");
									}
									this.BuildPositionPacket(entity, forceUpdate, entityPositionPackets, entityAnimPackets);
									frameProfiler.Mark("physicstick-buildpospacket");
								}
							}
							if (finalTick)
							{
								if (attrUpdateTick)
								{
									EntityTagPacket unsentTagPacket = this.BuildAttributesPackets(entity, ms, debugMode, efu, epu, edu);
									if (unsentTagPacket != null)
									{
										this.entitiesTagPackets[entity.EntityId] = unsentTagPacket;
									}
									if (frameProfiler != null)
									{
										frameProfiler.Mark("physicstick-buildattrpacket");
									}
								}
								entity.PositionTicked = true;
							}
						}
					}
					this.SendPositionsAndAnimations(entityPositionPackets, entityAnimPackets, threadNumber - 1, this.attrUpdateAccum == 0f);
					if (frameProfiler != null)
					{
						frameProfiler.Mark("physicsmanager-udp");
					}
				}
			}
			catch (Exception e)
			{
				ServerMain.Logger.Error("Error while enumerating tickables. Tickables total count is " + tickables.Count.ToString());
				ServerMain.Logger.Error(e);
			}
			entityPositionPackets.Clear();
			entityAnimPackets.Clear();
			if (frameProfiler != null)
			{
				frameProfiler.Leave();
			}
		}

		private void SendPositionsForNonTickableEntities(bool doBuildAttributes)
		{
			Dictionary<long, Packet_EntityPosition> entityPositionPackets = this.entitiesPositionPackets[0];
			Dictionary<long, AnimationPacket> entityAnimPackets = this.entitiesAnimPackets[0];
			IDictionary<long, EntityTagPacket> entityTagPackets = this.entitiesTagPackets;
			entityPositionPackets.Clear();
			entityAnimPackets.Clear();
			entityTagPackets.Clear();
			bool forceUpdate = this.currentTick % 30 < this.ticksToDo;
			Dictionary<long, Packet_EntityAttributes> eFullUpdate = null;
			Dictionary<long, Packet_EntityAttributeUpdate> ePartialUpdate = null;
			Dictionary<long, Packet_EntityAttributes> eDebugUpdate = null;
			bool debugMode = this.server.Config.EntityDebugMode;
			FastMemoryStream ms = null;
			if (doBuildAttributes)
			{
				eFullUpdate = this.entitiesFullUpdate[0];
				ePartialUpdate = this.entitiesPartialUpdate[0];
				eDebugUpdate = this.entitiesDebugUpdate[0];
				ms = new FastMemoryStream(64);
			}
			float dt = this.deltaT;
			FrameProfilerUtil frameProfiler = ServerMain.FrameProfiler;
			foreach (Entity entity in this.server.LoadedEntities.Values)
			{
				if (entity.PositionTicked)
				{
					entity.PositionTicked = false;
				}
				else
				{
					foreach (EntityBehavior behavior in entity.ServerBehaviorsThreadsafe)
					{
						behavior.OnGameTick(dt);
						frameProfiler.Mark(behavior.ProfilerName);
					}
					if (doBuildAttributes && this.UpdateTrackedEntityState(entity, this.ClientList, 0))
					{
						this.ActiveStateChanged(entity);
					}
					if (entity.IsTracked != 0)
					{
						if (this.ticksToDo > 0)
						{
							this.BuildPositionPacket(entity, forceUpdate, entityPositionPackets, entityAnimPackets);
						}
						if (doBuildAttributes)
						{
							EntityTagPacket tagPacket = this.BuildAttributesPackets(entity, ms, debugMode, eFullUpdate, ePartialUpdate, eDebugUpdate);
							if (tagPacket != null)
							{
								this.entitiesTagPackets[entity.EntityId] = tagPacket;
							}
							frameProfiler.Mark("physicstick-buildattrpacket");
						}
					}
				}
			}
			if (entityPositionPackets.Count + entityAnimPackets.Count + entityTagPackets.Count > 0)
			{
				this.SendPositionsAndAnimations(entityPositionPackets, entityAnimPackets, 0, doBuildAttributes);
			}
			entityPositionPackets.Clear();
			entityAnimPackets.Clear();
			entityTagPackets.Clear();
		}

		public bool ShouldExit()
		{
			return this.server.stopped || this.server.exit.exit;
		}

		public void HandleException(Exception e)
		{
			ServerMain.Logger.Error("Error thrown while ticking physics:\n{0}\n{1}", new object[] { e.Message, e.StackTrace });
		}

		public void StartWorkerThread(int threadNum)
		{
			try
			{
				while (this.tickables.Count < 120)
				{
					if (this.ShouldExit())
					{
						return;
					}
					Thread.Sleep(15);
				}
			}
			catch (Exception)
			{
			}
			this.server.EventManager.TriggerPhysicsThreadStart();
			this.loadBalancer.WorkerThreadLoop(this, threadNum, 1);
		}

		public void Dispose()
		{
			ServerMain serverMain = this.server;
			if (serverMain != null)
			{
				serverMain.UnregisterGameTickListener(this.listener);
			}
			this.tickables.Clear();
		}

		private const int HiResTrackingRange = 2500;

		public const float AttributesToClientsInterval = 0.2f;

		private const int firstTick = 1;

		public readonly ConcurrentQueue<IPhysicsTickable> toAdd = new ConcurrentQueue<IPhysicsTickable>();

		public readonly ConcurrentQueue<IPhysicsTickable> toRemove = new ConcurrentQueue<IPhysicsTickable>();

		private const float tickInterval = 0.033333335f;

		private readonly ICoreServerAPI sapi;

		private readonly ServerUdpNetwork udpNetwork;

		public readonly IServerNetworkChannel AnimationsAndTagsChannel;

		private readonly ServerMain server;

		private readonly LoadBalancer loadBalancer;

		private int maxPhysicsThreads;

		private int ticksToDo;

		private readonly long listener;

		private float physicsTickAccum;

		private float attrUpdateAccum;

		private readonly List<IPhysicsTickable> tickables = new List<IPhysicsTickable>();

		private ServerSystemEntitySimulation es;

		private List<Packet_EntityAttributes> cliententitiesFullUpdate = new List<Packet_EntityAttributes>();

		private List<Packet_EntityAttributeUpdate> cliententitiesPartialUpdate = new List<Packet_EntityAttributeUpdate>();

		private List<Packet_EntityAttributes> cliententitiesDebugUpdate = new List<Packet_EntityAttributes>();

		private Dictionary<long, Packet_EntityAttributes>[] entitiesFullUpdate;

		private Dictionary<long, Packet_EntityAttributeUpdate>[] entitiesPartialUpdate;

		private Dictionary<long, Packet_EntityAttributes>[] entitiesDebugUpdate;

		private Dictionary<long, Packet_EntityPosition>[] entitiesPositionPackets;

		private Dictionary<long, AnimationPacket>[] entitiesAnimPackets;

		private ConcurrentDictionary<long, EntityTagPacket> entitiesTagPackets;

		private FastMemoryStream[] entitiesUpdateReusableBuffers;

		private double[] positions = new double[3];

		private readonly EntityDespawnData outofRangeDespawnData = new EntityDespawnData
		{
			Reason = EnumDespawnReason.OutOfRange
		};

		private List<long> alreadyTracked = new List<long>();

		private List<Entity> newlyTracked = new List<Entity>();

		private List<ConnectedClient> ClientList;

		private ConcurrentBag<Entity> stateChanges = new ConcurrentBag<Entity>();

		private PhysicsManager.PhysicsOffthreadTasks offthreadProcess;

		private int currentTick;

		private static float rateModifier = 1f;

		private float deltaT;

		private class PhysicsOffthreadTasks
		{
			public PhysicsOffthreadTasks(PhysicsManager manager)
			{
				this.physicsManager = manager;
			}

			internal void Start()
			{
				this.physicsManager.server.EventManager.TriggerPhysicsThreadStart();
				while (!this.physicsManager.ShouldExit())
				{
					if (this.queue.IsEmpty)
					{
						try
						{
							this.idle = true;
							lock (this)
							{
								Monitor.Wait(this, 10);
							}
							this.idle = false;
						}
						catch (ThreadInterruptedException)
						{
						}
					}
					Action a;
					while (this.queue.TryDequeue(out a))
					{
						this.busy = true;
						if (!this.physicsManager.ShouldExit())
						{
							try
							{
								a();
								continue;
							}
							catch (Exception e)
							{
								ServerMain.Logger.Error(e);
								continue;
							}
							break;
						}
						break;
					}
					this.busy = false;
				}
			}

			internal void QueueAsyncTask(Action a)
			{
				if (this.physicsManager.server.ReducedServerThreads)
				{
					a();
					return;
				}
				this.queue.Enqueue(a);
				if (this.idle)
				{
					lock (this)
					{
						Monitor.Pulse(this);
					}
				}
			}

			internal bool Busy()
			{
				return this.busy || !this.queue.IsEmpty;
			}

			private readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();

			private readonly PhysicsManager physicsManager;

			private bool idle;

			private bool busy;

			internal FastMemoryStream buffer = new FastMemoryStream();
		}
	}
}
