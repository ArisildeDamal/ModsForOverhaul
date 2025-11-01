using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common
{
	public class BlockAccessorRevertable : BlockAccessorRelaxedBulkUpdate, IBlockAccessorRevertable, IBulkBlockAccessor, IBlockAccessor
	{
		public int CurrentHistoryState
		{
			get
			{
				return this._currentHistoryStateIndex;
			}
		}

		public event Action<HistoryState> OnStoreHistoryState;

		public event Action<HistoryState, int> OnRestoreHistoryState;

		public bool Relight
		{
			get
			{
				return this.relight;
			}
			set
			{
				this.relight = value;
			}
		}

		public BlockAccessorRevertable(WorldMap worldmap, IWorldAccessor worldAccessor, bool synchronize, bool relight, bool debug)
			: base(worldmap, worldAccessor, synchronize, relight, debug)
		{
			this.storeOldBlockEntityData = true;
		}

		public int QuantityHistoryStates
		{
			get
			{
				return this._maxQuantityStates;
			}
			set
			{
				this._maxQuantityStates = value;
				while (this._historyStates.Count > this._maxQuantityStates)
				{
					this._historyStates.RemoveAt(this._historyStates.Count - 1);
				}
			}
		}

		public int AvailableHistoryStates
		{
			get
			{
				return this._historyStates.Count;
			}
		}

		public void SetHistoryStateBlock(int posX, int posY, int posZ, int oldBlockId, int newBlockId)
		{
			BlockPos pos = new BlockPos(posX, posY, posZ);
			byte[] data = null;
			if (this.worldAccessor.Blocks[oldBlockId].EntityClass != null)
			{
				TreeAttribute tree = new TreeAttribute();
				this.GetBlockEntity(new BlockPos(posX, posY, posZ)).ToTreeAttributes(tree);
				data = tree.ToBytes();
			}
			ItemStack byStack = null;
			BlockUpdate bu;
			if (this.StagedBlocks.TryGetValue(pos, out bu) && bu.NewSolidBlockId == newBlockId)
			{
				byStack = bu.ByStack;
			}
			this.StagedBlocks[pos] = new BlockUpdate
			{
				OldBlockId = oldBlockId,
				NewSolidBlockId = newBlockId,
				NewFluidBlockId = ((bu != null) ? bu.NewFluidBlockId : (-1)),
				Pos = pos,
				ByStack = byStack,
				OldBlockEntityData = data
			};
		}

		public void BeginMultiEdit()
		{
			this._multiedit = true;
			this.synchronize = false;
		}

		public override List<BlockUpdate> Commit()
		{
			if (this._multiedit)
			{
				this._blockUpdates.AddRange(base.Commit());
				return this._blockUpdates;
			}
			List<BlockUpdate> blockUpdates = base.Commit();
			HistoryState hs = new HistoryState
			{
				BlockUpdates = blockUpdates.ToArray()
			};
			this.StoreHistoryState(hs);
			return blockUpdates;
		}

		public void StoreHistoryState(HistoryState historyState)
		{
			if (this._historyStates.Count >= this._maxQuantityStates)
			{
				this._historyStates.RemoveAt(this._historyStates.Count - 1);
			}
			while (this._currentHistoryStateIndex > 0)
			{
				this._currentHistoryStateIndex--;
				this._historyStates.RemoveAt(0);
			}
			this._historyStates.Insert(0, historyState);
			Action<HistoryState> onStoreHistoryState = this.OnStoreHistoryState;
			if (onStoreHistoryState == null)
			{
				return;
			}
			onStoreHistoryState(historyState);
		}

		public void StoreEntitySpawnToHistory(Entity entity)
		{
			HistoryState historyState2;
			HistoryState historyState = (historyState2 = this._historyStates[this._currentHistoryStateIndex]);
			if (historyState2.EntityUpdates == null)
			{
				historyState2.EntityUpdates = new List<EntityUpdate>();
			}
			historyState.EntityUpdates.Add(new EntityUpdate
			{
				EntityId = entity.EntityId,
				EntityProperties = entity.Properties,
				NewPosition = entity.ServerPos.Copy()
			});
		}

		public void StoreEntityMoveToHistory(BlockPos start, BlockPos end, Vec3i offset)
		{
			HistoryState historyState = this._historyStates[this._currentHistoryStateIndex];
			HistoryState historyState2 = historyState;
			if (historyState2.EntityUpdates == null)
			{
				historyState2.EntityUpdates = new List<EntityUpdate>();
			}
			foreach (Entity entity in this.worldAccessor.GetEntitiesInsideCuboid(start, end, (Entity e) => !(e is EntityPlayer)))
			{
				EntityPos newPosition = entity.ServerPos.Copy().Add((double)offset.X, (double)offset.Y, (double)offset.Z);
				historyState.EntityUpdates.Add(new EntityUpdate
				{
					EntityId = entity.EntityId,
					OldPosition = entity.ServerPos.Copy(),
					NewPosition = newPosition
				});
				entity.TeleportTo(newPosition, null);
			}
		}

		public void EndMultiEdit()
		{
			this._multiedit = false;
			if (this._blockUpdates.Count > 0)
			{
				HistoryState hs = new HistoryState
				{
					BlockUpdates = this._blockUpdates.ToArray()
				};
				this.worldmap.SendBlockUpdateBulk(this._blockUpdates.Select((BlockUpdate bu) => bu.Pos), this.relight);
				this.worldmap.SendDecorUpdateBulk(from b in this._blockUpdates
					where b.Decors != null && b.Pos != null
					select b into bu
					select bu.Pos);
				this.StoreHistoryState(hs);
			}
			this.CommitBlockEntityData();
			this._blockUpdates.Clear();
			this.synchronize = true;
		}

		public void CommitBlockEntityData()
		{
			if (this._multiedit)
			{
				return;
			}
			foreach (BlockUpdate update in this._historyStates[0].BlockUpdates)
			{
				if (update.NewSolidBlockId >= 0 && this.worldAccessor.Blocks[update.NewSolidBlockId].EntityClass != null)
				{
					TreeAttribute tree = new TreeAttribute();
					BlockEntity blockEntity = this.GetBlockEntity(update.Pos);
					if (blockEntity != null)
					{
						blockEntity.ToTreeAttributes(tree);
					}
					if (blockEntity != null)
					{
						blockEntity.MarkDirty(true, null);
					}
					update.NewBlockEntityData = tree.ToBytes();
				}
			}
		}

		public void ChangeHistoryState(int quantity = 1)
		{
			bool redo = quantity < 0;
			quantity = Math.Abs(quantity);
			while (quantity > 0)
			{
				this._currentHistoryStateIndex += (redo ? (-1) : 1);
				if (this._currentHistoryStateIndex < 0)
				{
					this._currentHistoryStateIndex = 0;
					return;
				}
				if (this._currentHistoryStateIndex > this.AvailableHistoryStates)
				{
					this._currentHistoryStateIndex = this.AvailableHistoryStates;
					return;
				}
				HistoryState hs;
				if (redo)
				{
					hs = this._historyStates[this._currentHistoryStateIndex];
					this.RedoUpdate(hs);
				}
				else
				{
					hs = this._historyStates[this._currentHistoryStateIndex - 1];
					this.UndoUpdate(hs);
				}
				quantity--;
				List<BlockUpdate> updatedBlocks = base.Commit();
				if (!redo)
				{
					base.PostCommitCleanup(updatedBlocks);
				}
				Action<HistoryState, int> onRestoreHistoryState = this.OnRestoreHistoryState;
				if (onRestoreHistoryState != null)
				{
					onRestoreHistoryState(hs, redo ? 1 : (-1));
				}
				for (int i = 0; i < hs.BlockUpdates.Length; i++)
				{
					BlockUpdate upd = hs.BlockUpdates[i];
					BlockEntity be = null;
					TreeAttribute tree = null;
					if (redo)
					{
						if (upd.NewSolidBlockId >= 0 && this.worldAccessor.Blocks[upd.NewSolidBlockId].EntityClass != null && upd.NewBlockEntityData != null)
						{
							tree = TreeAttribute.CreateFromBytes(upd.NewBlockEntityData);
							be = this.GetBlockEntity(upd.Pos);
						}
					}
					else if (upd.OldBlockId >= 0 && this.worldAccessor.Blocks[upd.OldBlockId].EntityClass != null)
					{
						tree = TreeAttribute.CreateFromBytes(upd.OldBlockEntityData);
						be = this.GetBlockEntity(upd.Pos);
					}
					if (be != null)
					{
						be.FromTreeAttributes(tree, this.worldAccessor);
					}
					if (be != null)
					{
						be.HistoryStateRestore();
					}
					if (be != null)
					{
						be.MarkDirty(true, null);
					}
				}
			}
		}

		private void RedoUpdate(HistoryState state)
		{
			foreach (BlockUpdate upd in state.BlockUpdates)
			{
				BlockPos copied = upd.Pos.Copy();
				BlockUpdate bu;
				if (this.StagedBlocks.TryGetValue(copied, out bu))
				{
					bu.NewSolidBlockId = upd.NewSolidBlockId;
					bu.NewFluidBlockId = upd.NewFluidBlockId;
					bu.ByStack = upd.ByStack;
				}
				else
				{
					this.StagedBlocks[copied] = new BlockUpdate
					{
						NewSolidBlockId = upd.NewSolidBlockId,
						NewFluidBlockId = upd.NewFluidBlockId,
						ByStack = upd.ByStack,
						Pos = copied
					};
				}
				if (upd.Decors != null)
				{
					BlockUpdate blockUpdate = this.StagedBlocks[copied];
					List<DecorUpdate> list;
					if ((list = blockUpdate.Decors) == null)
					{
						list = (blockUpdate.Decors = new List<DecorUpdate>());
					}
					List<DecorUpdate> copiedDecors = list;
					foreach (DecorUpdate update in upd.Decors)
					{
						copiedDecors.Add(update);
					}
				}
			}
			if (state.EntityUpdates != null)
			{
				foreach (EntityUpdate entityUpdate in state.EntityUpdates.Where((EntityUpdate e) => e.NewPosition != null && e.OldPosition != null))
				{
					Entity entity = this.worldAccessor.GetEntityById(entityUpdate.EntityId);
					if (entity != null)
					{
						entity.TeleportTo(entityUpdate.NewPosition, null);
					}
				}
				foreach (EntityUpdate entity2 in state.EntityUpdates.Where((EntityUpdate e) => e.OldPosition == null))
				{
					Entity entityById = this.worldAccessor.GetEntityById(entity2.EntityId);
					if (entityById != null)
					{
						if (entityById != null)
						{
							entityById.Die(EnumDespawnReason.Removed, null);
						}
					}
					else if (entity2.EntityProperties != null && entity2.NewPosition != null)
					{
						Entity newEntity = this.worldAccessor.ClassRegistry.CreateEntity(entity2.EntityProperties);
						newEntity.DidImportOrExport(entity2.NewPosition.AsBlockPos);
						newEntity.ServerPos.SetFrom(entity2.NewPosition);
						this.worldAccessor.SpawnEntity(newEntity);
						entity2.EntityId = newEntity.EntityId;
					}
				}
			}
		}

		private void UndoUpdate(HistoryState state)
		{
			BlockUpdate[] bUpdates = state.BlockUpdates;
			for (int i = bUpdates.Length - 1; i >= 0; i--)
			{
				BlockUpdate upd = bUpdates[i];
				BlockPos copied = upd.Pos.Copy();
				BlockUpdate bu;
				if (this.StagedBlocks.TryGetValue(copied, out bu))
				{
					bu.NewSolidBlockId = upd.OldBlockId;
					bu.NewFluidBlockId = upd.OldFluidBlockId;
					bu.ByStack = upd.ByStack;
				}
				else
				{
					this.StagedBlocks[copied] = new BlockUpdate
					{
						NewSolidBlockId = upd.OldBlockId,
						NewFluidBlockId = upd.OldFluidBlockId,
						ByStack = upd.ByStack,
						Pos = copied
					};
				}
				if (upd.OldDecors != null)
				{
					BlockUpdate blockUpdate = this.StagedBlocks[copied];
					List<DecorUpdate> list;
					if ((list = blockUpdate.Decors) == null)
					{
						list = (blockUpdate.Decors = new List<DecorUpdate>());
					}
					List<DecorUpdate> copiedDecors = list;
					foreach (DecorUpdate update in upd.OldDecors)
					{
						copiedDecors.Add(update);
					}
				}
			}
			if (state.EntityUpdates != null)
			{
				foreach (EntityUpdate entityUpdate in state.EntityUpdates.Where((EntityUpdate e) => e.NewPosition != null && e.OldPosition != null))
				{
					Entity entity = this.worldAccessor.GetEntityById(entityUpdate.EntityId);
					if (entity != null)
					{
						entity.TeleportTo(entityUpdate.OldPosition, null);
					}
				}
				foreach (EntityUpdate entity2 in state.EntityUpdates.Where((EntityUpdate e) => e.OldPosition == null))
				{
					Entity entityById = this.worldAccessor.GetEntityById(entity2.EntityId);
					if (entityById != null)
					{
						if (entityById != null)
						{
							entityById.Die(EnumDespawnReason.Removed, null);
						}
					}
					else if (entity2.EntityProperties != null && entity2.NewPosition != null)
					{
						Entity newEntity = this.worldAccessor.ClassRegistry.CreateEntity(entity2.EntityProperties);
						newEntity.DidImportOrExport(entity2.NewPosition.AsBlockPos);
						newEntity.ServerPos.SetFrom(entity2.NewPosition);
						this.worldAccessor.SpawnEntity(newEntity);
						entity2.EntityId = newEntity.EntityId;
					}
				}
			}
		}

		private readonly List<HistoryState> _historyStates = new List<HistoryState>();

		private int _currentHistoryStateIndex;

		private int _maxQuantityStates = 35;

		private bool _multiedit;

		private List<BlockUpdate> _blockUpdates = new List<BlockUpdate>();
	}
}
