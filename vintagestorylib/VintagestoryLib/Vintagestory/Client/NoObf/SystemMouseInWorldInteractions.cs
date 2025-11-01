using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class SystemMouseInWorldInteractions : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "miw";
			}
		}

		public SystemMouseInWorldInteractions(ClientMain game)
			: base(game)
		{
			game.RegisterGameTickListener(new Action<float>(this.OnEverySecond), 1000, 0);
			game.RegisterGameTickListener(new Action<float>(this.OnGameTick), 20, 0);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderOpaque), EnumRenderStage.Opaque, this.Name + "-op", 0.9);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderOit), EnumRenderStage.OIT, this.Name + "-oit", 0.9);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderOrtho), EnumRenderStage.Ortho, this.Name + "-2d", 0.9);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnFinalizeFrame), EnumRenderStage.Done, this.Name + "-done", 0.9);
		}

		private void OnEverySecond(float dt)
		{
			List<BlockPos> removeQueue = new List<BlockPos>();
			foreach (KeyValuePair<BlockPos, BlockDamage> var in this.game.damagedBlocks)
			{
				BlockDamage damagedBlock = var.Value;
				if (this.game.ElapsedMilliseconds - damagedBlock.LastBreakEllapsedMs > 1000L)
				{
					damagedBlock.LastBreakEllapsedMs = this.game.ElapsedMilliseconds;
					damagedBlock.RemainingResistance += 0.1f * damagedBlock.Block.GetResistance(this.game.BlockAccessor, damagedBlock.Position);
					ClientEventManager eventManager = this.game.eventManager;
					if (eventManager != null)
					{
						eventManager.TriggerBlockUnbreaking(damagedBlock);
					}
					if (damagedBlock.RemainingResistance >= damagedBlock.Block.GetResistance(this.game.BlockAccessor, damagedBlock.Position))
					{
						removeQueue.Add(var.Key);
					}
				}
			}
			foreach (BlockPos pos in removeQueue)
			{
				this.game.damagedBlocks.Remove(pos);
			}
			ScreenManager.FrameProfiler.Mark("miw-1s");
		}

		public void OnFinalizeFrame(float dt)
		{
			if (this.game.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
			{
				return;
			}
			ScreenManager.FrameProfiler.Mark("finaframe-beg");
			if (this.game.MouseGrabbed || this.game.mouseWorldInteractAnyway || this.game.player.worlddata.AreaSelectionMode)
			{
				this.UpdatePicking(dt);
			}
			this.prevMouseLeft = this.game.InWorldMouseState.Left;
			this.prevMouseRight = this.game.InWorldMouseState.Right;
			ScreenManager.FrameProfiler.Mark("finaframe-miw");
		}

		public override void OnMouseUp(MouseEvent args)
		{
			if (this.game.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
			{
				return;
			}
			if (this.game.player.worlddata.CurrentGameMode == EnumGameMode.Creative)
			{
				this.lastbuildMilliseconds = 0L;
			}
			this.StopBlockBreakSurvival();
		}

		private void OnGameTick(float dt)
		{
			ClientPlayerInventoryManager inventoryMgr = this.game.player.inventoryMgr;
			ItemStack itemStack;
			if (inventoryMgr == null)
			{
				itemStack = null;
			}
			else
			{
				ItemSlot activeHotbarSlot = inventoryMgr.ActiveHotbarSlot;
				itemStack = ((activeHotbarSlot != null) ? activeHotbarSlot.Itemstack : null);
			}
			ItemStack stack = itemStack;
			if (this.game.EntityPlayer.Controls.HandUse == EnumHandInteract.None && stack != null)
			{
				stack.Collectible.OnHeldIdle(this.game.player.inventoryMgr.ActiveHotbarSlot, this.game.EntityPlayer);
			}
			if (!this.game.EntityPlayer.LeftHandItemSlot.Empty)
			{
				this.game.EntityPlayer.LeftHandItemSlot.Itemstack.Collectible.OnHeldIdle(this.game.EntityPlayer.LeftHandItemSlot, this.game.EntityPlayer);
			}
			if (this.game.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
			{
				return;
			}
			if (this.game.EntityPlayer.Controls.HandUse == EnumHandInteract.None)
			{
				return;
			}
			if (!this.game.MouseGrabbed && !this.game.mouseWorldInteractAnyway)
			{
				if (this.game.EntityPlayer.Controls.HandUsingBlockSel != null)
				{
					ClientPlayer player = this.game.player;
					EntityPlayer entityPlr = this.game.EntityPlayer;
					Block block = this.game.BlockAccessor.GetBlock(entityPlr.Controls.HandUsingBlockSel.Position);
					EnumHandInteract beforeUseType = entityPlr.Controls.HandUse;
					if (block != null)
					{
						float secondsPassed = (float)(this.game.ElapsedMilliseconds - entityPlr.Controls.UsingBeginMS) / 1000f;
						entityPlr.Controls.HandUse = (block.OnBlockInteractCancel(secondsPassed, this.game, player, entityPlr.Controls.HandUsingBlockSel, EnumItemUseCancelReason.ReleasedMouse) ? EnumHandInteract.None : EnumHandInteract.BlockInteract);
						this.game.SendHandInteraction(2, this.game.BlockSelection, this.game.EntitySelection, beforeUseType, EnumHandInteractNw.CancelBlockUse, false, EnumItemUseCancelReason.ReleasedMouse);
					}
				}
				return;
			}
			this.accum += dt;
			if ((double)this.accum > 0.25)
			{
				this.accum = 0f;
				this.game.SendHandInteraction(2, this.game.BlockSelection, this.game.EntitySelection, this.game.EntityPlayer.Controls.HandUse, EnumHandInteractNw.StepHeldItemUse, false, EnumItemUseCancelReason.ReleasedMouse);
			}
			this.HandleHandInteraction(dt);
			ScreenManager.FrameProfiler.Mark("miw-handlehandinteraction");
		}

		private void OnRenderOrtho(float dt)
		{
			if (this.game.player.inventoryMgr.ActiveHotbarSlot.Itemstack != null)
			{
				this.game.player.inventoryMgr.ActiveHotbarSlot.Itemstack.Collectible.OnHeldRenderOrtho(this.game.player.inventoryMgr.ActiveHotbarSlot, this.game.player);
			}
		}

		private void OnRenderOit(float dt)
		{
			if (this.game.player.inventoryMgr.ActiveHotbarSlot.Itemstack != null)
			{
				this.game.player.inventoryMgr.ActiveHotbarSlot.Itemstack.Collectible.OnHeldRenderOit(this.game.player.inventoryMgr.ActiveHotbarSlot, this.game.player);
			}
		}

		private void OnRenderOpaque(float dt)
		{
			if (this.game.player.inventoryMgr.ActiveHotbarSlot.Itemstack != null)
			{
				this.game.player.inventoryMgr.ActiveHotbarSlot.Itemstack.Collectible.OnHeldRenderOpaque(this.game.player.inventoryMgr.ActiveHotbarSlot, this.game.player);
			}
		}

		internal void UpdatePicking(float dt)
		{
			this.UpdateCurrentSelection();
			if (!this.game.MouseGrabbed && !this.game.mouseWorldInteractAnyway)
			{
				this.ResetMouseInteractions();
				return;
			}
			if (this.game.EntityPlayer.Controls.HandUse != EnumHandInteract.None)
			{
				return;
			}
			if ((this.game.InWorldMouseState.Left > false) + (this.game.InWorldMouseState.Middle > false) + (this.game.InWorldMouseState.Right > false) > true)
			{
				this.ResetMouseInteractions();
				return;
			}
			if (this.game.BlockSelection == null)
			{
				this.HandleMouseInteractionsNoBlockSelected(dt);
				return;
			}
			this.HandleMouseInteractionsBlockSelected(dt);
		}

		private void HandleHandInteraction(float dt)
		{
			ClientPlayer player = this.game.player;
			EntityPlayer entityPlr = this.game.EntityPlayer;
			ItemSlot slot = this.game.player.inventoryMgr.ActiveHotbarSlot;
			float secondsPassed = (float)(this.game.ElapsedMilliseconds - entityPlr.Controls.UsingBeginMS) / 1000f;
			bool success = false;
			if (entityPlr.Controls.HandUse == EnumHandInteract.BlockInteract)
			{
				Block block = this.game.BlockAccessor.GetBlock(entityPlr.Controls.HandUsingBlockSel.Position);
				BlockSelection blockSelection = this.game.BlockSelection;
				if (((blockSelection != null) ? blockSelection.Position : null) == null || !this.game.BlockSelection.Position.Equals(entityPlr.Controls.HandUsingBlockSel.Position))
				{
					entityPlr.Controls.HandUse = (block.OnBlockInteractCancel(secondsPassed, this.game, player, entityPlr.Controls.HandUsingBlockSel, EnumItemUseCancelReason.MovedAway) ? EnumHandInteract.None : EnumHandInteract.BlockInteract);
					this.game.SendHandInteraction(2, entityPlr.Controls.HandUsingBlockSel, null, EnumHandInteract.BlockInteract, EnumHandInteractNw.CancelBlockUse, false, EnumItemUseCancelReason.MovedAway);
					return;
				}
				EnumHandInteract beforeUseType = entityPlr.Controls.HandUse;
				if (!this.game.InWorldMouseState.Right)
				{
					entityPlr.Controls.HandUse = (block.OnBlockInteractCancel(secondsPassed, this.game, player, this.game.BlockSelection, EnumItemUseCancelReason.ReleasedMouse) ? EnumHandInteract.None : EnumHandInteract.BlockInteract);
				}
				if (entityPlr.Controls.HandUse != EnumHandInteract.None)
				{
					entityPlr.Controls.HandUse = (block.OnBlockInteractStep(secondsPassed, this.game, player, this.game.BlockSelection) ? EnumHandInteract.BlockInteract : EnumHandInteract.None);
					entityPlr.Controls.UsingCount++;
					this.stepPacketAccum += dt;
					if ((double)this.stepPacketAccum > 0.15)
					{
						this.game.SendHandInteraction(2, entityPlr.Controls.HandUsingBlockSel, null, EnumHandInteract.BlockInteract, EnumHandInteractNw.StepBlockUse, false, EnumItemUseCancelReason.ReleasedMouse);
						this.stepPacketAccum = 0f;
					}
				}
				if (entityPlr.Controls.HandUse == EnumHandInteract.None)
				{
					block.OnBlockInteractStop(secondsPassed, this.game, player, this.game.BlockSelection);
					success = true;
				}
				if (entityPlr.Controls.HandUse == EnumHandInteract.None)
				{
					this.game.SendHandInteraction(2, this.game.BlockSelection, this.game.EntitySelection, beforeUseType, success ? EnumHandInteractNw.StopBlockUse : EnumHandInteractNw.CancelBlockUse, false, EnumItemUseCancelReason.ReleasedMouse);
				}
				return;
			}
			else
			{
				if (((slot != null) ? slot.Itemstack : null) == null)
				{
					entityPlr.Controls.HandUse = EnumHandInteract.None;
					return;
				}
				EnumHandInteract beforeUseType2 = entityPlr.Controls.HandUse;
				if ((!this.game.InWorldMouseState.Right && beforeUseType2 == EnumHandInteract.HeldItemInteract) || (!this.game.InWorldMouseState.Left && beforeUseType2 == EnumHandInteract.HeldItemAttack))
				{
					entityPlr.Controls.HandUse = slot.Itemstack.Collectible.OnHeldUseCancel(secondsPassed, slot, this.game.EntityPlayer, this.game.BlockSelection, this.game.EntitySelection, EnumItemUseCancelReason.ReleasedMouse);
				}
				if (entityPlr.Controls.HandUse != EnumHandInteract.None)
				{
					entityPlr.Controls.HandUse = slot.Itemstack.Collectible.OnHeldUseStep(secondsPassed, slot, this.game.EntityPlayer, this.game.BlockSelection, this.game.EntitySelection);
					entityPlr.Controls.UsingCount++;
				}
				if (entityPlr.Controls.HandUse == EnumHandInteract.None)
				{
					ItemStack itemstack = slot.Itemstack;
					if (itemstack != null)
					{
						itemstack.Collectible.OnHeldUseStop(secondsPassed, slot, this.game.EntityPlayer, this.game.BlockSelection, this.game.EntitySelection, beforeUseType2);
					}
					success = true;
				}
				if (slot.StackSize <= 0)
				{
					slot.Itemstack = null;
					slot.MarkDirty();
				}
				if (entityPlr.Controls.HandUse == EnumHandInteract.None)
				{
					this.game.SendHandInteraction(2, this.game.BlockSelection, this.game.EntitySelection, beforeUseType2, success ? EnumHandInteractNw.StopHeldItemUse : EnumHandInteractNw.CancelHeldItemUse, false, EnumItemUseCancelReason.ReleasedMouse);
				}
				return;
			}
		}

		private void UpdateCurrentSelection()
		{
			if (this.game.EntityPlayer == null)
			{
				return;
			}
			bool renderMeta = ClientSettings.RenderMetaBlocks;
			BlockFilter bfilter = delegate(BlockPos pos, Block block)
			{
				if (!((block == null) | renderMeta) && block.RenderPass == EnumChunkRenderPass.Meta)
				{
					IMetaBlock @interface = block.GetInterface<IMetaBlock>(this.game.api.World, pos);
					return @interface != null && @interface.IsSelectable(pos);
				}
				return true;
			};
			EntityFilter efilter = (Entity e) => e.IsInteractable && e.EntityId != this.game.EntityPlayer.EntityId;
			bool prevLiqSel = this.game.LiquidSelectable;
			if (!this.game.InWorldMouseState.Left && this.game.InWorldMouseState.Right)
			{
				ClientPlayer player = this.game.player;
				bool flag;
				if (player == null)
				{
					flag = null != null;
				}
				else
				{
					ClientPlayerInventoryManager inventoryMgr = player.inventoryMgr;
					if (inventoryMgr == null)
					{
						flag = null != null;
					}
					else
					{
						ItemSlot activeHotbarSlot = inventoryMgr.ActiveHotbarSlot;
						if (activeHotbarSlot == null)
						{
							flag = null != null;
						}
						else
						{
							ItemStack itemstack = activeHotbarSlot.Itemstack;
							flag = ((itemstack != null) ? itemstack.Collectible : null) != null;
						}
					}
				}
				if (flag && this.game.player.inventoryMgr.ActiveHotbarSlot.Itemstack.Collectible.LiquidSelectable)
				{
					this.game.forceLiquidSelectable = true;
				}
			}
			EntityPlayer entityPlayer = this.game.EntityPlayer;
			BlockSelection blockSelection = this.game.EntityPlayer.BlockSelection;
			entityPlayer.PreviousBlockSelection = ((blockSelection != null) ? blockSelection.Position.Copy() : null);
			if (!this.game.MouseGrabbed)
			{
				Ray ray = this.game.pickingRayUtil.GetPickingRayByMouseCoordinates(this.game);
				if (ray == null)
				{
					this.game.forceLiquidSelectable = prevLiqSel;
					return;
				}
				this.game.RayTraceForSelection(ray, ref this.game.EntityPlayer.BlockSelection, ref this.game.EntityPlayer.EntitySelection, bfilter, efilter);
			}
			else
			{
				this.game.RayTraceForSelection(this.game.player, ref this.game.EntityPlayer.BlockSelection, ref this.game.EntityPlayer.EntitySelection, bfilter, efilter);
			}
			this.game.forceLiquidSelectable = prevLiqSel;
			if (this.game.EntityPlayer.BlockSelection != null)
			{
				bool firstTick = this.game.EntityPlayer.PreviousBlockSelection == null || this.game.EntityPlayer.BlockSelection.Position != this.game.EntityPlayer.PreviousBlockSelection;
				this.game.EntityPlayer.BlockSelection.Block.OnBeingLookedAt(this.game.player, this.game.EntityPlayer.BlockSelection, firstTick);
			}
		}

		private void ResetMouseInteractions()
		{
			this.isSurvivalBreaking = false;
			this.survivalBreakingCounter = 0;
		}

		private void HandleMouseInteractionsNoBlockSelected(float dt)
		{
			this.StopBlockBreakSurvival();
			if ((float)(this.game.InWorldEllapsedMs - this.lastbuildMilliseconds) / 1000f >= this.BuildRepeatDelay(this.game))
			{
				if (this.game.InWorldMouseState.Left || this.game.InWorldMouseState.Right || this.game.InWorldMouseState.Middle)
				{
					this.lastbuildMilliseconds = this.game.InWorldEllapsedMs;
				}
				else
				{
					this.lastbuildMilliseconds = 0L;
				}
				if (this.game.InWorldMouseState.Left)
				{
					EnumHandling inworldhandling = EnumHandling.PassThrough;
					this.game.api.inputapi.TriggerInWorldAction(EnumEntityAction.InWorldLeftMouseDown, true, ref inworldhandling);
					if (inworldhandling != EnumHandling.PassThrough)
					{
						return;
					}
					EnumHandHandling handling = EnumHandHandling.NotHandled;
					this.TryBeginAttackWithActiveSlotItem(null, this.game.EntitySelection, ref handling);
					if (handling != EnumHandHandling.PreventDefaultAnimation && handling != EnumHandHandling.PreventDefault)
					{
						this.StartAttackAnimation();
					}
					if (this.game.EntitySelection != null && handling != EnumHandHandling.PreventDefaultAction && handling != EnumHandHandling.PreventDefault)
					{
						this.game.TryAttackEntity(this.game.EntitySelection);
					}
				}
				if (this.game.InWorldMouseState.Right)
				{
					EnumHandling inworldhandling2 = EnumHandling.PassThrough;
					this.game.api.inputapi.TriggerInWorldAction(EnumEntityAction.InWorldRightMouseDown, true, ref inworldhandling2);
					if (inworldhandling2 != EnumHandling.PassThrough)
					{
						return;
					}
					if (this.TryBeginUseActiveSlotItem(null, this.game.EntitySelection))
					{
						return;
					}
					if (this.game.EntitySelection != null)
					{
						EntitySelection esel = this.game.EntitySelection;
						this.game.EntitySelection.Entity.OnInteract(this.game.EntityPlayer, this.game.player.inventoryMgr.ActiveHotbarSlot, esel.HitPosition, EnumInteractMode.Interact);
						this.game.SendPacketClient(ClientPackets.EntityInteraction(1, esel.Entity.EntityId, esel.Face, esel.HitPosition, esel.SelectionBoxIndex));
					}
				}
			}
		}

		private void HandleMouseInteractionsBlockSelected(float dt)
		{
			BlockSelection blockSelection = this.game.BlockSelection;
			Block selectedBlock = blockSelection.Block ?? this.game.WorldMap.RelaxedBlockAccess.GetBlock(blockSelection.Position);
			ItemSlot selectedHotbarSlot = this.game.player.inventoryMgr.ActiveHotbarSlot;
			if ((float)(this.game.InWorldEllapsedMs - this.lastbuildMilliseconds) / 1000f >= this.BuildRepeatDelay(this.game))
			{
				if (this.game.InWorldMouseState.Left || this.game.InWorldMouseState.Right || this.game.InWorldMouseState.Middle)
				{
					this.lastbuildMilliseconds = this.game.InWorldEllapsedMs;
				}
				else
				{
					this.lastbuildMilliseconds = 0L;
					this.ResetMouseInteractions();
				}
				if (this.game.InWorldMouseState.Left)
				{
					EnumHandling handling = EnumHandling.PassThrough;
					this.game.api.inputapi.TriggerInWorldAction(EnumEntityAction.InWorldLeftMouseDown, true, ref handling);
					if (handling != EnumHandling.PassThrough)
					{
						return;
					}
					EnumHandHandling handled = EnumHandHandling.NotHandled;
					this.TryBeginUseActiveSlotItem(blockSelection, null, EnumHandInteract.HeldItemAttack, ref handled);
					if (handled != EnumHandHandling.PreventDefaultAnimation && handled != EnumHandHandling.PreventDefault)
					{
						this.StartAttackAnimation();
					}
					if (handled == EnumHandHandling.PreventDefaultAction || handled == EnumHandHandling.PreventDefault)
					{
						this.isSurvivalBreaking = false;
						this.survivalBreakingCounter = 0;
					}
					else if (this.game.player.worlddata.CurrentGameMode == EnumGameMode.Creative)
					{
						BlockDamage blockDamage;
						this.game.damagedBlocks.TryGetValue(blockSelection.Position, out blockDamage);
						if (blockDamage == null)
						{
							blockDamage = new BlockDamage
							{
								Block = selectedBlock,
								Facing = blockSelection.Face,
								Position = blockSelection.Position,
								ByPlayer = this.game.player
							};
						}
						this.game.damagedBlocks.Remove(blockSelection.Position);
						ClientEventManager eventManager = this.game.eventManager;
						if (eventManager != null)
						{
							eventManager.TriggerBlockBroken(blockDamage);
						}
						this.game.OnPlayerTryDestroyBlock(blockSelection);
						this.UpdateCurrentSelection();
						ClientMain game = this.game;
						BlockSounds sounds = selectedBlock.GetSounds(this.game.BlockAccessor, blockSelection, null);
						game.PlaySound((sounds != null) ? sounds.GetBreakSound(this.game.player) : null, true, 1f);
					}
					else
					{
						this.InitBlockBreakSurvival(blockSelection, dt);
					}
				}
				if (this.game.InWorldMouseState.Right)
				{
					EnumHandling handling2 = EnumHandling.PassThrough;
					this.game.api.inputapi.TriggerInWorldAction(EnumEntityAction.InWorldRightMouseDown, true, ref handling2);
					if (handling2 != EnumHandling.PassThrough)
					{
						return;
					}
					bool haveHeldItemstack = selectedHotbarSlot.Itemstack != null;
					bool canPlaceBlock = haveHeldItemstack && selectedHotbarSlot.Itemstack.Class == EnumItemClass.Block && (this.game.player.worlddata.CurrentGameMode == EnumGameMode.Survival || this.game.player.worlddata.CurrentGameMode == EnumGameMode.Creative);
					bool canInteractWithBlock = this.game.player.worlddata.CurrentGameMode != EnumGameMode.Spectator;
					if (canInteractWithBlock && !this.game.Player.Entity.Controls.ShiftKey && this.TryBeginUseBlock(selectedBlock, blockSelection))
					{
						return;
					}
					if (haveHeldItemstack && (!this.game.Player.Entity.Controls.ShiftKey || selectedHotbarSlot.Itemstack.Collectible.HeldPriorityInteract) && this.TryBeginUseActiveSlotItem(blockSelection, null))
					{
						return;
					}
					string failureCode = null;
					if (canInteractWithBlock && this.game.Player.Entity.Controls.ShiftKey && selectedBlock.PlacedPriorityInteract && this.TryBeginUseBlock(selectedBlock, blockSelection))
					{
						return;
					}
					if (canPlaceBlock && this.OnBlockBuild(blockSelection, selectedBlock, ref failureCode))
					{
						return;
					}
					if (haveHeldItemstack && this.game.Player.Entity.Controls.ShiftKey && this.TryBeginUseActiveSlotItem(blockSelection, null))
					{
						return;
					}
					if (canInteractWithBlock && this.game.Player.Entity.Controls.ShiftKey && this.TryBeginUseBlock(selectedBlock, blockSelection))
					{
						return;
					}
					if (failureCode != null && failureCode != "__ignore__")
					{
						ClientEventManager eventManager2 = this.game.eventManager;
						if (eventManager2 != null)
						{
							eventManager2.TriggerIngameError(this, failureCode, Lang.Get("placefailure-" + failureCode, Array.Empty<object>()));
						}
					}
				}
				if (this.game.PickBlock)
				{
					this.OnBlockPick(blockSelection.Position, selectedBlock);
				}
			}
			long ellapsedMs = this.game.ElapsedMilliseconds;
			if (this.isSurvivalBreaking && this.game.InWorldMouseState.Left && this.game.player.worlddata.CurrentGameMode == EnumGameMode.Survival && ellapsedMs - this.lastbreakMilliseconds >= 40L)
			{
				this.ContinueBreakSurvival(blockSelection, selectedBlock, dt);
				this.lastbreakMilliseconds = ellapsedMs;
				if (ellapsedMs - this.lastbreakNotifyMilliseconds > 80L)
				{
					this.lastbreakNotifyMilliseconds = ellapsedMs;
				}
			}
		}

		private void StartAttackAnimation()
		{
			this.game.HandSetAttackDestroy = true;
		}

		private void OnBlockPick(BlockPos pos, Block block)
		{
			ClientPlayerInventoryManager invMgr = this.game.player.inventoryMgr;
			IInventory hotbarInv = invMgr.GetHotbarInventory();
			if (hotbarInv != null)
			{
				ItemStack blockStack = block.OnPickBlock(this.game, pos);
				int firstFreeSlotId = -1;
				for (int i = 0; i < hotbarInv.Count; i++)
				{
					if ((hotbarInv[i].StorageType & (EnumItemStorageFlags.Backpack | EnumItemStorageFlags.Offhand)) == (EnumItemStorageFlags)0)
					{
						IItemStack itemstack = hotbarInv[i].Itemstack;
						if (firstFreeSlotId == -1 && hotbarInv[i].Empty && hotbarInv[i].CanTakeFrom(new DummySlot(blockStack), EnumMergePriority.AutoMerge))
						{
							firstFreeSlotId = i;
						}
						if (itemstack != null && itemstack.Equals(this.game, blockStack, GlobalConstants.IgnoredStackAttributes))
						{
							invMgr.ActiveHotbarSlotNumber = i;
							return;
						}
					}
				}
				bool creative = this.game.player.worlddata.CurrentGameMode == EnumGameMode.Creative;
				ItemSlot flipSlot = null;
				if (creative)
				{
					flipSlot = new DummySlot(blockStack);
				}
				else
				{
					this.game.player.Entity.WalkInventory(delegate(ItemSlot slot)
					{
						if (!(slot.Inventory is InventoryPlayerBackPacks))
						{
							return true;
						}
						ItemStack itemstack2 = slot.Itemstack;
						if (itemstack2 != null && itemstack2.Equals(this.game, blockStack, GlobalConstants.IgnoredStackAttributes))
						{
							flipSlot = slot;
						}
						return flipSlot == null;
					});
					if (flipSlot == null)
					{
						return;
					}
				}
				ItemSlot selectedHotbarSlot = invMgr.ActiveHotbarSlot;
				if ((selectedHotbarSlot.Itemstack != null || !selectedHotbarSlot.CanTakeFrom(flipSlot, EnumMergePriority.AutoMerge)) && firstFreeSlotId != -1)
				{
					selectedHotbarSlot = hotbarInv[firstFreeSlotId];
					invMgr.ActiveHotbarSlotNumber = firstFreeSlotId;
				}
				if (!selectedHotbarSlot.CanHold(flipSlot))
				{
					return;
				}
				if (creative)
				{
					selectedHotbarSlot.Itemstack = blockStack;
					selectedHotbarSlot.MarkDirty();
					this.game.SendPacketClient(new Packet_Client
					{
						Id = 10,
						CreateItemstack = new Packet_CreateItemstack
						{
							Itemstack = StackConverter.ToPacket(blockStack),
							TargetInventoryId = selectedHotbarSlot.Inventory.InventoryID,
							TargetSlot = invMgr.ActiveHotbarSlotNumber,
							TargetLastChanged = ((InventoryBase)hotbarInv).lastChangedSinceServerStart
						}
					});
					return;
				}
				this.game.SendPacketClient(hotbarInv.TryFlipItems(invMgr.ActiveHotbarSlotNumber, flipSlot) as Packet_Client);
			}
		}

		private bool OnBlockBuild(BlockSelection blockSelection, Block onBlock, ref string failureCode)
		{
			ItemSlot selectedHotbarSlot = this.game.player.inventoryMgr.ActiveHotbarSlot;
			Block newBlock = this.game.Blocks[selectedHotbarSlot.Itemstack.Id];
			BlockPos buildPos = blockSelection.Position;
			if (onBlock == null || !onBlock.IsReplacableBy(newBlock))
			{
				buildPos = buildPos.Offset(blockSelection.Face);
				blockSelection.DidOffset = true;
			}
			if (this.game.OnPlayerTryPlace(blockSelection, ref failureCode))
			{
				ClientMain game = this.game;
				BlockSounds sounds = newBlock.GetSounds(this.game.BlockAccessor, blockSelection, null);
				game.PlaySound((sounds != null) ? sounds.Place : null, true, 1f);
				this.game.HandSetAttackBuild = true;
				return true;
			}
			if (blockSelection.DidOffset)
			{
				buildPos.Offset(blockSelection.Face.Opposite);
				blockSelection.DidOffset = false;
			}
			return false;
		}

		private void loadOrCreateBlockDamage(BlockSelection blockSelection, Block block)
		{
			BlockDamage prevDmg = this.curBlockDmg;
			ClientPlayerInventoryManager inventoryMgr = this.game.player.inventoryMgr;
			EnumTool? enumTool;
			if (inventoryMgr == null)
			{
				enumTool = null;
			}
			else
			{
				ItemSlot activeHotbarSlot = inventoryMgr.ActiveHotbarSlot;
				if (activeHotbarSlot == null)
				{
					enumTool = null;
				}
				else
				{
					ItemStack itemstack = activeHotbarSlot.Itemstack;
					if (itemstack == null)
					{
						enumTool = null;
					}
					else
					{
						CollectibleObject collectible = itemstack.Collectible;
						enumTool = ((collectible != null) ? collectible.Tool : null);
					}
				}
			}
			EnumTool? tool = enumTool;
			this.curBlockDmg = this.game.loadOrCreateBlockDamage(blockSelection, block, tool, this.game.player);
			if (prevDmg != null && !prevDmg.Position.Equals(blockSelection.Position))
			{
				this.curBlockDmg.LastBreakEllapsedMs = this.game.ElapsedMilliseconds;
			}
		}

		private void InitBlockBreakSurvival(BlockSelection blockSelection, float dt)
		{
			Block block = blockSelection.Block ?? this.game.BlockAccessor.GetBlock(blockSelection.Position);
			this.loadOrCreateBlockDamage(blockSelection, block);
			this.curBlockDmg.LastBreakEllapsedMs = this.game.ElapsedMilliseconds;
			this.curBlockDmg.BeginBreakEllapsedMs = this.game.ElapsedMilliseconds;
			this.isSurvivalBreaking = true;
		}

		private void StopBlockBreakSurvival()
		{
			this.curBlockDmg = null;
			this.isSurvivalBreaking = false;
			this.survivalBreakingCounter = 0;
		}

		private void ContinueBreakSurvival(BlockSelection blockSelection, Block block, float dt)
		{
			this.loadOrCreateBlockDamage(blockSelection, block);
			long elapsedMs = this.game.ElapsedMilliseconds;
			int diff = (int)(elapsedMs - this.curBlockDmg.LastBreakEllapsedMs);
			long decorBreakPoint = this.curBlockDmg.BeginBreakEllapsedMs + 225L;
			if (elapsedMs >= decorBreakPoint && this.curBlockDmg.LastBreakEllapsedMs < decorBreakPoint)
			{
				WorldChunk c = this.game.BlockAccessor.GetChunkAtBlockPos(blockSelection.Position) as WorldChunk;
				if (c != null && this.game.tryAccess(blockSelection, EnumBlockAccessFlags.BuildOrBreak))
				{
					BlockPos pos = blockSelection.Position;
					int chunksize = 32;
					c.BreakDecor(this.game, pos, blockSelection.Face, null);
					this.game.WorldMap.MarkChunkDirty(pos.X / chunksize, pos.Y / chunksize, pos.Z / chunksize, true, false, null, true, false);
					this.game.SendPacketClient(ClientPackets.BlockInteraction(blockSelection, 2, 0));
				}
			}
			this.curBlockDmg.RemainingResistance = block.OnGettingBroken(this.game.player, blockSelection, this.game.player.inventoryMgr.ActiveHotbarSlot, this.curBlockDmg.RemainingResistance, (float)diff / 1000f, this.survivalBreakingCounter);
			this.survivalBreakingCounter++;
			this.curBlockDmg.Facing = blockSelection.Face;
			if (this.curBlockDmg.Position != blockSelection.Position || this.curBlockDmg.Block != block)
			{
				this.curBlockDmg.RemainingResistance = block.GetResistance(this.game.BlockAccessor, blockSelection.Position);
				this.curBlockDmg.Block = block;
				this.curBlockDmg.Position = blockSelection.Position;
			}
			if (this.curBlockDmg.RemainingResistance <= 0f)
			{
				ClientEventManager eventManager = this.game.eventManager;
				if (eventManager != null)
				{
					eventManager.TriggerBlockBroken(this.curBlockDmg);
				}
				this.game.OnPlayerTryDestroyBlock(blockSelection);
				this.game.damagedBlocks.Remove(blockSelection.Position);
				this.UpdateCurrentSelection();
			}
			else
			{
				ClientEventManager eventManager2 = this.game.eventManager;
				if (eventManager2 != null)
				{
					eventManager2.TriggerBlockBreaking(this.curBlockDmg);
				}
			}
			this.curBlockDmg.LastBreakEllapsedMs = elapsedMs;
		}

		internal float BuildRepeatDelay(ClientMain game)
		{
			return 0.25f;
		}

		private bool TryBeginUseActiveSlotItem(BlockSelection blockSel, EntitySelection entitySel)
		{
			EnumHandHandling handling = EnumHandHandling.NotHandled;
			bool flag = this.TryBeginUseActiveSlotItem(blockSel, entitySel, EnumHandInteract.HeldItemInteract, ref handling);
			if (flag && (handling == EnumHandHandling.PreventDefaultAction || handling == EnumHandHandling.Handled))
			{
				this.game.HandSetAttackBuild = true;
			}
			return flag;
		}

		private bool TryBeginAttackWithActiveSlotItem(BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
		{
			return this.TryBeginUseActiveSlotItem(blockSel, entitySel, EnumHandInteract.HeldItemAttack, ref handling);
		}

		private bool TryBeginUseActiveSlotItem(BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType, ref EnumHandHandling handling)
		{
			ItemSlot slot = this.game.player.inventoryMgr.ActiveHotbarSlot;
			if (((this.game.InWorldMouseState.Right && useType == EnumHandInteract.HeldItemInteract) || (this.game.InWorldMouseState.Left && useType == EnumHandInteract.HeldItemAttack)) && slot != null && slot.Itemstack != null)
			{
				EntityControls controls = this.game.EntityPlayer.Controls;
				bool firstEvent = (useType == EnumHandInteract.HeldItemInteract && !this.prevMouseRight) || (useType == EnumHandInteract.HeldItemAttack && !this.prevMouseLeft);
				slot.Itemstack.Collectible.OnHeldUseStart(slot, this.game.EntityPlayer, blockSel, entitySel, useType, firstEvent, ref handling);
				if (handling == EnumHandHandling.NotHandled)
				{
					controls.HandUse = EnumHandInteract.None;
				}
				else
				{
					controls.HandUse = useType;
				}
				if (handling != EnumHandHandling.NotHandled)
				{
					controls.UsingCount = 0;
					controls.UsingBeginMS = this.game.ElapsedMilliseconds;
					if (controls.LeftUsingHeldItemTransformBefore != null)
					{
						controls.LeftUsingHeldItemTransformBefore.Clear();
					}
					if (slot.StackSize <= 0)
					{
						slot.Itemstack = null;
						slot.MarkDirty();
					}
					this.game.SendHandInteraction(2, blockSel, entitySel, useType, EnumHandInteractNw.StartHeldItemUse, firstEvent, EnumItemUseCancelReason.ReleasedMouse);
					return true;
				}
			}
			return false;
		}

		private bool TryBeginUseBlock(Block selectedBlock, BlockSelection blockSelection)
		{
			if (!this.game.tryAccess(blockSelection, EnumBlockAccessFlags.Use))
			{
				return false;
			}
			if (selectedBlock.OnBlockInteractStart(this.game, this.game.player, blockSelection))
			{
				EntityControls controls = this.game.EntityPlayer.Controls;
				controls.HandUse = EnumHandInteract.BlockInteract;
				this.game.api.Network.SendPlayerPositionPacket();
				controls.UsingCount = 0;
				controls.UsingBeginMS = this.game.ElapsedMilliseconds;
				controls.HandUsingBlockSel = blockSelection.Clone();
				if (controls.LeftUsingHeldItemTransformBefore != null)
				{
					controls.LeftUsingHeldItemTransformBefore.Clear();
				}
				this.game.SendHandInteraction(2, blockSelection, null, EnumHandInteract.BlockInteract, EnumHandInteractNw.StartBlockUse, false, EnumItemUseCancelReason.ReleasedMouse);
				return true;
			}
			return false;
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		internal long lastbuildMilliseconds;

		internal long lastbreakMilliseconds;

		internal long lastbreakNotifyMilliseconds;

		private bool isSurvivalBreaking;

		private int survivalBreakingCounter;

		private BlockDamage curBlockDmg;

		public bool prevMouseLeft;

		public bool prevMouseRight;

		private float accum;

		private float stepPacketAccum;
	}
}
