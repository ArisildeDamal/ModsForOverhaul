using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Client.NoObf
{
	public class SystemRenderTerrain : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "ret";
			}
		}

		public SystemRenderTerrain(ClientMain game)
			: base(game)
		{
			this.lastPerformanceInfoupdateMilliseconds = 0L;
			ClientSettings.Inst.AddWatcher<bool>("smoothShadows", delegate(bool b)
			{
				this.RedrawAllBlocks();
			});
			ClientSettings.Inst.AddWatcher<bool>("instancedGrass", delegate(bool b)
			{
				this.RedrawAllBlocks();
			});
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderBefore), EnumRenderStage.Before, "ret-prep", 0.995);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderOpaque), EnumRenderStage.Opaque, "ret-op", 0.37);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderShadow), EnumRenderStage.ShadowFar, "ret-sf", 0.37);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderShadow), EnumRenderStage.ShadowNear, "ret-sn", 0.37);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderOIT), EnumRenderStage.OIT, "ret-oit", 0.37);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderAfterOIT), EnumRenderStage.AfterOIT, "ret-aoit", 0.37);
			game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.PlayerChunkPos, new OnPlayerPropertyChanged(this.OnPlayerLeaveChunk));
		}

		private void OnPlayerLeaveChunk(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
		{
			int num = newValues.PlayerChunkPos.X - oldValues.PlayerChunkPos.X;
			int dz = newValues.PlayerChunkPos.Z - oldValues.PlayerChunkPos.Z;
			if ((double)(num * num + dz * dz) > 25.0)
			{
				List<long> indexes = new List<long>(this.game.dirtyChunks.Count);
				object obj = this.game.dirtyChunksLock;
				lock (obj)
				{
					int count = this.game.dirtyChunks.Count;
					while (count-- > 0)
					{
						indexes.Add(this.game.dirtyChunks.Dequeue());
					}
				}
				obj = this.game.dirtyChunksLastLock;
				lock (obj)
				{
					for (int i = 0; i < indexes.Count; i++)
					{
						this.game.dirtyChunksLast.Enqueue(indexes[i]);
					}
				}
			}
		}

		public override void OnSeperateThreadGameTick(float dt)
		{
			base.OnSeperateThreadGameTick(dt);
			if (this.ready)
			{
				this.game.chunkRenderer.OnSeperateThreadTick(dt);
			}
		}

		public override void OnBlockTexturesLoaded()
		{
			this.game.chunkRenderer = new ChunkRenderer(this.game.BlockAtlasManager.AtlasTextures.Select((LoadedTexture t) => t.TextureId).ToArray<int>(), this.game);
		}

		public void OnRenderBefore(float deltaTime)
		{
			this.game.chunkRenderer.OnRenderBefore(deltaTime);
		}

		public void OnRenderOpaque(float deltaTime)
		{
			this.ready = true;
			if (this.game.Width == 0)
			{
				return;
			}
			if (this.game.ShouldRedrawAllBlocks)
			{
				this.game.ShouldRedrawAllBlocks = false;
				this.RedrawAllBlocks();
			}
			this.game.chunkRenderer.OnBeforeRenderOpaque(deltaTime);
			this.game.chunkRenderer.RenderOpaque(deltaTime);
			this.UpdatePerformanceInfo(deltaTime);
		}

		private void OnRenderShadow(float dt)
		{
			this.game.chunkRenderer.RenderShadow(dt);
		}

		public void OnRenderOIT(float deltaTime)
		{
			if (this.game.Width == 0)
			{
				return;
			}
			this.game.chunkRenderer.RenderOIT(deltaTime);
		}

		public void OnRenderAfterOIT(float deltaTime)
		{
			if (this.game.Width == 0)
			{
				return;
			}
			this.game.chunkRenderer.RenderAfterOIT(deltaTime);
		}

		public override void Dispose(ClientMain game)
		{
			if (this.game.chunkRenderer != null)
			{
				this.game.chunkRenderer.Dispose();
			}
		}

		public void RedrawAllBlocks()
		{
			this.game.Platform.Logger.Notification("Redrawing all blocks");
			UniqueQueue<long> dirtyChunks = new UniqueQueue<long>();
			object chunksLock = this.game.WorldMap.chunksLock;
			lock (chunksLock)
			{
				foreach (long index3d in this.game.WorldMap.chunks.Keys)
				{
					dirtyChunks.Enqueue(index3d);
				}
			}
			this.game.dirtyChunks = dirtyChunks;
		}

		internal void UpdatePerformanceInfo(float dt)
		{
			if ((float)(this.game.Platform.EllapsedMs - this.lastPerformanceInfoupdateMilliseconds) * this.msPerSecond >= 1f)
			{
				if (RuntimeStats.chunksTesselatedPerSecond == 0 && this.tesselationStop == 0L && RuntimeStats.chunksTesselatedTotal > 0)
				{
					this.tesselationStop = this.lastPerformanceInfoupdateMilliseconds;
				}
				this.lastPerformanceInfoupdateMilliseconds = this.game.Platform.EllapsedMs;
				long usedVideoMemory;
				long renderedTris;
				long allocatedTris;
				this.game.chunkRenderer.GetStats(out usedVideoMemory, out renderedTris, out allocatedTris);
				RuntimeStats.availableTriangles = (int)allocatedTris;
				RuntimeStats.renderedTriangles = (int)renderedTris;
				string videoMem = (usedVideoMemory / 1024L / 1024L).ToString("#.# MB");
				this.game.DebugScreenInfo["triangles"] = string.Concat(new string[]
				{
					"Terrain GPU Mem: ",
					videoMem,
					" in ",
					this.game.chunkRenderer.QuantityModelDataPools().ToString(),
					" pools, Tris: ",
					RuntimeStats.renderedTriangles.ToString("N0"),
					" / ",
					RuntimeStats.availableTriangles.ToString("N0")
				});
				if (this.game.extendedDebugInfo)
				{
					this.game.DebugScreenInfo["gpumemfrag"] = "Terrain GPU Mem Fragmentation: " + (this.game.chunkRenderer.CalcFragmentation() * 100f).ToString("#.#") + "%";
				}
				else
				{
					this.game.DebugScreenInfo["gpumemfrag"] = "";
				}
				int avtessPerS = (int)((float)RuntimeStats.chunksTesselatedTotal * 1000f / ((float)(this.tesselationStop - RuntimeStats.tesselationStart) + 0.0001f));
				if (avtessPerS < 0)
				{
					avtessPerS = 0;
				}
				this.game.DebugScreenInfo["chunkstats"] = string.Concat(new string[]
				{
					"Chunks rec=",
					RuntimeStats.chunksReceived.ToString(),
					", ld=",
					this.game.WorldMap.chunks.Count.ToString(),
					", tess/s=",
					RuntimeStats.chunksTesselatedPerSecond.ToString(),
					" (eo ",
					((int)((float)RuntimeStats.chunksTesselatedEdgeOnly * 100f / ((float)RuntimeStats.chunksTesselatedPerSecond + 0.0001f) + 0.5f)).ToString(),
					"%), avtess/s=",
					avtessPerS.ToString(),
					", tq=",
					RuntimeStats.chunksAwaitingTesselation.ToString(),
					", p=",
					RuntimeStats.chunksAwaitingPooling.ToString(),
					", rend=",
					this.game.chunkRenderer.QuantityRenderingChunks.ToString(),
					", unl=",
					RuntimeStats.chunksUnloaded.ToString()
				});
				RuntimeStats.chunksTesselatedPerSecond = 0;
				RuntimeStats.chunksTesselatedEdgeOnly = 0;
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Render;
		}

		private long lastPerformanceInfoupdateMilliseconds;

		private bool ready;

		private float msPerSecond = 0.001f;

		private long tesselationStop;
	}
}
