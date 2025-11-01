using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	internal class WorldgenWorldAccessor : IServerWorldAccessor, IWorldAccessor
	{
		public WorldgenWorldAccessor(IServerWorldAccessor worldAccessor, BlockAccessorWorldGen blockAccessorWorldGen)
		{
			this.waBase = worldAccessor;
			this.blockAccessorWorldGen = blockAccessorWorldGen;
		}

		public IBlockAccessor BlockAccessor
		{
			get
			{
				return this.blockAccessorWorldGen;
			}
		}

		public ITreeAttribute Config
		{
			get
			{
				return this.waBase.Config;
			}
		}

		public EntityPos DefaultSpawnPosition
		{
			get
			{
				return this.waBase.DefaultSpawnPosition;
			}
		}

		public FrameProfilerUtil FrameProfiler
		{
			get
			{
				return this.waBase.FrameProfiler;
			}
		}

		public ICoreAPI Api
		{
			get
			{
				return this.waBase.Api;
			}
		}

		public IChunkProvider ChunkProvider
		{
			get
			{
				return this.waBase.ChunkProvider;
			}
		}

		public ILandClaimAPI Claims
		{
			get
			{
				return this.waBase.Claims;
			}
		}

		public long[] LoadedChunkIndices
		{
			get
			{
				return this.waBase.LoadedChunkIndices;
			}
		}

		public long[] LoadedMapChunkIndices
		{
			get
			{
				return this.waBase.LoadedMapChunkIndices;
			}
		}

		public float[] BlockLightLevels
		{
			get
			{
				return this.waBase.BlockLightLevels;
			}
		}

		public float[] SunLightLevels
		{
			get
			{
				return this.waBase.SunLightLevels;
			}
		}

		public int SeaLevel
		{
			get
			{
				return this.waBase.SeaLevel;
			}
		}

		public int Seed
		{
			get
			{
				return this.waBase.Seed;
			}
		}

		public string SavegameIdentifier
		{
			get
			{
				return this.waBase.SavegameIdentifier;
			}
		}

		public int SunBrightness
		{
			get
			{
				return this.waBase.SunBrightness;
			}
		}

		public bool EntityDebugMode
		{
			get
			{
				return this.waBase.EntityDebugMode;
			}
		}

		public IAssetManager AssetManager
		{
			get
			{
				return this.waBase.AssetManager;
			}
		}

		public ILogger Logger
		{
			get
			{
				return this.waBase.Logger;
			}
		}

		public EnumAppSide Side
		{
			get
			{
				return this.waBase.Side;
			}
		}

		public IBulkBlockAccessor BulkBlockAccessor
		{
			get
			{
				return this.waBase.BulkBlockAccessor;
			}
		}

		public IClassRegistryAPI ClassRegistry
		{
			get
			{
				return this.waBase.ClassRegistry;
			}
		}

		public IGameCalendar Calendar
		{
			get
			{
				return this.waBase.Calendar;
			}
		}

		public CollisionTester CollisionTester
		{
			get
			{
				return this.waBase.CollisionTester;
			}
		}

		public Random Rand
		{
			get
			{
				return this.waBase.Rand;
			}
		}

		public long ElapsedMilliseconds
		{
			get
			{
				return this.waBase.ElapsedMilliseconds;
			}
		}

		public List<CollectibleObject> Collectibles
		{
			get
			{
				return this.waBase.Collectibles;
			}
		}

		public IList<Block> Blocks
		{
			get
			{
				return this.waBase.Blocks;
			}
		}

		public IList<Item> Items
		{
			get
			{
				return this.waBase.Items;
			}
		}

		public List<EntityProperties> EntityTypes
		{
			get
			{
				return this.waBase.EntityTypes;
			}
		}

		public List<string> EntityTypeCodes
		{
			get
			{
				return this.waBase.EntityTypeCodes;
			}
		}

		public List<GridRecipe> GridRecipes
		{
			get
			{
				return this.waBase.GridRecipes;
			}
		}

		public int DefaultEntityTrackingRange
		{
			get
			{
				return this.waBase.DefaultEntityTrackingRange;
			}
		}

		public IPlayer[] AllOnlinePlayers
		{
			get
			{
				return this.waBase.AllOnlinePlayers;
			}
		}

		public IPlayer[] AllPlayers
		{
			get
			{
				return this.waBase.AllPlayers;
			}
		}

		public AABBIntersectionTest InteresectionTester
		{
			get
			{
				return this.waBase.InteresectionTester;
			}
		}

		public ConcurrentDictionary<long, Entity> LoadedEntities
		{
			get
			{
				return this.waBase.LoadedEntities;
			}
		}

		public OrderedDictionary<AssetLocation, ITreeGenerator> TreeGenerators
		{
			get
			{
				return this.waBase.TreeGenerators;
			}
		}

		public Dictionary<string, string> RemappedEntities
		{
			get
			{
				return this.waBase.RemappedEntities;
			}
		}

		public string WorldName
		{
			get
			{
				return this.waBase.WorldName;
			}
		}

		public void CreateExplosion(BlockPos pos, EnumBlastType blastType, double destructionRadius, double injureRadius, float blockDropChanceMultiplier = 1f, string ignitedByPlayerUid = null)
		{
			this.waBase.CreateExplosion(pos, blastType, destructionRadius, injureRadius, blockDropChanceMultiplier, ignitedByPlayerUid);
		}

		public void DespawnEntity(Entity entity, EntityDespawnData reason)
		{
			this.waBase.DespawnEntity(entity, reason);
		}

		public Block GetBlock(int blockId)
		{
			return this.waBase.GetBlock(blockId);
		}

		public Block GetBlock(AssetLocation blockCode)
		{
			return this.waBase.GetBlock(blockCode);
		}

		public IBlockAccessor GetBlockAccessor(bool synchronize, bool relight, bool strict, bool debug = false)
		{
			return this.waBase.GetBlockAccessor(synchronize, relight, strict, debug);
		}

		public IBulkBlockAccessor GetBlockAccessorBulkMinimalUpdate(bool synchronize, bool debug = false)
		{
			return this.waBase.GetBlockAccessorBulkMinimalUpdate(synchronize, debug);
		}

		public IBulkBlockAccessor GetBlockAccessorBulkUpdate(bool synchronize, bool relight, bool debug = false)
		{
			return this.waBase.GetBlockAccessorBulkUpdate(synchronize, relight, debug);
		}

		public IBulkBlockAccessor GetBlockAccessorMapChunkLoading(bool synchronize, bool debug = false)
		{
			return this.waBase.GetBlockAccessorMapChunkLoading(synchronize, debug);
		}

		public IBlockAccessorPrefetch GetBlockAccessorPrefetch(bool synchronize, bool relight)
		{
			return this.waBase.GetBlockAccessorPrefetch(synchronize, relight);
		}

		public IBlockAccessorRevertable GetBlockAccessorRevertable(bool synchronize, bool relight, bool debug = false)
		{
			return this.waBase.GetBlockAccessorRevertable(synchronize, relight, debug);
		}

		public ICachingBlockAccessor GetCachingBlockAccessor(bool synchronize, bool relight)
		{
			return this.waBase.GetCachingBlockAccessor(synchronize, relight);
		}

		public Entity[] GetEntitiesAround(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null)
		{
			return this.waBase.GetEntitiesAround(position, horRange, vertRange, matches);
		}

		public Entity[] GetEntitiesInsideCuboid(BlockPos startPos, BlockPos endPos, ActionConsumable<Entity> matches = null)
		{
			return this.waBase.GetEntitiesInsideCuboid(startPos, endPos, matches);
		}

		public Entity GetEntityById(long entityId)
		{
			return this.waBase.GetEntityById(entityId);
		}

		public EntityProperties GetEntityType(AssetLocation entityCode)
		{
			return this.waBase.GetEntityType(entityCode);
		}

		public Entity[] GetIntersectingEntities(BlockPos basePos, Cuboidf[] collisionBoxes, ActionConsumable<Entity> matches = null)
		{
			return this.waBase.GetIntersectingEntities(basePos, collisionBoxes, matches);
		}

		public Item GetItem(int itemId)
		{
			return this.waBase.GetItem(itemId);
		}

		public Item GetItem(AssetLocation itemCode)
		{
			return this.waBase.GetItem(itemCode);
		}

		public IBlockAccessor GetLockFreeBlockAccessor()
		{
			return this.waBase.GetLockFreeBlockAccessor();
		}

		public Entity GetNearestEntity(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null)
		{
			return this.waBase.GetNearestEntity(position, horRange, vertRange, matches);
		}

		public IPlayer[] GetPlayersAround(Vec3d position, float horRange, float vertRange, ActionConsumable<IPlayer> matches = null)
		{
			return this.waBase.GetPlayersAround(position, horRange, vertRange, matches);
		}

		public void HighlightBlocks(IPlayer player, int highlightSlotId, List<BlockPos> blocks, List<int> colors, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary, float scale = 1f)
		{
			this.waBase.HighlightBlocks(player, highlightSlotId, blocks, colors, mode, shape, scale);
		}

		public void HighlightBlocks(IPlayer player, int highlightSlotId, List<BlockPos> blocks, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary)
		{
			this.waBase.HighlightBlocks(player, highlightSlotId, blocks, mode, shape);
		}

		public bool IsFullyLoadedChunk(BlockPos pos)
		{
			return this.waBase.IsFullyLoadedChunk(pos);
		}

		public IPlayer NearestPlayer(double x, double y, double z)
		{
			return this.waBase.NearestPlayer(x, y, z);
		}

		public IPlayer PlayerByUid(string playerUid)
		{
			return this.waBase.PlayerByUid(playerUid);
		}

		public bool PlayerHasPrivilege(int clientid, string privilege)
		{
			return this.waBase.PlayerHasPrivilege(clientid, privilege);
		}

		public void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
		{
			this.waBase.PlaySoundAt(location, posx, posy, posz, dualCallByPlayer, randomizePitch, range, volume);
		}

		public void PlaySoundAt(AssetLocation location, BlockPos pos, double yOffsetFromCenter, IPlayer ignorePlayerUid = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
		{
			this.waBase.PlaySoundAt(location, pos, yOffsetFromCenter, ignorePlayerUid, randomizePitch, range, volume);
		}

		public void PlaySoundAt(AssetLocation location, Entity atEntity, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
		{
			this.waBase.PlaySoundAt(location, atEntity, dualCallByPlayer, randomizePitch, range, volume);
		}

		public void PlaySoundAt(AssetLocation location, Entity atEntity, IPlayer dualCallByPlayer, float pitch, float range = 32f, float volume = 1f)
		{
			this.waBase.PlaySoundAt(location, atEntity, dualCallByPlayer, pitch, range, volume);
		}

		public void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer, float pitch, float range = 32f, float volume = 1f)
		{
			this.waBase.PlaySoundAt(location, posx, posy, posz, dualCallByPlayer, pitch, range, volume);
		}

		public void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer, EnumSoundType soundType, float pitch, float range = 32f, float volume = 1f)
		{
			this.waBase.PlaySoundAt(location, posx, posy, posz, dualCallByPlayer, soundType, pitch, range, volume);
		}

		public void PlaySoundAt(AssetLocation location, IPlayer atPlayer, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
		{
			this.waBase.PlaySoundAt(location, atPlayer, dualCallByPlayer, randomizePitch, range, volume);
		}

		public void PlaySoundFor(AssetLocation location, IPlayer forPlayer, bool randomizePitch = true, float range = 32f, float volume = 1f)
		{
			this.waBase.PlaySoundFor(location, forPlayer, randomizePitch, range, volume);
		}

		public void PlaySoundFor(AssetLocation location, IPlayer forPlayer, float pitch, float range = 32f, float volume = 1f)
		{
			this.waBase.PlaySoundFor(location, forPlayer, pitch, range, volume);
		}

		public void RayTraceForSelection(Vec3d fromPos, Vec3d toPos, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
		{
			this.waBase.RayTraceForSelection(fromPos, toPos, ref blockSelection, ref entitySelection, bfilter, efilter);
		}

		public void RayTraceForSelection(IWorldIntersectionSupplier supplier, Vec3d fromPos, Vec3d toPos, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
		{
			this.waBase.RayTraceForSelection(supplier, fromPos, toPos, ref blockSelection, ref entitySelection, bfilter, efilter);
		}

		public void RayTraceForSelection(Vec3d fromPos, float pitch, float yaw, float range, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
		{
			this.waBase.RayTraceForSelection(fromPos, pitch, yaw, range, ref blockSelection, ref entitySelection, bfilter, efilter);
		}

		public void RayTraceForSelection(Ray ray, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter filter = null, EntityFilter efilter = null)
		{
			this.waBase.RayTraceForSelection(ray, ref blockSelection, ref entitySelection, filter, efilter);
		}

		public long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay)
		{
			return this.waBase.RegisterCallback(OnTimePassed, millisecondDelay);
		}

		public long RegisterCallback(Action<IWorldAccessor, BlockPos, float> OnTimePassed, BlockPos pos, int millisecondDelay)
		{
			return this.waBase.RegisterCallback(OnTimePassed, pos, millisecondDelay);
		}

		public long RegisterCallbackUnique(Action<IWorldAccessor, BlockPos, float> OnGameTick, BlockPos pos, int millisecondInterval)
		{
			return this.waBase.RegisterCallbackUnique(OnGameTick, pos, millisecondInterval);
		}

		public long RegisterGameTickListener(Action<float> OnGameTick, int millisecondInterval, int initialDelayOffsetMs = 0)
		{
			return this.waBase.RegisterGameTickListener(OnGameTick, millisecondInterval, initialDelayOffsetMs);
		}

		public Block[] SearchBlocks(AssetLocation wildcard)
		{
			return this.waBase.SearchBlocks(wildcard);
		}

		public Item[] SearchItems(AssetLocation wildcard)
		{
			return this.waBase.SearchItems(wildcard);
		}

		public void SpawnCubeParticles(BlockPos blockPos, Vec3d pos, float radius, int quantity, float scale = 1f, IPlayer dualCallByPlayer = null, Vec3f velocity = null)
		{
			this.waBase.SpawnCubeParticles(blockPos, pos, radius, quantity, scale, dualCallByPlayer, velocity);
		}

		public void SpawnCubeParticles(Vec3d pos, ItemStack item, float radius, int quantity, float scale = 1f, IPlayer dualCallByPlayer = null, Vec3f velocity = null)
		{
			this.waBase.SpawnCubeParticles(pos, item, radius, quantity, scale, dualCallByPlayer, velocity);
		}

		public void SpawnEntity(Entity entity)
		{
			this.waBase.SpawnEntity(entity);
		}

		public void SpawnPriorityEntity(Entity entity)
		{
			this.waBase.SpawnEntity(entity);
		}

		public Entity SpawnItemEntity(ItemStack itemstack, Vec3d position, Vec3d velocity = null)
		{
			return this.waBase.SpawnItemEntity(itemstack, position, velocity);
		}

		public Entity SpawnItemEntity(ItemStack itemstack, BlockPos pos, Vec3d velocity = null)
		{
			return this.waBase.SpawnItemEntity(itemstack, pos, velocity);
		}

		public void SpawnParticles(float quantity, int color, Vec3d minPos, Vec3d maxPos, Vec3f minVelocity, Vec3f maxVelocity, float lifeLength, float gravityEffect, float scale = 1f, EnumParticleModel model = EnumParticleModel.Quad, IPlayer dualCallByPlayer = null)
		{
			this.waBase.SpawnParticles(quantity, color, minPos, maxPos, minVelocity, maxVelocity, lifeLength, gravityEffect, scale, model, dualCallByPlayer);
		}

		public void SpawnParticles(IParticlePropertiesProvider particlePropertiesProvider, IPlayer dualCallByPlayer = null)
		{
			this.waBase.SpawnParticles(particlePropertiesProvider, dualCallByPlayer);
		}

		public void UnregisterCallback(long listenerId)
		{
			this.waBase.UnregisterCallback(listenerId);
		}

		public void UnregisterGameTickListener(long listenerId)
		{
			this.waBase.UnregisterGameTickListener(listenerId);
		}

		public RecipeRegistryBase GetRecipeRegistry(string code)
		{
			throw new NotImplementedException();
		}

		public void UpdateEntityChunk(Entity entity, long newChunkIndex3d)
		{
			this.waBase.UpdateEntityChunk(entity, newChunkIndex3d);
		}

		public bool LoadEntity(Entity entity, long fromChunkIndex3d)
		{
			throw new InvalidOperationException("Cannot use LoadEntity from within WorldGenBlockAccessor");
		}

		private IServerWorldAccessor waBase;

		private BlockAccessorWorldGen blockAccessorWorldGen;
	}
}
