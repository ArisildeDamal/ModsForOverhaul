using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class CoreServerEventManager : ServerEventManager
	{
		internal void defragLists()
		{
			CoreServerEventManager.prune<GameTickListenerBlock>(this.server.EventManager.GameTickListenersBlock);
			CoreServerEventManager.prune<GameTickListener>(this.server.EventManager.GameTickListenersEntity);
			CoreServerEventManager.defrag<DelayedCallbackBlock>(this.server.EventManager.DelayedCallbacksBlock);
			ServerMain.Logger.Notification("Defragmented listener lists");
		}

		internal static void defrag<T>(List<T> list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i] == null)
				{
					list.RemoveAt(i);
					i--;
				}
			}
		}

		internal static void prune<T>(List<T> list) where T : GameTickListenerBase
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.ServerMainThreadId)
			{
				throw new InvalidOperationException("Attempting to defrag listeners outside of the main thread. This may produce a race condition!");
			}
			int last = list.Count - 1;
			while (last >= 0 && list[last] == null)
			{
				list.RemoveAt(last);
				last--;
			}
		}

		public CoreServerEventManager(ServerMain server, ServerEventManager modEventManager)
			: base(server)
		{
			this.modEventManager = modEventManager;
		}

		public override void TriggerPlayerInteractEntity(Entity entity, IPlayer byPlayer, ItemSlot slot, Vec3d hitPosition, int mode, ref EnumHandling handling)
		{
			base.TriggerPlayerInteractEntity(entity, byPlayer, slot, hitPosition, mode, ref handling);
			this.modEventManager.TriggerPlayerInteractEntity(entity, byPlayer, slot, hitPosition, mode, ref handling);
		}

		public override void TriggerDidBreakBlock(IServerPlayer player, int oldBlockId, BlockSelection blockSel)
		{
			base.TriggerDidBreakBlock(player, oldBlockId, blockSel);
			this.modEventManager.TriggerDidBreakBlock(player, oldBlockId, blockSel);
		}

		public override void TriggerBreakBlock(IServerPlayer player, BlockSelection blockSel, ref float dropQuantityMultiplier, ref EnumHandling handling)
		{
			base.TriggerBreakBlock(player, blockSel, ref dropQuantityMultiplier, ref handling);
			this.modEventManager.TriggerBreakBlock(player, blockSel, ref dropQuantityMultiplier, ref handling);
		}

		public override void TriggerDidPlaceBlock(IServerPlayer player, int oldBlockId, BlockSelection blockSel, ItemStack withItemStack)
		{
			base.TriggerDidPlaceBlock(player, oldBlockId, blockSel, withItemStack);
			this.modEventManager.TriggerDidPlaceBlock(player, oldBlockId, blockSel, withItemStack);
		}

		public override void TriggerDidUseBlock(IServerPlayer player, BlockSelection blockSel)
		{
			base.TriggerDidUseBlock(player, blockSel);
			this.modEventManager.TriggerDidUseBlock(player, blockSel);
		}

		public override void TriggerGameTick(long ellapsedMilliseconds, IWorldAccessor world)
		{
			base.TriggerGameTick(ellapsedMilliseconds, this.server);
			this.modEventManager.TriggerGameTick(ellapsedMilliseconds, this.server);
		}

		public override void TriggerGameTickDebug(long ellapsedMilliseconds, IWorldAccessor world)
		{
			base.TriggerGameTickDebug(ellapsedMilliseconds, this.server);
			this.modEventManager.TriggerGameTickDebug(ellapsedMilliseconds, this.server);
		}

		public override bool TriggerCanPlaceOrBreak(IServerPlayer player, BlockSelection blockSel, out string claimant)
		{
			return base.TriggerCanPlaceOrBreak(player, blockSel, out claimant) && this.modEventManager.TriggerCanPlaceOrBreak(player, blockSel, out claimant);
		}

		public override bool TriggerCanUse(IServerPlayer player, BlockSelection blockSel)
		{
			return base.TriggerCanUse(player, blockSel) && this.modEventManager.TriggerCanUse(player, blockSel);
		}

		public override void TriggerOnplayerChat(IServerPlayer player, int channelId, ref string message, ref string data, BoolRef consumed)
		{
			base.TriggerOnplayerChat(player, channelId, ref message, ref data, consumed);
			if (!consumed.value)
			{
				this.modEventManager.TriggerOnplayerChat(player, channelId, ref message, ref data, consumed);
			}
		}

		public override void TriggerPlayerDisconnect(IServerPlayer player)
		{
			base.TriggerPlayerDisconnect(player);
			this.modEventManager.TriggerPlayerDisconnect(player);
		}

		public override void TriggerPlayerJoin(IServerPlayer player)
		{
			base.TriggerPlayerJoin(player);
			this.modEventManager.TriggerPlayerJoin(player);
		}

		public override void TriggerPlayerNowPlaying(IServerPlayer player)
		{
			base.TriggerPlayerNowPlaying(player);
			this.modEventManager.TriggerPlayerNowPlaying(player);
		}

		public override void TriggerPlayerLeave(IServerPlayer player)
		{
			base.TriggerPlayerLeave(player);
			this.modEventManager.TriggerPlayerLeave(player);
		}

		public override bool TriggerBeforeActiveSlotChanged(IServerPlayer player, int fromSlot, int toSlot)
		{
			return this.modEventManager.TriggerBeforeActiveSlotChanged(player, fromSlot, toSlot) && base.TriggerBeforeActiveSlotChanged(player, fromSlot, toSlot);
		}

		public override void TriggerAfterActiveSlotChanged(IServerPlayer player, int fromSlot, int toSlot)
		{
			this.modEventManager.TriggerAfterActiveSlotChanged(player, fromSlot, toSlot);
			base.TriggerAfterActiveSlotChanged(player, fromSlot, toSlot);
		}

		public override void TriggerPlayerRespawn(IServerPlayer player)
		{
			base.TriggerPlayerRespawn(player);
			this.modEventManager.TriggerPlayerRespawn(player);
		}

		public override void TriggerPlayerCreate(IServerPlayer player)
		{
			base.TriggerPlayerCreate(player);
			this.modEventManager.TriggerPlayerCreate(player);
		}

		public override bool TriggerTrySpawnEntity(IBlockAccessor blockaccessor, ref EntityProperties properties, Vec3d position, long herdId)
		{
			return base.TriggerTrySpawnEntity(blockaccessor, ref properties, position, herdId) && this.modEventManager.TriggerTrySpawnEntity(blockaccessor, ref properties, position, herdId);
		}

		public override void TriggerGameWorldBeingSaved()
		{
			this.modEventManager.TriggerGameWorldBeingSaved();
			base.TriggerGameWorldBeingSaved();
		}

		public override void TriggerSaveGameLoaded()
		{
			base.TriggerSaveGameLoaded();
			this.modEventManager.TriggerSaveGameLoaded();
		}

		public override void TriggerEntitySpawned(Entity entity)
		{
			base.TriggerEntitySpawned(entity);
			this.modEventManager.TriggerEntitySpawned(entity);
		}

		public override void TriggerEntityDespawned(Entity entity, EntityDespawnData reason)
		{
			base.TriggerEntityDespawned(entity, reason);
			this.modEventManager.TriggerEntityDespawned(entity, reason);
		}

		public override void TriggerPlayerChangeGamemode(IServerPlayer player)
		{
			base.TriggerPlayerChangeGamemode(player);
			this.modEventManager.TriggerPlayerChangeGamemode(player);
		}

		public override void TriggerEntityLoaded(Entity entity)
		{
			base.TriggerEntityLoaded(entity);
			this.modEventManager.TriggerEntityLoaded(entity);
		}

		public override void TriggerPlayerDeath(IServerPlayer player, DamageSource source)
		{
			base.TriggerPlayerDeath(player, source);
			this.modEventManager.TriggerPlayerDeath(player, source);
		}

		public override void TriggerOnGetClimate(ref ClimateCondition climate, BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.WorldGenValues, double totalDays = 0.0)
		{
			base.TriggerOnGetClimate(ref climate, pos, mode, totalDays);
			this.modEventManager.TriggerOnGetClimate(ref climate, pos, mode, totalDays);
		}

		public override void TriggerOnGetWindSpeed(Vec3d pos, ref Vec3d windSpeed)
		{
			base.TriggerOnGetWindSpeed(pos, ref windSpeed);
			this.modEventManager.TriggerOnGetWindSpeed(pos, ref windSpeed);
		}

		private ServerEventManager modEventManager;
	}
}
