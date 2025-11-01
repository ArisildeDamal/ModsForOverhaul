using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common.Database;
using Vintagestory.Server;

namespace Vintagestory.Common
{
	public abstract class WorldMap
	{
		public abstract IWorldAccessor World { get; }

		public abstract ILogger Logger { get; }

		public abstract IList<Block> Blocks { get; }

		public abstract Dictionary<AssetLocation, Block> BlocksByCode { get; }

		public abstract int MapSizeX { get; }

		public abstract int MapSizeY { get; }

		public abstract int MapSizeZ { get; }

		public abstract int RegionMapSizeX { get; }

		public abstract int RegionMapSizeY { get; }

		public abstract int RegionMapSizeZ { get; }

		public abstract int ChunkSize { get; }

		public abstract int ChunkSizeMask { get; }

		public abstract Vec3i MapSize { get; }

		public abstract int RegionSize { get; }

		public abstract List<LandClaim> All { get; }

		public abstract bool DebugClaimPrivileges { get; }

		public int ChunkMapSizeX
		{
			get
			{
				return this.MapSizeX / 32;
			}
		}

		public int ChunkMapSizeY
		{
			get
			{
				return this.chunkMapSizeY;
			}
		}

		public int ChunkMapSizeZ
		{
			get
			{
				return this.MapSizeZ / 32;
			}
		}

		public int GetLightRGBsAsInt(int posX, int posY, int posZ)
		{
			int cx = posX / 32;
			int cy = posY / 32;
			int cz = posZ / 32;
			if (!this.IsValidPos(posX, posY, posZ))
			{
				return ColorUtil.HsvToRgba(0, 0, 0, (int)(this.SunLightLevels[this.SunBrightness] * 255f));
			}
			IWorldChunk chunk = this.GetChunk(cx, cy, cz);
			if (chunk == null)
			{
				return ColorUtil.HsvToRgba(0, 0, 0, (int)(this.SunLightLevels[this.SunBrightness] * 255f));
			}
			int index3d = MapUtil.Index3d(posX & this.ChunkSizeMask, posY & this.ChunkSizeMask, posZ & this.ChunkSizeMask, 32, 32);
			int blocksat;
			ushort num = chunk.Unpack_AndReadLight(index3d, out blocksat);
			int sunl = (int)(num & 31);
			int blockl = (num >> 5) & 31;
			int blockhue = num >> 10;
			int sunb = (int)(this.SunLightLevels[sunl] * 255f);
			int num2 = (int)this.hueLevels[blockhue];
			int blocks = (int)this.satLevels[blocksat];
			int blockb = (int)(this.BlockLightLevels[blockl] * 255f);
			return ColorUtil.HsvToRgba(num2, blocks, blockb, sunb);
		}

		public Vec4f GetLightRGBSVec4f(int posX, int posY, int posZ)
		{
			int levels = this.LoadLightHSVLevels(posX, posY, posZ);
			int num = (int)this.hueLevels[(levels >> 16) & 255];
			int blocks = (int)this.satLevels[(levels >> 24) & 255];
			int blockb = (int)(this.BlockLightLevels[(levels >> 8) & 255] * 255f);
			int rgb = ColorUtil.HsvToRgb(num, blocks, blockb);
			return new Vec4f((float)(rgb >> 16) / 255f, (float)((rgb >> 8) & 255) / 255f, (float)(rgb & 255) / 255f, this.SunLightLevels[levels & 255]);
		}

		public int[] GetLightHSVLevels(int posX, int posY, int posZ)
		{
			int[] array = new int[4];
			int levels = this.LoadLightHSVLevels(posX, posY, posZ);
			array[0] = levels & 255;
			array[1] = (levels >> 8) & 255;
			array[2] = (levels >> 16) & 255;
			array[3] = (levels >> 24) & 255;
			return array;
		}

		public int LoadLightHSVLevels(int posX, int posY, int posZ)
		{
			int cx = posX / 32;
			int cy = posY / 32;
			int cz = posZ / 32;
			if (!this.IsValidPos(posX, posY, posZ))
			{
				return this.SunBrightness;
			}
			IWorldChunk chunk = this.GetChunk(cx, cy, cz);
			if (chunk == null)
			{
				return this.SunBrightness;
			}
			int index3d = MapUtil.Index3d(posX & this.ChunkSizeMask, posY & this.ChunkSizeMask, posZ & this.ChunkSizeMask, 32, 32);
			int blocksat;
			int light = (int)chunk.Unpack_AndReadLight(index3d, out blocksat);
			return (light & 31) | ((light & 992) << 3) | ((light & 64512) << 6) | (blocksat << 24);
		}

		public LandClaim[] Get(BlockPos pos)
		{
			List<LandClaim> claims = new List<LandClaim>();
			long regionindex2d = this.MapRegionIndex2D(pos.X / this.RegionSize, pos.Z / this.RegionSize);
			if (!this.LandClaimByRegion.ContainsKey(regionindex2d))
			{
				return null;
			}
			foreach (LandClaim area in this.LandClaimByRegion[regionindex2d])
			{
				if (area.PositionInside(pos))
				{
					claims.Add(area);
				}
			}
			return claims.ToArray();
		}

		public bool TryAccess(IPlayer player, BlockPos pos, EnumBlockAccessFlags accessFlag)
		{
			string claimant;
			EnumWorldAccessResponse resp = this.TestBlockAccess(player, new BlockSelection
			{
				Position = pos
			}, accessFlag, out claimant);
			if (resp == EnumWorldAccessResponse.Granted)
			{
				return true;
			}
			if (player != null)
			{
				string code = "noprivilege-" + ((accessFlag == EnumBlockAccessFlags.Use) ? "use" : "buildbreak") + "-" + resp.ToString().ToLowerInvariant();
				string param = claimant;
				if (claimant.StartsWithOrdinal("custommessage-"))
				{
					code = "noprivilege-buildbreak-" + claimant.Substring("custommessage-".Length);
				}
				if (this.World.Side == EnumAppSide.Server)
				{
					(player as IServerPlayer).SendIngameError(code, null, new object[] { param });
				}
				else
				{
					(this.World as ClientMain).api.TriggerIngameError(this, code, Lang.Get("ingameerror-" + code, new object[] { claimant }));
				}
				if (player != null)
				{
					ItemSlot activeHotbarSlot = player.InventoryManager.ActiveHotbarSlot;
					if (activeHotbarSlot != null)
					{
						activeHotbarSlot.MarkDirty();
					}
				}
				this.World.BlockAccessor.MarkBlockEntityDirty(pos);
				this.World.BlockAccessor.MarkBlockDirty(pos, null);
			}
			return false;
		}

		public EnumWorldAccessResponse TestAccess(IPlayer player, BlockPos pos, EnumBlockAccessFlags accessFlag)
		{
			string text;
			return this.TestBlockAccess(player, new BlockSelection
			{
				Position = pos
			}, accessFlag, out text);
		}

		public EnumWorldAccessResponse TestBlockAccess(IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType)
		{
			string text;
			return this.TestBlockAccess(player, blockSel, accessType, out text);
		}

		public EnumWorldAccessResponse TestBlockAccess(IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType, out string claimant)
		{
			LandClaim claim;
			EnumWorldAccessResponse resp = this.testBlockAccessInternal(player, blockSel, accessType, out claimant, out claim);
			if (this.World.Side == EnumAppSide.Client)
			{
				resp = ((ClientEventAPI)this.World.Api.Event).TriggerTestBlockAccess(player, blockSel, accessType, ref claimant, claim, resp);
			}
			else
			{
				resp = ((ServerEventAPI)this.World.Api.Event).TriggerTestBlockAccess(player, blockSel, accessType, ref claimant, claim, resp);
			}
			return resp;
		}

		private EnumWorldAccessResponse testBlockAccessInternal(IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType, out string claimant, out LandClaim claim)
		{
			claim = null;
			EnumWorldAccessResponse resp = this.testBlockAccess(player, accessType, out claimant);
			if (resp != EnumWorldAccessResponse.Granted)
			{
				return resp;
			}
			bool canUseClaimed = player.HasPrivilege(Privilege.useblockseverywhere) && player.WorldData.CurrentGameMode == EnumGameMode.Creative;
			bool canBreakClaimed = player.HasPrivilege(Privilege.buildblockseverywhere) && player.WorldData.CurrentGameMode == EnumGameMode.Creative;
			if (this.DebugClaimPrivileges)
			{
				this.Logger.VerboseDebug("Privdebug: type: {3}, player: {0}, canUseClaimed: {1}, canBreakClaimed: {2}", new object[]
				{
					(player != null) ? player.PlayerName : null,
					canUseClaimed,
					canBreakClaimed,
					accessType
				});
			}
			ServerMain server = this.World as ServerMain;
			if (accessType == EnumBlockAccessFlags.Use)
			{
				if (!canUseClaimed)
				{
					LandClaim landClaim;
					claim = (landClaim = this.GetBlockingLandClaimant(player, blockSel.Position, EnumBlockAccessFlags.Use));
					if (landClaim != null)
					{
						claimant = claim.LastKnownOwnerName;
						return EnumWorldAccessResponse.LandClaimed;
					}
				}
				if (server != null && !server.EventManager.TriggerCanUse(player as IServerPlayer, blockSel))
				{
					return EnumWorldAccessResponse.DeniedByMod;
				}
				return EnumWorldAccessResponse.Granted;
			}
			else
			{
				if (!canBreakClaimed)
				{
					LandClaim landClaim;
					claim = (landClaim = this.GetBlockingLandClaimant(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak));
					if (landClaim != null)
					{
						claimant = claim.LastKnownOwnerName;
						return EnumWorldAccessResponse.LandClaimed;
					}
				}
				if (server != null && !server.EventManager.TriggerCanPlaceOrBreak(player as IServerPlayer, blockSel, out claimant))
				{
					return EnumWorldAccessResponse.DeniedByMod;
				}
				return EnumWorldAccessResponse.Granted;
			}
		}

		private EnumWorldAccessResponse testBlockAccess(IPlayer player, EnumBlockAccessFlags accessType, out string claimant)
		{
			if (player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
			{
				claimant = "custommessage-inspectatormode";
				return EnumWorldAccessResponse.InSpectatorMode;
			}
			if (!player.Entity.Alive)
			{
				claimant = "custommessage-dead";
				return EnumWorldAccessResponse.PlayerDead;
			}
			if (accessType == EnumBlockAccessFlags.BuildOrBreak)
			{
				if (player.WorldData.CurrentGameMode == EnumGameMode.Guest)
				{
					claimant = "custommessage-inguestmode";
					return EnumWorldAccessResponse.InGuestMode;
				}
				if (!player.HasPrivilege(Privilege.buildblocks))
				{
					claimant = "custommessage-nobuildprivilege";
					return EnumWorldAccessResponse.NoPrivilege;
				}
				claimant = null;
				return EnumWorldAccessResponse.Granted;
			}
			else
			{
				if (!player.HasPrivilege(Privilege.useblock))
				{
					claimant = "custommessage-nouseprivilege";
					return EnumWorldAccessResponse.NoPrivilege;
				}
				claimant = null;
				return EnumWorldAccessResponse.Granted;
			}
		}

		public LandClaim GetBlockingLandClaimant(IPlayer forPlayer, BlockPos pos, EnumBlockAccessFlags accessFlag)
		{
			long regionindex2d = this.MapRegionIndex2D(pos.X / this.RegionSize, pos.Z / this.RegionSize);
			if (!this.LandClaimByRegion.ContainsKey(regionindex2d))
			{
				if (this.DebugClaimPrivileges)
				{
					this.Logger.VerboseDebug("Privdebug: No land claim in this region. Pos: {0}/{1}", new object[] { pos.X, pos.Z });
				}
				return null;
			}
			if (this.DebugClaimPrivileges && this.LandClaimByRegion[regionindex2d].Count == 0)
			{
				this.Logger.VerboseDebug("Privdebug: Land claim list in this region is empty. Pos: {0}/{1}", new object[] { pos.X, pos.Z });
			}
			if (accessFlag == EnumBlockAccessFlags.Use)
			{
				Block block = this.World.BlockAccessor.GetBlock(pos);
				IMultiblockOffset multiblockOffset = block.GetInterface<IMultiblockOffset>(this.World, pos);
				if (multiblockOffset != null)
				{
					pos = multiblockOffset.GetControlBlockPos(pos);
					block = this.World.BlockAccessor.GetBlock(pos);
				}
				IClaimTraverseable @interface = block.GetInterface<IClaimTraverseable>(this.World, pos);
				if (@interface != null && @interface.AllowTraverse())
				{
					accessFlag = EnumBlockAccessFlags.Traverse;
				}
			}
			foreach (LandClaim claim in this.LandClaimByRegion[regionindex2d])
			{
				if (this.DebugClaimPrivileges)
				{
					this.Logger.VerboseDebug("Privdebug: posinside: {0}, claim owned by: {3}, forplayer: {1}, canaccess: {2}", new object[]
					{
						claim.PositionInside(pos),
						(forPlayer != null) ? forPlayer.PlayerName : null,
						(forPlayer == null) ? EnumPlayerAccessResult.Denied : claim.TestPlayerAccess(forPlayer, accessFlag),
						claim.LastKnownOwnerName
					});
				}
				if (accessFlag == EnumBlockAccessFlags.Traverse)
				{
					if (claim.PositionInside(pos) && !claim.AllowTraverseEveryone && !claim.AllowUseEveryone && (forPlayer == null || (claim.TestPlayerAccess(forPlayer, accessFlag) == EnumPlayerAccessResult.Denied && claim.TestPlayerAccess(forPlayer, EnumBlockAccessFlags.Use) == EnumPlayerAccessResult.Denied)))
					{
						return claim;
					}
				}
				else if (claim.PositionInside(pos) && (forPlayer == null || claim.TestPlayerAccess(forPlayer, accessFlag) == EnumPlayerAccessResult.Denied) && (!claim.AllowUseEveryone || accessFlag != EnumBlockAccessFlags.Use))
				{
					return claim;
				}
			}
			if (forPlayer != null && forPlayer.Role.PrivilegeLevel >= 0)
			{
				return null;
			}
			return this.ServerLandClaim;
		}

		public void RebuildLandClaimPartitions()
		{
			if (this.RegionSize == 0)
			{
				this.Logger.Warning("Call to RebuildLandClaimPartitions, but RegionSize is 0. Wrong startup sequence? Will ignore for now.");
				return;
			}
			HashSet<long> regions = new HashSet<long>();
			this.LandClaimByRegion.Clear();
			foreach (LandClaim claim in this.All)
			{
				regions.Clear();
				foreach (Cuboidi cuboidi in claim.Areas)
				{
					int minx = cuboidi.MinX / this.RegionSize;
					int maxx = cuboidi.MaxX / this.RegionSize;
					int minz = cuboidi.MinZ / this.RegionSize;
					int maxz = cuboidi.MaxZ / this.RegionSize;
					for (int x = minx; x <= maxx; x++)
					{
						for (int z = minz; z <= maxz; z++)
						{
							regions.Add(this.MapRegionIndex2D(x, z));
						}
					}
				}
				foreach (long index2d in regions)
				{
					List<LandClaim> claims;
					if (!this.LandClaimByRegion.TryGetValue(index2d, out claims))
					{
						claims = (this.LandClaimByRegion[index2d] = new List<LandClaim>());
					}
					claims.Add(claim);
				}
			}
		}

		public long MapRegionIndex2D(int regionX, int regionZ)
		{
			return ((long)regionZ << 32) + (long)regionX;
		}

		[Obsolete("Use dimension aware versions instead, or else the BlockPos or EntityPos overloads")]
		public long ChunkIndex3D(int chunkX, int chunkY, int chunkZ)
		{
			return ((long)chunkY * (long)this.index3dMulZ + (long)chunkZ) * (long)this.index3dMulX + (long)chunkX;
		}

		public long ChunkIndex3D(int chunkX, int chunkY, int chunkZ, int dim)
		{
			return ((long)(chunkY + dim * 1024) * (long)this.index3dMulZ + (long)chunkZ) * (long)this.index3dMulX + (long)chunkX;
		}

		public long ChunkIndex3D(EntityPos pos)
		{
			ChunkPos cpos = ChunkPos.FromPosition((int)pos.X, (int)pos.Y, (int)pos.Z, pos.Dimension);
			return this.ChunkIndex3D(cpos);
		}

		public long ChunkIndex3D(ChunkPos cpos)
		{
			return ((long)(cpos.Y + cpos.Dimension * 1024) * (long)this.index3dMulZ + (long)cpos.Z) * (long)this.index3dMulX + (long)cpos.X;
		}

		public long MapChunkIndex2D(int chunkX, int chunkZ)
		{
			return (long)chunkZ * (long)this.ChunkMapSizeX + (long)chunkX;
		}

		public ChunkPos ChunkPosFromChunkIndex3D(long chunkIndex3d)
		{
			int internalCY = (int)(chunkIndex3d / ((long)this.index3dMulX * (long)this.index3dMulZ));
			return new ChunkPos((int)(chunkIndex3d % (long)this.index3dMulX), internalCY % 1024, (int)(chunkIndex3d / (long)this.index3dMulX % (long)this.index3dMulZ), internalCY / 1024);
		}

		public ChunkPos ChunkPosFromChunkIndex2D(long index2d)
		{
			return new ChunkPos((int)(index2d % (long)this.ChunkMapSizeX), 0, (int)(index2d / (long)this.ChunkMapSizeX), 0);
		}

		public int ChunkSizedIndex3D(int lX, int lY, int lZ)
		{
			return (lY * 32 + lZ) * 32 + lX;
		}

		public bool IsValidPos(BlockPos pos)
		{
			return (pos.X | pos.Y | pos.Z) >= 0 && ((pos.X < this.MapSizeX && pos.Z < this.MapSizeZ) || pos.InternalY >= 32768);
		}

		public bool IsValidPos(int posX, int posY, int posZ)
		{
			return (posX | posY | posZ) >= 0 && ((posX < this.MapSizeX && posZ < this.MapSizeZ) || posY >= 32768);
		}

		public bool IsValidChunkPos(int chunkX, int chunkY, int chunkZ)
		{
			return (chunkX | chunkY | chunkZ) >= 0 && ((chunkX < this.ChunkMapSizeX && chunkZ < this.ChunkMapSizeZ) || chunkY >= 1024);
		}

		public abstract void MarkChunkDirty(int chunkX, int chunkY, int chunkZ, bool priority = false, bool sunRelight = false, Action OnRetesselated = null, bool fireDirtyEvent = true, bool edgeOnly = false);

		public abstract void TriggerNeighbourBlockUpdate(BlockPos pos);

		public abstract void MarkBlockModified(BlockPos pos, bool doRelight = true);

		public abstract void MarkBlockDirty(BlockPos pos, Action OnRetesselated);

		public abstract void MarkBlockDirty(BlockPos pos, IPlayer skipPlayer = null);

		public abstract void MarkBlockEntityDirty(BlockPos pos);

		public bool IsMovementRestrictedPos(double posX, double posY, double posZ, int dimension)
		{
			if (posX < 0.0 || posZ < 0.0 || posX >= (double)this.MapSizeX || posZ >= (double)this.MapSizeZ)
			{
				return this.World.Config.GetString("worldEdge", null) == "blocked";
			}
			return posY >= 0.0 && posY < (double)this.MapSizeY && this.GetChunkAtPos((int)posX, (int)posY + dimension * 32768, (int)posZ) == null;
		}

		internal bool IsPosLoaded(BlockPos pos)
		{
			return this.GetChunkAtPos(pos.X, pos.Y, pos.Z) != null;
		}

		internal bool AnyLoadedChunkInMapRegion(int chunkx, int chunkz)
		{
			int cwdt = this.RegionSize / 32;
			for (int cdx = -1; cdx < cwdt + 1; cdx++)
			{
				for (int cdz = -1; cdz < cwdt + 1; cdz++)
				{
					if (this.IsValidChunkPos(chunkx + cdx, 0, chunkz + cdz) && this.GetMapChunk(chunkx + cdx, chunkz + cdz) != null)
					{
						return true;
					}
				}
			}
			return false;
		}

		public abstract IWorldChunk GetChunk(long chunkIndex3D);

		public abstract IWorldChunk GetChunkNonLocking(int chunkX, int chunkY, int chunkZ);

		public abstract IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ);

		public abstract IMapRegion GetMapRegion(int regionX, int regionZ);

		public abstract IMapChunk GetMapChunk(int chunkX, int chunkZ);

		public abstract IWorldChunk GetChunkAtPos(int posX, int posY, int posZ);

		public abstract WorldChunk GetChunk(BlockPos pos);

		public abstract void MarkDecorsDirty(BlockPos pos);

		public virtual void PrintChunkMap(Vec2i markChunkPos = null)
		{
		}

		public abstract void SendSetBlock(int blockId, int posX, int posY, int posZ);

		public abstract void SendExchangeBlock(int blockId, int posX, int posY, int posZ);

		public abstract void UpdateLighting(int oldblockid, int newblockid, BlockPos pos);

		public abstract void RemoveBlockLight(byte[] oldLightHsV, BlockPos pos);

		public abstract void UpdateLightingAfterAbsorptionChange(int oldAbsorption, int newAbsorption, BlockPos pos);

		public abstract void SendBlockUpdateBulk(IEnumerable<BlockPos> blockUpdates, bool doRelight);

		public abstract void SendBlockUpdateBulkMinimal(Dictionary<BlockPos, BlockUpdate> blockUpdates);

		public abstract void UpdateLightingBulk(Dictionary<BlockPos, BlockUpdate> blockUpdates);

		public abstract void SpawnBlockEntity(string classname, BlockPos position, ItemStack byItemStack = null);

		public abstract void SpawnBlockEntity(BlockEntity be);

		public abstract void RemoveBlockEntity(BlockPos position);

		public abstract BlockEntity GetBlockEntity(BlockPos position);

		public abstract ClimateCondition GetClimateAt(BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.WorldGenValues, double totalDays = 0.0);

		public abstract ClimateCondition GetClimateAt(BlockPos pos, ClimateCondition baseClimate, EnumGetClimateMode mode, double totalDays);

		public abstract ClimateCondition GetClimateAt(BlockPos pos, int climate);

		public abstract Vec3d GetWindSpeedAt(BlockPos pos);

		public abstract Vec3d GetWindSpeedAt(Vec3d pos);

		public abstract void DamageBlock(BlockPos pos, BlockFacing facing, float damage, IPlayer dualCallByPlayer = null);

		public abstract void SendDecorUpdateBulk(IEnumerable<BlockPos> updatedDecorPositions);

		public const int chunksize = 32;

		public int index3dMulX;

		public int chunkMapSizeY;

		public int index3dMulZ;

		public float[] BlockLightLevels;

		public byte[] BlockLightLevelsByte;

		public byte[] hueLevels;

		public byte[] satLevels;

		public float[] SunLightLevels;

		public byte[] SunLightLevelsByte;

		public int SunBrightness;

		public Dictionary<long, List<LandClaim>> LandClaimByRegion = new Dictionary<long, List<LandClaim>>();

		private LandClaim ServerLandClaim = new LandClaim
		{
			LastKnownOwnerName = "Server"
		};
	}
}
