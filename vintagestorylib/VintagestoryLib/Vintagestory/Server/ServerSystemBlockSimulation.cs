using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Database;

namespace Vintagestory.Server
{
	public class ServerSystemBlockSimulation : ServerSystem
	{
		public ServerSystemBlockSimulation(ServerMain server)
			: base(server)
		{
			server.RegisterGameTickListener(new Action<float>(this.UpdateEvery100ms), 100, 0);
			server.PacketHandlers[3] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleBlockPlaceOrBreak);
			server.PacketHandlers[22] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleBlockEntityPacket);
			server.OnHandleBlockInteract = new HandleHandInteractionDelegate(this.HandleBlockInteract);
		}

		public override void OnBeginInitialization()
		{
			this.server.api.RegisterBlock(new Block
			{
				DrawType = EnumDrawType.Empty,
				MatterState = EnumMatterState.Gas,
				BlockMaterial = EnumBlockMaterial.Air,
				Code = new AssetLocation("air"),
				Sounds = new BlockSounds(),
				RenderPass = EnumChunkRenderPass.Liquid,
				Replaceable = 9999,
				MaterialDensity = 1,
				LightAbsorption = 0,
				CollisionBoxes = null,
				SelectionBoxes = null,
				RainPermeable = true,
				SideSolid = new SmallBoolArray(0),
				SideAo = new SmallBoolArray(0),
				AllSidesOpaque = false
			});
			Item noitem = new Item(0);
			this.server.api.RegisterItem(new Item
			{
				Code = new AssetLocation("air")
			});
			for (int i = 1; i < 4000; i++)
			{
				this.server.Items.Add(noitem);
			}
			this.server.api.eventapi.ChunkColumnLoaded += this.Event_ChunkColumnLoaded;
		}

		private void Event_ChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
		{
		}

		public override void OnLoadAssets()
		{
			this.server.api.Logger.VerboseDebug("Block simulation resolving collectibles");
			this.server.LoadCollectibles(this.server.Items, this.server.Blocks);
			IList<Block> serverBlocks = this.server.Blocks;
			for (int i = 0; i < serverBlocks.Count; i++)
			{
				Block block = serverBlocks[i];
				if (block != null)
				{
					AssetLocation blockName = block.Code;
					if (block.Drops != null)
					{
						BlockDropItemStack[] drops = block.Drops;
						for (int j = 0; j < drops.Length; j++)
						{
							drops[j].Resolve(this.server, "Block ", blockName);
						}
					}
					if (block.CreativeInventoryStacks != null)
					{
						for (int k = 0; k < block.CreativeInventoryStacks.Length; k++)
						{
							CreativeTabAndStackList list = block.CreativeInventoryStacks[k];
							for (int l = 0; l < list.Stacks.Length; l++)
							{
								list.Stacks[l].Resolve(this.server, "Creative inventory stack of block ", blockName, true);
							}
						}
					}
					CombustibleProperties combustibleProps = block.CombustibleProps;
					if (((combustibleProps != null) ? combustibleProps.SmeltedStack : null) != null)
					{
						block.CombustibleProps.SmeltedStack.Resolve(this.server, "Smeltedstack of Block ", blockName, true);
					}
					FoodNutritionProperties nutritionProps = block.NutritionProps;
					if (((nutritionProps != null) ? nutritionProps.EatenStack : null) != null)
					{
						block.NutritionProps.EatenStack.Resolve(this.server, "Eatenstack of Block ", blockName, true);
					}
					if (block.TransitionableProps != null)
					{
						foreach (TransitionableProperties props in block.TransitionableProps)
						{
							if (props.Type != EnumTransitionType.None)
							{
								JsonItemStack transitionedStack = props.TransitionedStack;
								if (transitionedStack != null)
								{
									transitionedStack.Resolve(this.server, props.Type.ToString() + " Transition stack of Block ", blockName, true);
								}
							}
						}
					}
					GrindingProperties grindingProps = block.GrindingProps;
					if (((grindingProps != null) ? grindingProps.GroundStack : null) != null)
					{
						block.GrindingProps.GroundStack.Resolve(this.server, "Grinded stack of Block ", blockName, true);
						if (block.GrindingProps.usedObsoleteNotation)
						{
							this.server.api.Logger.Warning("Block code {0}: Property GrindedStack is obsolete, please use GroundStack instead", new object[] { block.Code });
						}
					}
					CrushingProperties crushingProps = block.CrushingProps;
					if (((crushingProps != null) ? crushingProps.CrushedStack : null) != null)
					{
						block.CrushingProps.CrushedStack.Resolve(this.server, "Crushed stack of Block ", blockName, true);
					}
				}
			}
			this.server.api.Logger.VerboseDebug("Resolved blocks stacks");
			((List<Item>)this.server.Items).ForEach(delegate(Item item)
			{
				if (item != null)
				{
					AssetLocation itemName = item.Code;
					if (itemName != null)
					{
						CreativeTabAndStackList[] creativeInventoryStacks = item.CreativeInventoryStacks;
						if (creativeInventoryStacks != null)
						{
							for (int n = 0; n < creativeInventoryStacks.Length; n++)
							{
								JsonItemStack[] list2 = creativeInventoryStacks[n].Stacks;
								for (int k2 = 0; k2 < list2.Length; k2++)
								{
									list2[k2].Resolve(this.server, "Creative inventory stack of Item ", itemName, true);
								}
							}
						}
						if (item.CombustibleProps != null && item.CombustibleProps.SmeltedStack != null)
						{
							item.CombustibleProps.SmeltedStack.Resolve(this.server, "Combustible props for Item ", itemName, true);
						}
						FoodNutritionProperties nutritionProps2 = item.NutritionProps;
						if (((nutritionProps2 != null) ? nutritionProps2.EatenStack : null) != null)
						{
							item.NutritionProps.EatenStack.Resolve(this.server, "Eatenstack of Item ", itemName, true);
						}
						if (item.TransitionableProps != null)
						{
							foreach (TransitionableProperties props2 in item.TransitionableProps)
							{
								if (props2.Type != EnumTransitionType.None)
								{
									JsonItemStack transitionedStack2 = props2.TransitionedStack;
									if (transitionedStack2 != null)
									{
										transitionedStack2.Resolve(this.server, props2.Type.ToString() + " Transition stack of Item ", itemName, true);
									}
								}
							}
						}
						GrindingProperties grindingProps2 = item.GrindingProps;
						if (((grindingProps2 != null) ? grindingProps2.GroundStack : null) != null)
						{
							item.GrindingProps.GroundStack.Resolve(this.server, "Grinded stack of item ", itemName, true);
							if (item.GrindingProps.usedObsoleteNotation)
							{
								this.server.api.Logger.Warning("Item code {0}: Property GrindedStack is obsolete, please use GroundStack instead", new object[] { item.Code });
							}
						}
						CrushingProperties crushingProps2 = item.CrushingProps;
						if (((crushingProps2 != null) ? crushingProps2.CrushedStack : null) != null)
						{
							item.CrushingProps.CrushedStack.Resolve(this.server, "Crushed stack of item ", itemName, true);
						}
					}
				}
			});
			this.server.api.Logger.VerboseDebug("Resolved items stacks");
		}

		public override void OnBeginConfiguration()
		{
			IChatCommandApi chatCommands = this.server.api.ChatCommands;
			CommandArgumentParsers parsers = this.server.api.ChatCommands.Parsers;
			ServerCoreAPI api = this.server.api;
			chatCommands.Get("debug").BeginSub("bt").WithDesc("Block ticking debug subsystem")
				.BeginSub("at")
				.WithDesc("Tick a block at given position")
				.WithArgs(new ICommandArgumentParser[] { parsers.WorldPosition("position") })
				.HandleWith(new OnCommandDelegate(this.onTickBlockCmd))
				.EndSub()
				.BeginSub("qi")
				.WithDesc("Queue info")
				.HandleWith(new OnCommandDelegate(this.onTickQueueCmd))
				.EndSub()
				.BeginSub("qc")
				.WithDesc("Clear tick queue")
				.HandleWith(new OnCommandDelegate(this.onTickQueueClearCmd))
				.EndSub()
				.EndSub();
			base.OnBeginConfiguration();
		}

		private TextCommandResult onTickQueueClearCmd(TextCommandCallingArgs args)
		{
			this.queuedTicks = new ConcurrentQueue<object>();
			return TextCommandResult.Success("Queue is now cleared", null);
		}

		private TextCommandResult onTickQueueCmd(TextCommandCallingArgs args)
		{
			return TextCommandResult.Success(this.queuedTicks.Count.ToString() + " elements in queue", null);
		}

		private TextCommandResult onTickBlockCmd(TextCommandCallingArgs args)
		{
			TextCommandResult textCommandResult;
			try
			{
				BlockPos blockPos = (args[0] as Vec3d).AsBlockPos;
				Block block = this.server.Api.World.BlockAccessor.GetBlock(blockPos);
				if (this.tryTickBlock(block, blockPos))
				{
					textCommandResult = TextCommandResult.Success(string.Concat(new string[]
					{
						"Accepted tick [block=",
						block.Code,
						"] at [",
						blockPos.ToString(),
						"]"
					}), null);
				}
				else
				{
					textCommandResult = TextCommandResult.Success(string.Concat(new string[]
					{
						"Declined tick [block=",
						block.Code,
						"] at [",
						blockPos.ToString(),
						"]"
					}), null);
				}
			}
			catch (Exception e)
			{
				ServerMain.Logger.Error(e);
				textCommandResult = TextCommandResult.Success("An unexpected error occurred trying to tick block: " + e.Message, null);
			}
			return textCommandResult;
		}

		public override void OnBeginModsAndConfigReady()
		{
			IList<Block> serverBlocks = this.server.Blocks;
			for (int i = 0; i < serverBlocks.Count; i++)
			{
				Block block = serverBlocks[i];
				if (block != null)
				{
					block.OnLoadedNative(this.server.api);
				}
			}
			this.server.api.Logger.Debug("Block simulation loaded blocks");
			((List<Item>)this.server.Items).ForEach(delegate(Item item)
			{
				if (item != null)
				{
					item.OnLoadedNative(this.server.api);
				}
			});
			this.server.api.Logger.Debug("Block simulation loaded items");
		}

		public override void OnPlayerJoin(ServerPlayer player)
		{
			object obj = this.clientIdsLock;
			lock (obj)
			{
				this.clientIds.Add(player.ClientId);
			}
		}

		public override void OnPlayerDisconnect(ServerPlayer player)
		{
			object obj = this.clientIdsLock;
			lock (obj)
			{
				this.clientIds.Remove(player.ClientId);
			}
		}

		private void HandleBlockEntityPacket(Packet_Client packet, ConnectedClient client)
		{
			Packet_BlockEntityPacket p = packet.BlockEntityPacket;
			BlockEntity be = this.server.WorldMap.GetBlockEntity(new BlockPos(p.X, p.Y, p.Z));
			if (be != null)
			{
				be.OnReceivedClientPacket(client.Player, p.Packetid, p.Data);
			}
		}

		internal void HandleBlockPlaceOrBreak(Packet_Client packet, ConnectedClient client)
		{
			Packet_ClientBlockPlaceOrBreak p = packet.BlockPlaceOrBreak;
			BlockSelection blockSel = new BlockSelection
			{
				DidOffset = (p.DidOffset > 0),
				Face = BlockFacing.ALLFACES[p.OnBlockFace],
				Position = new BlockPos(p.X, p.Y, p.Z),
				HitPosition = new Vec3d(CollectibleNet.DeserializeDouble(p.HitX), CollectibleNet.DeserializeDouble(p.HitY), CollectibleNet.DeserializeDouble(p.HitZ)),
				SelectionBoxIndex = p.SelectionBoxIndex
			};
			if (client.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
			{
				return;
			}
			string claimant;
			EnumWorldAccessResponse resp;
			if ((resp = this.server.WorldMap.TestBlockAccess(client.Player, blockSel, EnumBlockAccessFlags.BuildOrBreak, out claimant)) != EnumWorldAccessResponse.Granted)
			{
				this.RevertBlockInteractions(client.Player, blockSel.Position);
				string code = "noprivilege-buildbreak-" + resp.ToString().ToLowerInvariant();
				if (claimant == null)
				{
					claimant = "?";
				}
				else if (claimant.StartsWithOrdinal("custommessage-"))
				{
					code = "noprivilege-buildbreak-" + claimant.Substring("custommessage-".Length);
				}
				client.Player.SendIngameError(code, null, new object[] { claimant });
				return;
			}
			if (p.Mode == 2)
			{
				WorldChunk c = this.server.BlockAccessor.GetChunkAtBlockPos(blockSel.Position) as WorldChunk;
				if (c != null)
				{
					c.BreakDecor(this.server, blockSel.Position, blockSel.Face, null);
					c.MarkModified();
				}
				return;
			}
			Block potentiallyIce = this.server.WorldMap.RelaxedBlockAccess.GetBlock(blockSel.Position, 2);
			int oldBlockId;
			if (potentiallyIce.SideSolid.Any)
			{
				oldBlockId = potentiallyIce.BlockId;
			}
			else
			{
				oldBlockId = this.server.WorldMap.RelaxedBlockAccess.GetBlock(blockSel.Position).Id;
			}
			ItemSlot activeHotbarSlot = client.Player.inventoryMgr.ActiveHotbarSlot;
			ItemStack placedBlockStack = ((activeHotbarSlot != null) ? activeHotbarSlot.Itemstack : null);
			if (!this.TryModifyBlockInWorld(client.Player, p))
			{
				this.RevertBlockInteractions(client.Player, blockSel.Position);
				return;
			}
			this.server.TriggerNeighbourBlocksUpdate(blockSel.Position);
			int mode = p.Mode;
			if (mode != 0)
			{
				if (mode == 1)
				{
					this.server.EventManager.TriggerDidPlaceBlock(client.Player, oldBlockId, blockSel, placedBlockStack);
					return;
				}
			}
			else
			{
				this.server.EventManager.TriggerDidBreakBlock(client.Player, oldBlockId, blockSel);
			}
		}

		internal void HandleBlockInteract(Packet_Client packet, ConnectedClient client)
		{
			ServerPlayer player = client.Player;
			Packet_ClientHandInteraction p = packet.HandInteraction;
			if (client.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
			{
				return;
			}
			if (p.UseType == 0)
			{
				return;
			}
			if (p.MouseButton != 2)
			{
				return;
			}
			BlockPos pos = new BlockPos(p.X, p.Y, p.Z);
			BlockFacing facing = BlockFacing.ALLFACES[p.OnBlockFace];
			Vec3d hitPos = new Vec3d(CollectibleNet.DeserializeDoublePrecise(p.HitX), CollectibleNet.DeserializeDoublePrecise(p.HitY), CollectibleNet.DeserializeDoublePrecise(p.HitZ));
			BlockSelection blockSel = new BlockSelection
			{
				Position = pos,
				Face = facing,
				HitPosition = hitPos,
				SelectionBoxIndex = p.SelectionBoxIndex
			};
			EnumWorldAccessResponse resp;
			if ((resp = this.server.WorldMap.TestBlockAccess(client.Player, blockSel, EnumBlockAccessFlags.Use)) != EnumWorldAccessResponse.Granted)
			{
				this.RevertBlockInteractions(client.Player, blockSel.Position);
				string code = "noprivilege-use-" + resp.ToString().ToLowerInvariant();
				LandClaim claim = this.server.WorldMap.GetBlockingLandClaimant(client.Player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak);
				client.Player.SendIngameError(code, null, new object[] { (claim != null) ? claim.LastKnownOwnerName : null });
				return;
			}
			Block block = this.server.BlockAccessor.GetBlock(pos);
			EntityControls controls = player.Entity.Controls;
			float secondsPassed = (float)(this.server.ElapsedMilliseconds - controls.UsingBeginMS) / 1000f;
			switch (p.EnumHandInteract)
			{
			case 4:
				controls.HandUse = (block.OnBlockInteractStart(this.server, player, blockSel) ? EnumHandInteract.BlockInteract : EnumHandInteract.None);
				controls.UsingBeginMS = this.server.ElapsedMilliseconds;
				controls.UsingCount = 0;
				this.server.EventManager.TriggerDidUseBlock(client.Player, blockSel);
				return;
			case 5:
			{
				while (controls.HandUse != EnumHandInteract.None && controls.UsingCount < p.UsingCount)
				{
					this.callOnUsingBlock(player, block, blockSel, ref secondsPassed, false);
				}
				EnumItemUseCancelReason cancelReason = (EnumItemUseCancelReason)p.CancelReason;
				controls.HandUse = (block.OnBlockInteractCancel(secondsPassed, this.server, player, blockSel, cancelReason) ? EnumHandInteract.None : EnumHandInteract.BlockInteract);
				return;
			}
			case 6:
				while (controls.HandUse != EnumHandInteract.None && controls.UsingCount < p.UsingCount)
				{
					this.callOnUsingBlock(player, block, blockSel, ref secondsPassed, true);
				}
				if (controls.HandUse != EnumHandInteract.None)
				{
					controls.HandUse = EnumHandInteract.None;
					block.OnBlockInteractStop(secondsPassed, this.server, player, blockSel);
					return;
				}
				break;
			case 7:
				while (controls.HandUse != EnumHandInteract.None && controls.UsingCount < p.UsingCount)
				{
					this.callOnUsingBlock(player, block, blockSel, ref secondsPassed, true);
				}
				break;
			default:
				return;
			}
		}

		private void callOnUsingBlock(ServerPlayer player, Block block, BlockSelection blockSel, ref float secondsPassed, bool callStop = true)
		{
			EntityControls controls = player.Entity.Controls;
			controls.HandUse = (block.OnBlockInteractStep(secondsPassed, this.server, player, blockSel) ? EnumHandInteract.BlockInteract : EnumHandInteract.None);
			controls.UsingCount++;
			if (callStop && controls.HandUse == EnumHandInteract.None)
			{
				block.OnBlockInteractStop(secondsPassed, this.server, player, blockSel);
			}
			secondsPassed += 0.02f;
		}

		private void RevertBlockInteractions(IServerPlayer targetPlayer, BlockPos pos)
		{
			this.RevertBlockInteraction2(targetPlayer, pos, false);
			this.RevertBlockInteraction2(targetPlayer, pos.AddCopy(BlockFacing.NORTH), false);
			this.RevertBlockInteraction2(targetPlayer, pos.AddCopy(BlockFacing.EAST), false);
			this.RevertBlockInteraction2(targetPlayer, pos.AddCopy(BlockFacing.SOUTH), false);
			this.RevertBlockInteraction2(targetPlayer, pos.AddCopy(BlockFacing.UP), false);
			this.RevertBlockInteraction2(targetPlayer, pos.AddCopy(BlockFacing.DOWN), false);
			this.server.SendOwnPlayerData(targetPlayer, true, false);
		}

		private void RevertBlockInteraction2(IServerPlayer targetPlayer, BlockPos pos, bool sendPlayerData = true)
		{
			this.server.SendSetBlock(targetPlayer, this.server.WorldMap.RawRelaxedBlockAccess.GetBlockId(pos), pos.X, pos.InternalY, pos.Z, false);
			BlockEntity be = this.server.WorldMap.RawRelaxedBlockAccess.GetBlockEntity(pos);
			if (be != null)
			{
				this.server.SendBlockEntity(targetPlayer, be);
			}
			if (sendPlayerData)
			{
				this.server.SendOwnPlayerData(targetPlayer, true, false);
			}
		}

		private bool TryModifyBlockInWorld(ServerPlayer player, Packet_ClientBlockPlaceOrBreak cmd)
		{
			Vec3d hitPosition = new Vec3d(CollectibleNet.DeserializeDouble(cmd.HitX), CollectibleNet.DeserializeDouble(cmd.HitY), CollectibleNet.DeserializeDouble(cmd.HitZ));
			Vec3d target = new Vec3d((double)cmd.X + hitPosition.X, (double)cmd.Y + hitPosition.Y, (double)cmd.Z + hitPosition.Z);
			Vec3d source = player.Entity.Pos.XYZ.Add(player.Entity.LocalEyePos);
			bool pickRangeAllowed = this.server.PlayerHasPrivilege(player.ClientId, Privilege.pickingrange);
			ItemSlot hotbarSlot = player.inventoryMgr.ActiveHotbarSlot;
			if (this.server.Config.AntiAbuse != EnumProtectionLevel.Off && !pickRangeAllowed && source.SquareDistanceTo(target) > (player.WorldData.PickingRange + 0.7f) * (player.WorldData.PickingRange + 0.7f))
			{
				ServerMain.Logger.Notification("Client {0} tried to use/place a block out of range", new object[] { player.PlayerName });
				hotbarSlot.MarkDirty();
				return false;
			}
			BlockPos pos = new BlockPos(cmd.X, cmd.Y, cmd.Z);
			BlockSelection blockSel = new BlockSelection
			{
				Face = BlockFacing.ALLFACES[cmd.OnBlockFace],
				Position = pos,
				HitPosition = hitPosition,
				SelectionBoxIndex = cmd.SelectionBoxIndex,
				DidOffset = (cmd.DidOffset > 0)
			};
			if (cmd.Mode == 1)
			{
				if (hotbarSlot == null || hotbarSlot.Itemstack == null)
				{
					ServerMain.Logger.Notification("Client {0} tried to place a block but rejected because the client hand is empty", new object[] { player.PlayerName });
					return false;
				}
				if (hotbarSlot.Itemstack.Class != EnumItemClass.Block)
				{
					ServerMain.Logger.Notification("Client {0} tried to place a block but rejected because the itemstck in client hand is not a block", new object[] { player.PlayerName });
					return false;
				}
				int newBlockID = hotbarSlot.Itemstack.Id;
				Block newBlock = this.server.Blocks[newBlockID];
				if (newBlock == null)
				{
					ServerMain.Logger.Notification("Client {0} tried to place a block of id, which does not exist", new object[] { player.PlayerName, newBlockID });
					return false;
				}
				Block oldBlock = this.server.WorldMap.RawRelaxedBlockAccess.GetBlock(pos, newBlock.ForFluidsLayer ? 2 : 1);
				if (!oldBlock.IsReplacableBy(newBlock))
				{
					JsonObject attributes = newBlock.Attributes;
					JsonObject obj = ((attributes != null) ? attributes["ignoreServerReplaceableTest"] : null);
					if ((obj == null || !obj.Exists || !obj.AsBool(false)) && this.server.Blocks[newBlockID].decorBehaviorFlags == 0)
					{
						ServerMain.Logger.Notification("Client {0} tried to place a block but rejected because the client tried to overwrite an existing, non-replacable block {1}, id {2}", new object[] { player.PlayerName, oldBlock.Code, oldBlock.Id });
						return false;
					}
				}
				if (this.IsAnyPlayerInBlock(pos, newBlock, player))
				{
					ServerMain.Logger.Notification("Client {0} tried to place a block but rejected because it would intersect with another player", new object[] { player.PlayerName });
					return false;
				}
				string failureCode = "";
				if (!newBlock.TryPlaceBlock(this.server, player, hotbarSlot.Itemstack, blockSel, ref failureCode))
				{
					ServerMain.Logger.Notification("Client {0} tried to place a block but rejected because OnPlaceBlock returns false. Failure code {1}", new object[] { player.PlayerName, failureCode });
					return false;
				}
				ServerChunk serverchunk = this.server.WorldMap.GetChunk(blockSel.Position) as ServerChunk;
				if (serverchunk != null)
				{
					serverchunk.BlocksPlaced++;
					serverchunk.DirtyForSaving = true;
				}
				if (player.WorldData.CurrentGameMode != EnumGameMode.Creative)
				{
					ItemStack itemstack = hotbarSlot.Itemstack;
					int stackSize = itemstack.StackSize;
					itemstack.StackSize = stackSize - 1;
					if (hotbarSlot.Itemstack.StackSize <= 0)
					{
						hotbarSlot.Itemstack = null;
						this.server.BroadcastHotbarSlot(player, true);
					}
					hotbarSlot.MarkDirty();
				}
			}
			else
			{
				Block potentiallyIce = this.server.WorldMap.RelaxedBlockAccess.GetBlock(pos, 2);
				int blockid;
				if (potentiallyIce.SideSolid.Any)
				{
					blockid = potentiallyIce.BlockId;
				}
				else
				{
					blockid = this.server.WorldMap.RelaxedBlockAccess.GetBlock(pos, 1).Id;
				}
				Block block = this.server.Blocks[blockid];
				blockSel.Block = block;
				IItemStack heldItemstack = hotbarSlot.Itemstack;
				int miningTier = 0;
				if (heldItemstack != null)
				{
					miningTier = heldItemstack.Collectible.ToolTier;
				}
				if (player.WorldData.CurrentGameMode != EnumGameMode.Creative && block.RequiredMiningTier > miningTier)
				{
					ServerMain.Logger.Notification("Client {0} tried to break a block but rejected because his tools mining tier is too low", new object[] { player.PlayerName });
					return false;
				}
				float dropMul = 1f;
				EnumHandling handling = EnumHandling.PassThrough;
				this.server.EventManager.TriggerBreakBlock(player, blockSel, ref dropMul, ref handling);
				if (handling == EnumHandling.PassThrough)
				{
					if (heldItemstack != null)
					{
						heldItemstack.Collectible.OnBlockBrokenWith(this.server, player.Entity, hotbarSlot, blockSel, dropMul);
					}
					else
					{
						block.OnBlockBroken(this.server, pos, player, dropMul);
					}
					ServerChunk serverchunk2 = this.server.WorldMap.GetChunk(blockSel.Position) as ServerChunk;
					if (serverchunk2 != null)
					{
						serverchunk2.BlocksRemoved++;
						serverchunk2.DirtyForSaving = true;
					}
				}
				else
				{
					this.server.WorldMap.MarkBlockModified(blockSel.Position, true);
					this.server.WorldMap.MarkBlockEntityDirty(blockSel.Position);
				}
				if (hotbarSlot.Itemstack == null && heldItemstack != null)
				{
					this.server.BroadcastHotbarSlot(player, true);
				}
			}
			player.client.IsInventoryDirty = true;
			return true;
		}

		internal bool IsAnyPlayerInBlock(BlockPos pos, Block block, IPlayer ignorePlayer)
		{
			Cuboidf[] collisionboxes = block.GetCollisionBoxes(this.server.BlockAccessor, pos);
			if (collisionboxes == null)
			{
				return false;
			}
			foreach (IPlayer player in this.server.AllOnlinePlayers)
			{
				if (player.Entity != null)
				{
					int clientId = player.ClientId;
					int? num = ((ignorePlayer != null) ? new int?(ignorePlayer.ClientId) : null);
					if (!((clientId == num.GetValueOrDefault()) & (num != null)))
					{
						for (int i = 0; i < collisionboxes.Length; i++)
						{
							if (CollisionTester.AabbIntersect(collisionboxes[i], (double)pos.X, (double)pos.Y, (double)pos.Z, player.Entity.SelectionBox, player.Entity.Pos.XYZ))
							{
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		public override int GetUpdateInterval()
		{
			return this.server.Config.BlockTickInterval;
		}

		private void UpdateEvery100ms(float t1)
		{
			this.HandleDirtyAndUpdatedBlocks();
			this.SendDirtyBlockEntities();
		}

		private void HandleDirtyAndUpdatedBlocks()
		{
			int i = 0;
			while (this.server.UpdatedBlocks.Count > 0)
			{
				if (i++ >= 500)
				{
					break;
				}
				BlockPos pos = this.server.UpdatedBlocks.Dequeue();
				this.server.TriggerNeighbourBlocksUpdate(pos);
			}
			Vec4i vec;
			while (!this.server.DirtyBlocks.IsEmpty && this.server.DirtyBlocks.TryDequeue(out vec))
			{
				this.server.SendSetBlock(this.server.BlockAccessor.GetBlockRaw(vec.X, vec.Y, vec.Z, 0).Id, vec.X, vec.Y, vec.Z, vec.W, true);
			}
			if (!this.server.ModifiedBlocks.IsEmpty)
			{
				List<BlockPos> dirtyBlocks = new List<BlockPos>();
				BlockPos pos2;
				while (!this.server.ModifiedBlocks.IsEmpty && this.server.ModifiedBlocks.TryDequeue(out pos2))
				{
					this.server.WorldMap.RelaxedBlockAccess.GetBlock(pos2).OnNeighbourBlockChange(this.server, pos2, pos2);
					dirtyBlocks.Add(pos2);
				}
				this.server.SendSetBlocksPacket(dirtyBlocks, 47);
			}
			if (!this.server.ModifiedBlocksNoRelight.IsEmpty)
			{
				List<BlockPos> dirtyBlocksNoRelight = new List<BlockPos>();
				BlockPos pos3;
				while (!this.server.ModifiedBlocksNoRelight.IsEmpty && this.server.ModifiedBlocksNoRelight.TryDequeue(out pos3))
				{
					this.server.WorldMap.RelaxedBlockAccess.GetBlock(pos3).OnNeighbourBlockChange(this.server, pos3, pos3);
					dirtyBlocksNoRelight.Add(pos3);
				}
				this.server.SendSetBlocksPacket(dirtyBlocksNoRelight, 63);
			}
			if (this.server.ModifiedBlocksMinimal.Count > 0)
			{
				this.server.SendSetBlocksPacket(this.server.ModifiedBlocksMinimal, 70);
				this.server.ModifiedBlocksMinimal.Clear();
			}
			if (!this.server.ModifiedDecors.IsEmpty)
			{
				List<BlockPos> decorPositions = new List<BlockPos>();
				BlockPos pos4;
				while (!this.server.ModifiedDecors.IsEmpty && this.server.ModifiedDecors.TryDequeue(out pos4))
				{
					decorPositions.Add(pos4);
				}
				this.server.SendSetDecorsPackets(decorPositions);
			}
		}

		private void SendDirtyBlockEntities()
		{
			if (this.server.DirtyBlockEntities.IsEmpty)
			{
				return;
			}
			this.blockEntitiesPacked.Clear();
			this.noblockEntities.Clear();
			this.positionsDone.Clear();
			ConcurrentQueue<BlockPos> DirtyBlockEntities = this.server.DirtyBlockEntities;
			if (!DirtyBlockEntities.IsEmpty)
			{
				FastMemoryStream fastMemoryStream;
				if ((fastMemoryStream = ServerSystemBlockSimulation.reusableSendingStream) == null)
				{
					fastMemoryStream = (ServerSystemBlockSimulation.reusableSendingStream = new FastMemoryStream());
				}
				using (FastMemoryStream reusableStream = fastMemoryStream)
				{
					while (!DirtyBlockEntities.IsEmpty)
					{
						BlockPos pos;
						if (!this.server.DirtyBlockEntities.TryDequeue(out pos))
						{
							break;
						}
						if (this.positionsDone.Add(pos))
						{
							BlockEntity blockEntity = this.server.WorldMap.GetBlockEntity(pos);
							if (blockEntity != null)
							{
								this.blockEntitiesPacked.Add(this.BlockEntityToPacket(blockEntity, reusableStream));
							}
							else
							{
								this.noblockEntities.Add(pos);
							}
						}
					}
				}
			}
			if (this.blockEntitiesPacked.Count > 0 || this.noblockEntities.Count > 0)
			{
				foreach (ConnectedClient client in this.server.Clients.Values)
				{
					if (client.State != EnumClientState.Offline)
					{
						this.playerBlockEntitiesPacked.Clear();
						foreach (Packet_BlockEntity be in this.blockEntitiesPacked)
						{
							long index3d = this.server.WorldMap.ChunkIndex3D(be.PosX / 32, be.PosY / 32, be.PosZ / 32);
							if (client.DidSendChunk(index3d))
							{
								this.playerBlockEntitiesPacked.Add(be);
							}
						}
						if (this.playerBlockEntitiesPacked.Count + this.noblockEntities.Count > 0)
						{
							this.SendBlockEntitiesPacket(client, this.playerBlockEntitiesPacked, this.noblockEntities);
						}
					}
				}
			}
		}

		public override void OnServerTick(float dt)
		{
			if (this.server.RunPhase != EnumServerRunPhase.RunGame)
			{
				return;
			}
			int blockTickCount = 0;
			while (!this.queuedTicks.IsEmpty && blockTickCount < this.server.Config.MaxMainThreadBlockTicks)
			{
				object tickItem;
				if (this.queuedTicks.TryDequeue(out tickItem))
				{
					Block block = null;
					try
					{
						if (tickItem is FluidBlockPos)
						{
							BlockPos pos = (BlockPos)tickItem;
							block = this.server.api.World.BlockAccessor.GetBlock(pos, 2);
							block.OnServerGameTick(this.server.api.World, pos, null);
						}
						else if (tickItem is BlockPos)
						{
							BlockPos pos2 = (BlockPos)tickItem;
							block = this.server.api.World.BlockAccessor.GetBlock(pos2);
							block.OnServerGameTick(this.server.api.World, pos2, null);
						}
						else
						{
							ServerSystemBlockSimulation.BlockPosWithExtraObject tickContext = (ServerSystemBlockSimulation.BlockPosWithExtraObject)tickItem;
							block = this.server.api.World.BlockAccessor.GetBlock(tickContext.pos);
							block.OnServerGameTick(this.server.api.World, tickContext.pos, tickContext.extra);
						}
					}
					catch (Exception e)
					{
						ServerMain.Logger.Error("Exception thrown in block.OnServerGameTick() for block code '{0}':", new object[] { (block != null) ? block.Code : null });
						ServerMain.Logger.Error(e);
					}
					blockTickCount++;
				}
			}
		}

		public override void OnSeparateThreadTick()
		{
			if (this.server.RunPhase != EnumServerRunPhase.RunGame)
			{
				return;
			}
			this.chunksToBeTicked.Clear();
			int range = this.server.Config.BlockTickChunkRange;
			object obj = this.clientIdsLock;
			lock (obj)
			{
				foreach (int clientid in this.clientIds)
				{
					ConnectedClient client;
					if (this.server.Clients.TryGetValue(clientid, out client) && client.State == EnumClientState.Playing)
					{
						ChunkPos playerChunkPos = this.server.WorldMap.ChunkPosFromChunkIndex3D(client.Entityplayer.InChunkIndex3d);
						for (int dx = -range; dx <= range; dx++)
						{
							for (int dy = -range; dy <= range; dy++)
							{
								for (int dz = -range; dz <= range; dz++)
								{
									int cx = playerChunkPos.X + dx;
									int cy = playerChunkPos.Y + dy;
									int cz = playerChunkPos.Z + dz;
									if (this.server.WorldMap.IsValidChunkPos(cx, cy, cz))
									{
										long index3d = this.server.WorldMap.ChunkIndex3D(cx, cy, cz, playerChunkPos.Dimension);
										ServerChunk chunk = this.server.WorldMap.GetServerChunk(index3d);
										if (chunk != null)
										{
											this.chunksToBeTicked[index3d] = chunk;
											chunk.MarkFresh();
											chunk.MapChunk.MarkFresh();
										}
									}
								}
							}
						}
					}
				}
			}
			foreach (KeyValuePair<long, ServerChunk> val in this.chunksToBeTicked)
			{
				try
				{
					this.tickChunk(val.Key, val.Value);
				}
				catch (Exception e)
				{
					ServerMain.Logger.Warning("Exception thrown when trying to tick a chunk.");
					ServerMain.Logger.Warning(e);
				}
			}
		}

		private void tickChunk(long index3d, ServerChunk chunk)
		{
			ChunkPos cpos = this.server.WorldMap.ChunkPosFromChunkIndex3D(index3d);
			int baseX = 32 * cpos.X;
			int baseY = 32 * cpos.Y;
			int baseZ = 32 * cpos.Z;
			this.tmpPos.SetDimension(cpos.Dimension);
			this.tmpLiquidPos.SetDimension(cpos.Dimension);
			chunk.Unpack();
			float samples = (float)((int)((float)this.server.Config.RandomBlockTicksPerChunk * this.server.Calendar.SpeedOfTime / 60f));
			int cnt = (int)samples + ((this.server.rand.Value.NextDouble() < (double)(samples - (float)((int)samples))) ? 1 : 0);
			for (int i = 0; i < cnt; i++)
			{
				int randX = this.rand.Next(32);
				int randZ = this.rand.Next(32);
				int randY = this.rand.Next(32);
				int cIndex = this.server.WorldMap.ChunkSizedIndex3D(randX, randY, randZ);
				int blockId = chunk.Data.GetFluid(cIndex);
				if (blockId != 0)
				{
					this.tryTickBlock(this.server.WorldMap.Blocks[blockId], this.tmpLiquidPos.Set(baseX + randX, baseY + randY, baseZ + randZ));
				}
				else
				{
					blockId = chunk.Data[cIndex];
					if (blockId != 0)
					{
						this.tryTickBlock(this.server.WorldMap.Blocks[blockId], this.tmpPos.Set(baseX + randX, baseY + randY, baseZ + randZ));
					}
				}
			}
		}

		private bool tryTickBlock(Block block, BlockPos atPos)
		{
			object extra;
			if (!block.ShouldReceiveServerGameTicks(this.server.api.World, atPos, this.rand, out extra))
			{
				return false;
			}
			if (extra == null)
			{
				this.queuedTicks.Enqueue(atPos.Copy());
			}
			else
			{
				this.queuedTicks.Enqueue(new ServerSystemBlockSimulation.BlockPosWithExtraObject(atPos.Copy(), extra));
			}
			return true;
		}

		private Packet_BlockEntity BlockEntityToPacket(BlockEntity blockEntity, FastMemoryStream ms)
		{
			ms.Reset();
			BinaryWriter writer = new BinaryWriter(ms);
			TreeAttribute tree = new TreeAttribute();
			blockEntity.ToTreeAttributes(tree);
			tree.ToBytes(writer);
			string classname = ServerMain.ClassRegistry.blockEntityTypeToClassnameMapping[blockEntity.GetType()];
			byte[] data = ms.ToArray();
			Packet_BlockEntity packet_BlockEntity = new Packet_BlockEntity();
			packet_BlockEntity.Classname = classname;
			packet_BlockEntity.PosX = blockEntity.Pos.X;
			packet_BlockEntity.PosY = blockEntity.Pos.InternalY;
			packet_BlockEntity.PosZ = blockEntity.Pos.Z;
			packet_BlockEntity.SetData(data);
			if (this.server.doNetBenchmark)
			{
				int now;
				this.server.packetBenchmarkBlockEntitiesBytes.TryGetValue(classname, out now);
				this.server.packetBenchmarkBlockEntitiesBytes[classname] = now + data.Length;
			}
			return packet_BlockEntity;
		}

		private void SendBlockEntitiesPacket(ConnectedClient client, List<Packet_BlockEntity> blockEntitiesPacked, List<BlockPos> noBlockEntities)
		{
			Packet_BlockEntity[] blockentitiespackets = new Packet_BlockEntity[blockEntitiesPacked.Count + noBlockEntities.Count];
			int i = 0;
			foreach (Packet_BlockEntity packed in blockEntitiesPacked)
			{
				blockentitiespackets[i++] = packed;
			}
			for (int j = 0; j < noBlockEntities.Count; j++)
			{
				BlockPos pos = noBlockEntities[j];
				blockentitiespackets[i++] = new Packet_BlockEntity
				{
					Classname = null,
					Data = null,
					PosX = pos.X,
					PosY = pos.InternalY,
					PosZ = pos.Z
				};
			}
			Packet_BlockEntities packet = new Packet_BlockEntities();
			packet.SetBlockEntitites(blockentitiespackets);
			this.server.SendPacket(client.Id, new Packet_Server
			{
				Id = 48,
				BlockEntities = packet
			});
		}

		private ConcurrentQueue<object> queuedTicks = new ConcurrentQueue<object>();

		private Dictionary<long, ServerChunk> chunksToBeTicked = new Dictionary<long, ServerChunk>();

		private object clientIdsLock = new object();

		private List<int> clientIds = new List<int>();

		private Random rand = new Random();

		[ThreadStatic]
		private static FastMemoryStream reusableSendingStream;

		private List<Packet_BlockEntity> blockEntitiesPacked = new List<Packet_BlockEntity>();

		private List<BlockPos> noblockEntities = new List<BlockPos>();

		private List<Packet_BlockEntity> playerBlockEntitiesPacked = new List<Packet_BlockEntity>();

		private HashSet<BlockPos> positionsDone = new HashSet<BlockPos>();

		private BlockPos tmpPos = new BlockPos();

		private FluidBlockPos tmpLiquidPos = new FluidBlockPos();

		private class BlockPosWithExtraObject
		{
			public BlockPosWithExtraObject(BlockPos pos, object extra)
			{
				this.pos = pos;
				this.extra = extra;
			}

			public BlockPos pos;

			public object extra;
		}
	}
}
