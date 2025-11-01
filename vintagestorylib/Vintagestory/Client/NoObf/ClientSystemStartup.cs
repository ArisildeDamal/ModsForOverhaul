using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class ClientSystemStartup : ClientSystem
	{
		private ILogger logger
		{
			get
			{
				return this.game.Logger;
			}
		}

		public ClientSystemStartup(ClientMain game)
			: base(game)
		{
			ClientSystemStartup.instance = this;
			game.PacketHandlers[77] = new ServerPacketHandler<Packet_Server>(this.HandleLoginTokenAnswer);
			game.PacketHandlers[1] = new ServerPacketHandler<Packet_Server>(this.HandleServerIdent);
			game.PacketHandlers[82] = new ServerPacketHandler<Packet_Server>(this.HandleQueue);
			game.PacketHandlers[73] = new ServerPacketHandler<Packet_Server>(this.HandleServerReady);
			game.PacketHandlers[4] = new ServerPacketHandler<Packet_Server>(this.HandleLevelInitialize);
			game.PacketHandlers[5] = new ServerPacketHandler<Packet_Server>(this.HandleLevelDataChunk);
			game.PacketHandlers[21] = new ServerPacketHandler<Packet_Server>(this.HandleWorldMetaData);
			game.PacketHandlers[19] = new ServerPacketHandler<Packet_Server>(this.HandleServerAssets);
			game.PacketHandlers[6] = new ServerPacketHandler<Packet_Server>(this.HandleLevelFinalize);
			game.ServerInfo = new ServerInformation();
		}

		private void HandleQueue(Packet_Server packet)
		{
			this.game.Logger.Notification("Client is in connect queue at position: " + packet.QueuePacket.Position.ToString());
			this.game.Connectdata.PositionInQueue = packet.QueuePacket.Position;
		}

		private void HandleLoginTokenAnswer(Packet_Server packet)
		{
			this.game.networkProc.StartUdpConnectRequest(packet.Token.Token);
			if (this.game.IsSingleplayer)
			{
				this.sendIdentificationPacket();
				return;
			}
			if (this.game.ScreenRunningGame.ScreenManager.ClientIsOffline)
			{
				this.onMpTokenReceived(EnumAuthServerResponse.Good, "offline");
				return;
			}
			this.multiplayerAuthAttempts = 0;
			this.multiplayerAuthRetryToken = packet.Token.Token;
			this.game.ScreenRunningGame.ScreenManager.sessionManager.RequestMpToken(new Action<EnumAuthServerResponse, string>(this.onMpTokenReceived), packet.Token.Token);
		}

		private void onMpTokenReceived(EnumAuthServerResponse resp, string errorreason)
		{
			if (resp == EnumAuthServerResponse.Good)
			{
				this.multiplayerAuthAttempts = 0;
				this.multiplayerAuthRetryToken = null;
				this.logger.Debug("Okay, received single use mp token from auth server. Sending ident packet");
				this.game.EnqueueGameLaunchTask(new Action(this.sendIdentificationPacket), "sendIdentPacket");
				return;
			}
			if (resp == EnumAuthServerResponse.Offline && this.multiplayerAuthRetryToken != null)
			{
				int num = this.multiplayerAuthAttempts;
				this.multiplayerAuthAttempts = num + 1;
				if (num < 1)
				{
					Thread.Sleep(900);
					this.game.ScreenRunningGame.ScreenManager.sessionManager.RequestMpToken(new Action<EnumAuthServerResponse, string>(this.onMpTokenReceived), this.multiplayerAuthRetryToken);
					return;
				}
			}
			this.game.Connectdata.ErrorMessage = Lang.Get("Failed requesting mp token from auth server. Server says: {0}", new object[] { errorreason });
			this.multiplayerAuthRetryToken = null;
		}

		private void sendIdentificationPacket()
		{
			this.game.SendPacketClient(ClientPackets.CreateIdentificationPacket(this.game.Platform, this.game.Connectdata));
			this.logger.Debug("Ident packet sent.");
		}

		private void HandleServerAssets(Packet_Server packet)
		{
			this.pkt_srvrassets = packet.Assets;
			if (this.lightLevelsReceived)
			{
				this.game.EnqueueGameLaunchTask(new Action(this.HandleServerAssets_Initial), "serverassetsreceived");
				return;
			}
			this.logger.VerboseDebug("Received server assets packet; waiting on light levels packet before decoding.");
		}

		private void HandleServerIdent(Packet_Server packet)
		{
			this.game.ServerNetworkVersion = packet.Identification.NetworkVersion;
			this.game.ServerGameVersion = packet.Identification.GameVersion;
			this.game.Connectdata.PositionInQueue = 0;
			if ("1.21.9" != packet.Identification.NetworkVersion)
			{
				this.game.disconnectReason = Lang.Get("disconnect-wrongversion", new object[]
				{
					"1.21.5",
					"1.21.9",
					this.game.ServerGameVersion,
					this.game.ServerNetworkVersion
				});
				this.game.Platform.Logger.Warning(this.game.disconnectReason);
				this.game.exitReason = "client<=>server game version mismatch";
				this.game.DestroyGameSession(true);
				return;
			}
			this.game.TrySetWorldConfig(packet.Identification.WorldConfiguration);
			this.game.ServerMods = ClientSystemStartup.parseMods(packet.Identification.Mods, packet.Identification.ModsCount);
			int cnt = packet.Identification.ServerModIdBlackListCount;
			if (cnt > 0)
			{
				string[] blockedModIds = new string[cnt];
				Array.Copy(packet.Identification.ServerModIdBlackList, blockedModIds, cnt);
				this.game.ServerModIdBlacklist = new List<string>(blockedModIds);
			}
			cnt = packet.Identification.ServerModIdWhiteListCount;
			if (cnt > 0)
			{
				string[] whitelistedModIds = new string[cnt];
				Array.Copy(packet.Identification.ServerModIdWhiteList, whitelistedModIds, cnt);
				this.game.ServerModIdWhitelist = new List<string>(whitelistedModIds);
			}
			this.logger.VerboseDebug("Handling ServerIdentification packet; requires remapping is " + (packet.Identification.RequireRemapping > 0).ToString());
			this.game.ServerInfo.connectdata = this.game.Connectdata;
			this.game.ServerInfo.Seed = packet.Identification.Seed;
			this.game.ServerInfo.SavegameIdentifier = packet.Identification.SavegameIdentifier;
			this.game.ServerInfo.ServerName = packet.Identification.ServerName;
			this.game.ServerInfo.Playstyle = packet.Identification.PlayStyle;
			this.game.ServerInfo.PlayListCode = packet.Identification.PlayListCode;
			this.game.ServerInfo.RequiresRemappings = packet.Identification.RequireRemapping > 0;
			for (int i = 0; i < this.game.clientSystems.Length; i++)
			{
				this.game.clientSystems[i].OnServerIdentificationReceived();
				if (!this.game.IsSingleplayer && this.game.clientSystems[i] is SystemModHandler)
				{
					this.AfterAssetsLoaded();
				}
			}
			this.game.Platform.Logger.Notification("Processed server identification");
			if (packet.Identification.MapSizeX != this.game.WorldMap.MapSizeX || packet.Identification.MapSizeY != this.game.WorldMap.MapSizeY || packet.Identification.MapSizeZ != this.game.WorldMap.MapSizeZ)
			{
				this.game.WorldMap.OnMapSizeReceived(new Vec3i(packet.Identification.MapSizeX, packet.Identification.MapSizeY, packet.Identification.MapSizeZ), new Vec3i(packet.Identification.RegionMapSizeX, packet.Identification.RegionMapSizeY, packet.Identification.RegionMapSizeZ));
			}
			this.game.Platform.Logger.Notification("Map initialized");
		}

		private static List<ModId> parseMods(Packet_ModId[] mods, int modCount)
		{
			List<ModId> servermods = new List<ModId>();
			for (int i = 0; i < modCount; i++)
			{
				Packet_ModId p = mods[i];
				ModId modid = new ModId
				{
					Id = p.Modid,
					Version = p.Version,
					Name = p.Name,
					NetworkVersion = p.Networkversion,
					RequiredOnClient = p.RequiredOnClient
				};
				servermods.Add(modid);
			}
			return servermods;
		}

		private void HandleWorldMetaData(Packet_Server packet)
		{
			this.game.TrySetWorldConfig(packet.WorldMetaData.WorldConfiguration);
			this.game.WorldMap.BlockLightLevels = new float[packet.WorldMetaData.BlockLightlevelsCount];
			this.game.WorldMap.BlockLightLevelsByte = new byte[this.game.WorldMap.BlockLightLevels.Length];
			this.game.WorldMap.SunLightLevels = new float[packet.WorldMetaData.SunLightlevelsCount];
			this.game.WorldMap.SunLightLevelsByte = new byte[this.game.WorldMap.SunLightLevels.Length];
			this.game.WorldMap.SunBrightness = packet.WorldMetaData.SunBrightness;
			for (int i = 0; i < packet.WorldMetaData.BlockLightlevelsCount; i++)
			{
				this.game.WorldMap.BlockLightLevels[i] = CollectibleNet.DeserializeFloat(packet.WorldMetaData.BlockLightlevels[i]);
				this.game.WorldMap.BlockLightLevelsByte[i] = (byte)(255f * this.game.WorldMap.BlockLightLevels[i]);
				this.game.WorldMap.SunLightLevels[i] = CollectibleNet.DeserializeFloat(packet.WorldMetaData.SunLightlevels[i]);
				this.game.WorldMap.SunLightLevelsByte[i] = (byte)(255f * this.game.WorldMap.SunLightLevels[i]);
			}
			this.game.WorldMap.hueLevels = new byte[ColorUtil.HueQuantities];
			for (int j = 0; j < ColorUtil.HueQuantities; j++)
			{
				this.game.WorldMap.hueLevels[j] = (byte)(4 * j);
			}
			this.game.WorldMap.satLevels = new byte[ColorUtil.SatQuantities];
			for (int k = 0; k < ColorUtil.SatQuantities; k++)
			{
				this.game.WorldMap.satLevels[k] = (byte)(32 * k);
			}
			ClientWorldMap.seaLevel = packet.WorldMetaData.SeaLevel;
			ScreenManager.Platform.Logger.VerboseDebug("Received world meta data");
			this.game.TerrainChunkTesselator.LightlevelsReceived();
			this.game.WorldMap.OnLightLevelsReceived();
			if (!this.lightLevelsReceived)
			{
				this.lightLevelsReceived = true;
				if (this.pkt_srvrassets != null)
				{
					this.game.EnqueueGameLaunchTask(delegate
					{
						this.HandleServerAssets_Initial();
					}, "lightlevelsreceived");
				}
			}
		}

		private void HandleServerAssets_Initial()
		{
			this.logger.Notification("Received server assets");
			this.logger.VerboseDebug("Received server assets");
			this.game.AssetsReceived = true;
			if (this.game.IsSingleplayer)
			{
				this.game.modHandler.SinglePlayerStart();
				if (this.game.exitToDisconnectScreen)
				{
					return;
				}
				this.game.modHandler.PreStartMods();
				this.logger.VerboseDebug("Single player game - starting mods on the client-side");
				this.game.modHandler.StartMods();
				TyronThreadPool.QueueTask(new Action(this.ReloadExternalAssets_Async));
			}
			this.logger.VerboseDebug("Server assets - done step 1, next steps off-thread");
			this.game.SuspendMainThreadTasks = true;
			this.game.AssetLoadingOffThread = true;
			TyronThreadPool.QueueTask(new Action(this.HandleServerAssets_Step1));
		}

		private void ReloadExternalAssets_Async()
		{
			try
			{
				this.game.modHandler.ReloadExternalAssets();
			}
			finally
			{
				this.game.EnqueueGameLaunchTask(new Action(this.AfterAssetsLoaded), "assetsLoaded");
			}
		}

		private void HandleServerAssets_Step1()
		{
			this.LoadTags();
			if (this.game.disposed)
			{
				return;
			}
			this.LoadEntityTypes();
			if (this.game.disposed)
			{
				return;
			}
			this.StartLoadingEntityShapesWhenReady();
			this.LoadItemTypes();
			if (this.game.disposed)
			{
				return;
			}
			this.StartLoadingItemShapesWhenReady();
			this.LoadBlockTypes();
			if (this.game.disposed)
			{
				return;
			}
			this.StartLoadingBlockShapesWhenReady();
		}

		internal void AfterAssetsLoaded()
		{
			this.game.modHandler.OnAssetsLoaded();
			this.logger.VerboseDebug("All client-side assets loaded and patched; configs, shapes, textures and sounds are now available for loading");
			if (this.game.disposed)
			{
				return;
			}
			this.game.BlockAtlasManager.CreateNewAtlas("blocks");
			this.game.TesselatorManager.PrepareToLoadShapes();
			this.StartLoadingEntityShapesWhenReady();
			this.StartLoadingItemShapesWhenReady();
			this.StartLoadingBlockShapesWhenReady();
		}

		private void LoadBlockTypes()
		{
			int maxBlockId = 0;
			Packet_BlockType[] packetisedBlocks = this.pkt_srvrassets.Blocks;
			int maxCount = this.pkt_srvrassets.BlocksCount;
			int i = 0;
			while (i < packetisedBlocks.Length && i < maxCount)
			{
				maxBlockId = Math.Max(maxBlockId, packetisedBlocks[i].BlockId);
				i++;
			}
			Block[] blocks = new Block[maxBlockId + 1];
			int quantityBlocks = this.PopulateBlocks(blocks, 0, maxCount);
			this.game.FastBlockTextureSubidsByBlockAndFace = new int[blocks.Length][];
			this.game.Blocks = new BlockList(this.game, blocks);
			this.logger.VerboseDebug("Populated " + quantityBlocks.ToString() + " BlockTypes from server, highest BlockID is " + maxBlockId.ToString());
		}

		private void LoadItemTypes()
		{
			int maxItemId = 0;
			Packet_ItemType[] packetisedItems = this.pkt_srvrassets.Items;
			int maxCount = this.pkt_srvrassets.ItemsCount;
			int i = 0;
			while (i < packetisedItems.Length && i < maxCount)
			{
				maxItemId = Math.Max(maxItemId, packetisedItems[i].ItemId);
				i++;
			}
			int listSize = Math.Max(4000, maxItemId + 1);
			List<Item> items = new List<Item>(listSize);
			int quantityItems = this.PopulateItems(items, listSize);
			this.game.Items = items;
			this.logger.VerboseDebug("Populated " + quantityItems.ToString() + " ItemTypes from server, highest ItemID is " + maxItemId.ToString());
		}

		private void LoadTags()
		{
			if (this.pkt_srvrassets.Tags.EntityTags != null)
			{
				this.game.TagRegistry.RegisterEntityTagsOnClient(RuntimeHelpers.GetSubArray<string>(this.pkt_srvrassets.Tags.EntityTags, Range.EndAt(this.pkt_srvrassets.Tags.EntityTagsCount)));
			}
			if (this.pkt_srvrassets.Tags.BlockTags != null)
			{
				this.game.TagRegistry.RegisterBlockTagsOnClient(RuntimeHelpers.GetSubArray<string>(this.pkt_srvrassets.Tags.BlockTags, Range.EndAt(this.pkt_srvrassets.Tags.BlockTagsCount)));
			}
			if (this.pkt_srvrassets.Tags.ItemTags != null)
			{
				this.game.TagRegistry.RegisterItemTagsOnClient(RuntimeHelpers.GetSubArray<string>(this.pkt_srvrassets.Tags.ItemTags, Range.EndAt(this.pkt_srvrassets.Tags.ItemTagsCount)));
			}
		}

		public void StartLoadingBlockShapesWhenReady()
		{
			if (Interlocked.Increment(ref this.blockShapesPrerequisites) == 2)
			{
				this.blockShapesPrerequisites = 0;
				this.logger.VerboseDebug("Starting to load block shapes");
				this.collectionThreadBlockAtlas = new Thread(delegate
				{
					this.loadBlockAtlasManagerAsync(this.game.Blocks);
				});
				this.collectionThreadBlockAtlas.IsBackground = true;
				this.collectionThreadBlockAtlas.Name = "collecttexturesasync";
				this.collectionThreadBlockAtlas.Start();
			}
		}

		public void StartLoadingItemShapesWhenReady()
		{
			if (Interlocked.Increment(ref this.itemShapesPrerequisites) == 2)
			{
				this.itemShapesPrerequisites = 0;
				this.logger.VerboseDebug("Starting to load item shapes");
				this.collectionThreadItemAtlas = new Thread(delegate
				{
					this.prepareAsync(this.game.Items);
				});
				this.collectionThreadItemAtlas.IsBackground = true;
				this.collectionThreadItemAtlas.Start();
			}
		}

		public void StartLoadingEntityShapesWhenReady()
		{
			if (Interlocked.Increment(ref this.entityShapesPrerequisites) == 2)
			{
				this.entityShapesPrerequisites = 0;
				this.logger.VerboseDebug("Starting to load entity shapes");
				this.collectionThreadEntityAtlas = new Thread(delegate
				{
					this.loadAsyncEntityAtlas(this.game.EntityTypes);
				});
				this.collectionThreadEntityAtlas.IsBackground = true;
				this.collectionThreadEntityAtlas.Priority = ThreadPriority.AboveNormal;
				this.collectionThreadEntityAtlas.Start();
			}
		}

		public void StartSlowLoadingSoundsWhenReady()
		{
			if (Interlocked.Increment(ref this.loadSoundsSlowPrerequisites) == 2)
			{
				this.loadSoundsSlowPrerequisites = 0;
				this.logger.VerboseDebug("Starting to load sounds fully");
				ScreenManager.soundAudioDataAsyncLoadTemp.Clear();
				foreach (KeyValuePair<AssetLocation, AudioData> val in ScreenManager.soundAudioData)
				{
					ScreenManager.soundAudioDataAsyncLoadTemp[val.Key] = val.Value;
				}
				ScreenManager.LoadSoundsSlow_Async(this.game);
			}
		}

		private void loadBlockAtlasManagerAsync(IList<Block> blocks)
		{
			try
			{
				this.ResolveBlockItemStacks();
				OrderedDictionary<AssetLocation, UnloadableShape> shapes = this.game.TesselatorManager.LoadBlockShapes(blocks);
				this.game.Logger.VerboseDebug("BlockTextureAtlas start collecting textures (already holds " + (this.game.BlockAtlasManager.Count - 1).ToString() + " colormap textures)");
				this.game.BlockAtlasManager.CollectTextures(blocks, shapes);
				if (!this.game.disposed)
				{
					this.WaitFor(ref this.game.DoneColorMaps, 2000, "colormaps", "loading all the colormaps from file: config / colormaps.json");
					if (!this.game.disposed)
					{
						this.game.BlockAtlasManager.PopulateTextureAtlassesFromTextures();
						if (!this.game.disposed)
						{
							this.StartSlowLoadingMusicAndSounds();
							this.game.EnqueueGameLaunchTask(delegate
							{
								this.FinaliseTextureAtlas(this.game.BlockAtlasManager, "block", new Action(this.HandleServerAssets_Step9));
							}, "ServerAssetsReceived");
						}
					}
				}
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception e)
			{
				this.game.Logger.Fatal("Caught unhandled exception in BlockTextureCollection thread. Exiting game.");
				this.game.Logger.Fatal(e);
				this.game.Platform.XPlatInterface.ShowMessageBox("Client Thread Crash", "Whoops, a client game thread crashed, please check the client-main.log for more Information. I will now exit the game (and stop the server if in singleplayer). Sorry about that :(");
				this.game.KillNextFrame = true;
			}
		}

		private void loadAsyncEntityAtlas(List<EntityProperties> entityClasses)
		{
			try
			{
				this.game.TesselatorManager.LoadEntityShapesAsync(entityClasses, this.game.api);
				TyronThreadPool.QueueTask(new Action(this.LoadColorMapsAndCatalogSoundsIfSingleplayer));
				this.game.EntityAtlasManager.CollectTextures(entityClasses);
				if (!this.game.disposed)
				{
					this.game.EntityAtlasManager.CreateNewAtlas("entities");
					if (!this.game.disposed)
					{
						this.game.EntityAtlasManager.PopulateTextureAtlassesFromTextures();
						if (!this.game.disposed)
						{
							this.game.EnqueueGameLaunchTask(delegate
							{
								this.FinaliseTextureAtlas(this.game.EntityAtlasManager, "entity", null);
							}, "ServerAssetsReceived");
						}
					}
				}
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception e)
			{
				this.game.Logger.Fatal("Caught unhandled exception in EntityTextureCollection thread. Exiting game.");
				this.game.Logger.Fatal(e);
				this.game.Platform.XPlatInterface.ShowMessageBox("Client Thread Crash", "Whoops, a client game thread crashed, please check the client-main.log for more Information. I will now exit the game (and stop the server if in singleplayer). Sorry about that :(");
				this.game.KillNextFrame = true;
			}
		}

		private void prepareAsync(IList<Item> items)
		{
			try
			{
				Dictionary<AssetLocation, UnloadableShape> shapes = this.game.TesselatorManager.LoadItemShapes(items);
				this.game.ItemAtlasManager.CollectTextures(items, shapes);
				if (!this.game.disposed)
				{
					this.game.ItemAtlasManager.CreateNewAtlas("items");
					if (!this.game.disposed)
					{
						this.game.ItemAtlasManager.PopulateTextureAtlassesFromTextures();
						if (!this.game.disposed)
						{
							this.game.EnqueueGameLaunchTask(delegate
							{
								this.FinaliseTextureAtlas(this.game.ItemAtlasManager, "item", new Action(this.BeginItemTesselation));
							}, "ServerAssetsReceived");
						}
					}
				}
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception e)
			{
				this.game.Logger.Fatal("Caught unhandled exception in ItemTextureCollection thread. Exiting game.");
				this.game.Logger.Fatal(e);
				this.game.Platform.XPlatInterface.ShowMessageBox("Client Thread Crash", "Whoops, a client game thread crashed, please check the client-main.log for more Information. I will now exit the game (and stop the server if in singleplayer). Sorry about that :(");
				this.game.KillNextFrame = true;
			}
		}

		private int PopulateBlocks(Block[] blocks, int start, int maxCount)
		{
			int quantityBlocks = 0;
			Packet_BlockType[] packetisedBlocks = this.pkt_srvrassets.Blocks;
			int i = start;
			while (i < packetisedBlocks.Length && i < maxCount)
			{
				Packet_BlockType pt = packetisedBlocks[i];
				if (pt.Code != null)
				{
					blocks[pt.BlockId] = BlockTypeNet.ReadBlockTypePacket(pt, this.game, ClientMain.ClassRegistry);
					quantityBlocks++;
				}
				i++;
			}
			return quantityBlocks;
		}

		private int PopulateItems(List<Item> items, int listSize)
		{
			int quantityItems = 0;
			Item noitem = new Item(0);
			for (int i = 0; i < listSize; i++)
			{
				items.Add(noitem);
			}
			Packet_ItemType[] packetisedItems = this.pkt_srvrassets.Items;
			int maxCount = this.pkt_srvrassets.ItemsCount;
			int j = 0;
			while (j < packetisedItems.Length && j < maxCount)
			{
				Packet_ItemType pt = packetisedItems[j];
				if (pt.Code != null)
				{
					items[pt.ItemId] = ItemTypeNet.ReadItemTypePacket(pt, this.game, ClientMain.ClassRegistry);
					quantityItems++;
				}
				j++;
			}
			return quantityItems;
		}

		private void LoadEntityTypes()
		{
			for (int i = 0; i < this.pkt_srvrassets.EntitiesCount; i++)
			{
				Packet_EntityType packet = this.pkt_srvrassets.Entities[i];
				try
				{
					EntityProperties config = EntityTypeNet.FromPacket(packet, null);
					this.game.EntityClassesByCode[config.Code] = config;
				}
				catch (Exception e)
				{
					this.logger.Error("Loading error for entity " + packet.Code);
					this.logger.Error(e);
				}
			}
			this.logger.VerboseDebug("Populated " + this.pkt_srvrassets.EntitiesCount.ToString() + " EntityTypes from server");
		}

		internal void LoadColorMapsAndCatalogSoundsIfSingleplayer()
		{
			int count = this.game.WorldMap.LoadColorMaps();
			this.logger.VerboseDebug("Loaded " + count.ToString() + " ColorMap textures");
			this.game.DoneColorMaps = true;
			if (this.game.IsSingleplayer)
			{
				ScreenManager.CatalogSounds(new Action(this.StartSlowLoadingSoundsWhenReady));
			}
		}

		internal void ResolveBlockItemStacks()
		{
			this.game.LoadCollectibles(this.game.Items, this.game.Blocks);
			for (int i = 0; i < this.game.Items.Count; i++)
			{
				Item item = this.game.Items[i];
				if (!(item.Code == null))
				{
					this.game.ItemsByCode[item.Code] = item;
					CombustibleProperties combustibleProps = item.CombustibleProps;
					bool flag;
					if (combustibleProps == null)
					{
						flag = null != null;
					}
					else
					{
						JsonItemStack smeltedStack = combustibleProps.SmeltedStack;
						flag = ((smeltedStack != null) ? smeltedStack.ResolvedItemstack : null) != null;
					}
					if (flag)
					{
						item.CombustibleProps.SmeltedStack.ResolvedItemstack.ResolveBlockOrItem(this.game);
					}
					FoodNutritionProperties nutritionProps = item.NutritionProps;
					bool flag2;
					if (nutritionProps == null)
					{
						flag2 = null != null;
					}
					else
					{
						JsonItemStack eatenStack = nutritionProps.EatenStack;
						flag2 = ((eatenStack != null) ? eatenStack.ResolvedItemstack : null) != null;
					}
					if (flag2)
					{
						item.NutritionProps.EatenStack.ResolvedItemstack.ResolveBlockOrItem(this.game);
					}
					if (item.TransitionableProps != null)
					{
						TransitionableProperties[] array = item.TransitionableProps;
						for (int l = 0; l < array.Length; l++)
						{
							JsonItemStack transitionedStack = array[l].TransitionedStack;
							if (transitionedStack != null)
							{
								ItemStack resolvedItemstack = transitionedStack.ResolvedItemstack;
								if (resolvedItemstack != null)
								{
									resolvedItemstack.ResolveBlockOrItem(this.game);
								}
							}
						}
					}
					if (item.GrindingProps != null)
					{
						JsonItemStack groundStack = item.GrindingProps.GroundStack;
						if (((groundStack != null) ? groundStack.ResolvedItemstack : null) != null)
						{
							item.GrindingProps.GroundStack.ResolvedItemstack.ResolveBlockOrItem(this.game);
						}
					}
					if (item.CrushingProps != null)
					{
						JsonItemStack crushedStack = item.CrushingProps.CrushedStack;
						if (((crushedStack != null) ? crushedStack.ResolvedItemstack : null) != null)
						{
							item.CrushingProps.CrushedStack.ResolvedItemstack.ResolveBlockOrItem(this.game);
						}
					}
				}
			}
			this.logger.Notification("Received {0} item types from server", new object[] { this.game.ItemsByCode.Count });
			int[] unknownTextureSubIds = new int[7];
			Cuboidf[] boxes = new Cuboidf[] { Block.DefaultCollisionBox };
			int blockCount = 0;
			for (int j = 0; j < this.game.Blocks.Count; j++)
			{
				Block block = this.game.Blocks[j];
				if (block.Code == null)
				{
					this.game.FastBlockTextureSubidsByBlockAndFace[j] = unknownTextureSubIds;
					block.DrawType = EnumDrawType.Cube;
					block.SelectionBoxes = boxes;
					block.CollisionBoxes = boxes;
				}
				else
				{
					blockCount++;
					this.game.BlocksByCode[block.Code] = block;
					this.game.FastBlockTextureSubidsByBlockAndFace[j] = new int[7];
					CombustibleProperties combustibleProps2 = block.CombustibleProps;
					bool flag3;
					if (combustibleProps2 == null)
					{
						flag3 = null != null;
					}
					else
					{
						JsonItemStack smeltedStack2 = combustibleProps2.SmeltedStack;
						flag3 = ((smeltedStack2 != null) ? smeltedStack2.ResolvedItemstack : null) != null;
					}
					if (flag3)
					{
						block.CombustibleProps.SmeltedStack.ResolvedItemstack.ResolveBlockOrItem(this.game);
					}
					FoodNutritionProperties nutritionProps2 = block.NutritionProps;
					bool flag4;
					if (nutritionProps2 == null)
					{
						flag4 = null != null;
					}
					else
					{
						JsonItemStack eatenStack2 = nutritionProps2.EatenStack;
						flag4 = ((eatenStack2 != null) ? eatenStack2.ResolvedItemstack : null) != null;
					}
					if (flag4)
					{
						block.NutritionProps.EatenStack.ResolvedItemstack.ResolveBlockOrItem(this.game);
					}
					if (block.TransitionableProps != null)
					{
						TransitionableProperties[] array = block.TransitionableProps;
						for (int l = 0; l < array.Length; l++)
						{
							JsonItemStack transitionedStack2 = array[l].TransitionedStack;
							if (transitionedStack2 != null)
							{
								ItemStack resolvedItemstack2 = transitionedStack2.ResolvedItemstack;
								if (resolvedItemstack2 != null)
								{
									resolvedItemstack2.ResolveBlockOrItem(this.game);
								}
							}
						}
					}
					GrindingProperties grindingProps = block.GrindingProps;
					bool flag5;
					if (grindingProps == null)
					{
						flag5 = null != null;
					}
					else
					{
						JsonItemStack groundStack2 = grindingProps.GroundStack;
						flag5 = ((groundStack2 != null) ? groundStack2.ResolvedItemstack : null) != null;
					}
					if (flag5)
					{
						block.GrindingProps.GroundStack.ResolvedItemstack.ResolveBlockOrItem(this.game);
					}
					CrushingProperties crushingProps = block.CrushingProps;
					bool flag6;
					if (crushingProps == null)
					{
						flag6 = null != null;
					}
					else
					{
						JsonItemStack crushedStack2 = crushingProps.CrushedStack;
						flag6 = ((crushedStack2 != null) ? crushedStack2.ResolvedItemstack : null) != null;
					}
					if (flag6)
					{
						block.CrushingProps.CrushedStack.ResolvedItemstack.ResolveBlockOrItem(this.game);
					}
					if (block.Drops != null)
					{
						for (int k = 0; k < block.Drops.Length; k++)
						{
							block.Drops[k].ResolvedItemstack.ResolveBlockOrItem(this.game);
						}
					}
					if (block.SeasonColorMap != null)
					{
						this.game.ColorMaps.TryGetValue(block.SeasonColorMap, out block.SeasonColorMapResolved);
					}
					if (block.ClimateColorMap != null)
					{
						this.game.ColorMaps.TryGetValue(block.ClimateColorMap, out block.ClimateColorMapResolved);
					}
				}
			}
			foreach (EntityProperties entityProperties in this.game.EntityTypes)
			{
				entityProperties.PopulateDrops(this.game.api.World);
			}
			this.logger.Notification("Loaded {0} block types from server", new object[] { blockCount });
			this.logger.VerboseDebug("Resolved blocks and items stacks and drops");
		}

		internal void FinaliseTextureAtlas(TextureAtlasManager manager, string type, Action onCompleted)
		{
			this.logger.VerboseDebug("Server assets - composing " + type + " texture atlas on main thread");
			manager.ComposeTextureAtlasses_StageA();
			this.logger.VerboseDebug("Server assets - done " + type + " textures composition");
			if (ClientSettings.OffThreadMipMapCreation)
			{
				TyronThreadPool.QueueTask(delegate
				{
					this.FinaliseTextureAtlas_StageB(manager, "off-thread " + type);
				});
			}
			else
			{
				this.game.EnqueueGameLaunchTask(delegate
				{
					this.FinaliseTextureAtlas_StageB(manager, type);
				}, "ServerAssetsReceived");
			}
			TyronThreadPool.QueueTask(delegate
			{
				this.FinaliseTextureAtlas_StageC(manager, type, onCompleted);
			});
		}

		private void FinaliseTextureAtlas_StageB(TextureAtlasManager manager, string type)
		{
			manager.ComposeTextureAtlasses_StageB();
			this.logger.VerboseDebug("Server assets - done " + type + " textures mipmap creation");
		}

		private void FinaliseTextureAtlas_StageC(TextureAtlasManager manager, string type, Action onCompleted)
		{
			manager.ComposeTextureAtlasses_StageC();
			this.logger.VerboseDebug("Server assets - done " + type + " textures random and average color collection");
			if (onCompleted != null)
			{
				onCompleted();
			}
		}

		internal void BeginItemTesselation()
		{
			if (this.game.disposed)
			{
				return;
			}
			this.WaitFor(ref this.game.DoneBlockAndItemShapeLoading, 12000, "block shape loading", "loading block and item shapes, it did not complete");
			if (this.game.disposed)
			{
				return;
			}
			this.game.EnqueueGameLaunchTask(delegate
			{
				this.game.TesselatorManager.TesselateItems_Pre(this.game.Items);
			}, "ServerAssetsReceived");
			this.game.EnqueueGameLaunchTask(delegate
			{
				this.HandleServerAssets_Step7(0, 1);
			}, "ServerAssetsReceived");
		}

		private void HandleServerAssets_Step7(int index, int frameCount)
		{
			index = this.game.TesselatorManager.TesselateItems(this.game.Items, index, this.game.Items.Count);
			if (index < this.game.Items.Count)
			{
				this.game.EnqueueGameLaunchTask(delegate
				{
					ClientSystemStartup <>4__this = this;
					int index2 = index;
					int num = frameCount + 1;
					frameCount = num;
					<>4__this.HandleServerAssets_Step7(index2, num);
				}, "ServerAssetsReceived");
				return;
			}
			this.logger.VerboseDebug("Server assets - done item tesselation spread over " + frameCount.ToString() + " frames");
		}

		internal void StartSlowLoadingMusicAndSounds()
		{
			this.game.MusicEngine.Initialise_SeparateThread();
			this.StartSlowLoadingSoundsWhenReady();
		}

		private void HandleServerAssets_Step9()
		{
			this.game.EnqueueGameLaunchTask(delegate
			{
				this.game.SuspendMainThreadTasks = false;
				this.game.AssetLoadingOffThread = false;
				this.game.WorldMap.BlockTexturesLoaded();
				this.game.TesselatorManager.TesselateBlocks_Pre();
				TyronThreadPool.QueueTask(delegate
				{
					this.game.TesselatorManager.TesselateBlocks_Async(this.game.Blocks);
				});
				TyronThreadPool.QueueTask(delegate
				{
					this.game.TesselatorManager.TesselateBlocksForInventory_ASync(this.game.Blocks);
				});
				this.logger.VerboseDebug("Server assets - done step 9");
				this.game.EnqueueGameLaunchTask(new Action(this.HandleServerAssets_Step10), "ServerAssetsReceived");
			}, "ServerAssetsReceived");
		}

		private void HandleServerAssets_Step10()
		{
			this.game.BlockAtlasManager.GenFramebuffer();
			this.logger.VerboseDebug("Server assets - done texture atlas frame buffer");
			this.game.EnqueueGameLaunchTask(new Action(this.HandleServerAssets_Step11), "ServerAssetsLoaded");
		}

		internal void HandleServerAssets_Step11()
		{
			for (int i = 0; i < this.pkt_srvrassets.RecipesCount; i++)
			{
				Packet_Recipes rp = this.pkt_srvrassets.Recipes[i];
				this.game.GetRecipeRegistry(rp.Code).FromBytes(this.game, rp.Quantity, rp.Data);
			}
			this.logger.Notification("Server assets loaded");
			this.pkt_srvrassets = null;
			this.FinishAssetLoadingIfUnfinished();
		}

		internal bool FinishAssetLoadingIfUnfinished()
		{
			if (Interlocked.Increment(ref this.assetsLoadedPrerequisites) == 2)
			{
				this.game.SuspendMainThreadTasks = true;
				this.logger.VerboseDebug("World configs received and block tesselation complete");
				this.game.EnqueueGameLaunchTask(new Action(this.AllAssetsLoadedAndSpawnChunkReceived), "onAllAssetsLoaded");
				this.assetsLoadedPrerequisites = 0;
				return false;
			}
			return true;
		}

		private void HandleServerReady(Packet_Server packet)
		{
			this.logger.VerboseDebug("Handling ServerReady packet");
			if (this.game.exitReason != null)
			{
				return;
			}
			this.game.modHandler.StartModsFully();
			this.game.api.Shader.ReloadShaders();
			this.logger.Notification("Reloaded shaders now with mod assets");
			if (!this.game.IsSingleplayer)
			{
				ScreenManager.CatalogSounds(new Action(this.StartSlowLoadingSoundsWhenReady));
			}
			this.game.SendRequestJoin();
		}

		private void HandleLevelInitialize(Packet_Server packet)
		{
			this.game.WorldMap.ServerChunkSize = packet.LevelInitialize.ServerChunkSize;
			this.game.WorldMap.MapChunkSize = packet.LevelInitialize.ServerMapChunkSize;
			this.game.WorldMap.regionSize = packet.LevelInitialize.ServerMapRegionSize;
			this.game.WorldMap.MaxViewDistance = packet.LevelInitialize.MaxViewDistance;
			if (this.game.WorldMap.ServerChunkSize == 0 || this.game.WorldMap.MapChunkSize == 0 || this.game.WorldMap.RegionSize == 0)
			{
				throw new Exception("Invalid server response, it sent wrong chunk/map/region sizes during LevelInitialize");
			}
			this.game.sendRuntimeSettings();
			this.logger.Notification("Received level init");
		}

		private void HandleLevelDataChunk(Packet_Server packet)
		{
			if (packet.LevelDataChunk.PercentComplete == 100 && this.FinishAssetLoadingIfUnfinished())
			{
				this.logger.VerboseDebug(this.game.IsSingleplayer ? "Received server configs and level data, but not yet completed block tesselation" : "Received 100% map level data");
				this.game.SuspendMainThreadTasks = true;
			}
		}

		internal void AllAssetsLoadedAndSpawnChunkReceived()
		{
			this.game.terrainIlluminator.OnBlockTexturesLoaded();
			this.game.EnqueueGameLaunchTask(delegate
			{
				this.OnAllAssetsLoaded_ClientSystems(0);
			}, "onAllAssetsLoaded");
		}

		internal void OnAllAssetsLoaded_ClientSystems(int i)
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			while (i < this.game.clientSystems.Length)
			{
				this.game.clientSystems[i].OnBlockTexturesLoaded();
				if (sw.ElapsedMilliseconds >= 60L)
				{
					if (sw.ElapsedMilliseconds > 65L)
					{
						this.logger.VerboseDebug("Slow to load clientSystem " + this.game.clientSystems[i].Name);
					}
					this.game.EnqueueGameLaunchTask(delegate
					{
						this.OnAllAssetsLoaded_ClientSystems(i + 1);
					}, "onAllAssetsLoaded");
					return;
				}
				int i2 = i;
				i = i2 + 1;
			}
			this.logger.VerboseDebug("Done client systems OnLoaded");
			this.game.EnqueueGameLaunchTask(delegate
			{
				this.OnAllAssetsLoaded_Blocks(0);
			}, "onAllAssetsLoaded");
		}

		internal void OnAllAssetsLoaded_Blocks(int i)
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			Action <>9__1;
			while (i < this.game.Blocks.Count)
			{
				Block block = this.game.Blocks[i];
				if (block != null)
				{
					block.OnLoadedNative(this.game.api);
				}
				if (i > 0 && i % 4 == 0 && sw.ElapsedMilliseconds >= 60L)
				{
					if (sw.ElapsedMilliseconds > 61L)
					{
						this.logger.VerboseDebug(string.Concat(new string[]
						{
							"Slow to load blocks (>1ms) ",
							this.game.Blocks[i - 4].Code,
							",",
							this.game.Blocks[i - 3].Code,
							",",
							this.game.Blocks[i - 2].Code,
							",",
							this.game.Blocks[i - 1].Code
						}));
					}
					ClientMain game = this.game;
					Action action;
					if ((action = <>9__1) == null)
					{
						action = (<>9__1 = delegate
						{
							this.OnAllAssetsLoaded_Blocks(i + 1);
						});
					}
					game.EnqueueGameLaunchTask(action, "onAllAssetsLoaded");
					return;
				}
				int i2 = i;
				i = i2 + 1;
			}
			this.logger.VerboseDebug("Done blocks OnLoaded");
			this.game.EnqueueGameLaunchTask(delegate
			{
				this.OnAllAssetsLoaded_Items(0);
			}, "onAllAssetsLoaded");
		}

		internal void OnAllAssetsLoaded_Items(int i)
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			while (i < this.game.Items.Count)
			{
				Item item = this.game.Items[i];
				if (item != null)
				{
					item.OnLoadedNative(this.game.api);
				}
				if (i > 0 && i % 4 == 0 && sw.ElapsedMilliseconds >= 60L)
				{
					if (sw.ElapsedMilliseconds > 61L)
					{
						this.logger.VerboseDebug(string.Concat(new string[]
						{
							"Slow to load items (>1ms) ",
							this.game.Items[i - 4].Code,
							",",
							this.game.Items[i - 3].Code,
							",",
							this.game.Items[i - 2].Code,
							",",
							this.game.Items[i - 1].Code
						}));
					}
					this.game.EnqueueGameLaunchTask(delegate
					{
						this.OnAllAssetsLoaded_Items(i + 1);
					}, "onAllAssetsLoaded");
					return;
				}
				int i2 = i;
				i = i2 + 1;
			}
			this.logger.VerboseDebug("Done items OnLoaded");
			this.game.EnqueueGameLaunchTask(new Action(this.WaitForBlockTesselation), "blockTesselation");
		}

		private void WaitForBlockTesselation()
		{
			if (this.game.TesselatorManager.finishedAsyncBlockTesselation < 2)
			{
				this.game.EnqueueGameLaunchTask(new Action(this.WaitForBlockTesselation), "BlockTesselation");
				return;
			}
			this.game.EnqueueGameLaunchTask(new Action(this.OnAllAssetsLoadedAndClientJoined), "OnAllAssetsLoaded");
		}

		internal void OnAllAssetsLoadedAndClientJoined()
		{
			this.game.SuspendMainThreadTasks = false;
			this.game.BlocksReceivedAndLoaded = true;
			CompositeTexture.basicTexturesCache = null;
			CompositeTexture.wildcardsCache = null;
			this.game.Platform.AssetManager.UnloadAssets(AssetCategory.textures);
			this.game.TesselatorManager.LoadDone();
			Action action;
			if ((action = ClientSystemStartup.<>O.<0>__InitialiseSearch) == null)
			{
				action = (ClientSystemStartup.<>O.<0>__InitialiseSearch = new Action(Lang.InitialiseSearch));
			}
			TyronThreadPool.QueueTask(action);
			this.logger.VerboseDebug("All clientside asset loading complete, game launch can proceed");
		}

		private void HandleLevelFinalize(Packet_Server packet)
		{
			this.logger.Notification("Received level finalize");
			this.logger.VerboseDebug("Received level finalize");
			this.game.InWorldStopwatch.Start();
			ClientSystem[] clientSystems = this.game.clientSystems;
			for (int i = 0; i < clientSystems.Length; i++)
			{
				clientSystems[i].OnLevelFinalize();
			}
			this.logger.VerboseDebug("Done level finalize clientsystems");
			this.game.api.eventapi.TriggerLevelFinalize();
			if (ClientSettings.PauseGameOnLostFocus && !this.game.Platform.IsFocused)
			{
				ClientEventManager eventManager = this.game.eventManager;
				if (eventManager != null)
				{
					eventManager.AddDelayedCallback(delegate(float dt)
					{
						if (this.game.Platform.IsFocused)
						{
							return;
						}
						if (this.game.IsSingleplayer && !this.game.OpenedToLan)
						{
							this.logger.Notification("Window not focused. Pausing game.");
							this.game.PauseGame(true);
						}
						ScreenManager.hotkeyManager.HotKeys["escapemenudialog"].Handler(new KeyCombination());
					}, 1000L);
				}
			}
			this.logger.VerboseDebug("Done level finalize");
			this.game.AmbientManager.LateInit();
			if (GL.GetString(StringName.Renderer).Contains("Arc(TM)") && ClientSettings.AllowSSBOs)
			{
				ClientEventManager eventManager2 = this.game.eventManager;
				if (eventManager2 == null)
				{
					return;
				}
				eventManager2.AddDelayedCallback(delegate(float dt)
				{
					this.game.api.ShowChatMessage(Lang.Get("advise-intelarc-ssbos", new object[] { ".cf allowSSBOs off" }));
				}, 3000L);
			}
		}

		internal static bool ReceiveAssetsPacketDirectly(Packet_Server packet)
		{
			if (ClientSystemStartup.instance == null || ClientSystemStartup.instance.game == null)
			{
				return false;
			}
			ClientSystemStartup.instance.HandleServerAssets(packet);
			return true;
		}

		internal static bool ReceiveServerPacketDirectly(Packet_Server packet)
		{
			if (ClientSystemStartup.instance == null || ClientSystemStartup.instance.game == null)
			{
				return false;
			}
			ServerPacketHandler<Packet_Server> handler = ClientSystemStartup.instance.game.PacketHandlers[packet.Id];
			if (handler == null)
			{
				return false;
			}
			if (packet.Id == 73)
			{
				ClientSystemStartup.instance.game.ServerReady = true;
			}
			ClientSystemStartup.instance.game.EnqueueMainThreadTask(delegate
			{
				handler(packet);
			}, "readpacket" + packet.Id.ToString());
			return true;
		}

		internal void WaitFor(ref bool flag, int timeOut, string logMessage, string logError)
		{
			bool logged = false;
			while (!flag && timeOut-- > 0 && !this.game.disposed)
			{
				if (!logged)
				{
					this.logger.VerboseDebug("Waiting for " + logMessage);
					logged = true;
				}
				Thread.Sleep(10);
			}
			if (!this.game.disposed && (timeOut <= 0 || !flag))
			{
				this.logger.Fatal("The game probably cannot continue to launch.  There was a problem " + logError);
				return;
			}
			if (logged && flag)
			{
				this.logger.VerboseDebug("Done " + logMessage);
			}
		}

		public override string Name
		{
			get
			{
				return "startup";
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		public override void Dispose(ClientMain game)
		{
			int timeOut = 250;
			while (game.AssetLoadingOffThread && timeOut-- > 0)
			{
				Thread.Sleep(10);
			}
			ClientSystemStartup.instance = null;
		}

		public static ClientSystemStartup instance;

		private int assetsLoadedPrerequisites;

		private volatile int blockShapesPrerequisites;

		private volatile int itemShapesPrerequisites;

		private volatile int entityShapesPrerequisites;

		public volatile int loadSoundsSlowPrerequisites;

		private Thread collectionThreadBlockAtlas;

		private Thread collectionThreadEntityAtlas;

		private Thread collectionThreadItemAtlas;

		private bool lightLevelsReceived;

		private Packet_ServerAssets pkt_srvrassets;

		private int multiplayerAuthAttempts;

		private string multiplayerAuthRetryToken;

		[CompilerGenerated]
		private static class <>O
		{
			public static Action <0>__InitialiseSearch;
		}
	}
}
