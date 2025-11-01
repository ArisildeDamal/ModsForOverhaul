using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common
{
	public abstract class GameMain : IWorldIntersectionSupplier
	{
		public Block WaterBlock { get; set; }

		public abstract ClassRegistry ClassRegistryInt { get; set; }

		public abstract IWorldAccessor World { get; }

		protected abstract WorldMap worldmap { get; }

		public GameMain()
		{
			this.Blocks = new BlockList(this, 10000);
			this.ClassRegistryInt = new ClassRegistry();
			this.GridRecipes = this.RegisterRecipeRegistry<RecipeRegistryGeneric<GridRecipe>>("gridrecipes").Recipes;
			this.interesectionTester = new AABBIntersectionTest(this);
		}

		public float RandomPitch()
		{
			return (float)this.World.Rand.NextDouble() * 0.5f + 0.75f;
		}

		public RecipeRegistryBase GetRecipeRegistry(string code)
		{
			RecipeRegistryBase obj;
			this.recipeRegistries.TryGetValue(code, out obj);
			return obj;
		}

		public T RegisterRecipeRegistry<T>(string recipeRegistryCode) where T : RecipeRegistryBase
		{
			this.ClassRegistryInt.RegisterRecipeRegistry<T>(recipeRegistryCode);
			T rec = this.ClassRegistryInt.CreateRecipeRegistry<T>(recipeRegistryCode);
			this.recipeRegistries[recipeRegistryCode] = rec;
			return rec;
		}

		public AABBIntersectionTest InteresectionTester
		{
			get
			{
				return this.interesectionTester;
			}
		}

		public virtual Vec3i MapSize
		{
			get
			{
				return null;
			}
		}

		public void LoadCollectibles(IList<Item> items, IList<Block> blocks)
		{
			this.Collectibles = new List<CollectibleObject>();
			foreach (Item item in items)
			{
				if (!(((item != null) ? item.Code : null) == null) && !item.IsMissing)
				{
					this.Collectibles.Add(item);
				}
			}
			foreach (Block block in blocks)
			{
				if (!block.IsMissing)
				{
					this.Collectibles.Add(block);
				}
			}
		}

		public void RayTraceForSelection(IWorldIntersectionSupplier supplier, Vec3d fromPos, Vec3d toPos, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
		{
			Ray ray = Ray.FromPositions(fromPos, toPos);
			if (ray == null)
			{
				return;
			}
			this.RayTraceForSelection(supplier, ray, ref blockSelection, ref entitySelection, bfilter, efilter);
		}

		public void RayTraceForSelection(Vec3d fromPos, Vec3d toPos, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
		{
			Ray ray = Ray.FromPositions(fromPos, toPos);
			if (ray == null)
			{
				return;
			}
			this.RayTraceForSelection(this, ray, ref blockSelection, ref entitySelection, bfilter, efilter);
		}

		public void RayTraceForSelection(Vec3d fromPos, float pitch, float yaw, float range, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
		{
			Ray ray = Ray.FromAngles(fromPos, pitch, yaw, range);
			if (ray == null)
			{
				return;
			}
			this.RayTraceForSelection(this, ray, ref blockSelection, ref entitySelection, bfilter, efilter);
		}

		public void RayTraceForSelection(Ray ray, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
		{
			this.RayTraceForSelection(this, ray, ref blockSelection, ref entitySelection, bfilter, efilter);
		}

		public void RayTraceForSelection(IWorldIntersectionSupplier supplier, Ray ray, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
		{
			this.interesectionTester.LoadRayAndPos(ray);
			this.interesectionTester.bsTester = supplier;
			float range = (float)ray.Length;
			blockSelection = this.interesectionTester.GetSelectedBlock(range, bfilter, false);
			Entity[] entities = supplier.GetEntitiesAround(ray.origin, range, range, (Entity entity) => efilter == null || efilter(entity));
			Entity collidedEntity = null;
			double collidedDistance = double.MaxValue;
			foreach (Entity entity2 in entities)
			{
				int selectionBoxIndex = 0;
				double intersectionDistance;
				if (entity2.IntersectsRay(ray, this.interesectionTester, out intersectionDistance, ref selectionBoxIndex) && intersectionDistance < collidedDistance)
				{
					collidedEntity = entity2;
					collidedDistance = intersectionDistance;
					this.entitySelTmp.SelectionBoxIndex = selectionBoxIndex;
					this.entitySelTmp.Entity = entity2;
					this.entitySelTmp.Face = this.interesectionTester.hitOnBlockFace;
					this.entitySelTmp.HitPosition = this.interesectionTester.hitPosition.SubCopy(entity2.SidedPos.X, entity2.SidedPos.Y, entity2.SidedPos.Z);
					this.entitySelTmp.Position = entity2.SidedPos.XYZ;
				}
			}
			entitySelection = null;
			if (collidedEntity == null)
			{
				return;
			}
			if (blockSelection != null)
			{
				BlockPos pos = blockSelection.Position;
				Vec3d blockHitPosition = new Vec3d((double)pos.X, (double)pos.InternalY, (double)pos.Z).Add(blockSelection.HitPosition);
				Vec3d entityHitPosition = new Vec3d(collidedEntity.SidedPos.X, collidedEntity.SidedPos.Y, collidedEntity.SidedPos.Z).Add(this.entitySelTmp.HitPosition);
				float num = ray.origin.SquareDistanceTo(entityHitPosition);
				float bdist = ray.origin.SquareDistanceTo(blockHitPosition);
				if (num < bdist)
				{
					blockSelection = null;
					entitySelection = this.entitySelTmp.Clone();
					return;
				}
			}
			else
			{
				entitySelection = this.entitySelTmp.Clone();
			}
		}

		public void RayTraceForSelection(IPlayer player, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
		{
			Vec3d pos = player.Entity.Pos.XYZ.Add(player.Entity.LocalEyePos);
			this.RayTraceForSelection(pos, player.Entity.SidedPos.Pitch, player.Entity.SidedPos.Yaw, player.WorldData.PickingRange, ref blockSelection, ref entitySelection, bfilter, efilter);
		}

		public Entity[] GetIntersectingEntities(BlockPos basePos, Cuboidf[] collisionBoxes, ActionConsumable<Entity> matches = null)
		{
			if (collisionBoxes == null)
			{
				return Array.Empty<Entity>();
			}
			return this.GetEntitiesAround(GameMain.MidVec3d.AddCopy(basePos), 5f, 5f, delegate(Entity e)
			{
				if (!matches(e))
				{
					return false;
				}
				for (int i = 0; i < collisionBoxes.Length; i++)
				{
					if (CollisionTester.AabbIntersect(collisionBoxes[i], (double)basePos.X, (double)basePos.Y, (double)basePos.Z, e.SelectionBox, e.Pos.XYZ))
					{
						return true;
					}
				}
				return false;
			});
		}

		public Entity[] GetEntitiesAround(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null)
		{
			int num = (int)((position.X - (double)horRange) / 32.0);
			int maxcx = (int)((position.X + (double)horRange) / 32.0);
			int mincy = (int)((position.Y - (double)vertRange) / 32.0);
			int maxcy = (int)((position.Y + (double)vertRange) / 32.0);
			int mincz = (int)((position.Z - (double)horRange) / 32.0);
			int maxcz = (int)((position.Z + (double)horRange) / 32.0);
			List<Entity> entities = new List<Entity>();
			float horRangeSq = horRange * horRange;
			if (matches == null)
			{
				matches = (Entity e) => true;
			}
			for (int cx = num; cx <= maxcx; cx++)
			{
				for (int cy = mincy; cy <= maxcy; cy++)
				{
					for (int cz = mincz; cz <= maxcz; cz++)
					{
						IWorldChunk chunk = this.World.BlockAccessor.GetChunk(cx, cy, cz);
						if (chunk != null && chunk.Entities != null)
						{
							for (int i = 0; i < chunk.Entities.Length; i++)
							{
								Entity entity = chunk.Entities[i];
								if (entity == null)
								{
									if (i >= chunk.EntitiesCount)
									{
										break;
									}
								}
								else if (entity.State != EnumEntityState.Despawned && matches(entity) && entity.InRangeOf(position, horRangeSq, vertRange))
								{
									entities.Add(entity);
								}
							}
						}
					}
				}
			}
			return entities.ToArray();
		}

		public Entity[] GetEntitiesInsideCuboid(BlockPos startPos, BlockPos endPos, ActionConsumable<Entity> matches = null)
		{
			int startX = Math.Min(startPos.X, endPos.X);
			int startY = Math.Min(startPos.InternalY, endPos.InternalY);
			int startZ = Math.Min(startPos.Z, endPos.Z);
			int endX = Math.Max(startPos.X, endPos.X);
			int endY = Math.Max(startPos.InternalY, endPos.InternalY);
			int endZ = Math.Max(startPos.Z, endPos.Z);
			int num = startX / 32;
			int maxcx = endX / 32;
			int mincy = startY / 32;
			int maxcy = endY / 32;
			int mincz = startZ / 32;
			int maxcz = endZ / 32;
			List<Entity> entities = new List<Entity>();
			if (matches == null)
			{
				matches = (Entity e) => true;
			}
			for (int cx = num; cx <= maxcx; cx++)
			{
				for (int cy = mincy; cy <= maxcy; cy++)
				{
					for (int cz = mincz; cz <= maxcz; cz++)
					{
						IWorldChunk chunk = this.World.BlockAccessor.GetChunk(cx, cy, cz);
						if (chunk != null && chunk.Entities != null)
						{
							for (int i = 0; i < chunk.EntitiesCount; i++)
							{
								Entity entity = chunk.Entities[i];
								EntityPos pos = entity.SidedPos;
								if (pos.X >= (double)startX && pos.InternalY >= (double)startY && pos.Z >= (double)startZ && pos.X <= (double)endX && pos.InternalY <= (double)endY && pos.Z <= (double)endZ && entity != null && matches(entity) && entity.State != EnumEntityState.Despawned)
								{
									entities.Add(entity);
								}
							}
						}
					}
				}
			}
			return entities.ToArray();
		}

		public Cuboidf[] GetBlockIntersectionBoxes(BlockPos pos, bool liquidSelectable)
		{
			int decorCount = 0;
			WorldChunk chunk = this.blockAccessor.GetChunkAtBlockPos(pos) as WorldChunk;
			List<Cuboidf> decors;
			if (chunk != null)
			{
				decors = chunk.GetDecorSelectionBoxes(this.blockAccessor, pos);
				decorCount = decors.Count;
			}
			else
			{
				decors = null;
			}
			Block liquidBlock;
			int resultIndex;
			Cuboidf[] result;
			if (liquidSelectable && (liquidBlock = this.blockAccessor.GetBlock(pos, 2)).IsLiquid())
			{
				resultIndex = 1;
				result = new Cuboidf[resultIndex + decorCount];
				result[0] = new Cuboidf(0f, 0f, 0f, 1f, (float)liquidBlock.LiquidLevel / 8f, 1f);
			}
			else
			{
				Cuboidf[] boxen = this.blockAccessor.GetBlock(pos).GetSelectionBoxes(this.blockAccessor, pos);
				if (decorCount == 0)
				{
					return boxen;
				}
				resultIndex = ((boxen == null) ? 0 : boxen.Length);
				result = new Cuboidf[resultIndex + decorCount];
				for (int i = 0; i < resultIndex; i++)
				{
					result[i] = boxen[i];
				}
			}
			for (int j = 0; j < decorCount; j++)
			{
				result[resultIndex + j] = decors[j];
			}
			return result;
		}

		public abstract IBlockAccessor blockAccessor { get; }

		public abstract Block GetBlock(BlockPos pos);

		public abstract bool IsValidPos(BlockPos pos);

		public Item GetItem(AssetLocation itemCode)
		{
			Item item;
			this.ItemsByCode.TryGetValue(itemCode, out item);
			return item;
		}

		public Block GetBlock(AssetLocation blockCode)
		{
			if (blockCode == null)
			{
				return null;
			}
			Block block;
			this.BlocksByCode.TryGetValue(blockCode, out block);
			return block;
		}

		public Block[] SearchBlocks(AssetLocation wildcard)
		{
			return (this.Blocks as BlockList).Search(wildcard);
		}

		public Item[] SearchItems(AssetLocation wildcard)
		{
			List<Item> foundItems = new List<Item>();
			foreach (Item item in this.Items)
			{
				if (((item != null) ? item.Code : null) != null && !item.IsMissing && item.WildCardMatch(wildcard))
				{
					foundItems.Add(item);
				}
			}
			return foundItems.ToArray();
		}

		public ICachingBlockAccessor GetCachingBlockAccessor(bool synchronize, bool relight)
		{
			return new BlockAccessorCaching(this.worldmap, this.World, synchronize, relight);
		}

		public IBlockAccessor GetLockFreeBlockAccessor()
		{
			return new BlockAccessorReadLockfree(this.worldmap, this.World);
		}

		public IBlockAccessor GetBlockAccessor(bool synchronize, bool relight, bool strict, bool debug = false)
		{
			if (strict)
			{
				return new BlockAccessorStrict(this.worldmap, this.World, synchronize, relight, debug);
			}
			return new BlockAccessorRelaxed(this.worldmap, this.World, synchronize, relight);
		}

		public IBulkBlockAccessor GetBlockAccessorBulkUpdate(bool synchronize, bool relight, bool debug = false)
		{
			return new BlockAccessorRelaxedBulkUpdate(this.worldmap, this.World, synchronize, relight, debug);
		}

		public IBulkBlockAccessor GetBlockAccessorBulkMinimalUpdate(bool synchronize, bool debug = false)
		{
			return new BlockAccessorBulkMinimalUpdate(this.worldmap, this.World, synchronize, debug);
		}

		public IBlockAccessorRevertable GetBlockAccessorRevertable(bool synchronize, bool relight, bool debug = false)
		{
			return new BlockAccessorRevertable(this.worldmap, this.World, synchronize, relight, debug);
		}

		public IBlockAccessorPrefetch GetBlockAccessorPrefetch(bool synchronize, bool relight)
		{
			return new BlockAccessorPrefetch(this.worldmap, this.World, synchronize, relight);
		}

		public IBulkBlockAccessor GetBlockAccessorMapChunkLoading(bool synchronize, bool debug = false)
		{
			return new BlockAccessorMapChunkLoading(this.worldmap, this.World, synchronize, debug);
		}

		public virtual Cuboidf[] GetBlockIntersectionBoxes(BlockPos pos)
		{
			return this.GetBlockIntersectionBoxes(pos, false);
		}

		public AABBIntersectionTest interesectionTester;

		public List<CollectibleObject> Collectibles = new List<CollectibleObject>();

		public IList<Item> Items = new List<Item>();

		public IList<Block> Blocks;

		public TagRegistry TagRegistry = new TagRegistry();

		public List<GridRecipe> GridRecipes = new List<GridRecipe>();

		public OrderedDictionary<string, ColorMap> ColorMaps = new OrderedDictionary<string, ColorMap>();

		public Dictionary<string, RecipeRegistryBase> recipeRegistries = new Dictionary<string, RecipeRegistryBase>();

		public Dictionary<AssetLocation, Item> ItemsByCode = new Dictionary<AssetLocation, Item>();

		public Dictionary<AssetLocation, Block> BlocksByCode = new Dictionary<AssetLocation, Block>();

		private EntitySelection entitySelTmp = new EntitySelection();

		private static readonly Vec3d MidVec3d = new Vec3d(0.5, 0.5, 0.5);
	}
}
