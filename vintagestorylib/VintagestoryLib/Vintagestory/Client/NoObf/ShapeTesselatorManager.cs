using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Server;

namespace Vintagestory.Client.NoObf
{
	public class ShapeTesselatorManager : AsyncHelper.Multithreaded, ITesselatorManager
	{
		public ShapeTesselator Tesselator
		{
			get
			{
				ShapeTesselator shapeTesselator;
				if ((shapeTesselator = ShapeTesselatorManager.TLTesselator) == null)
				{
					shapeTesselator = (ShapeTesselatorManager.TLTesselator = new ShapeTesselator(this.game, this.shapes, this.objs, this.gltfs));
				}
				return shapeTesselator;
			}
		}

		public MeshData GetDefaultBlockMesh(Block block)
		{
			if (this.blockModelDatas[block.BlockId] == null)
			{
				this.TesselateBlock(block, false);
			}
			return this.blockModelDatas[block.BlockId];
		}

		internal ITesselatorAPI GetNewTesselator()
		{
			return new ShapeTesselator(this.game, this.shapes, this.objs, this.gltfs);
		}

		public ShapeTesselatorManager(ClientMain game)
		{
			this.game = game;
			ClientEventManager em = game.eventManager;
			if (em != null)
			{
				em.OnReloadShapes += this.TesselateBlocksAndItems;
			}
		}

		public ShapeTesselatorManager(ServerMain server)
		{
		}

		private void TesselateBlocksAndItems()
		{
			this.PrepareToLoadShapes();
			this.LoadItemShapesAsync(this.game.Items);
			this.LoadBlockShapes(this.game.Blocks);
			ShapeTesselatorManager.TLTesselator = new ShapeTesselator(this.game, this.shapes, this.objs, this.gltfs);
			this.TesselateBlocks_Pre();
			TyronThreadPool.QueueTask(delegate
			{
				this.TesselateBlocks_Async(this.game.Blocks);
			});
			for (int i = 0; i < this.game.Blocks.Count; i = this.TesselateBlocksForInventory(this.game.Blocks, i, this.game.Blocks.Count))
			{
			}
			for (int i = 0; i < this.game.Items.Count; i = this.TesselateItems(this.game.Items, i, this.game.Items.Count))
			{
			}
			while (this.finishedAsyncBlockTesselation != 2 && !this.game.disposed)
			{
				Thread.Sleep(30);
			}
			this.LoadDone();
		}

		public void LoadDone()
		{
			if (ClientSettings.OptimizeRamMode == 2)
			{
				foreach (KeyValuePair<AssetLocation, UnloadableShape> val in this.shapes)
				{
					if (val.Value != this.basicCubeShape)
					{
						val.Value.Unload();
					}
				}
			}
		}

		public void PrepareToLoadShapes()
		{
			base.ResetThreading();
			this.shapes = new OrderedDictionary<AssetLocation, UnloadableShape>();
			this.shapes2 = new Dictionary<AssetLocation, UnloadableShape>();
			this.shapes3 = new Dictionary<AssetLocation, UnloadableShape>();
			this.shapes4 = new Dictionary<AssetLocation, UnloadableShape>();
			this.itemshapes = new Dictionary<AssetLocation, UnloadableShape>();
			this.objs = new OrderedDictionary<AssetLocation, IAsset>();
			this.gltfs = new OrderedDictionary<AssetLocation, GltfType>();
			this.shapes[new AssetLocation("block/basic/cube")] = this.BasicCube(this.game.api);
		}

		internal void LoadItemShapesAsync(IList<Item> items)
		{
			this.itemloadingdone = false;
			TyronThreadPool.QueueTask(delegate
			{
				this.LoadItemShapes(items);
			});
		}

		internal Dictionary<AssetLocation, UnloadableShape> LoadItemShapes(IList<Item> items)
		{
			try
			{
				HashSet<AssetLocationAndSource> shapelocations = new HashSet<AssetLocationAndSource>();
				for (int i = 0; i < items.Count; i++)
				{
					if (this.game.disposed)
					{
						return this.itemshapes;
					}
					Item item = items[i];
					if (item != null && item.Shape != null)
					{
						CompositeShape shape = item.Shape;
						if (!shape.VoxelizeTexture)
						{
							shapelocations.Add(new AssetLocationAndSource(shape.Base, "Shape for item ", item.Code, -1));
							shape.LoadAlternates(this.game.api.Assets, this.game.Logger);
						}
						if (shape.BakedAlternates != null)
						{
							for (int j = 0; j < shape.BakedAlternates.Length; j++)
							{
								if (this.game.disposed)
								{
									return this.itemshapes;
								}
								if (!shape.BakedAlternates[j].VoxelizeTexture)
								{
									shapelocations.Add(new AssetLocationAndSource(shape.BakedAlternates[j].Base, "Alternate shape for item ", item.Code, j));
								}
							}
						}
						if (shape.Overlays != null)
						{
							for (int k = 0; k < shape.Overlays.Length; k++)
							{
								if (this.game.disposed)
								{
									return this.itemshapes;
								}
								if (!shape.Overlays[k].VoxelizeTexture)
								{
									shapelocations.Add(new AssetLocationAndSource(shape.Overlays[k].Base, "Overlay shape for item ", item.Code, k));
								}
							}
						}
					}
				}
				this.game.Platform.Logger.VerboseDebug("[LoadShapes] Searched through items...");
				this.LoadShapes(shapelocations, this.itemshapes, "for items");
			}
			finally
			{
				this.itemloadingdone = true;
			}
			return this.itemshapes;
		}

		internal OrderedDictionary<AssetLocation, UnloadableShape> LoadBlockShapes(IList<Block> blocks)
		{
			this.game.Platform.Logger.VerboseDebug("[LoadShapes] Searching through blocks...");
			int maxBlockId = blocks.Count;
			IDisposable[] array = this.blockModelRefsInventory;
			this.DisposeArray(array);
			this.blockModelDatas = new MeshData[maxBlockId + 1];
			this.altblockModelDatasLod0 = new MeshData[maxBlockId + 1][];
			this.altblockModelDatasLod1 = new MeshData[maxBlockId + 1][];
			this.altblockModelDatasLod2 = new MeshData[maxBlockId + 1][];
			this.blockModelRefsInventory = new MultiTextureMeshRef[maxBlockId + 1];
			CompositeShape basicCube = new CompositeShape
			{
				Base = new AssetLocation("block/basic/cube")
			};
			int availableCores = Environment.ProcessorCount / 2 - 3;
			availableCores = Math.Min(availableCores, 4);
			availableCores = Math.Max(availableCores, 2);
			TargetSet[] sets = new TargetSet[1];
			int count = 0;
			for (int i = 0; i < sets.Length; i++)
			{
				TargetSet set = new TargetSet();
				sets[i] = set;
				int start = i * blocks.Count / sets.Length;
				int end = (i + 1) * blocks.Count / sets.Length;
				if (i < sets.Length - 1)
				{
					TyronThreadPool.QueueTask(delegate
					{
						this.CollectBlockShapes(blocks, start, end, set, basicCube, ref count);
					}, "collectblockshapes");
				}
				else
				{
					this.CollectBlockShapes(blocks, start, end, set, basicCube, ref count);
				}
			}
			HashSet<AssetLocationAndSource> shapelocations = new HashSet<AssetLocationAndSource>();
			HashSet<AssetLocationAndSource> objlocations = new HashSet<AssetLocationAndSource>();
			HashSet<AssetLocationAndSource> gltflocations = new HashSet<AssetLocationAndSource>();
			shapelocations.Add(new AssetLocationAndSource(basicCube.Base));
			foreach (TargetSet set2 in sets)
			{
				while (!set2.finished && !this.game.disposed)
				{
					Thread.Sleep(10);
				}
				foreach (AssetLocationAndSource val in set2.shapelocations)
				{
					shapelocations.Add(val);
				}
				foreach (AssetLocationAndSource val2 in set2.objlocations)
				{
					objlocations.Add(val2);
				}
				foreach (AssetLocationAndSource val3 in set2.gltflocations)
				{
					gltflocations.Add(val3);
				}
			}
			this.game.Platform.Logger.VerboseDebug("[LoadShapes] Searched through " + count.ToString() + " blocks");
			while (base.WorkerThreadsInProgress() && !this.game.disposed)
			{
				Thread.Sleep(10);
			}
			this.game.Platform.Logger.VerboseDebug("[LoadShapes] Starting to parse block shapes...");
			if (availableCores >= 2)
			{
				base.StartWorkerThread(delegate
				{
					this.LoadShapes(shapelocations, this.shapes2, "(2nd block loading thread)");
				});
			}
			if (availableCores >= 3)
			{
				base.StartWorkerThread(delegate
				{
					this.LoadShapes(shapelocations, this.shapes3, "(3rd block loading thread)");
				});
			}
			if (availableCores >= 4)
			{
				base.StartWorkerThread(delegate
				{
					this.LoadShapes(shapelocations, this.shapes4, "(4th block loading thread)");
				});
			}
			this.LoadShapes(objlocations, gltflocations);
			this.LoadShapes(shapelocations, this.shapes, "for " + count.ToString() + " blocks" + ((availableCores > 1) ? ", some others done offthread" : ""));
			this.FinalizeLoading();
			return this.shapes;
		}

		private void CollectBlockShapes(IList<Block> blocks, int start, int maxCount, TargetSet targetSet, CompositeShape basicCube, ref int totalCount)
		{
			int count = 0;
			try
			{
				for (int i = start; i < maxCount; i++)
				{
					if (this.game.disposed)
					{
						break;
					}
					Block block = blocks[i];
					if (!(block.Code == null))
					{
						count++;
						if (block.Shape == null || block.Shape.Base.Path.Length == 0)
						{
							block.Shape = basicCube;
						}
						else
						{
							CompositeShape shape = block.Shape;
							shape.LoadAlternates(this.game.api.Assets, this.game.Logger);
							targetSet.Add(shape, "Shape for block ", block.Code, -1);
							if (shape.BakedAlternates != null)
							{
								for (int j = 0; j < shape.BakedAlternates.Length; j++)
								{
									if (this.game.disposed)
									{
										return;
									}
									CompositeShape alternateShape = shape.BakedAlternates[j];
									if (alternateShape != null && !(alternateShape.Base == null))
									{
										targetSet.Add(alternateShape, "Alternate shape for block ", block.Code, j);
									}
								}
							}
							if (block.Shape.Overlays != null)
							{
								for (int k = 0; k < block.Shape.Overlays.Length; k++)
								{
									if (this.game.disposed)
									{
										return;
									}
									CompositeShape overlayshape = block.Shape.Overlays[k];
									if (overlayshape != null && !(overlayshape.Base == null))
									{
										targetSet.Add(overlayshape, "Overlay shape for block ", block.Code, k);
									}
								}
							}
						}
						if (block.ShapeInventory != null)
						{
							if (this.game.disposed)
							{
								break;
							}
							targetSet.Add(block.ShapeInventory, "Inventory shape for block ", block.Code, -1);
							if (block.ShapeInventory.Overlays != null)
							{
								for (int l = 0; l < block.ShapeInventory.Overlays.Length; l++)
								{
									if (this.game.disposed)
									{
										return;
									}
									CompositeShape overlayshape2 = block.ShapeInventory.Overlays[l];
									if (overlayshape2 != null && !(overlayshape2.Base == null))
									{
										targetSet.Add(overlayshape2, "Inventory overlay shape for block ", block.Code, l);
									}
								}
							}
						}
						if (block.Lod0Shape != null)
						{
							if (this.game.disposed)
							{
								break;
							}
							block.Lod0Shape.LoadAlternates(this.game.api.Assets, this.game.Logger);
							targetSet.Add(block.Lod0Shape, "Lod0 shape for block ", block.Code, -1);
							if (block.Lod0Shape.BakedAlternates != null)
							{
								for (int m = 0; m < block.Lod0Shape.BakedAlternates.Length; m++)
								{
									if (this.game.disposed)
									{
										return;
									}
									CompositeShape alternateShape2 = block.Lod0Shape.BakedAlternates[m];
									if (alternateShape2 != null && !(alternateShape2.Base == null))
									{
										targetSet.Add(alternateShape2, "Alternate lod 0 for block ", block.Code, m);
									}
								}
							}
						}
						if (block.Lod2Shape != null)
						{
							if (this.game.disposed)
							{
								break;
							}
							block.Lod2Shape.LoadAlternates(this.game.api.Assets, this.game.Logger);
							targetSet.Add(block.Lod2Shape, "Lod2 shape for block ", block.Code, -1);
							if (block.Lod2Shape.BakedAlternates != null)
							{
								for (int n = 0; n < block.Lod2Shape.BakedAlternates.Length; n++)
								{
									if (this.game.disposed)
									{
										return;
									}
									CompositeShape alternateShape3 = block.Lod2Shape.BakedAlternates[n];
									if (alternateShape3 != null && !(alternateShape3.Base == null))
									{
										targetSet.Add(alternateShape3, "Alternate lod 2 for block ", block.Code, n);
									}
								}
							}
						}
					}
				}
			}
			finally
			{
				targetSet.finished = true;
				Interlocked.Add(ref totalCount, count);
			}
		}

		internal void FinalizeLoading()
		{
			while (!this.itemloadingdone || (base.WorkerThreadsInProgress() && !this.game.disposed))
			{
				Thread.Sleep(10);
			}
			ILogger logger = this.game.Platform.Logger;
			this.shapes.AddRange(this.shapes2, logger);
			this.shapes2.Clear();
			this.shapes2 = null;
			this.shapes.AddRange(this.shapes3, logger);
			this.shapes3.Clear();
			this.shapes3 = null;
			this.shapes.AddRange(this.shapes4, logger);
			this.shapes4.Clear();
			this.shapes4 = null;
			this.shapes.AddRange(this.itemshapes, logger);
			this.itemshapes = null;
			this.game.DoneBlockAndItemShapeLoading = true;
			logger.Notification("Collected {0} shapes to tesselate.", new object[] { this.shapes.Count });
		}

		internal void LoadShapes(HashSet<AssetLocationAndSource> shapelocations, IDictionary<AssetLocation, UnloadableShape> shapes, string typeForLog)
		{
			int count = 0;
			foreach (AssetLocationAndSource srcandLoc in shapelocations)
			{
				if (this.game.disposed)
				{
					break;
				}
				if (AsyncHelper.CanProceedOnThisThread(ref srcandLoc.loadedAlready))
				{
					count++;
					UnloadableShape shape = new UnloadableShape();
					shape.Loaded = true;
					if (!shape.Load(this.game, srcandLoc))
					{
						shapes[srcandLoc] = this.basicCubeShape;
					}
					else
					{
						shapes[srcandLoc] = shape;
					}
				}
			}
			this.game.Platform.Logger.VerboseDebug("[LoadShapes] parsed " + count.ToString() + " shapes from JSON " + typeForLog);
		}

		internal void LoadShapes(HashSet<AssetLocationAndSource> objlocations, HashSet<AssetLocationAndSource> gltflocations)
		{
			int count = 0;
			foreach (AssetLocationAndSource srcandLoc in objlocations)
			{
				if (this.game.disposed)
				{
					return;
				}
				AssetLocation newLocation = srcandLoc.CopyWithPathPrefixAndAppendixOnce("shapes/", ".obj");
				IAsset asset = ScreenManager.Platform.AssetManager.TryGet(newLocation, true);
				if (this.game.disposed)
				{
					return;
				}
				if (asset == null)
				{
					this.game.Platform.Logger.Warning("Did not find required obj {0} anywhere. (defined in {1})", new object[] { newLocation, srcandLoc.Source });
				}
				else
				{
					this.objs[srcandLoc] = asset;
					count++;
				}
			}
			foreach (AssetLocationAndSource srcandLoc2 in gltflocations)
			{
				if (this.game.disposed)
				{
					return;
				}
				AssetLocation newLocation2 = srcandLoc2.CopyWithPathPrefixAndAppendixOnce("shapes/", ".gltf");
				IAsset asset = ScreenManager.Platform.AssetManager.TryGet(newLocation2, true);
				if (this.game.disposed)
				{
					return;
				}
				if (asset == null)
				{
					this.game.Platform.Logger.Warning("Did not find required gltf {0} anywhere. (defined in {1})", new object[] { newLocation2, srcandLoc2.Source });
				}
				else
				{
					this.gltfs[srcandLoc2] = asset.ToObject<GltfType>(null);
					count++;
				}
			}
			if (count > 0)
			{
				this.game.Platform.Logger.VerboseDebug("[LoadShapes] loaded " + count.ToString() + " block shapes in obj and gltf formats");
			}
		}

		private UnloadableShape BasicCube(ICoreAPI api)
		{
			if (this.basicCubeShape == null)
			{
				AssetLocation pathLoc = new AssetLocation("shapes/block/basic/cube.json");
				IAsset asset = api.Assets.TryGet(pathLoc, true);
				if (asset == null)
				{
					throw new Exception("Shape shapes/block/basic/cube.json not found, it is required to run the game");
				}
				ShapeElement.locationForLogging = pathLoc;
				this.basicCubeShape = asset.ToObject<UnloadableShape>(null);
				this.basicCubeShape.Loaded = true;
			}
			return this.basicCubeShape;
		}

		public void LoadEntityShapesAsync(IEnumerable<EntityProperties> entities, ICoreAPI api)
		{
			base.OnWorkerThread(delegate
			{
				this.LoadEntityShapes(entities, api);
			});
		}

		public void LoadEntityShapes(IEnumerable<EntityProperties> entities, ICoreAPI api)
		{
			Dictionary<AssetLocation, Shape> entityShapes = new Dictionary<AssetLocation, Shape>();
			entityShapes[new AssetLocation("block/basic/cube")] = this.BasicCube(api);
			api.Logger.VerboseDebug("Entity shape loading starting ...");
			foreach (EntityProperties val in entities)
			{
				if (this.game != null && this.game.disposed)
				{
					return;
				}
				if (val != null && val.Client != null)
				{
					try
					{
						this.LoadShape(val, api, entityShapes);
					}
					catch (Exception)
					{
						api.Logger.Error("Error while attempting to load shape file for entity: " + val.Code.ToShortString());
						throw;
					}
				}
			}
			api.Logger.VerboseDebug("Entity shape loading completed");
		}

		private void LoadShape(EntityProperties entity, ICoreAPI api, Dictionary<AssetLocation, Shape> entityShapes)
		{
			EntityClientProperties clientProperties = entity.Client;
			Shape shape = this.LoadEntityShape(clientProperties.Shape, entity.Code, api, entityShapes);
			clientProperties.LoadedShape = shape;
			if (api is ICoreServerAPI && shape != null)
			{
				shape.FreeRAMServer();
			}
			CompositeShape shape2 = clientProperties.Shape;
			CompositeShape[] alternates = ((shape2 != null) ? shape2.Alternates : null);
			if (alternates != null)
			{
				Shape[] loadedAlternates = (clientProperties.LoadedAlternateShapes = new Shape[alternates.Length]);
				for (int i = 0; i < alternates.Length; i++)
				{
					if (this.game != null && this.game.disposed)
					{
						return;
					}
					shape = this.LoadEntityShape(alternates[i], entity.Code, api, entityShapes);
					loadedAlternates[i] = shape;
					if (api is ICoreServerAPI && shape != null)
					{
						shape.FreeRAMServer();
					}
				}
			}
		}

		private Shape LoadEntityShape(CompositeShape cShape, AssetLocation entityTypeForLogging, ICoreAPI api, Dictionary<AssetLocation, Shape> entityShapes)
		{
			if (cShape == null)
			{
				return null;
			}
			if (cShape.Base == null || cShape.Base.Path.Length == 0)
			{
				if (cShape == null || !cShape.VoxelizeTexture)
				{
					api.Logger.Warning("No entity shape supplied for entity {0}, using cube shape", new object[] { entityTypeForLogging });
				}
				cShape.Base = new AssetLocation("block/basic/cube");
				return this.basicCubeShape;
			}
			Shape entityShape;
			if (entityShapes.TryGetValue(cShape.Base, out entityShape))
			{
				if (entityShape == null)
				{
					api.Logger.Error("Entity shape for entity {0} not found or errored, was supposed to be at shapes/{1}.json. Entity will be invisible!", new object[] { entityTypeForLogging, cShape.Base });
				}
				return entityShape;
			}
			AssetLocation shapePath = cShape.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");
			entityShape = Shape.TryGet(api, shapePath);
			entityShapes[cShape.Base] = entityShape;
			if (entityShape == null)
			{
				api.Logger.Error("Entity shape for entity {0} not found or errored, was supposed to be at {1}. Entity will be invisible!", new object[] { entityTypeForLogging, shapePath });
				return null;
			}
			entityShape.ResolveReferences(api.Logger, cShape.Base.ToString());
			if (api.Side == EnumAppSide.Client)
			{
				ShapeTesselatorManager.CacheInvTransforms(entityShape.Elements);
			}
			return entityShape;
		}

		private static void CacheInvTransforms(ShapeElement[] elements)
		{
			if (elements == null)
			{
				return;
			}
			foreach (ShapeElement shapeElement in elements)
			{
				shapeElement.CacheInverseTransformMatrix();
				ShapeTesselatorManager.CacheInvTransforms(shapeElement.Children);
			}
		}

		public void TesselateBlocks_Pre()
		{
			if (this.unknownBlockModelRef == null)
			{
				this.unknownBlockModelRef = this.game.api.renderapi.UploadMultiTextureMesh(this.unknownBlockModelData);
			}
			if (this.shapes == null)
			{
				throw new Exception("Can't tesselate, shapes not loaded yet!");
			}
			this.finishedAsyncBlockTesselation = 0;
		}

		public int TesselateBlocksForInventory(IList<Block> blocks, int offset, int maxCount)
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			int i = offset;
			int cnt = 0;
			while (i < maxCount)
			{
				Block block = blocks[i];
				if (!(block.Code == null) && block.BlockId != 0)
				{
					MeshData modeldataInv = this.TesselateBlockForInventory(block);
					MultiTextureMeshRef multiTextureMeshRef = this.blockModelRefsInventory[block.BlockId];
					if (multiTextureMeshRef != null)
					{
						multiTextureMeshRef.Dispose();
					}
					this.blockModelRefsInventory[block.BlockId] = this.game.api.renderapi.UploadMultiTextureMesh(modeldataInv);
					if (cnt++ % 4 == 0 && sw.ElapsedMilliseconds >= 60L)
					{
						i++;
						break;
					}
				}
				i++;
			}
			if (i == blocks.Count)
			{
				this.BlockTesselationHalfCompleted();
			}
			return i;
		}

		public void TesselateBlocksForInventory_ASync(IList<Block> blocks)
		{
			if (ShapeTesselatorManager.TLTesselator != null)
			{
				throw new Exception("A previous threadpool thread did not call ThreadDispose() when finished with the TesselatorManager");
			}
			MeshData[] meshes = new MeshData[blocks.Count];
			try
			{
				for (int i = 0; i < blocks.Count; i++)
				{
					Block block = blocks[i];
					if (!(block.Code == null) && block.BlockId != 0)
					{
						meshes[i] = this.TesselateBlockForInventory(block);
					}
				}
			}
			finally
			{
				this.game.EnqueueGameLaunchTask(delegate
				{
					this.FinishInventoryMeshes(meshes, 0);
				}, "blockInventoryTesselation");
				this.BlockTesselationHalfCompleted();
				this.ThreadDispose();
			}
		}

		private void FinishInventoryMeshes(MeshData[] meshes, int start)
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			int cnt = 0;
			int i;
			Action <>9__0;
			int j;
			for (i = start; i < meshes.Length; i = j + 1)
			{
				MeshData modeldataInv = meshes[i];
				if (modeldataInv != null)
				{
					if (modeldataInv == this.unknownBlockModelData)
					{
						this.blockModelRefsInventory[i] = this.unknownBlockModelRef;
					}
					else
					{
						MultiTextureMeshRef multiTextureMeshRef = this.blockModelRefsInventory[i];
						if (multiTextureMeshRef != null)
						{
							multiTextureMeshRef.Dispose();
						}
						this.blockModelRefsInventory[i] = this.game.api.renderapi.UploadMultiTextureMesh(modeldataInv);
					}
					if (cnt++ % 4 == 0 && sw.ElapsedMilliseconds >= 60L)
					{
						ClientMain clientMain = this.game;
						Action action;
						if ((action = <>9__0) == null)
						{
							action = (<>9__0 = delegate
							{
								this.FinishInventoryMeshes(meshes, i + 1);
							});
						}
						clientMain.EnqueueGameLaunchTask(action, "blockInventoryTesselation");
						return;
					}
				}
				j = i;
			}
		}

		public void TesselateBlocks_Async(IList<Block> blocks)
		{
			if (ShapeTesselatorManager.TLTesselator != null)
			{
				throw new Exception("A previous threadpool thread did not call ThreadDispose() when finished with the TesselatorManager");
			}
			try
			{
				for (int i = 0; i < blocks.Count; i++)
				{
					Block block = blocks[i];
					if (block != null && !(block.Code == null) && block.BlockId != 0)
					{
						this.TesselateBlock(block, true);
						ShapeTesselatorManager.CreateFastTextureAlternates(block);
					}
				}
			}
			finally
			{
				this.BlockTesselationHalfCompleted();
				this.ThreadDispose();
			}
		}

		private void BlockTesselationHalfCompleted()
		{
			if (Interlocked.Increment(ref this.finishedAsyncBlockTesselation) == 2)
			{
				this.game.Logger.Notification("Blocks tesselated");
				this.game.Logger.VerboseDebug("Server assets - done block tesselation");
			}
		}

		public static void CreateFastTextureAlternates(Block block)
		{
			if (block.HasAlternates && block.DrawType != EnumDrawType.JSON)
			{
				BakedCompositeTexture[][] ftv = (block.FastTextureVariants = new BakedCompositeTexture[6][]);
				foreach (BlockFacing facing in BlockFacing.ALLFACES)
				{
					CompositeTexture faceTexture;
					if (block.Textures.TryGetValue(facing.Code, out faceTexture))
					{
						BakedCompositeTexture[] variants = faceTexture.Baked.BakedVariants;
						if (variants != null && variants.Length != 0)
						{
							ftv[facing.Index] = variants;
						}
					}
				}
			}
			if (block.HasTiles && block.DrawType != EnumDrawType.JSON)
			{
				BakedCompositeTexture[][] ftv2 = (block.FastTextureVariants = new BakedCompositeTexture[6][]);
				foreach (BlockFacing facing2 in BlockFacing.ALLFACES)
				{
					CompositeTexture faceTexture2;
					if (block.Textures.TryGetValue(facing2.Code, out faceTexture2))
					{
						BakedCompositeTexture[] tiles = faceTexture2.Baked.BakedTiles;
						if (tiles != null && tiles.Length != 0)
						{
							ftv2[facing2.Index] = tiles;
						}
					}
				}
			}
		}

		public void TesselateItems_Pre(IList<Item> itemtypes)
		{
			if (this.unknownItemModelRef == null)
			{
				CompositeTexture tex = new CompositeTexture(new AssetLocation("unknown"));
				tex.Bake(this.game.Platform.AssetManager);
				BakedBitmap bcBmp = TextureAtlasManager.LoadCompositeBitmap(this.game, new AssetLocationAndSource(tex.Baked.BakedName));
				this.unknownItemModelData = ShapeTesselator.VoxelizeTextureStatic(bcBmp.TexturePixels, bcBmp.Width, bcBmp.Height, this.game.BlockAtlasManager.UnknownTexturePos, null);
				this.unknownItemModelRef = this.game.api.renderapi.UploadMultiTextureMesh(this.unknownItemModelData);
			}
			if (this.itemModelRefsInventory == null)
			{
				this.itemModelRefsInventory = new MultiTextureMeshRef[itemtypes.Count];
			}
			if (this.altItemModelRefsInventory == null)
			{
				this.altItemModelRefsInventory = new MultiTextureMeshRef[itemtypes.Count][];
			}
		}

		public int TesselateItems(IList<Item> itemtypes, int offset, int maxCount)
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			int cnt = 0;
			int i;
			for (i = offset; i < maxCount; i++)
			{
				Item item = itemtypes[i];
				if (item != null)
				{
					if (item.Code == null || ((item.FirstTexture == null || item.FirstTexture.Base.Path == "unknown") && item.Shape == null))
					{
						this.itemModelRefsInventory[item.ItemId] = this.unknownItemModelRef;
					}
					else
					{
						MeshData modeldata;
						this.Tesselator.TesselateItem(item, item.Shape, out modeldata);
						if (this.itemModelRefsInventory[item.ItemId] != null)
						{
							this.itemModelRefsInventory[item.ItemId].Dispose();
						}
						this.itemModelRefsInventory[item.ItemId] = this.game.api.renderapi.UploadMultiTextureMesh(modeldata);
						CompositeShape shape = item.Shape;
						if (((shape != null) ? shape.BakedAlternates : null) != null)
						{
							if (this.altItemModelRefsInventory[item.ItemId] == null)
							{
								this.altItemModelRefsInventory[item.ItemId] = new MultiTextureMeshRef[item.Shape.BakedAlternates.Length];
							}
							int j = 0;
							while (item.Shape.BakedAlternates.Length > j)
							{
								MeshData modeldataalt;
								this.Tesselator.TesselateItem(item, item.Shape.BakedAlternates[j], out modeldataalt);
								if (this.altItemModelRefsInventory[item.ItemId][j] != null)
								{
									this.altItemModelRefsInventory[item.ItemId][j].Dispose();
								}
								this.altItemModelRefsInventory[item.ItemId][j] = this.game.api.renderapi.UploadMultiTextureMesh(modeldataalt);
								j++;
							}
						}
						if (cnt++ % 4 == 0 && sw.ElapsedMilliseconds >= 60L)
						{
							i++;
							break;
						}
					}
				}
			}
			return i;
		}

		private void TesselateBlock(Block block, bool lazyLoad)
		{
			if (block.IsMissing)
			{
				this.blockModelDatas[block.BlockId] = this.unknownBlockModelData;
				return;
			}
			int altTextureCount = this.Tesselator.AltTexturesCount(block);
			int altShapeCount = ((block.Shape.BakedAlternates == null) ? 0 : block.Shape.BakedAlternates.Length);
			int tilesCount = this.Tesselator.TileTexturesCount(block);
			block.HasAlternates = Math.Max(altTextureCount, altShapeCount) != 0;
			block.HasTiles = tilesCount > 0;
			if (lazyLoad)
			{
				return;
			}
			TextureSource texSource = new TextureSource(this.game, this.game.BlockAtlasManager.Size, block, false);
			if (block.Lod0Shape != null)
			{
				block.Lod0Mesh = this.Tesselate(texSource, block, block.Lod0Shape, this.altblockModelDatasLod0, altTextureCount, tilesCount);
				ShapeTesselatorManager.setLod0Flag(block.Lod0Mesh);
				MeshData[] alts = this.altblockModelDatasLod0[block.Id];
				int i = 0;
				while (alts != null && i < alts.Length)
				{
					ShapeTesselatorManager.setLod0Flag(alts[i]);
					i++;
				}
			}
			this.blockModelDatas[block.BlockId] = this.Tesselate(texSource, block, block.Shape, this.altblockModelDatasLod1, altTextureCount, tilesCount);
			if (block.Lod2Shape != null)
			{
				block.Lod2Mesh = this.Tesselate(texSource, block, block.Lod2Shape, this.altblockModelDatasLod2, altTextureCount, tilesCount);
			}
		}

		private MeshData TesselateBlockForInventory(Block block)
		{
			if (block.IsMissing)
			{
				return this.unknownBlockModelData;
			}
			TextureSource texSource = new TextureSource(this.game, this.game.BlockAtlasManager.Size, block, true);
			texSource.blockShape = block.Shape;
			if (block.ShapeInventory != null)
			{
				texSource.blockShape = block.ShapeInventory;
			}
			MeshData modeldataInv;
			try
			{
				if (block.Shape.VoxelizeTexture)
				{
					BakedBitmap bcBmp = TextureAtlasManager.LoadCompositeBitmap(this.game, new AssetLocationAndSource(block.FirstTextureInventory.Baked.BakedName, "Block code ", block.Code, -1));
					int textureSubId = block.FirstTextureInventory.Baked.TextureSubId;
					TextureAtlasPosition pos = this.game.BlockAtlasManager.TextureAtlasPositionsByTextureSubId[textureSubId];
					modeldataInv = ShapeTesselator.VoxelizeTextureStatic(bcBmp.TexturePixels, bcBmp.Width, bcBmp.Height, pos, null);
				}
				else
				{
					this.Tesselator.TesselateBlock(block, texSource.blockShape, out modeldataInv, texSource, null, null);
				}
			}
			catch (Exception e)
			{
				ILogger logger = this.game.Platform.Logger;
				string text = "Exception thrown when trying to tesselate block {0} with first texture {1}:";
				object[] array = new object[2];
				array[0] = block;
				int num = 1;
				CompositeTexture firstTextureInventory = block.FirstTextureInventory;
				object obj;
				if (firstTextureInventory == null)
				{
					obj = null;
				}
				else
				{
					BakedCompositeTexture baked = firstTextureInventory.Baked;
					obj = ((baked != null) ? baked.BakedName : null);
				}
				array[num] = obj;
				logger.Error(text, array);
				this.game.Platform.Logger.Error(e);
				throw;
			}
			int count = modeldataInv.GetVerticesCount() / 4;
			for (int i = 0; i < count; i++)
			{
				byte[] climateColorMapIds = modeldataInv.ClimateColorMapIds;
				int curColorMapId = (int)((climateColorMapIds != null && climateColorMapIds.Length != 0) ? modeldataInv.ClimateColorMapIds[i] : 0);
				if (curColorMapId != 0)
				{
					JsonObject attributes = block.Attributes;
					if (attributes == null || !attributes.IsTrue("ignoreTintInventory"))
					{
						string colorMap = this.game.ColorMaps.GetKeyAtIndex(curColorMapId - 1);
						byte[] tintBytes = ColorUtil.ToBGRABytes(this.game.WorldMap.ApplyColorMapOnRgba(colorMap, null, -1, 180, 138, false, 0f, 0f));
						for (int j = 0; j < 4; j++)
						{
							int curVertex = i * 4 + j;
							for (int colind = 0; colind < 3; colind++)
							{
								int index = 4 * curVertex + colind;
								modeldataInv.Rgba[index] = modeldataInv.Rgba[index] * tintBytes[colind] / byte.MaxValue;
							}
						}
					}
				}
			}
			modeldataInv.CompactBuffers();
			return modeldataInv;
		}

		private MeshData Tesselate(TextureSource texSource, Block block, CompositeShape shape, MeshData[][] altblockModelDatas, int altTextureCount, int tilesCount)
		{
			MeshData modeldata;
			try
			{
				this.Tesselator.TesselateBlock(block, shape, out modeldata, texSource, null, null);
			}
			catch (Exception e)
			{
				this.game.Platform.Logger.Error("Exception thrown when trying to tesselate block {0}:", new object[] { block });
				this.game.Platform.Logger.Error(e);
				throw;
			}
			modeldata.CompactBuffers();
			int altShapeCount = ((shape.BakedAlternates == null) ? 0 : shape.BakedAlternates.Length);
			int alternateCount = Math.Max(altTextureCount, altShapeCount);
			if (alternateCount != 0)
			{
				MeshData[] meshes = new MeshData[alternateCount];
				for (int i = 0; i < alternateCount; i++)
				{
					if (altTextureCount > 0)
					{
						texSource.UpdateVariant(block, i % altTextureCount);
					}
					CompositeShape altShape = ((altShapeCount == 0) ? shape : shape.BakedAlternates[i % altShapeCount]);
					MeshData altModeldata;
					this.Tesselator.TesselateBlock(block, altShape, out altModeldata, texSource, null, null);
					altModeldata.CompactBuffers();
					meshes[i] = altModeldata;
				}
				altblockModelDatas[block.BlockId] = meshes;
			}
			else if (tilesCount != 0)
			{
				MeshData[] meshes2 = new MeshData[tilesCount];
				for (int j = 0; j < tilesCount; j++)
				{
					texSource.UpdateVariant(block, j % tilesCount);
					CompositeShape altShape2 = ((altShapeCount == 0) ? shape : shape.BakedAlternates[j % altShapeCount]);
					MeshData altModeldata2;
					this.Tesselator.TesselateBlock(block, altShape2, out altModeldata2, texSource, null, null);
					altModeldata2.CompactBuffers();
					meshes2[j] = altModeldata2;
				}
				altblockModelDatas[block.BlockId] = meshes2;
			}
			return modeldata;
		}

		private static void setLod0Flag(MeshData altModeldata)
		{
			for (int i = 0; i < altModeldata.FlagsCount; i++)
			{
				altModeldata.Flags[i] |= 4096;
			}
		}

		internal void Dispose()
		{
			IDisposable[] array = this.blockModelRefsInventory;
			this.DisposeArray(array);
			array = this.itemModelRefsInventory;
			this.DisposeArray(array);
			int i = 0;
			while (this.altItemModelRefsInventory != null && i < this.altItemModelRefsInventory.Length)
			{
				array = this.altItemModelRefsInventory[i];
				this.DisposeArray(array);
				i++;
			}
			MultiTextureMeshRef multiTextureMeshRef = this.unknownItemModelRef;
			if (multiTextureMeshRef != null)
			{
				multiTextureMeshRef.Dispose();
			}
			MultiTextureMeshRef multiTextureMeshRef2 = this.unknownBlockModelRef;
			if (multiTextureMeshRef2 != null)
			{
				multiTextureMeshRef2.Dispose();
			}
			ShapeTesselatorManager.TLTesselator = null;
		}

		private void DisposeArray(IDisposable[] refs)
		{
			if (refs == null)
			{
				return;
			}
			foreach (IDisposable disposable in refs)
			{
				if (disposable != null)
				{
					disposable.Dispose();
				}
			}
		}

		public MultiTextureMeshRef GetDefaultBlockMeshRef(Block block)
		{
			return this.blockModelRefsInventory[block.Id];
		}

		public MultiTextureMeshRef GetDefaultItemMeshRef(Item item)
		{
			return this.itemModelRefsInventory[item.Id];
		}

		public Shape GetCachedShape(AssetLocation location)
		{
			UnloadableShape shape;
			this.shapes.TryGetValue(location, out shape);
			if (shape != null && !shape.Loaded)
			{
				shape.Load(this.game, new AssetLocationAndSource(location));
			}
			return shape;
		}

		public MeshData CreateMesh(string typeForLogging, CompositeShape cshape, TextureSourceBuilder texgen, ITexPositionSource texSource = null)
		{
			cshape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
			IAsset asset = this.game.api.Assets.TryGet(cshape.Base, true);
			Shape shape = ((asset != null) ? asset.ToObject<Shape>(null) : null);
			if (shape == null)
			{
				return new MeshData(4, 3, false, true, true, true);
			}
			if (texSource == null)
			{
				texSource = texgen(shape, cshape.Base.ToShortString());
			}
			MeshData meshdata;
			this.Tesselator.TesselateShape(typeForLogging, shape, out meshdata, texSource, (cshape.rotateX == 0f && cshape.rotateY == 0f && cshape.rotateZ == 0f) ? null : new Vec3f(cshape.rotateX, cshape.rotateY, cshape.rotateZ), 0, 0, 0, null, null);
			return meshdata;
		}

		public void ThreadDispose()
		{
			ShapeTesselatorManager.TLTesselator = null;
		}

		public OrderedDictionary<AssetLocation, UnloadableShape> shapes;

		public OrderedDictionary<AssetLocation, IAsset> objs;

		public OrderedDictionary<AssetLocation, GltfType> gltfs;

		public MeshData[] blockModelDatas;

		public MeshData[][] altblockModelDatasLod0;

		public MeshData[][] altblockModelDatasLod1;

		public MeshData[][] altblockModelDatasLod2;

		public MultiTextureMeshRef[] blockModelRefsInventory;

		public MultiTextureMeshRef[] itemModelRefsInventory;

		public MultiTextureMeshRef[][] altItemModelRefsInventory;

		public MeshData unknownItemModelData = QuadMeshUtilExt.GetCustomQuadModelData(0f, 0f, 0f, 1f, 1f, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, 0);

		public MultiTextureMeshRef unknownItemModelRef;

		public MeshData unknownBlockModelData = CubeMeshUtil.GetCubeOnlyScaleXyz(0.5f, 0.5f, new Vec3f(0.5f, 0.5f, 0.5f));

		public MultiTextureMeshRef unknownBlockModelRef;

		private ClientMain game;

		internal volatile int finishedAsyncBlockTesselation;

		[ThreadStatic]
		private static ShapeTesselator TLTesselator;

		private UnloadableShape basicCubeShape;

		private bool itemloadingdone;

		private Dictionary<AssetLocation, UnloadableShape> shapes2;

		private Dictionary<AssetLocation, UnloadableShape> shapes3;

		private Dictionary<AssetLocation, UnloadableShape> shapes4;

		private Dictionary<AssetLocation, UnloadableShape> itemshapes;
	}
}
