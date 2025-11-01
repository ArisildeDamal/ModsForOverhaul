using System;
using System.Collections.Generic;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Common
{
	public class ChunkIlluminator
	{
		public bool IsValidPos(int x, int y, int z)
		{
			return x >= 0 && y >= 0 && z >= 0 && x < this.mapsizex && y % 32768 <= this.mapsizey && z <= this.mapsizez;
		}

		public ChunkIlluminator(IChunkProvider chunkProvider, IBlockAccessor readBlockAccess, int chunkSize)
		{
			this.readBlockAccess = readBlockAccess;
			this.chunkProvider = chunkProvider;
			this.chunkSize = chunkSize;
			this.YPlus = chunkSize * chunkSize;
			this.ZPlus = chunkSize;
			this.currentVisited = new int[250047];
		}

		public void InitForWorld(IList<Block> blockTypes, ushort defaultSunLight, int mapsizex, int mapsizey, int mapsizez)
		{
			this.blockTypes = blockTypes;
			this.defaultSunLight = defaultSunLight;
			this.mapsizex = mapsizex;
			this.mapsizey = mapsizey;
			this.mapsizez = mapsizez;
		}

		public void FullRelight(BlockPos minPos, BlockPos maxPos)
		{
			int chunkSize = this.chunkSize;
			Dictionary<Vec3i, IWorldChunk> chunks = new Dictionary<Vec3i, IWorldChunk>();
			int minx = GameMath.Clamp(Math.Min(minPos.X, maxPos.X) - chunkSize, 0, this.mapsizex - 1);
			int miny = GameMath.Clamp(Math.Min(minPos.Y, maxPos.Y) - chunkSize, 0, this.mapsizey - 1);
			int minz = GameMath.Clamp(Math.Min(minPos.Z, maxPos.Z) - chunkSize, 0, this.mapsizez - 1);
			int maxx = GameMath.Clamp(Math.Max(minPos.X, maxPos.X) + chunkSize, 0, this.mapsizex - 1);
			int maxy = GameMath.Clamp(Math.Max(minPos.Y, maxPos.Y) + chunkSize, 0, this.mapsizey - 1);
			int num = GameMath.Clamp(Math.Max(minPos.Z, maxPos.Z) + chunkSize, 0, this.mapsizez - 1);
			int mincx = minx / chunkSize;
			int mincy = miny / chunkSize;
			int mincz = minz / chunkSize;
			int maxcx = maxx / chunkSize;
			int maxcy = maxy / chunkSize;
			int maxcz = num / chunkSize;
			int dimensionOffsetY = minPos.dimension * 1024;
			for (int cx = mincx; cx <= maxcx; cx++)
			{
				for (int cy = mincy; cy <= maxcy; cy++)
				{
					for (int cz = mincz; cz <= maxcz; cz++)
					{
						IWorldChunk chunk = this.chunkProvider.GetChunk(cx, cy + dimensionOffsetY, cz);
						if (chunk != null)
						{
							chunk.Unpack();
							chunks[new Vec3i(cx, cy, cz)] = chunk;
						}
					}
				}
			}
			foreach (IWorldChunk chunk2 in chunks.Values)
			{
				if (chunk2 != null)
				{
					chunk2.Lighting.ClearLight();
				}
			}
			IWorldChunk[] chunkColumn = new IWorldChunk[this.mapsizey / chunkSize];
			for (int cx2 = mincx; cx2 <= maxcx; cx2++)
			{
				for (int cz2 = mincz; cz2 <= maxcz; cz2++)
				{
					bool anyNullChunks = false;
					for (int cy2 = 0; cy2 < chunkColumn.Length; cy2++)
					{
						IWorldChunk chunk3 = this.chunkProvider.GetChunk(cx2, cy2 + dimensionOffsetY, cz2);
						if (chunk3 == null)
						{
							anyNullChunks = true;
						}
						chunkColumn[cy2] = chunk3;
					}
					if (!anyNullChunks)
					{
						this.Sunlight(chunkColumn, cx2, chunkColumn.Length - 1, cz2, minPos.dimension);
						this.SunlightFlood(chunkColumn, cx2, chunkColumn.Length - 1, cz2);
						this.SunLightFloodNeighbourChunks(chunkColumn, cx2, chunkColumn.Length - 1, cz2, minPos.dimension);
					}
				}
			}
			Dictionary<BlockPos, Block> lightSources = new Dictionary<BlockPos, Block>();
			foreach (KeyValuePair<Vec3i, IWorldChunk> val in chunks)
			{
				Vec3i chunkPos = val.Key;
				IWorldChunk chunk4 = val.Value;
				if (chunk4 != null)
				{
					int posX = chunkPos.X * chunkSize;
					int posY = chunkPos.Y * chunkSize;
					int posZ = chunkPos.Z * chunkSize;
					foreach (int index3d in chunk4.LightPositions)
					{
						int lposy = chunkPos.Y * chunkSize + index3d / (chunkSize * chunkSize);
						int lposz = chunkPos.Z * chunkSize + index3d / chunkSize % chunkSize;
						int lposx = chunkPos.X * chunkSize + index3d % chunkSize;
						lightSources[new BlockPos(posX + lposx, posY + lposy, posZ + lposz, minPos.dimension)] = this.blockTypes[chunk4.Data[index3d]];
					}
				}
			}
			foreach (KeyValuePair<BlockPos, Block> val2 in lightSources)
			{
				byte[] lightHsv = val2.Value.GetLightHsv(this.readBlockAccess, val2.Key, null);
				this.PlaceBlockLight(lightHsv, val2.Key.X, val2.Key.InternalY, val2.Key.Z);
			}
		}

		public void Sunlight(IWorldChunk[] chunks, int chunkX, int chunkY, int chunkZ, int dim)
		{
			this.tmpPosDimensionAware.dimension = dim;
			int chunkSize = this.chunkSize;
			if (chunkY != chunks.Length - 1)
			{
				chunks[chunkY + 1].Unpack();
			}
			for (int cy = chunkY; cy >= 0; cy--)
			{
				chunks[cy].Unpack();
			}
			int baseX = chunkX * chunkSize;
			int baseZ = chunkZ * chunkSize;
			for (int lx = 0; lx < chunkSize; lx++)
			{
				for (int lz = 0; lz < chunkSize; lz++)
				{
					int sunLight = (int)this.defaultSunLight;
					if (chunkY != chunks.Length - 1)
					{
						sunLight = chunks[chunkY + 1].Lighting.GetSunlight(lz * chunkSize + lx);
					}
					for (int cy2 = chunkY; cy2 >= 0; cy2--)
					{
						int index3d = ((chunkSize - 1) * chunkSize + lz) * chunkSize + lx;
						IWorldChunk chunk = chunks[cy2];
						IChunkLight chunklighting = chunks[cy2].Lighting;
						this.tmpPosDimensionAware.Set(baseX + lx, cy2 * chunkSize + chunkSize - 1, baseZ + lz);
						for (int ly = chunkSize - 1; ly >= 0; ly--)
						{
							int absorption = chunk.GetLightAbsorptionAt(index3d, this.tmpPosDimensionAware, this.blockTypes);
							chunklighting.SetSunlight(index3d, sunLight);
							index3d -= this.YPlus;
							if (absorption > sunLight)
							{
								cy2 = -1;
								break;
							}
							sunLight -= (int)((ushort)absorption);
							this.tmpPosDimensionAware.Y--;
						}
					}
				}
			}
		}

		public void SunlightFlood(IWorldChunk[] chunks, int chunkX, int chunkY, int chunkZ)
		{
			int chunkSize = this.chunkSize;
			Stack<BlockPos> stack = new Stack<BlockPos>();
			int baseX = chunkX * chunkSize;
			int baseZ = chunkZ * chunkSize;
			for (int cy = chunkY; cy >= 0; cy--)
			{
				IWorldChunk chunk = chunks[cy];
				chunk.Unpack();
				IChunkBlocks data = chunk.Data;
				IChunkLight chunklighting = chunk.Lighting;
				for (int lx = 0; lx < chunkSize; lx++)
				{
					this.tmpPosDimensionAware.Set(baseX + lx, cy * chunkSize + chunkSize, baseZ);
					for (int lz = 0; lz < chunkSize; lz++)
					{
						int index3d = (chunkSize * chunkSize + lz) * chunkSize + lx;
						this.tmpPosDimensionAware.Z = baseZ + lz;
						for (int ly = chunkSize - 1; ly >= 0; ly--)
						{
							index3d -= this.YPlus;
							this.tmpPosDimensionAware.Y--;
							int spreadLight = chunklighting.GetSunlight(index3d) - 1;
							if (spreadLight <= 0)
							{
								break;
							}
							int absorption = chunk.GetLightAbsorptionAt(index3d, this.tmpPosDimensionAware, this.blockTypes);
							spreadLight -= absorption;
							if (spreadLight > 0 && ((lx < chunkSize - 1 && chunklighting.GetSunlight(index3d + this.XPlus) < spreadLight) || (lz < chunkSize - 1 && chunklighting.GetSunlight(index3d + this.ZPlus) < spreadLight) || (lx > 0 && chunklighting.GetSunlight(index3d - this.XPlus) < spreadLight) || (lz > 0 && chunklighting.GetSunlight(index3d - this.ZPlus) < spreadLight)))
							{
								stack.Push(new BlockPos(baseX + lx, cy * chunkSize + ly, baseZ + lz, this.tmpPosDimensionAware.dimension));
								if (stack.Count > 50)
								{
									this.SpreadSunLightInColumn(stack, chunks);
								}
							}
						}
					}
				}
			}
			this.SpreadSunLightInColumn(stack, chunks);
		}

		public byte SunLightFloodNeighbourChunks(IWorldChunk[] curChunks, int chunkX, int chunkY, int chunkZ, int dimension)
		{
			this.tmpPosDimensionAware.dimension = dimension;
			int chunkSize = this.chunkSize;
			byte spreadFaces = 0;
			Stack<BlockPos> curStack = new Stack<BlockPos>();
			Stack<BlockPos> neibStack = new Stack<BlockPos>();
			int[] mapping = new int[2];
			int[] lpos = new int[3];
			IWorldChunk[] neibChunks = new IWorldChunk[curChunks.Length];
			int baseX = chunkX * chunkSize;
			int baseZ = chunkZ * chunkSize;
			foreach (BlockFacing facing in BlockFacing.HORIZONTALS)
			{
				bool neibLoaded = true;
				int facingNormaliX = facing.Normali.X;
				int facingNormaliZ = facing.Normali.Z;
				int cy = 0;
				while (cy < curChunks.Length)
				{
					neibChunks[cy] = this.chunkProvider.GetChunk(chunkX + facingNormaliX, cy + dimension * 1024, chunkZ + facingNormaliZ);
					if (neibChunks[cy] == null)
					{
						neibLoaded = false;
						if (cy != 0)
						{
							this.chunkProvider.Logger.Error("not full column loaded @{0} {1} {2}, lighting error will probably happen", new object[] { chunkX, cy, chunkZ });
							break;
						}
						break;
					}
					else
					{
						neibChunks[cy].Unpack();
						curChunks[cy].Unpack();
						cy++;
					}
				}
				if (neibLoaded)
				{
					int facingNormaliY = facing.Normali.Y;
					lpos[0] = (chunkSize - 1) * Math.Max(0, facingNormaliX);
					lpos[1] = (chunkSize - 1) * Math.Max(0, facingNormaliY);
					lpos[2] = (chunkSize - 1) * Math.Max(0, facingNormaliZ);
					int neibbaseX = (chunkX + facingNormaliX) * chunkSize;
					int i = 0;
					if (facingNormaliX == 0)
					{
						mapping[i++] = 0;
					}
					if (facingNormaliY == 0)
					{
						mapping[i++] = 1;
					}
					if (facingNormaliZ == 0)
					{
						mapping[i++] = 2;
					}
					for (int cy2 = chunkY; cy2 >= 0; cy2--)
					{
						IWorldChunk neibChunk = neibChunks[cy2];
						IWorldChunk curChunk = curChunks[cy2];
						IChunkLight neibChunklighting = neibChunk.Lighting;
						IChunkLight curChunklighting = curChunk.Lighting;
						for (int a = chunkSize - 1; a >= 0; a--)
						{
							lpos[mapping[0]] = a;
							for (int b = chunkSize - 1; b >= 0; b--)
							{
								lpos[mapping[1]] = b;
								int ownIndex3d = (lpos[1] * chunkSize + lpos[2]) * chunkSize + lpos[0];
								int neibX = GameMath.Mod(lpos[0] + facingNormaliX, chunkSize);
								int neibZ = GameMath.Mod(lpos[2] + facingNormaliZ, chunkSize);
								int neibIndex3d = (lpos[1] * chunkSize + neibZ) * chunkSize + neibX;
								int neibLight = neibChunklighting.GetSunlight(neibIndex3d) - 1;
								int ownLight = curChunklighting.GetSunlight(ownIndex3d) - 1;
								this.tmpPosDimensionAware.Set(baseX + lpos[0], cy2 * chunkSize + lpos[1], baseZ + lpos[2]);
								int ownabsorption = curChunk.GetLightAbsorptionAt(ownIndex3d, this.tmpPosDimensionAware, this.blockTypes);
								this.tmpPosDimensionAware.Set(neibbaseX + neibX, cy2 * chunkSize + lpos[1], neibbaseX + neibZ);
								int neibabsorption = neibChunk.GetLightAbsorptionAt(neibIndex3d, this.tmpPosDimensionAware, this.blockTypes);
								int spreadNeibLight = neibLight - neibabsorption;
								int spreadOwnLight = ownLight - ownabsorption;
								if (spreadOwnLight > neibLight)
								{
									neibChunklighting.SetSunlight(neibIndex3d, spreadOwnLight);
									neibStack.Push(new BlockPos(baseX + neibX, cy2 * chunkSize + lpos[1], baseZ + neibZ, dimension));
									spreadFaces |= facing.Flag;
								}
								else if (spreadNeibLight > ownLight)
								{
									curChunklighting.SetSunlight(ownIndex3d, spreadNeibLight);
									curStack.Push(new BlockPos(baseX + lpos[0], cy2 * chunkSize + lpos[1], baseZ + lpos[2], dimension));
								}
							}
						}
					}
					if (neibStack.Count > 0)
					{
						this.SpreadSunLightInColumn(neibStack, neibChunks);
						for (int j = 0; j < neibChunks.Length; j++)
						{
							neibChunks[j].MarkModified();
						}
					}
					if (curStack.Count > 0)
					{
						this.SpreadSunLightInColumn(curStack, curChunks);
					}
				}
			}
			return spreadFaces;
		}

		public void SpreadSunLightInColumn(Stack<BlockPos> stack, IWorldChunk[] chunks)
		{
			int chunkSize = this.chunkSize;
			while (stack.Count > 0)
			{
				BlockPos pos = stack.Pop();
				int cx = pos.X / chunkSize;
				int cy = pos.Y / chunkSize;
				int cz = pos.Z / chunkSize;
				int baseLx = pos.X % chunkSize;
				int num = pos.Y % chunkSize;
				int baseLz = pos.Z % chunkSize;
				int index3d = (num * chunkSize + baseLz) * chunkSize + baseLx;
				IWorldChunk chunk = chunks[cy];
				int absorption = chunk.GetLightAbsorptionAt(index3d, pos, this.blockTypes);
				int spreadLight = chunk.Lighting.GetSunlight(index3d) - absorption - 1;
				if (spreadLight > 0)
				{
					int oldcy = cy;
					for (int i = 0; i < 6; i++)
					{
						Vec3i facingVector = BlockFacing.ALLNORMALI[i];
						int posY = pos.Y + facingVector.Y;
						int nlx = baseLx + facingVector.X;
						int nlz = baseLz + facingVector.Z;
						if (nlx >= 0 && posY >= 0 && nlz >= 0 && nlx < chunkSize && posY < this.mapsizey && nlz < chunkSize)
						{
							cy = posY / chunkSize;
							if (cy != oldcy)
							{
								chunk = chunks[cy];
								chunk.Unpack();
								oldcy = cy;
							}
							index3d = (posY % chunkSize * chunkSize + nlz) * chunkSize + nlx;
							if (chunk.Lighting.GetSunlight(index3d) < spreadLight)
							{
								chunk.Lighting.SetSunlight(index3d, spreadLight);
								stack.Push(new BlockPos(cx * chunkSize + nlx, posY, cz * chunkSize + nlz, pos.dimension));
							}
						}
					}
				}
			}
		}

		private int SunLightLevelAt(int posX, int posY, int posZ, bool substractAbsorb = false)
		{
			int chunkSize = this.chunkSize;
			IWorldChunk chunk = this.chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize, true);
			if (chunk == null)
			{
				return (int)this.defaultSunLight;
			}
			int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
			return chunk.Lighting.GetSunlight(index3d) - (substractAbsorb ? chunk.GetLightAbsorptionAt(index3d, this.tmpPos.Set(posX, posY, posZ), this.blockTypes) : 0);
		}

		private void SetSunLightLevelAt(int posX, int posY, int posZ, int level)
		{
			int chunkSize = this.chunkSize;
			IWorldChunk chunk = this.chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize, true);
			if (chunk == null)
			{
				return;
			}
			int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
			chunk.Lighting.SetSunlight(index3d, level);
		}

		private void ClearSunLightLevelAt(int posX, int posY, int posZ)
		{
			this.SetSunLightLevelAt(posX, posY, posZ, 0);
		}

		private int GetSunLightFromNeighbour(int posX, int posY, int posZ, bool directlyIlluminated)
		{
			int dimensionFloor = posY / 32768 * 32768;
			int sunLightFromNeighbours = 0;
			for (int i = 0; i < 6; i++)
			{
				Vec3i face = BlockFacing.ALLNORMALI[i];
				int nposX = posX + face.X;
				int nposY = posY + face.Y;
				int nposZ = posZ + face.Z;
				if ((nposX | nposZ) >= 0 && nposY >= dimensionFloor && nposX < this.mapsizex && nposY < this.mapsizey + dimensionFloor && nposZ < this.mapsizez)
				{
					IWorldChunk chunk = this.chunkProvider.GetUnpackedChunkFast(nposX / this.chunkSize, nposY / this.chunkSize, nposZ / this.chunkSize, false);
					if (chunk != null)
					{
						int index3d = (nposY % this.chunkSize * this.chunkSize + nposZ % this.chunkSize) * this.chunkSize + nposX % this.chunkSize;
						int absorb = chunk.GetLightAbsorptionAt(index3d, this.tmpPos.Set(nposX, nposY, nposZ), this.blockTypes);
						int neibSunlight = chunk.Lighting.GetSunlight(index3d) - absorb - ((i == 4 && directlyIlluminated) ? 0 : 1);
						sunLightFromNeighbours = Math.Max(sunLightFromNeighbours, neibSunlight);
					}
				}
			}
			return sunLightFromNeighbours;
		}

		public FastSetOfLongs UpdateSunLight(int posX, int posY, int posZ, int oldAbsorb, int newAbsorb)
		{
			FastSetOfLongs touchedChunks = new FastSetOfLongs();
			if (newAbsorb == oldAbsorb)
			{
				return touchedChunks;
			}
			int dimensionFloor = posY / 32768 * 32768;
			if (posX < 0 || posY < 0 || posZ < 0 || posX >= this.mapsizex || posY >= dimensionFloor + this.mapsizey || posZ >= this.mapsizez)
			{
				return touchedChunks;
			}
			QueueOfInt needToSpreadFromPositions = new QueueOfInt();
			bool directlyIlluminated = this.IsDirectlyIlluminated(posX, posY, posZ);
			BlockPos centerPos = new BlockPos(posX, posY, posZ);
			if (newAbsorb > oldAbsorb)
			{
				IWorldChunk chunk = this.chunkProvider.GetUnpackedChunkFast(posX / this.chunkSize, posY / this.chunkSize, posZ / this.chunkSize, true);
				if (chunk == null)
				{
					return touchedChunks;
				}
				int index3d = (posY % this.chunkSize * this.chunkSize + posZ % this.chunkSize) * this.chunkSize + posX % this.chunkSize;
				int oldLightLevel = chunk.Lighting.GetSunlight(index3d);
				chunk.Lighting.SetSunlight_Buffered(index3d, 0);
				QueueOfInt unhandledPositions = new QueueOfInt();
				for (int i = 0; i < 6; i++)
				{
					Vec3i face = BlockFacing.ALLNORMALI[i];
					int neibPosX = posX + face.X;
					int neibPosY = posY + face.Y;
					int neibPosZ = posZ + face.Z;
					if (neibPosX >= 0 && neibPosY >= dimensionFloor && neibPosZ >= 0 && neibPosX < this.mapsizex && neibPosY < dimensionFloor + this.mapsizey && neibPosZ < this.mapsizez)
					{
						int neibPosW = oldLightLevel - oldAbsorb - 1 + ((directlyIlluminated && i == 5) ? 1 : 0);
						int neibLightNow = this.SunLightLevelAt(neibPosX, neibPosY, neibPosZ, false);
						if (neibPosW >= neibLightNow)
						{
							unhandledPositions.Enqueue(face.X, face.Y, face.Z, neibPosW + (TileSideEnum.GetOpposite(i) + 1 << 5));
						}
					}
				}
				this.ClearSunlightAt(unhandledPositions, centerPos, directlyIlluminated, needToSpreadFromPositions, touchedChunks);
			}
			needToSpreadFromPositions.Enqueue(0, 0, 0, this.GetSunLightFromNeighbour(posX, posY, posZ, directlyIlluminated));
			this.SpreadSunlightAt(needToSpreadFromPositions, centerPos, directlyIlluminated, touchedChunks);
			if (posY > 0)
			{
				this.SetSunLightLevelAt(posX, posY - 1, posZ, this.GetSunLightFromNeighbour(posX, posY - 1, posZ, directlyIlluminated));
			}
			if (newAbsorb > oldAbsorb)
			{
				for (int j = 0; j < 6; j++)
				{
					Vec3i face2 = BlockFacing.ALLNORMALI[j];
					int x = posX + face2.X;
					int y = posY + face2.Y;
					int z = posZ + face2.Z;
					if (this.IsValidPos(x, y, z))
					{
						int neiblight = this.GetSunLightFromNeighbour(x, y, z, this.IsDirectlyIlluminated(x, y, z));
						if (neiblight > this.SunLightLevelAt(x, y, z, false))
						{
							this.SetSunLightLevelAt(x, y, z, neiblight);
						}
					}
				}
			}
			return touchedChunks;
		}

		public bool IsDirectlyIlluminated(int posX, int posY, int posZ)
		{
			int chunkSize = this.chunkSize;
			int totalAbsorption = 0;
			int ownSunLightLevel = this.SunLightLevelAt(posX, posY, posZ, false);
			while (posY < this.mapsizey)
			{
				posY++;
				IWorldChunk chunk = this.chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize, false);
				if (chunk == null)
				{
					break;
				}
				int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
				int sunlightLevel = chunk.Lighting.GetSunlight(index3d);
				this.tmpDiPos.Set(posX, posY, posZ);
				totalAbsorption += chunk.GetLightAbsorptionAt(index3d, this.tmpDiPos, this.blockTypes);
				if ((int)this.defaultSunLight - totalAbsorption < ownSunLightLevel)
				{
					return false;
				}
				if (sunlightLevel == (int)this.defaultSunLight)
				{
					return true;
				}
				if (ownSunLightLevel > sunlightLevel)
				{
					return false;
				}
			}
			return (int)this.defaultSunLight - totalAbsorption == ownSunLightLevel;
		}

		public void SpreadSunlightAt(QueueOfInt unhandledPositions, BlockPos centerPos, bool isDirectlyIlluminated, FastSetOfLongs touchedChunks)
		{
			int chunkSize = this.chunkSize;
			this.tmpPos.dimension = centerPos.dimension;
			while (unhandledPositions.Count > 0)
			{
				int ipos = unhandledPositions.Dequeue();
				int posW = (ipos >> 24) & 31;
				if (posW != 0)
				{
					int posX = (ipos & 255) - 128 + centerPos.X;
					int posY = ((ipos >> 8) & 255) - 128 + centerPos.Y;
					int posZ = ((ipos >> 16) & 255) - 128 + centerPos.Z;
					IWorldChunk chunk = this.chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize + centerPos.dimension * 1024, posZ / chunkSize, false);
					if (chunk != null)
					{
						int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
						chunk.Lighting.SetSunlight_Buffered(index3d, posW);
						int absorb = chunk.GetLightAbsorptionAt(index3d, this.tmpPos.Set(posX, posY, posZ), this.blockTypes);
						if (posW - absorb > 0)
						{
							int directionIn = ((ipos >> 29) & 7) - 1;
							for (int i = 0; i < 6; i++)
							{
								if (i != directionIn)
								{
									Vec3i face = BlockFacing.ALLNORMALI[i];
									int nposX = posX + face.X;
									int nposY = posY + face.Y;
									int nposZ = posZ + face.Z;
									if ((nposX | nposY | nposZ) >= 0 && nposX < this.mapsizex && nposY < this.mapsizey && nposZ < this.mapsizez)
									{
										chunk = this.chunkProvider.GetUnpackedChunkFast(nposX / chunkSize, nposY / chunkSize + centerPos.dimension * 1024, nposZ / chunkSize, false);
										if (chunk != null)
										{
											touchedChunks.Add(this.chunkProvider.ChunkIndex3D(nposX / chunkSize, nposY / chunkSize + centerPos.dimension * 1024, nposZ / chunkSize));
											index3d = (nposY % chunkSize * chunkSize + nposZ % chunkSize) * chunkSize + nposX % chunkSize;
											int spreadLight = posW - absorb - ((isDirectlyIlluminated && nposX == centerPos.X && nposZ == centerPos.Z && i == 5) ? 0 : 1);
											if (chunk.Lighting.GetSunlight(index3d) < spreadLight)
											{
												unhandledPositions.EnqueueIfLarger(nposX - centerPos.X, nposY - centerPos.Y, nposZ - centerPos.Z, spreadLight + (TileSideEnum.GetOpposite(i) + 1 << 5));
											}
										}
									}
								}
							}
						}
					}
				}
			}
			this.tmpPos.dimension = 0;
		}

		public void ClearSunlightAt(QueueOfInt positionsToClear, BlockPos centerPos, bool isDirectlyIlluminated, QueueOfInt retainedLightToSpread, FastSetOfLongs touchedChunks)
		{
			int chunkSize = this.chunkSize;
			FastSetOfInts needToSpreadTmp = new FastSetOfInts();
			this.tmpPos.dimension = centerPos.dimension;
			while (positionsToClear.Count > 0)
			{
				int ipos = positionsToClear.Dequeue();
				int posX = (ipos & 255) - 128 + centerPos.X;
				int posY = ((ipos >> 8) & 255) - 128 + centerPos.Y;
				int posZ = ((ipos >> 16) & 255) - 128 + centerPos.Z;
				IWorldChunk chunk = this.chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize + centerPos.dimension * 1024, posZ / chunkSize, false);
				if (chunk != null)
				{
					int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
					int neibLightOld = chunk.Lighting.GetSunlight(index3d);
					if (neibLightOld != 0)
					{
						needToSpreadTmp.RemoveIfMatches(posX - centerPos.X, posY - centerPos.Y, posZ - centerPos.Z, neibLightOld);
					}
					chunk.Lighting.SetSunlight_Buffered(index3d, 0);
					int absorb = chunk.GetLightAbsorptionAt(index3d, this.tmpPos.Set(posX, posY, posZ), this.blockTypes);
					int oldLight = ((ipos >> 24) & 31) - absorb;
					if (oldLight > 0)
					{
						int directionIn = ((ipos >> 29) & 7) - 1;
						for (int i = 0; i < 6; i++)
						{
							if (i != directionIn)
							{
								Vec3i face = BlockFacing.ALLNORMALI[i];
								int nposX = posX + face.X;
								int nposY = posY + face.Y;
								int nposZ = posZ + face.Z;
								if ((nposX | nposY | nposZ) >= 0 && nposX < this.mapsizex && nposY < this.mapsizey && nposZ < this.mapsizez)
								{
									chunk = this.chunkProvider.GetUnpackedChunkFast(nposX / chunkSize, nposY / chunkSize + centerPos.dimension * 1024, nposZ / chunkSize, false);
									if (chunk != null)
									{
										touchedChunks.Add(this.chunkProvider.ChunkIndex3D(nposX / chunkSize, nposY / chunkSize + centerPos.dimension * 1024, nposZ / chunkSize));
										int spreadLight = oldLight - 1 + ((isDirectlyIlluminated && nposX == centerPos.X && nposZ == centerPos.Z && i == 5) ? 1 : 0);
										if (spreadLight > 0)
										{
											index3d = (nposY % chunkSize * chunkSize + nposZ % chunkSize) * chunkSize + nposX % chunkSize;
											int neibLight = chunk.Lighting.GetSunlight(index3d);
											if (neibLight != 0)
											{
												if (neibLight <= spreadLight)
												{
													positionsToClear.EnqueueIfLarger(nposX - centerPos.X, nposY - centerPos.Y, nposZ - centerPos.Z, spreadLight + (TileSideEnum.GetOpposite(i) + 1 << 5));
												}
												else
												{
													needToSpreadTmp.Add(nposX - centerPos.X, nposY - centerPos.Y, nposZ - centerPos.Z, neibLight);
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			foreach (int val in needToSpreadTmp)
			{
				retainedLightToSpread.Enqueue(val);
			}
			this.tmpPos.dimension = 0;
		}

		public FastSetOfLongs PlaceBlockLight(byte[] lightHsv, int posX, int posY, int posZ)
		{
			FastSetOfLongs touchedChunks = new FastSetOfLongs();
			IWorldChunk chunk = this.GetChunkAtPos(posX, posY, posZ);
			if (chunk == null)
			{
				return touchedChunks;
			}
			chunk.LightPositions.Add(this.InChunkIndex(posX, posY, posZ));
			this.UpdateLightAt((int)lightHsv[2], posX, posY, posZ, touchedChunks);
			return touchedChunks;
		}

		public void PlaceNonBlendingBlockLight(byte[] lightHsv, int posX, int posY, int posZ)
		{
			this.SetBlockLightLevel(lightHsv[0], lightHsv[1], (int)lightHsv[2], posX, posY, posZ);
			foreach (BlockFacing face in BlockFacing.ALLFACES)
			{
				this.NonBlendingLightAxis(lightHsv[0], lightHsv[1], (int)lightHsv[2], posX, posY, posZ, face);
			}
		}

		private void SetBlockLightLevel(byte hue, byte saturation, int value, int posX, int posY, int posZ)
		{
			IWorldChunk chunk = this.GetChunkAtPos(posX, posY, posZ);
			if (chunk == null)
			{
				return;
			}
			if (this.GetBlock(posX, posY, posZ) == null)
			{
				return;
			}
			chunk.LightPositions.Add(this.InChunkIndex(posX, posY, posZ));
			int index3d = (posY % this.chunkSize * this.chunkSize + posZ % this.chunkSize) * this.chunkSize + posX % this.chunkSize;
			chunk.Lighting.SetBlocklight_Buffered(index3d, (value << 5) | ((int)hue << 10) | ((int)saturation << 16));
		}

		private int GetBlockLight(int x, int y, int z)
		{
			IWorldChunk chunk = this.GetChunkAtPos(x, y, z);
			if (chunk != null)
			{
				int index3d = (y % this.chunkSize * this.chunkSize + z % this.chunkSize) * this.chunkSize + x % this.chunkSize;
				chunk.Unpack_ReadOnly();
				return chunk.Lighting.GetBlocklight(index3d);
			}
			return 0;
		}

		private void NonBlendingLightAxis(byte hue, byte saturation, int lightLevel, int x, int y, int z, BlockFacing face)
		{
			for (int curLight = lightLevel - 1; curLight > 0; curLight--)
			{
				x += face.Normali.X;
				y += face.Normali.Y;
				z += face.Normali.Z;
				if (y < 0 || y > this.mapsizey)
				{
					break;
				}
				Block block = this.GetBlock(x, y, z);
				if (block == null || block.BlockId != 0 || this.GetBlockLight(x, y, z) >= curLight)
				{
					break;
				}
				this.SetBlockLightLevel(hue, saturation, curLight, x, y, z);
				if (face.Axis == EnumAxis.X)
				{
					this.NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.UP);
					this.NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.DOWN);
					this.NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.NORTH);
					this.NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.SOUTH);
				}
				else if (face.Axis == EnumAxis.Y)
				{
					this.NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.WEST);
					this.NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.EAST);
					this.NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.NORTH);
					this.NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.SOUTH);
				}
				else if (face.Axis == EnumAxis.Z)
				{
					this.NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.UP);
					this.NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.DOWN);
					this.NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.WEST);
					this.NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.EAST);
				}
			}
		}

		public FastSetOfLongs RemoveBlockLight(byte[] oldLightHsv, int posX, int posY, int posZ)
		{
			FastSetOfLongs touchedChunks = new FastSetOfLongs();
			IWorldChunk chunk = this.GetChunkAtPos(posX, posY, posZ);
			if (chunk == null)
			{
				return touchedChunks;
			}
			chunk.LightPositions.Remove(this.InChunkIndex(posX, posY, posZ));
			int baseRange = (int)oldLightHsv[2];
			if (baseRange == 18)
			{
				baseRange = 20;
			}
			int range = baseRange - chunk.GetLightAbsorptionAt(this.InChunkIndex(posX, posY, posZ), this.tmpPos.Set(posX, posY, posZ), this.blockTypes) - 1;
			this.SpreadDarkness(range, posX, posY, posZ, touchedChunks);
			this.UpdateLightAt(baseRange, posX, posY, posZ, touchedChunks);
			return touchedChunks;
		}

		public FastSetOfLongs UpdateBlockLight(int oldLightAbsorb, int newLightAbsorb, int posX, int posY, int posZ)
		{
			FastSetOfLongs touchedChunks = new FastSetOfLongs();
			int chunkSize = this.chunkSize;
			IWorldChunk chunk = this.chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize, true);
			if (chunk == null)
			{
				return touchedChunks;
			}
			int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
			int v = chunk.Lighting.GetBlocklight(index3d);
			if (oldLightAbsorb == newLightAbsorb)
			{
				return touchedChunks;
			}
			if (v == 0)
			{
				return touchedChunks;
			}
			if (newLightAbsorb > oldLightAbsorb)
			{
				int range = v - oldLightAbsorb - 1;
				this.SpreadDarkness(range, posX, posY, posZ, touchedChunks);
			}
			this.UpdateLightAt(v, posX, posY, posZ, touchedChunks);
			return touchedChunks;
		}

		private void UpdateLightAt(int range, int posX, int posY, int posZ, FastSetOfLongs touchedChunks)
		{
			this.VisitedNodes.Clear();
			int chunkSize = this.chunkSize;
			this.LoadNearbyLightSources(posX, posY, posZ, range);
			foreach (NearbyLightSource nls in this.nearbyLightSources)
			{
				this.CollectLightValuesForLightSource(nls.posX, nls.posY, nls.posZ, posX, posY, posZ, range);
			}
			foreach (KeyValuePair<Vec3i, LightSourcesAtBlock> val in this.VisitedNodes)
			{
				this.RecalcBlockLightAtPos(val.Key, val.Value);
				touchedChunks.Add(this.chunkProvider.ChunkIndex3D(val.Key.X / chunkSize, val.Key.Y / chunkSize, val.Key.Z / chunkSize));
			}
		}

		private void SpreadDarkness(int rangeNext, int posX, int posY, int posZ, FastSetOfLongs touchedChunks)
		{
			if (rangeNext <= 0)
			{
				return;
			}
			int chunkSize = this.chunkSize;
			QueueOfInt bfsQueue = new QueueOfInt();
			bfsQueue.Enqueue(2039583 | (rangeNext << 24));
			bool nearMapEdge = posX < rangeNext - 1 || posZ < rangeNext - 1 || posX >= this.mapsizex - rangeNext + 1 || posZ >= this.mapsizez - rangeNext + 1;
			IWorldChunk chunk = this.chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize, true);
			if (chunk == null)
			{
				return;
			}
			int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
			chunk.Lighting.SetBlocklight(index3d, 0);
			touchedChunks.Add(this.chunkProvider.ChunkIndex3D(posX / chunkSize, posY / chunkSize, posZ / chunkSize));
			int num = this.iteration + 1;
			this.iteration = num;
			int iteration = num;
			posX -= 31;
			posY -= 31;
			posZ -= 31;
			int visitedIndex = 125023;
			this.currentVisited[visitedIndex] = iteration;
			while (bfsQueue.Count > 0)
			{
				int pos = bfsQueue.Dequeue();
				for (int i = 0; i < 6; i++)
				{
					Vec3i facingVector = BlockFacing.ALLNORMALI[i];
					int ox = (pos & 255) + facingVector.X;
					int oy = ((pos >> 8) & 255) + facingVector.Y;
					int oz = ((pos >> 16) & 255) + facingVector.Z;
					visitedIndex = ox + (oy * 63 + oz) * 63;
					if (this.currentVisited[visitedIndex] != iteration)
					{
						this.currentVisited[visitedIndex] = iteration;
						int nx = ox + posX;
						int ny = oy + posY;
						int nz = oz + posZ;
						if (ny >= 0 && ny % 32768 < this.mapsizey && (!nearMapEdge || (nx >= 0 && nz >= 0 && nx < this.mapsizex && nz < this.mapsizez)))
						{
							chunk = this.chunkProvider.GetUnpackedChunkFast(nx / chunkSize, ny / chunkSize, nz / chunkSize, false);
							if (chunk != null)
							{
								index3d = (ny % chunkSize * chunkSize + nz % chunkSize) * chunkSize + nx % chunkSize;
								if (chunk.Lighting.GetBlocklight(index3d) > 0)
								{
									touchedChunks.Add(this.chunkProvider.ChunkIndex3D(nx / chunkSize, ny / chunkSize, nz / chunkSize));
									chunk.Lighting.SetBlocklight_Buffered(index3d, 0);
								}
								int newRange = (pos >> 24) - chunk.GetLightAbsorptionAt(index3d, this.tmpPos.Set(nx, ny, nz), this.blockTypes) - 1;
								if (newRange > 0)
								{
									bfsQueue.Enqueue(ox | (oy << 8) | (oz << 16) | (newRange << 24));
								}
							}
						}
					}
				}
			}
		}

		private void CollectLightValuesForLightSource(int posX, int posY, int posZ, int forPosX, int forPosY, int forPosZ, int forRange)
		{
			int chunkSize = this.chunkSize;
			QueueOfInt bfsQueue = new QueueOfInt();
			Block block = this.GetBlock(posX, posY, posZ);
			if (block == null)
			{
				return;
			}
			byte[] lightHsv = block.GetLightHsv(this.readBlockAccess, this.tmpPos.Set(posX, posY, posZ), null);
			byte h = lightHsv[0];
			byte s = lightHsv[1];
			byte v = lightHsv[2];
			bfsQueue.Enqueue(2039583 | ((int)v << 24));
			Vec3i npos = new Vec3i(posX, posY, posZ);
			LightSourcesAtBlock lsab;
			this.VisitedNodes.TryGetValue(npos, out lsab);
			if (lsab == null)
			{
				lsab = (this.VisitedNodes[npos] = new LightSourcesAtBlock());
			}
			lsab.AddHsv(h, s, v);
			bool nearMapEdge = posX < (int)(v - 1) || posZ < (int)(v - 1) || posX >= this.mapsizex - (int)v + 1 || posZ >= this.mapsizez - (int)v + 1;
			int num = this.iteration + 1;
			this.iteration = num;
			int iteration = num;
			posX -= 31;
			posY -= 31;
			posZ -= 31;
			int visitedIndex = 125023;
			this.currentVisited[visitedIndex] = iteration;
			while (bfsQueue.Count > 0)
			{
				int pos = bfsQueue.Dequeue();
				int ox = (pos & 255) + posX;
				int oy = ((pos >> 8) & 255) + posY;
				int oz = ((pos >> 16) & 255) + posZ;
				IWorldChunk chunk = this.chunkProvider.GetUnpackedChunkFast(ox / chunkSize, oy / chunkSize, oz / chunkSize, false);
				if (chunk != null)
				{
					int index3d = (oy % chunkSize * chunkSize + oz % chunkSize) * chunkSize + ox % chunkSize;
					int spreadBright = (pos >> 24) - chunk.GetLightAbsorptionAt(index3d, this.tmpPos.Set(ox, oy, oz), this.blockTypes) - 1;
					if (spreadBright > 0)
					{
						for (int i = 0; i < 6; i++)
						{
							Vec3i facingVector = BlockFacing.ALLNORMALI[i];
							int nx = ox + facingVector.X;
							int ny = oy + facingVector.Y;
							int nz = oz + facingVector.Z;
							visitedIndex = ((ny - posY) * 63 + nz - posZ) * 63 + nx - posX;
							if (this.currentVisited[visitedIndex] != iteration)
							{
								this.currentVisited[visitedIndex] = iteration;
								if (ny >= 0 && ny % 32768 < this.mapsizey && (!nearMapEdge || (nx >= 0 && nz >= 0 && nx < this.mapsizex && nz < this.mapsizez)) && Math.Abs(nx - forPosX) + Math.Abs(ny - forPosY) + Math.Abs(nz - forPosZ) < forRange + spreadBright)
								{
									bfsQueue.Enqueue((nx - posX) | (ny - posY << 8) | (nz - posZ << 16) | (spreadBright << 24));
									npos = new Vec3i(nx, ny, nz);
									this.VisitedNodes.TryGetValue(npos, out lsab);
									if (lsab == null)
									{
										lsab = (this.VisitedNodes[npos] = new LightSourcesAtBlock());
									}
									lsab.AddHsv(h, s, (byte)spreadBright);
								}
							}
						}
					}
				}
			}
		}

		private void RecalcBlockLightAtPos(Vec3i pos, LightSourcesAtBlock lsab)
		{
			int chunkSize = this.chunkSize;
			IWorldChunk chunk = this.chunkProvider.GetUnpackedChunkFast(pos.X / chunkSize, pos.Y / chunkSize, pos.Z / chunkSize, true);
			if (chunk == null)
			{
				return;
			}
			int index3d = (pos.Y % chunkSize * chunkSize + pos.Z % chunkSize) * chunkSize + pos.X % chunkSize;
			float totalBright = 0f;
			int maxBright = 0;
			int lightCount = (int)lsab.lightCount;
			for (int i = 0; i < lightCount; i++)
			{
				int v = (int)lsab.lightHsvs[i * 3 + 2];
				maxBright = Math.Max(maxBright, v);
				totalBright += (float)v;
			}
			if (maxBright == 0)
			{
				chunk.Lighting.SetBlocklight(index3d, 0);
				return;
			}
			float finalRgbR = 0.5f;
			float finalRgbG = 0.5f;
			float finalRgbB = 0.5f;
			for (int j = 0; j < lightCount; j++)
			{
				int v2 = (int)lsab.lightHsvs[j * 3 + 2];
				int rgb = ColorUtil.HsvToRgb((int)(lsab.lightHsvs[j * 3] * 4), (int)(lsab.lightHsvs[j * 3 + 1] * 32), v2 * 8);
				float weight = (float)v2 / totalBright;
				finalRgbR += (float)(rgb >> 16) * weight;
				finalRgbG += (float)((rgb >> 8) & 255) * weight;
				finalRgbB += (float)(rgb & 255) * weight;
			}
			int num = ColorUtil.Rgb2Hsv(finalRgbR, finalRgbG, finalRgbB);
			int hBits = Math.Min((int)((float)(num & 255) / 4f + 0.5f), ColorUtil.HueQuantities - 1);
			int sBits = Math.Min((int)((float)((num >> 8) & 255) / 32f + 0.5f), ColorUtil.SatQuantities - 1);
			chunk.Lighting.SetBlocklight(index3d, (maxBright << 5) | (hBits << 10) | (sBits << 16));
		}

		private Block GetBlock(int posX, int posY, int posZ)
		{
			if ((posX | posY | posZ) < 0)
			{
				return null;
			}
			int chunkSize = this.chunkSize;
			IWorldChunk chunk = this.chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize, true);
			if (chunk == null)
			{
				return null;
			}
			int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
			return this.blockTypes[chunk.Data[index3d]];
		}

		private int GetBlockLightAbsorb(int posX, int posY, int posZ)
		{
			if ((posX | posY | posZ) < 0)
			{
				return 0;
			}
			int chunkSize = this.chunkSize;
			IWorldChunk chunk = this.chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize, true);
			if (chunk == null)
			{
				return 0;
			}
			int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
			return chunk.GetLightAbsorptionAt(index3d, this.tmpPos.Set(posX, posY, posZ), this.blockTypes);
		}

		private IWorldChunk GetChunkAtPos(int posX, int posY, int posZ)
		{
			return this.chunkProvider.GetUnpackedChunkFast(posX / this.chunkSize, posY / this.chunkSize, posZ / this.chunkSize, true);
		}

		private int InChunkIndex(int posX, int posY, int posZ)
		{
			return (posY % this.chunkSize * this.chunkSize + posZ % this.chunkSize) * this.chunkSize + posX % this.chunkSize;
		}

		internal long GetChunkIndexForPos(int posX, int posY, int posZ)
		{
			return this.chunkProvider.ChunkIndex3D(posX / this.chunkSize, posY / this.chunkSize, posZ / this.chunkSize);
		}

		private void LoadNearbyLightSources(int posX, int posY, int posZ, int range)
		{
			this.nearbyLightSources.Clear();
			int chunkX = posX / this.chunkSize;
			int chunkY = posY / this.chunkSize;
			int chunkZ = posZ / this.chunkSize;
			for (int cx = -1; cx <= 1; cx++)
			{
				for (int cy = -1; cy <= 1; cy++)
				{
					for (int cz = -1; cz <= 1; cz++)
					{
						IWorldChunk chunk = this.chunkProvider.GetChunk(chunkX + cx, chunkY + cy, chunkZ + cz);
						if (chunk != null)
						{
							chunk.Unpack_ReadOnly();
							foreach (int index3d in chunk.LightPositions)
							{
								int lposy = (chunkY + cy) * this.chunkSize + index3d / (this.chunkSize * this.chunkSize);
								int lposz = (chunkZ + cz) * this.chunkSize + index3d / this.chunkSize % this.chunkSize;
								int lposx = (chunkX + cx) * this.chunkSize + index3d % this.chunkSize;
								int manhattenDist = Math.Abs(posX - lposx) + Math.Abs(posY - lposy) + Math.Abs(posZ - lposz);
								Block blockEmitter = this.blockTypes[chunk.Data[index3d]];
								if ((int)blockEmitter.GetLightHsv(this.readBlockAccess, this.tmpPos.Set(lposx, lposy, lposz), null)[2] + range > manhattenDist)
								{
									this.nearbyLightSources.Add(new NearbyLightSource
									{
										block = blockEmitter,
										posX = lposx,
										posY = lposy,
										posZ = lposz
									});
								}
							}
						}
					}
				}
			}
		}

		private ushort defaultSunLight;

		private const int MAXLIGHTSPREAD = 31;

		private const int VISITED_WIDTH = 63;

		private int mapsizex;

		private int mapsizey;

		private int mapsizez;

		private int XPlus = 1;

		private int YPlus;

		private int ZPlus;

		private IList<Block> blockTypes;

		private int chunkSize;

		internal IChunkProvider chunkProvider;

		private IBlockAccessor readBlockAccess;

		private Dictionary<Vec3i, LightSourcesAtBlock> VisitedNodes = new Dictionary<Vec3i, LightSourcesAtBlock>();

		private List<NearbyLightSource> nearbyLightSources = new List<NearbyLightSource>();

		private BlockPos tmpDiPos = new BlockPos();

		private BlockPos tmpPos = new BlockPos();

		private BlockPos tmpPosDimensionAware = new BlockPos();

		private int[] currentVisited;

		private int iteration;
	}
}
