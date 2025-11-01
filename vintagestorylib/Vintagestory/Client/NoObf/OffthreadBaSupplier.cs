using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class OffthreadBaSupplier : IWorldIntersectionSupplier
	{
		public OffthreadBaSupplier(ClientMain game)
		{
			this.ba = game.GetLockFreeBlockAccessor();
		}

		public Vec3i MapSize
		{
			get
			{
				return this.ba.MapSize;
			}
		}

		public IBlockAccessor blockAccessor
		{
			get
			{
				return this.ba;
			}
		}

		public Block GetBlock(BlockPos pos)
		{
			return this.ba.GetBlock(pos);
		}

		public bool IsValidPos(BlockPos pos)
		{
			return this.ba.IsValidPos(pos);
		}

		public Cuboidf[] GetBlockIntersectionBoxes(BlockPos pos)
		{
			return this.ba.GetBlock(pos).GetSelectionBoxes(this.ba, pos);
		}

		public Entity[] GetEntitiesAround(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null)
		{
			throw new NotImplementedException();
		}

		private IBlockAccessor ba;
	}
}
