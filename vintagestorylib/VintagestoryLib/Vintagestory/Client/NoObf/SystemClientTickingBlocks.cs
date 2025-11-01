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
	public class SystemClientTickingBlocks : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "ctb";
			}
		}

		public SystemClientTickingBlocks(ClientMain game)
			: base(game)
		{
			game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.PlayerPosDiv8, new OnPlayerPropertyChanged(this.PlayerPosDiv8Changed));
			game.eventManager.OnBlockChanged.Add(new BlockChangedDelegate(this.OnBlockChanged));
			game.api.eventapi.RegisterAsyncParticleSpawner(new ContinousParticleSpawnTaskDelegate(this.onOffThreadParticleTick));
			game.api.ChatCommands.Create("ctblocks").WithDescription("Lets to toggle on/off the updating of client ticking blocks. This can be useful when recording water falls and such").WithArgs(new ICommandArgumentParser[] { game.api.ChatCommands.Parsers.OptionalBool("freezeCtBlocks", "on") })
				.HandleWith(new OnCommandDelegate(this.OnCmdCtBlocks));
			this.searchBlockAccessor = new BlockAccessorRelaxed(game.WorldMap, game, false, false);
			this.finalScanPosition = SystemClientTickingBlocks.scanSize * SystemClientTickingBlocks.scanSize * SystemClientTickingBlocks.scanSize;
		}

		private TextCommandResult OnCmdCtBlocks(TextCommandCallingArgs args)
		{
			this.freezeCtBlocks = (bool)args[0];
			return TextCommandResult.Success("Ct block updating now " + (this.freezeCtBlocks ? "frozen" : "active"), null);
		}

		public override void OnBlockTexturesLoaded()
		{
			for (int i = 0; i < this.game.Blocks.Count; i++)
			{
				if (this.game.Blocks[i] != null)
				{
					this.game.Blocks[i].DetermineTopMiddlePos();
				}
			}
			this.game.RegisterCallback(delegate(float dt)
			{
				object obj = this.shouldStartScanningLock;
				lock (obj)
				{
					this.shouldStartScanning = true;
				}
			}, 1000);
			this.game.RegisterGameTickListener(new Action<float>(this.onTick20Secs), 20000, 123);
		}

		private void onTick20Secs(float dt)
		{
			object obj = this.shouldStartScanningLock;
			lock (obj)
			{
				this.shouldStartScanning = true;
			}
		}

		private void OnBlockChanged(BlockPos pos, Block oldBlock)
		{
			Block block = this.game.WorldMap.RelaxedBlockAccess.GetBlock(pos);
			bool isWindAffected;
			if (block.ShouldReceiveClientParticleTicks(this.game, this.game.player, pos, out isWindAffected))
			{
				int baseX = this.commitedPlayerPosDiv8.X * 8 - SystemClientTickingBlocks.scanRange;
				int baseY = this.commitedPlayerPosDiv8.Y * 8 - SystemClientTickingBlocks.scanRange;
				int baseZ = this.commitedPlayerPosDiv8.Z * 8 - SystemClientTickingBlocks.scanRange;
				int num = pos.X - baseX;
				int dy = pos.Y - baseY;
				int dz = pos.Z - baseZ;
				int deltaIndex3d = num | (dy << 10) | (dz << 20);
				object obj = this.committedTickersLock;
				lock (obj)
				{
					if (!this.committedTickers.ContainsKey(deltaIndex3d))
					{
						this.blockChangedTickers.Enqueue(new TickingBlockData
						{
							DeltaIndex3d = deltaIndex3d,
							IsWindAffected = isWindAffected,
							WindAffectedNess = (isWindAffected ? this.SearchWindAffectedNess(pos, this.game.BlockAccessor) : 0f)
						});
					}
				}
			}
			BlockSounds sounds = block.Sounds;
			AssetLocation assetLocation = ((sounds != null) ? sounds.Ambient : null);
			AssetLocation assetLocation2;
			if (oldBlock == null)
			{
				assetLocation2 = null;
			}
			else
			{
				BlockSounds sounds2 = oldBlock.Sounds;
				assetLocation2 = ((sounds2 != null) ? sounds2.Ambient : null);
			}
			if (assetLocation != assetLocation2)
			{
				object obj = this.shouldStartScanningLock;
				lock (obj)
				{
					this.shouldStartScanning = true;
				}
			}
		}

		private bool onOffThreadParticleTick(float dt, IAsyncParticleManager manager)
		{
			bool updateWindAffectedness = false;
			this.offthreadAccum += dt;
			if (this.offthreadAccum > 4f)
			{
				this.offthreadAccum = 0f;
				updateWindAffectedness = true;
			}
			object obj = this.committedTickersLock;
			Dictionary<int, TickerMetaData> tickers;
			lock (obj)
			{
				tickers = this.committedTickers;
				while (this.blockChangedTickers.Count > 0)
				{
					TickingBlockData data = this.blockChangedTickers.Dequeue();
					tickers[data.DeltaIndex3d] = new TickerMetaData
					{
						TickingSinceMs = this.game.ElapsedMilliseconds,
						IsWindAffected = data.IsWindAffected,
						WindAffectedNess = data.WindAffectedNess
					};
				}
			}
			ICachingBlockAccessor icba = manager.BlockAccess as ICachingBlockAccessor;
			if (icba != null)
			{
				icba.Begin();
			}
			int baseX = this.commitedPlayerPosDiv8.X * 8 - SystemClientTickingBlocks.scanRange;
			int baseY = this.commitedPlayerPosDiv8.Y * 8 - SystemClientTickingBlocks.scanRange;
			int baseZ = this.commitedPlayerPosDiv8.Z * 8 - SystemClientTickingBlocks.scanRange;
			long ellapseMs = this.game.ElapsedMilliseconds;
			foreach (KeyValuePair<int, TickerMetaData> val in tickers)
			{
				BlockPos pos = new BlockPos(baseX + (val.Key & 1023), baseY + ((val.Key >> 10) & 1023), baseZ + ((val.Key >> 20) & 1023));
				if (updateWindAffectedness && val.Value.IsWindAffected)
				{
					val.Value.WindAffectedNess = this.SearchWindAffectedNess(pos, manager.BlockAccess);
				}
				Block block = manager.BlockAccess.GetBlock(pos);
				if (block != null)
				{
					block.OnAsyncClientParticleTick(manager, pos, val.Value.WindAffectedNess, (float)(ellapseMs - val.Value.TickingSinceMs) / 1000f);
				}
			}
			return true;
		}

		private void PlayerPosDiv8Changed(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
		{
			object obj = this.shouldStartScanningLock;
			lock (obj)
			{
				this.shouldStartScanning = true;
				this.currentPlayerPosDiv8 = newValues.PlayerPosDiv8.ToVec3i();
			}
		}

		public void CommitScan()
		{
			if (this.freezeCtBlocks)
			{
				this.currentTickers.Clear();
				return;
			}
			object obj = this.shouldStartScanningLock;
			List<AmbientSound> sounds;
			lock (obj)
			{
				long elapsedMs = this.game.ElapsedMilliseconds;
				Dictionary<int, TickerMetaData> newCommittedTickers = new Dictionary<int, TickerMetaData>();
				int diffX = (this.currentPlayerPosDiv8.X - this.commitedPlayerPosDiv8.X) * 8;
				int diffY = (this.currentPlayerPosDiv8.Y - this.commitedPlayerPosDiv8.Y) * 8;
				int diffZ = (this.currentPlayerPosDiv8.Z - this.commitedPlayerPosDiv8.Z) * 8;
				foreach (TickingBlockData val in this.currentTickers)
				{
					int num = val.DeltaIndex3d & 1023;
					int dy = (val.DeltaIndex3d >> 10) & 1023;
					int dz = (val.DeltaIndex3d >> 20) & 1023;
					int index = (num + diffX) | (dy + diffY << 10) | (dz + diffZ << 20);
					long thiselapsedms = elapsedMs;
					TickerMetaData prevData;
					if (this.committedTickers.TryGetValue(index, out prevData))
					{
						thiselapsedms = prevData.TickingSinceMs;
					}
					newCommittedTickers[val.DeltaIndex3d] = new TickerMetaData
					{
						TickingSinceMs = thiselapsedms,
						IsWindAffected = val.IsWindAffected,
						WindAffectedNess = val.WindAffectedNess
					};
				}
				this.commitedPlayerPosDiv8 = this.currentPlayerPosDiv8;
				this.currentTickers.Clear();
				object obj2 = this.committedTickersLock;
				lock (obj2)
				{
					this.committedTickers = newCommittedTickers;
				}
				sounds = this.MergeEqualAmbientSounds();
			}
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.OnAmbientSoundsScanComplete(sounds);
		}

		private List<AmbientSound> MergeEqualAmbientSounds()
		{
			Dictionary<AssetLocation, List<AmbientSound>> mergeddict = new Dictionary<AssetLocation, List<AmbientSound>>();
			foreach (Dictionary<AssetLocation, AmbientSound> sectionsounds in this.currentAmbientSoundsBySection.Values)
			{
				foreach (AssetLocation assetloc in sectionsounds.Keys)
				{
					bool added = false;
					List<AmbientSound> sounds;
					if (mergeddict.TryGetValue(assetloc, out sounds))
					{
						for (int i = 0; i < sounds.Count; i++)
						{
							AmbientSound sound = sounds[i];
							if (sound.DistanceTo(sectionsounds[assetloc]) < sound.MaxDistanceMerge)
							{
								sound.BoundingBoxes.AddRange(sectionsounds[assetloc].BoundingBoxes);
								sound.QuantityNearbyBlocks += sectionsounds[assetloc].QuantityNearbyBlocks;
								added = true;
								break;
							}
						}
						if (!added)
						{
							sounds.Add(sectionsounds[assetloc]);
						}
					}
					else
					{
						mergeddict[assetloc] = new List<AmbientSound> { sectionsounds[assetloc] };
					}
				}
			}
			List<AmbientSound> merged = new List<AmbientSound>();
			foreach (KeyValuePair<AssetLocation, List<AmbientSound>> val in mergeddict)
			{
				merged.AddRange(val.Value);
			}
			return merged;
		}

		public override int SeperateThreadTickIntervalMs()
		{
			return 5;
		}

		public override void OnSeperateThreadGameTick(float dt)
		{
			if (this.shouldStartScanning && this.scanState != BlockScanState.Done)
			{
				this.scanState = BlockScanState.Scanning;
				this.scanPosition = 0;
				this.currentLeavesCount = 0;
				object obj = this.shouldStartScanningLock;
				lock (obj)
				{
					this.shouldStartScanning = false;
				}
				this.currentTickers.Clear();
				this.currentAmbientSoundsBySection.Clear();
			}
			if (this.scanState != BlockScanState.Scanning)
			{
				return;
			}
			int baseX = this.currentPlayerPosDiv8.X * 8 - SystemClientTickingBlocks.scanRange;
			int baseY = this.currentPlayerPosDiv8.Y * 8 - SystemClientTickingBlocks.scanRange;
			int baseZ = this.currentPlayerPosDiv8.Z * 8 - SystemClientTickingBlocks.scanRange;
			IWorldChunk chunk = null;
			int cxBefore = 0;
			int cyBefore = -1;
			int czBefore = -912312;
			BlockPos tmpPos = new BlockPos();
			IList<Block> blocks = this.game.Blocks;
			for (int i = 0; i < 11000; i++)
			{
				int dx = this.scanPosition % SystemClientTickingBlocks.scanSize;
				int dy = this.scanPosition / (SystemClientTickingBlocks.scanSize * SystemClientTickingBlocks.scanSize);
				int dz = this.scanPosition / SystemClientTickingBlocks.scanSize % SystemClientTickingBlocks.scanSize;
				tmpPos.Set(baseX + dx, baseY + dy, baseZ + dz);
				if (!this.game.WorldMap.IsValidPos(tmpPos))
				{
					this.scanPosition++;
				}
				else
				{
					int cx = tmpPos.X / 32;
					int cy = tmpPos.Y / 32;
					int cz = tmpPos.Z / 32;
					if (cx != cxBefore || cy != cyBefore || cz != czBefore)
					{
						cxBefore = cx;
						cyBefore = cy;
						czBefore = cz;
						chunk = this.game.WorldMap.GetChunk(cx, cy, cz);
						if (chunk != null)
						{
							chunk.Unpack();
						}
					}
					if (chunk == null)
					{
						this.scanPosition++;
					}
					else
					{
						int lx = tmpPos.X % 32;
						int ly = tmpPos.Y % 32;
						int lz = tmpPos.Z % 32;
						Block block = blocks[chunk.Data[(ly * 32 + lz) * 32 + lx]];
						AssetLocation assetLocation;
						if (block == null)
						{
							assetLocation = null;
						}
						else
						{
							BlockSounds sounds = block.Sounds;
							assetLocation = ((sounds != null) ? sounds.Ambient : null);
						}
						float str;
						if (assetLocation != null && (str = block.GetAmbientSoundStrength(this.game, tmpPos)) > 0f)
						{
							Vec3i sectionPos = new Vec3i(tmpPos.X / SystemClientTickingBlocks.scanSectionSize, tmpPos.Y / SystemClientTickingBlocks.scanSectionSize, tmpPos.Z / SystemClientTickingBlocks.scanSectionSize);
							Dictionary<AssetLocation, AmbientSound> ambSoundsofSection;
							if (!this.currentAmbientSoundsBySection.TryGetValue(sectionPos, out ambSoundsofSection))
							{
								ambSoundsofSection = (this.currentAmbientSoundsBySection[sectionPos] = new Dictionary<AssetLocation, AmbientSound>());
							}
							AmbientSound ambSound;
							ambSoundsofSection.TryGetValue(block.Sounds.Ambient, out ambSound);
							if (ambSound == null)
							{
								ambSound = new AmbientSound
								{
									AssetLoc = block.Sounds.Ambient,
									Ratio = block.Sounds.AmbientBlockCount,
									VolumeMul = str,
									SoundType = block.Sounds.AmbientSoundType,
									SectionPos = sectionPos,
									MaxDistanceMerge = (double)block.Sounds.AmbientMaxDistanceMerge
								};
								ambSound.BoundingBoxes.Add(new Cuboidi(tmpPos.X, tmpPos.Y, tmpPos.Z, tmpPos.X + 1, tmpPos.Y + 1, tmpPos.Z + 1));
								ambSoundsofSection[block.Sounds.Ambient] = ambSound;
							}
							else
							{
								ambSound.VolumeMul = str;
							}
							ambSound.QuantityNearbyBlocks++;
							Cuboidi box = ambSound.BoundingBoxes[0];
							box.GrowToInclude(tmpPos);
							if (tmpPos.X == box.X2)
							{
								box.X2++;
							}
							if (tmpPos.Y == box.Y2)
							{
								box.Y2++;
							}
							if (tmpPos.Z == box.Z2)
							{
								box.Z2++;
							}
						}
						if (block.BlockMaterial == EnumBlockMaterial.Leaves)
						{
							this.currentLeavesCount++;
						}
						bool isWindAffected;
						if (block.ShouldReceiveClientParticleTicks(this.game, this.game.player, tmpPos, out isWindAffected))
						{
							this.currentTickers.Add(new TickingBlockData
							{
								DeltaIndex3d = (dx | (dy << 10) | (dz << 20)),
								IsWindAffected = isWindAffected,
								WindAffectedNess = (isWindAffected ? this.SearchWindAffectedNess(tmpPos, this.searchBlockAccessor) : 0f)
							});
						}
						this.scanPosition++;
						if (this.scanPosition >= this.finalScanPosition)
						{
							if (this.scanState == BlockScanState.Scanning)
							{
								this.scanState = BlockScanState.Done;
								this.game.EnqueueMainThreadTask(delegate
								{
									this.CommitScan();
									GlobalConstants.CurrentNearbyRelLeavesCountClient = (float)this.currentLeavesCount / (float)this.finalScanPosition;
									this.scanState = BlockScanState.Idle;
								}, "commitscan");
								return;
							}
							break;
						}
					}
				}
			}
		}

		private float SearchWindAffectedNess(BlockPos pos, IBlockAccessor blockAccess)
		{
			return Math.Max(0f, 1f - (float)blockAccess.GetDistanceToRainFall(pos, 4, 1) / 5f);
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		private Vec3i commitedPlayerPosDiv8 = new Vec3i();

		private Queue<TickingBlockData> blockChangedTickers = new Queue<TickingBlockData>();

		private Dictionary<int, TickerMetaData> committedTickers = new Dictionary<int, TickerMetaData>();

		private object committedTickersLock = new object();

		private List<TickingBlockData> currentTickers = new List<TickingBlockData>();

		private Dictionary<Vec3i, Dictionary<AssetLocation, AmbientSound>> currentAmbientSoundsBySection = new Dictionary<Vec3i, Dictionary<AssetLocation, AmbientSound>>();

		private Vec3i currentPlayerPosDiv8 = new Vec3i();

		private bool shouldStartScanning;

		private object shouldStartScanningLock = new object();

		private BlockScanState scanState;

		private int scanPosition;

		private int finalScanPosition;

		private static int scanRange = 37;

		private static int scanSize = 2 * SystemClientTickingBlocks.scanRange;

		private static int scanSectionSize = SystemClientTickingBlocks.scanSize / 8;

		private IBlockAccessor searchBlockAccessor;

		private bool freezeCtBlocks;

		private float offthreadAccum;

		private int currentLeavesCount;
	}
}
