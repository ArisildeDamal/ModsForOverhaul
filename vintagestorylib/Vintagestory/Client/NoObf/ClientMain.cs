using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Database;

namespace Vintagestory.Client.NoObf
{
	public sealed class ClientMain : GameMain, IWorldIntersectionSupplier, IClientWorldAccessor, IWorldAccessor
	{
		public override IWorldAccessor World
		{
			get
			{
				return this;
			}
		}

		protected override WorldMap worldmap
		{
			get
			{
				return this.WorldMap;
			}
		}

		public int Seed
		{
			get
			{
				return this.ServerInfo.Seed;
			}
		}

		public string SavegameIdentifier
		{
			get
			{
				return this.ServerInfo.SavegameIdentifier;
			}
		}

		FrameProfilerUtil IWorldAccessor.FrameProfiler
		{
			get
			{
				return ScreenManager.FrameProfiler;
			}
		}

		public long InWorldEllapsedMs
		{
			get
			{
				return this.InWorldStopwatch.ElapsedMilliseconds;
			}
		}

		public bool LiquidSelectable
		{
			get
			{
				return this.forceLiquidSelectable;
			}
		}

		public bool AmbientParticles
		{
			get
			{
				return ClientSettings.AmbientParticles;
			}
			set
			{
				ClientSettings.AmbientParticles = value;
			}
		}

		public IClientPlayer Player
		{
			get
			{
				return this.player;
			}
		}

		public EntityPlayer EntityPlayer
		{
			get
			{
				ClientPlayer clientPlayer = this.player;
				if (clientPlayer == null)
				{
					return null;
				}
				return clientPlayer.Entity;
			}
		}

		public override ClassRegistry ClassRegistryInt
		{
			get
			{
				return ClientMain.ClassRegistry;
			}
			set
			{
				ClientMain.ClassRegistry = value;
			}
		}

		public EntityPos DefaultSpawnPosition
		{
			get
			{
				return this.SpawnPosition;
			}
		}

		public ClientPlayer GetPlayerFromEntityId(long entityId)
		{
			return this.PlayersByUid.Values.FirstOrDefault((ClientPlayer player) => player.Entity != null && player.Entity.EntityId == entityId);
		}

		public ClientPlayer GetPlayerFromClientId(int clientId)
		{
			return this.PlayersByUid.Values.FirstOrDefault((ClientPlayer player) => player.ClientId == clientId);
		}

		public BlockSelection BlockSelection
		{
			get
			{
				EntityPlayer entityPlayer = this.EntityPlayer;
				if (entityPlayer == null)
				{
					return null;
				}
				return entityPlayer.BlockSelection;
			}
			set
			{
				if (this.EntityPlayer != null)
				{
					this.EntityPlayer.BlockSelection = value;
				}
			}
		}

		public EntitySelection EntitySelection
		{
			get
			{
				EntityPlayer entityPlayer = this.EntityPlayer;
				if (entityPlayer == null)
				{
					return null;
				}
				return entityPlayer.EntitySelection;
			}
			set
			{
				if (this.EntityPlayer != null)
				{
					this.EntityPlayer.EntitySelection = value;
				}
			}
		}

		public long[] LoadedChunkIndices
		{
			get
			{
				return this.WorldMap.chunks.Keys.ToArray<long>();
			}
		}

		public long[] LoadedMapChunkIndices
		{
			get
			{
				return this.WorldMap.MapChunks.Keys.ToArray<long>();
			}
		}

		public Dictionary<long, IWorldChunk> GetAllChunks()
		{
			Dictionary<long, IWorldChunk> dict = new Dictionary<long, IWorldChunk>();
			object chunksLock = this.WorldMap.chunksLock;
			lock (chunksLock)
			{
				foreach (KeyValuePair<long, ClientChunk> val in this.WorldMap.chunks)
				{
					if (val.Key < 4503599627370496L)
					{
						dict[val.Key] = val.Value;
					}
				}
			}
			return dict;
		}

		public ClientMain(GuiScreenRunningGame screenRunningGame, ClientPlatformAbstract platform)
		{
			ClientMain <>4__this = this;
			RuntimeStats.Reset();
			this.Platform = platform;
			ShapeElement.Logger = platform.Logger;
			this.ScreenRunningGame = screenRunningGame;
			this.eventManager = new ClientEventManager(this);
			this.MainCamera = new PlayerCamera(this);
			this.WorldMap = new ClientWorldMap(this);
			this.terrainIlluminator = new TerrainIlluminator(this);
			this.AmbientManager = new AmbientManager(this);
			this.BlockAtlasManager = new BlockTextureAtlasManager(this);
			this.ItemAtlasManager = new ItemTextureAtlasManager(this);
			this.EntityAtlasManager = new EntityTextureAtlasManager(this);
			this.TesselatorManager = new ShapeTesselatorManager(this);
			MeshData.Recycler = new MeshDataRecycler(this);
			if (ClientSettings.Inst.Bool.Get("disableMeshRecycler", false))
			{
				MeshData.Recycler.Dispose();
			}
			this.DebugScreenInfo = new OrderedDictionary<string, string>();
			this.DebugScreenInfo["fps"] = "";
			this.DebugScreenInfo["mem"] = "";
			this.DebugScreenInfo["triangles"] = "";
			this.DebugScreenInfo["occludedchunks"] = "";
			this.DebugScreenInfo["gpumemfrag"] = "";
			this.DebugScreenInfo["chunkstats"] = "";
			this.DebugScreenInfo["position"] = "";
			this.DebugScreenInfo["chunkpos"] = "";
			this.DebugScreenInfo["regpos"] = "";
			this.DebugScreenInfo["orientation"] = "";
			this.DebugScreenInfo["curblock"] = "";
			this.DebugScreenInfo["curblockentity"] = "";
			this.DebugScreenInfo["curblocklight"] = "";
			this.DebugScreenInfo["curblocklight2"] = "";
			this.DebugScreenInfo["entitycount"] = "";
			this.DebugScreenInfo["quadparticlepool"] = "";
			this.DebugScreenInfo["cubeparticlepool"] = "";
			this.DebugScreenInfo["incomingbytes"] = "";
			this.AutoJumpEnabled = false;
			this.MvMatrix = new StackMatrix4(1024);
			this.PMatrix = new StackMatrix4(1024);
			this.MvMatrix.Push(Mat4d.Create());
			this.PMatrix.Push(Mat4d.Create());
			this.whitetexture = -1;
			this.ShouldRender2DOverlays = true;
			this.AllowFreemove = true;
			this.AllowCameraControl = true;
			this.texturesByLocation = new Dictionary<AssetLocation, LoadedTexture>();
			this.FrameRateMode = 0;
			this.MainCamera.Fov = (float)ClientSettings.FieldOfView * 0.017453292f;
			ClientSettings.Inst.AddWatcher<int>("leftDialogMargin", delegate(int newvalue)
			{
				GuiStyle.LeftDialogMargin = newvalue;
			});
			ClientSettings.Inst.AddWatcher<int>("rightDialogMargin", delegate(int newvalue)
			{
				GuiStyle.RightDialogMargin = newvalue;
			});
			GuiStyle.LeftDialogMargin = ClientSettings.LeftDialogMargin;
			GuiStyle.RightDialogMargin = ClientSettings.RightDialogMargin;
			ClientSettings.Inst.AddWatcher<int>("fieldOfView", new OnSettingsChanged<int>(this.OnFowChanged));
			ClientSettings.Inst.AddWatcher<bool>("extendedDebugInfo", delegate(bool newvalue)
			{
				<>4__this.extendedDebugInfo = newvalue;
			});
			this.EntityDebugMode = ClientSettings.ShowEntityDebugInfo;
			ClientSettings.Inst.AddWatcher<bool>("showEntityDebugInfo", delegate(bool newvalue)
			{
				<>4__this.EntityDebugMode = newvalue;
			});
			ClientSettings.Inst.AddWatcher<bool>("showBlockInfo", delegate(bool newvalue)
			{
				<>4__this.Drawblockinfo = newvalue;
			});
			ClientSettings.Inst.AddWatcher<float>("ssaa", delegate(float newvalue)
			{
				platform.RebuildFrameBuffers();
			});
			ClientSettings.Inst.AddWatcher<int>("ssaoQuality", delegate(int newvalue)
			{
				ShaderRegistry.ReloadShaders();
				platform.RebuildFrameBuffers();
				ClientEventManager clientEventManager2 = <>4__this.eventManager;
				if (clientEventManager2 == null)
				{
					return;
				}
				clientEventManager2.TriggerReloadShaders();
			});
			ClientSettings.Inst.AddWatcher<float>("minbrightness", delegate(float newvalue)
			{
				ShaderRegistry.ReloadShaders();
				ClientEventManager clientEventManager3 = <>4__this.eventManager;
				if (clientEventManager3 == null)
				{
					return;
				}
				clientEventManager3.TriggerReloadShaders();
			});
			ClientSettings.Inst.AddWatcher<float>("guiScale", delegate(float val)
			{
				<>4__this.GuiComposers.MarkAllDialogsForRecompose();
			});
			this.extendedDebugInfo = ClientSettings.ExtendedDebugInfo;
			this.particleLevel = ClientSettings.ParticleLevel;
			ClientSettings.Inst.AddWatcher<int>("particleLevel", delegate(int val)
			{
				<>4__this.particleLevel = val;
			});
			ClientSettings.Inst.AddWatcher<bool>("immersiveFpMode", delegate(bool newvalue)
			{
				<>4__this.sendRuntimeSettings();
			});
			ClientSettings.Inst.AddWatcher<int>("itemCollectMode", delegate(int newvalue)
			{
				<>4__this.sendRuntimeSettings();
			});
			ClientSettings.Inst.AddWatcher<bool>("renderMetaBlocks", delegate(bool newvalue)
			{
				<>4__this.player.worlddata.RenderMetablocks = newvalue;
				<>4__this.player.worlddata.RequestMode(<>4__this);
			});
			this.rotationspeed = 0.15f;
			this.swimmingMouseSmoothing = ClientSettings.SwimmingMouseSmoothing;
			ClientSettings.Inst.AddWatcher<float>("swimmingMouseSmoothing", delegate(float newvalue)
			{
				<>4__this.swimmingMouseSmoothing = newvalue;
			});
			this.KeyboardState = new bool[512];
			this.PreviousKeyboardState = new bool[512];
			this.KeyboardStateRaw = new bool[512];
			for (int i = 0; i < 512; i++)
			{
				this.KeyboardState[i] = false;
				this.KeyboardStateRaw[i] = false;
				this.PreviousKeyboardState[i] = false;
			}
			this.glScaleTempVec3 = Vec3Utilsd.Create();
			this.glRotateTempVec3 = Vec3Utilsd.Create();
			this.identityMatrix = Mat4d.Identity(Mat4d.Create());
			this.set3DProjectionTempMat4 = Mat4d.Create();
			this.PacketHandlers = new ServerPacketHandler<Packet_Server>[256];
			this.csStartup = new ClientSystemStartup(this);
			this.MainThreadTasks = new Queue<ClientTask>();
			this.GameLaunchTasks = new Queue<ClientTask>();
			ClientSettings.Inst.AddWatcher<int>("viewDistance", new OnSettingsChanged<int>(this.ViewDistanceChanged));
			ClientSettings.Inst.AddWatcher<int>("vsyncMode", new OnSettingsChanged<int>(this.OnVsyncChanged));
			this.api = new ClientCoreAPI(this);
			this.GuiComposers = new GuiComposerManager(this.api);
			this.ColorPreset = new ColorPresets(this, this.api);
			this.TagRegistry.Side = EnumAppSide.Client;
			ClientEventManager clientEventManager = this.eventManager;
			if (clientEventManager == null)
			{
				return;
			}
			clientEventManager.AddGameTickListener(new Action<float>(this.freeMouseOnDefocus), 500, 0);
		}

		public void sendRuntimeSettings()
		{
			this.SendPacketClient(new Packet_Client
			{
				Id = 32,
				RuntimeSetting = new Packet_RuntimeSetting
				{
					ImmersiveFpMode = ((ClientSettings.ImmersiveFpMode > false) ? 1 : 0),
					ItemCollectMode = ClientSettings.ItemCollectMode
				}
			});
		}

		private void freeMouseOnDefocus(float t1)
		{
			if (!this.Platform.IsFocused && this.Platform.MouseGrabbed)
			{
				this.MouseGrabbed = false;
			}
		}

		private void OnFowChanged(int newValue)
		{
			this.MainCamera.Fov = (float)ClientSettings.FieldOfView * 0.017453292f;
			this.MainCamera.ZNear = GameMath.Clamp(0.1f - (float)ClientSettings.FieldOfView / 90f / 25f, 0.025f, 0.1f);
			this.Reset3DProjection();
		}

		public void Start()
		{
			Compression.Reset();
			this.Platform.ResetGamePauseAndUptimeState();
			this.disconnectAction = null;
			this.disconnectMissingMods = null;
			this.quadModel = this.Platform.UploadMesh(QuadMeshUtilExt.GetQuadModelData());
			FrustumCulling frustumculling = new FrustumCulling();
			this.frustumCuller = frustumculling;
			this.frustumCuller.UpdateViewDistance(ClientSettings.ViewDistance);
			ChunkTesselator terrainchunktesselator = new ChunkTesselator(this);
			this.TerrainChunkTesselator = terrainchunktesselator;
			this.Platform.AddOnCrash(OnCrashHandlerLeave.Create(this));
			this.rand = new ThreadLocal<Random>(() => new Random(Environment.TickCount));
			this.macroManager = new MacroManager(this);
			this.Platform.ShaderUniforms = this.shUniforms;
			this._clientThreadsCts = new CancellationTokenSource();
			ClientSystem compresschunks = new SystemCompressChunks(this);
			Thread thread = new Thread(new ThreadStart(new ClientThread(this, "compresschunks", new ClientSystem[] { compresschunks }, this._clientThreadsCts.Token).Process));
			thread.IsBackground = true;
			thread.Start();
			thread.Name = "compresschunks";
			this.clientThreads.Add(thread);
			ClientSystem blockTicking = new SystemClientTickingBlocks(this);
			Thread thread2 = new Thread(new ThreadStart(new ClientThread(this, "blockticking", new ClientSystem[] { blockTicking }, this._clientThreadsCts.Token).Process));
			thread2.IsBackground = true;
			thread2.Start();
			thread2.Name = "blockticking";
			this.clientThreads.Add(thread2);
			ClientSystem relight = new ClientSystemRelight(this);
			Thread thread3 = new Thread(new ThreadStart(new ClientThread(this, "relight", new ClientSystem[] { relight }, this._clientThreadsCts.Token).Process));
			thread3.IsBackground = true;
			thread3.Start();
			thread3.Name = "relight";
			this.clientThreads.Add(thread3);
			ClientSystem tesselateterrain = new ChunkTesselatorManager(this);
			Thread thread4 = new Thread(new ThreadStart(new ClientThread(this, "tesselateterrain", new ClientSystem[] { tesselateterrain }, this._clientThreadsCts.Token).Process));
			thread4.IsBackground = true;
			thread4.Start();
			thread4.Name = "tesselateterrain";
			this.clientThreads.Add(thread4);
			ClientSystem chunkvis = new SystemChunkVisibilityCalc(this);
			Thread thread5 = new Thread(new ThreadStart(new ClientThread(this, "chunkvis", new ClientSystem[] { chunkvis }, this._clientThreadsCts.Token).Process));
			thread5.IsBackground = true;
			thread5.Start();
			thread5.Name = "chunkvis";
			this.clientThreads.Add(thread5);
			this.networkProc = new SystemNetworkProcess(this);
			Thread thread6 = new Thread(new ThreadStart(new ClientThread(this, "networkproc", new ClientSystem[] { this.networkProc }, this._clientThreadsCts.Token).Process));
			thread6.IsBackground = true;
			thread6.Start();
			thread6.Name = "networkproc";
			this.clientThreads.Add(thread6);
			ClientSystem rendTerra = new SystemRenderTerrain(this);
			Thread thread7 = new Thread(new ThreadStart(new ClientThread(this, "chunkculling", new ClientSystem[] { rendTerra }, this._clientThreadsCts.Token).Process));
			thread7.IsBackground = true;
			thread7.Start();
			thread7.Name = "chunkculling";
			this.clientThreads.Add(thread7);
			ClientSystem particleSimulation = new SystemRenderParticles(this);
			Thread thread8 = new Thread(new ThreadStart(new ClientThread(this, "asyncparticles", new ClientSystem[] { particleSimulation }, this._clientThreadsCts.Token).Process));
			thread8.IsBackground = true;
			thread8.Start();
			thread8.Name = "asyncparticles";
			this.clientThreads.Add(thread8);
			if (PhysicsBehaviorBase.collisionTester == null)
			{
				PhysicsBehaviorBase.collisionTester = new CachingCollisionTester();
			}
			new GeneralPacketHandler(this);
			new ClientSystemDebugCommands(this);
			new ClientSystemEntities(this);
			new SystemClientCommands(this);
			this.modHandler = new SystemModHandler(this);
			this.MusicEngineCts = new CancellationTokenSource();
			this.clientSystems = new ClientSystem[]
			{
				new SystemSoundEngine(this),
				this.modHandler,
				new ClientSystemIntroMenu(this),
				new SystemHotkeys(this),
				this.api.guiapi.guimgr = new GuiManager(this),
				this.MusicEngine = new SystemMusicEngine(this, this.MusicEngineCts.Token),
				this.networkProc,
				compresschunks,
				tesselateterrain,
				relight,
				chunkvis,
				new SystemRenderFrameBufferDebug(this),
				new SystemUnloadChunks(this),
				new SystemPlayerSounds(this),
				new SystemRenderSkyColor(this),
				new SystemRenderNightSky(this),
				new SystemRenderSunMoon(this),
				new SystemCinematicCamera(this),
				new SystemRenderShadowMap(this),
				new SystemRenderDebugWireframes(this),
				rendTerra,
				new SystemRenderRiftTest(this),
				new SystemRenderEntities(this),
				new SystemRenderDecals(this),
				particleSimulation,
				new SystemRenderPlayerEffects(this),
				new SystemRenderInsideBlock(this),
				new SystemHighlightBlocks(this),
				new SystemSelectedBlockOutline(this),
				new SystemMouseInWorldInteractions(this),
				new SystemPlayerControl(this),
				new SystemRenderAim(this),
				new SystemRenderPlayerAimAcc(this),
				new SystemScreenshot(this),
				new SystemPlayerEnvAwarenessTracker(this),
				new SystemCalendar(this),
				new SystemVideoRecorder(this),
				blockTicking,
				this.csStartup
			};
			ScreenManager.Platform.CheckGlError("init end");
			this.LastReceivedMilliseconds = this.Platform.EllapsedMs;
			this.Platform.GlDepthMask(true);
			this.Platform.GlEnableDepthTest();
			this.Platform.GlCullFaceBack();
			this.Platform.GlEnableCullFace();
			ScreenManager.Platform.CheckGlError(null);
			this.Reset3DProjection();
			ScreenManager.Platform.CheckGlError(null);
		}

		public void OnOwnPlayerDataReceived()
		{
			this.ScreenRunningGame.BubbleUpEvent("maploaded");
			this.EntityPlayer.PhysicsUpdateWatcher = new PhysicsTickDelegate(this.MainCamera.OnPlayerPhysicsTick);
			this.EntityPlayer.SetCurrentlyControlledPlayer();
			for (int i = 0; i < this.clientSystems.Length; i++)
			{
				this.clientSystems[i].OnOwnPlayerDataReceived();
			}
		}

		public void MainGameLoop(float deltaTime)
		{
			if (this.disposed)
			{
				return;
			}
			if (this.KillNextFrame)
			{
				this.SendLeave(0);
				this.exitReason = "client thread crash";
				this.DestroyGameSession(false);
				this.KillNextFrame = false;
				return;
			}
			if (this.DeltaTimeLimiter > 0f)
			{
				deltaTime = this.DeltaTimeLimiter;
			}
			this.MainRenderLoop(deltaTime);
			this.ExecuteMainThreadTasks(deltaTime);
		}

		public void ExecuteMainThreadTasks(float deltaTime)
		{
			ScreenManager.FrameProfiler.Mark("beginMTT");
			if (this.GameLaunchTasks.Count > 0)
			{
				this.GameLaunchTasks.Dequeue().Action();
				return;
			}
			if (this.SuspendMainThreadTasks)
			{
				return;
			}
			object mainThreadTasksLock = this.MainThreadTasksLock;
			lock (mainThreadTasksLock)
			{
				while (this.MainThreadTasks.Count > 0)
				{
					this.reversedQueue.Enqueue(this.MainThreadTasks.Dequeue());
				}
				goto IL_00CA;
			}
			IL_007F:
			ClientTask task = this.reversedQueue.Dequeue();
			task.Action();
			if (this.extendedDebugInfo)
			{
				ScreenManager.FrameProfiler.Mark(task.Code);
			}
			if (this.SuspendMainThreadTasks && this.reversedQueue.Count > 0)
			{
				this.requeueTasks();
			}
			IL_00CA:
			if (this.reversedQueue.Count <= 0)
			{
				ScreenManager.FrameProfiler.Mark("doneMTT");
				return;
			}
			goto IL_007F;
		}

		private void requeueTasks()
		{
			object mainThreadTasksLock = this.MainThreadTasksLock;
			lock (mainThreadTasksLock)
			{
				while (this.MainThreadTasks.Count > 0)
				{
					this.holdingQueue.Enqueue(this.MainThreadTasks.Dequeue());
				}
				while (this.reversedQueue.Count > 0)
				{
					this.MainThreadTasks.Enqueue(this.reversedQueue.Dequeue());
				}
				while (this.holdingQueue.Count > 0)
				{
					this.MainThreadTasks.Enqueue(this.holdingQueue.Dequeue());
				}
			}
		}

		public void TriggerRenderStage(EnumRenderStage stage, float dt)
		{
			ScreenManager.FrameProfiler.Mark("beginrenderstage-", stage);
			this.currentRenderStage = stage;
			ClientEventManager clientEventManager = this.eventManager;
			if (clientEventManager != null)
			{
				clientEventManager.TriggerRenderStage(stage, dt);
			}
			this.Platform.CheckGlError("After render stage " + stage.ToString());
		}

		public void MainRenderLoop(float dt)
		{
			ScreenManager.FrameProfiler.Mark("mrl");
			if ((this.timelapsedCurrent += this.timelapse) > this.timelapseEnd)
			{
				this.timelapse = 0f;
				this.timelapsedCurrent = 0f;
				this.timelapseEnd = float.MaxValue;
				this.ShouldRender2DOverlays = true;
			}
			this.GameWorldCalendar.Timelapse = this.timelapsedCurrent;
			this.UpdateResize();
			this.UpdateFreeMouse();
			this.UpdateCameraYawPitch(dt);
			EntityPlayer entityPlayer = this.EntityPlayer;
			if (((entityPlayer != null) ? entityPlayer.Pos : null) != null)
			{
				this.shUniforms.FlagFogDensity = this.AmbientManager.BlendedFlatFogDensity;
				this.shUniforms.FlatFogStartYPos = this.AmbientManager.BlendedFlatFogYPosForShader;
			}
			if (!this.IsPaused)
			{
				ClientEventManager clientEventManager = this.eventManager;
				if (clientEventManager != null)
				{
					clientEventManager.TriggerGameTick(this.InWorldEllapsedMs, this);
				}
			}
			ScreenManager.FrameProfiler.Mark("gametick");
			if (this.LagSimulation)
			{
				this.Platform.ThreadSpinWait(10000000);
			}
			this.shUniforms.Update(dt, this.api);
			this.shUniforms.ZNear = this.MainCamera.ZNear;
			this.shUniforms.ZFar = this.MainCamera.ZFar;
			this.TriggerRenderStage(EnumRenderStage.Before, dt);
			this.Platform.GlEnableDepthTest();
			this.Platform.GlDepthMask(true);
			ScreenManager.FrameProfiler.Mark("rendOpaque-12before");
			if (this.AmbientManager.ShadowQuality > 0 && (double)this.AmbientManager.DropShadowIntensity > 0.01)
			{
				this.TriggerRenderStage(EnumRenderStage.ShadowFar, dt);
				this.TriggerRenderStage(EnumRenderStage.ShadowFarDone, dt);
				if (this.AmbientManager.ShadowQuality > 1)
				{
					this.TriggerRenderStage(EnumRenderStage.ShadowNear, dt);
					this.TriggerRenderStage(EnumRenderStage.ShadowNearDone, dt);
				}
			}
			ScreenManager.FrameProfiler.Mark("rendOpaque-3shadows");
			this.GlMatrixModeModelView();
			this.GlLoadMatrix(this.MainCamera.CameraMatrix);
			double[] pmat = this.api.Render.PMatrix.Top;
			double[] mvmat = this.api.Render.MvMatrix.Top;
			for (int i = 0; i < 16; i++)
			{
				this.PerspectiveProjectionMat[i] = pmat[i];
				this.PerspectiveViewMat[i] = mvmat[i];
			}
			this.frustumCuller.CalcFrustumEquations(this.player.Entity.Pos.AsBlockPos, pmat, mvmat);
			float lod0Bias = (float)Math.Min(640, ClientSettings.ViewDistance) * ClientSettings.LodBias;
			float lod2Bias = (float)Math.Min(640, ClientSettings.ViewDistance) * ClientSettings.LodBiasFar;
			if (ClientSettings.ViewDistance <= 64)
			{
				lod2Bias = (float)ClientSettings.ViewDistance;
			}
			this.frustumCuller.lod0BiasSq = lod0Bias * lod0Bias;
			this.frustumCuller.lod2BiasSq = (double)(lod2Bias * lod2Bias);
			ScreenManager.FrameProfiler.Mark("rendOpaque-4setup");
			this.TriggerRenderStage(EnumRenderStage.Opaque, dt);
			ScreenManager.FrameProfiler.Mark("rendOpaque-4");
			if (this.doTransparentRenderPass)
			{
				ScreenManager.FrameProfiler.Enter("rendTransparent");
				this.Platform.LoadFrameBuffer(EnumFrameBuffer.Transparent);
				ScreenManager.FrameProfiler.Mark("rendTransp-fbloaded");
				this.Platform.ClearFrameBuffer(EnumFrameBuffer.Transparent);
				ScreenManager.FrameProfiler.Mark("rendTransp-bufscleared");
				this.TriggerRenderStage(EnumRenderStage.OIT, dt);
				this.Platform.UnloadFrameBuffer(EnumFrameBuffer.Transparent);
				ScreenManager.FrameProfiler.Mark("rendTranspDone");
				this.Platform.MergeTransparentRenderPass();
				ScreenManager.FrameProfiler.Leave();
			}
			this.Platform.GlDepthMask(true);
			this.Platform.GlEnableDepthTest();
			this.Platform.GlCullFaceBack();
			this.Platform.GlEnableCullFace();
			this.TriggerRenderStage(EnumRenderStage.AfterOIT, dt);
		}

		public void RenderAfterPostProcessing(float dt)
		{
			if (this.DeltaTimeLimiter > 0f)
			{
				dt = this.DeltaTimeLimiter;
			}
			this.TriggerRenderStage(EnumRenderStage.AfterPostProcessing, dt);
		}

		public void RenderAfterFinalComposition(float dt)
		{
			if (this.DeltaTimeLimiter > 0f)
			{
				dt = this.DeltaTimeLimiter;
			}
			this.TriggerRenderStage(EnumRenderStage.AfterFinalComposition, dt);
			this.Platform.CheckGlErrorAlways("after final compo ");
		}

		public void RenderAfterBlit(float dt)
		{
			if (this.DeltaTimeLimiter > 0f)
			{
				dt = this.DeltaTimeLimiter;
			}
			this.TriggerRenderStage(EnumRenderStage.AfterBlit, dt);
			this.Platform.CheckGlErrorAlways("after blit");
		}

		public void RenderToDefaultFramebuffer(float dt)
		{
			if (this.DeltaTimeLimiter > 0f)
			{
				dt = this.DeltaTimeLimiter;
			}
			if (this.ShouldRender2DOverlays)
			{
				this.guiShaderProg = ShaderPrograms.Gui;
				this.guiShaderProg.Use();
				this.OrthoMode(this.Platform.WindowSize.Width, this.Platform.WindowSize.Height, false);
				this.TriggerRenderStage(EnumRenderStage.Ortho, dt);
				this.guiShaderProg.Stop();
			}
			ScreenManager.FrameProfiler.Mark("rendOrthoDone");
			this.PerspectiveMode();
			this.Platform.GlDepthFunc(EnumDepthFunction.Less);
			this.TriggerRenderStage(EnumRenderStage.Done, dt);
			ScreenManager.FrameProfiler.Mark("finfr");
			this.tickSummary = ScreenManager.FrameProfiler.summary;
		}

		public void Render2DBitmapFile(AssetLocation filename, float x, float y, float w, float h)
		{
			if (this.tmpTex == null)
			{
				this.tmpTex = new LoadedTexture(this.api);
			}
			this.GetOrLoadCachedTexture(filename, ref this.tmpTex);
			this.Render2DTexture(this.tmpTex.TextureId, x, y, w, h, 10f, null);
		}

		public void Render2DLoadedTexture(LoadedTexture texture, float posX, float posY, float z = 50f, Vec4f color = null)
		{
			this.Render2DTexture(texture.TextureId, posX, posY, (float)texture.Width, (float)texture.Height, z, color);
		}

		public void RenderTextureIntoTexture(LoadedTexture fromTexture, LoadedTexture intoTexture, float x1, float y1)
		{
			this.RenderTextureIntoTexture(fromTexture, 0f, 0f, (float)fromTexture.Width, (float)fromTexture.Height, intoTexture, x1, y1, 0.005f);
		}

		public FrameBufferRef CreateFrameBuffer(LoadedTexture intoTexture)
		{
			return this.Platform.CreateFramebuffer(new FramebufferAttrs("Render2DLoadedTexture", intoTexture.Width, intoTexture.Height)
			{
				Attachments = new FramebufferAttrsAttachment[]
				{
					new FramebufferAttrsAttachment
					{
						AttachmentType = EnumFramebufferAttachment.ColorAttachment0,
						Texture = new RawTexture
						{
							Width = intoTexture.Width,
							Height = intoTexture.Height,
							TextureId = intoTexture.TextureId
						}
					}
				}
			});
		}

		public void RenderTextureIntoTexture(LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, LoadedTexture intoTexture, float targetX, float targetY, float alphaTest = 0.005f)
		{
			FrameBufferRef fb = this.CreateFrameBuffer(intoTexture);
			this.RenderTextureIntoFrameBuffer(0, fromTexture, sourceX, sourceY, sourceWidth, sourceHeight, fb, targetX, targetY, alphaTest);
			this.DestroyFrameBuffer(fb);
		}

		public void DestroyFrameBuffer(FrameBufferRef fb)
		{
			this.Platform.DisposeFrameBuffer(fb, false);
		}

		public void RenderTextureIntoFrameBuffer(int atlasTextureId, LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, FrameBufferRef fb, float targetX, float targetY, float alphaTest = 0.005f)
		{
			if (this.disposed)
			{
				return;
			}
			ShaderProgramBase oldprog = ShaderProgramBase.CurrentShaderProgram;
			if (oldprog != null)
			{
				oldprog.Stop();
			}
			ShaderProgramTexture2texture texture2texture = ShaderPrograms.Texture2texture;
			if (atlasTextureId == 0)
			{
				this.Platform.LoadFrameBuffer(fb);
			}
			else
			{
				this.Platform.LoadFrameBuffer(fb, atlasTextureId);
			}
			this.Platform.GlDisableDepthTest();
			this.Platform.GlToggleBlend(alphaTest >= 0f, EnumBlendMode.Standard);
			texture2texture.Use();
			texture2texture.Tex2d2D = fromTexture.TextureId;
			texture2texture.Texu = sourceX / (float)fromTexture.Width;
			texture2texture.Texv = sourceY / (float)fromTexture.Height;
			texture2texture.Texw = sourceWidth / (float)fromTexture.Width;
			texture2texture.Texh = sourceHeight / (float)fromTexture.Height;
			texture2texture.AlphaTest = alphaTest;
			texture2texture.Xs = targetX / (float)fb.Width;
			texture2texture.Ys = targetY / (float)fb.Height;
			texture2texture.Width = sourceWidth / (float)fb.Width;
			texture2texture.Height = sourceHeight / (float)fb.Height;
			this.Platform.RenderMesh(this.quadModel);
			this.Platform.GlEnableDepthTest();
			this.Platform.LoadFrameBuffer((((oldprog != null) ? oldprog.PassName : null) == "gui") ? EnumFrameBuffer.Default : EnumFrameBuffer.Primary);
			texture2texture.Stop();
			if (oldprog != null)
			{
				oldprog.Use();
			}
		}

		public void Render2DTexture(int textureid, float x1, float y1, float width, float height, float z = 10f, Vec4f color = null)
		{
			this.Render2DTexture(this.quadModel, textureid, x1, y1, width, height, z, color);
		}

		public void Render2DTexture(MeshRef quadModel, int textureid, float x1, float y1, float width, float height, float z = 10f, Vec4f color = null)
		{
			this.guiShaderProg.RgbaIn = ((color == null) ? ColorUtil.WhiteArgbVec : color);
			this.guiShaderProg.ExtraGlow = 0;
			this.guiShaderProg.ApplyColor = 0;
			this.guiShaderProg.NoTexture = 0f;
			this.guiShaderProg.Tex2d2D = textureid;
			this.guiShaderProg.OverlayOpacity = 0f;
			this.guiShaderProg.NormalShaded = 0;
			this.GlPushMatrix();
			this.GlTranslate((double)x1, (double)y1, (double)z);
			this.GlScale((double)width, (double)height, 0.0);
			this.GlScale(0.5, 0.5, 0.0);
			this.GlTranslate(1.0, 1.0, 0.0);
			this.guiShaderProg.ProjectionMatrix = this.CurrentProjectionMatrix;
			this.guiShaderProg.ModelViewMatrix = this.CurrentModelViewMatrix;
			this.Platform.RenderMesh(quadModel);
			this.GlPopMatrix();
		}

		public void Render2DTexture(MultiTextureMeshRef meshRef, float x1, float y1, float width, float height, float z = 10f, Vec4f color = null)
		{
			this.guiShaderProg.RgbaIn = ((color == null) ? ColorUtil.WhiteArgbVec : color);
			this.guiShaderProg.ExtraGlow = 0;
			this.guiShaderProg.ApplyColor = 0;
			this.guiShaderProg.NoTexture = 0f;
			this.guiShaderProg.OverlayOpacity = 0f;
			this.guiShaderProg.NormalShaded = 0;
			this.GlPushMatrix();
			this.GlTranslate((double)x1, (double)y1, (double)z);
			this.GlScale((double)width, (double)height, 0.0);
			this.GlScale(0.5, 0.5, 0.0);
			this.GlTranslate(1.0, 1.0, 0.0);
			this.guiShaderProg.ProjectionMatrix = this.CurrentProjectionMatrix;
			this.guiShaderProg.ModelViewMatrix = this.CurrentModelViewMatrix;
			for (int i = 0; i < meshRef.meshrefs.Length; i++)
			{
				MeshRef j = meshRef.meshrefs[i];
				this.guiShaderProg.BindTexture2D("tex2d", meshRef.textureids[i], 0);
				this.Platform.RenderMesh(j);
			}
			this.GlPopMatrix();
		}

		public void Render2DTexture(int textureid, ModelTransform transform, Vec4f color = null)
		{
			this.guiShaderProg.RgbaIn = ((color == null) ? ColorUtil.WhiteArgbVec : color);
			this.guiShaderProg.ExtraGlow = 0;
			this.guiShaderProg.ApplyColor = 0;
			this.guiShaderProg.NoTexture = 0f;
			this.guiShaderProg.Tex2d2D = textureid;
			this.guiShaderProg.OverlayOpacity = 0f;
			this.guiShaderProg.NormalShaded = 0;
			this.GlPushMatrix();
			this.GlTranslate((double)transform.Translation.X, (double)transform.Translation.Y, (double)transform.Translation.Z);
			this.GlRotate(transform.Rotation.X, 1.0, 0.0, 0.0);
			this.GlRotate(transform.Rotation.Y, 0.0, 1.0, 0.0);
			this.GlRotate(transform.Rotation.Z, 0.0, 0.0, 1.0);
			this.GlScale((double)transform.ScaleXYZ.X, (double)transform.ScaleXYZ.Y, 0.0);
			this.GlScale(0.5, 0.5, 0.0);
			this.GlTranslate(1.0, 1.0, 0.0);
			this.guiShaderProg.ProjectionMatrix = this.CurrentProjectionMatrix;
			this.guiShaderProg.ModelViewMatrix = this.CurrentModelViewMatrix;
			this.Platform.RenderMesh(this.quadModel);
			this.GlPopMatrix();
		}

		public void Render2DTextureFlipped(int textureid, float x1, float y1, float width, float height, float z = 10f, Vec4f color = null)
		{
			this.guiShaderProg.RgbaIn = ((color == null) ? ColorUtil.WhiteArgbVec : color);
			this.guiShaderProg.ExtraGlow = 0;
			this.guiShaderProg.ApplyColor = 0;
			this.guiShaderProg.NoTexture = 0f;
			this.guiShaderProg.Tex2d2D = textureid;
			this.guiShaderProg.NormalShaded = 0;
			this.GlPushMatrix();
			this.GlTranslate((double)x1, (double)y1, (double)z);
			this.GlScale((double)width, (double)height, 0.0);
			this.GlScale(0.5, 0.5, 0.0);
			this.GlTranslate(1.0, 1.0, 0.0);
			this.GlRotate(180f, 1.0, 0.0, 0.0);
			this.guiShaderProg.ProjectionMatrix = this.CurrentProjectionMatrix;
			this.guiShaderProg.ModelViewMatrix = this.CurrentModelViewMatrix;
			this.Platform.RenderMesh(this.quadModel);
			this.GlPopMatrix();
		}

		public void Set3DProjection(float zfar, float fov)
		{
			float aspectRatio = (float)this.Platform.WindowSize.Width / (float)this.Platform.WindowSize.Height;
			Mat4d.Perspective(this.set3DProjectionTempMat4, (double)fov, (double)aspectRatio, (double)this.MainCamera.ZNear, (double)zfar);
			this.GlMatrixModeProjection();
			this.GlLoadMatrix(this.set3DProjectionTempMat4);
			this.shUniforms.ZNear = this.MainCamera.ZNear;
			this.shUniforms.ZFar = this.MainCamera.ZFar;
			this.GlMatrixModeModelView();
		}

		public void Reset3DProjection()
		{
			this.Set3DProjection(this.MainCamera.ZFar, this.MainCamera.Fov);
		}

		public void Set3DProjection(float zfar)
		{
			this.Set3DProjection(zfar, this.MainCamera.Fov);
		}

		public void GlMatrixModeModelView()
		{
			this.CurrentMatrixModeProjection = false;
		}

		public void GlMatrixModeProjection()
		{
			this.CurrentMatrixModeProjection = true;
		}

		public float[] CurrentProjectionMatrix
		{
			get
			{
				for (int i = 0; i < 16; i++)
				{
					this.tmpMatrix[i] = (float)this.PMatrix.Top[i];
				}
				return this.tmpMatrix;
			}
		}

		public float[] CurrentModelViewMatrix
		{
			get
			{
				for (int i = 0; i < 16; i++)
				{
					this.tmpMatrix[i] = (float)this.MvMatrix.Top[i];
				}
				return this.tmpMatrix;
			}
		}

		public double[] CurrentModelViewMatrixd
		{
			get
			{
				for (int i = 0; i < 16; i++)
				{
					this.tmpMatrixd[i] = this.MvMatrix.Top[i];
				}
				return this.tmpMatrixd;
			}
		}

		public void GlLoadMatrix(double[] m)
		{
			if (this.CurrentMatrixModeProjection)
			{
				if (this.PMatrix.Count > 0)
				{
					this.PMatrix.Pop();
				}
				this.PMatrix.Push(m);
				return;
			}
			if (this.MvMatrix.Count > 0)
			{
				this.MvMatrix.Pop();
			}
			this.MvMatrix.Push(m);
		}

		public void GlPopMatrix()
		{
			if (this.CurrentMatrixModeProjection)
			{
				if (this.PMatrix.Count > 1)
				{
					this.PMatrix.Pop();
					return;
				}
			}
			else if (this.MvMatrix.Count > 1)
			{
				this.MvMatrix.Pop();
			}
		}

		public void GlScale(double x, double y, double z)
		{
			double[] i;
			if (this.CurrentMatrixModeProjection)
			{
				i = this.PMatrix.Top;
			}
			else
			{
				i = this.MvMatrix.Top;
			}
			Vec3Utilsd.Set(this.glScaleTempVec3, x, y, z);
			Mat4d.Scale(i, i, this.glScaleTempVec3);
		}

		public void GlRotate(float angle, double x, double y, double z)
		{
			angle /= 360f;
			angle *= 6.2831855f;
			double[] i;
			if (this.CurrentMatrixModeProjection)
			{
				i = this.PMatrix.Top;
			}
			else
			{
				i = this.MvMatrix.Top;
			}
			Vec3Utilsd.Set(this.glRotateTempVec3, x, y, z);
			Mat4d.Rotate(i, i, (double)angle, this.glRotateTempVec3);
		}

		public void GlTranslate(Vec3d vec)
		{
			this.GlTranslate((double)((float)vec.X), (double)((float)vec.Y), (double)((float)vec.Z));
		}

		public void GlTranslate(double x, double y, double z)
		{
			double[] i;
			if (this.CurrentMatrixModeProjection)
			{
				i = this.PMatrix.Top;
			}
			else
			{
				i = this.MvMatrix.Top;
			}
			Mat4d.Translate(i, i, new double[] { x, y, z });
		}

		public void GlPushMatrix()
		{
			if (this.CurrentMatrixModeProjection)
			{
				this.PMatrix.Push(this.PMatrix.Top);
				return;
			}
			this.MvMatrix.Push(this.MvMatrix.Top);
		}

		public void GlLoadIdentity()
		{
			if (this.CurrentMatrixModeProjection)
			{
				if (this.PMatrix.Count > 0)
				{
					this.PMatrix.Pop();
				}
				this.PMatrix.Push(this.identityMatrix);
				return;
			}
			if (this.MvMatrix.Count > 0)
			{
				this.MvMatrix.Pop();
			}
			this.MvMatrix.Push(this.identityMatrix);
		}

		public void GlOrtho(double left, double right, double bottom, double top, double zNear, double zFar)
		{
			if (this.CurrentMatrixModeProjection)
			{
				Mat4d.Ortho(this.PMatrix.Top, left, right, bottom, top, zNear, zFar);
				return;
			}
			throw new Exception("Invalid call. CurrentMatrixModeProjection is false");
		}

		public void OrthoMode(int width, int height, bool inverseY = false)
		{
			this.GlMatrixModeProjection();
			this.GlPushMatrix();
			this.GlLoadIdentity();
			if (inverseY)
			{
				this.GlOrtho(0.0, (double)width, 0.0, (double)height, 0.4000000059604645, 20001.0);
			}
			else
			{
				this.GlOrtho(0.0, (double)width, (double)height, 0.0, 0.4000000059604645, 20001.0);
			}
			this.GlMatrixModeModelView();
			this.GlPushMatrix();
			this.GlLoadIdentity();
			GL.DepthRange(0f, 20000f);
			this.GlTranslate(0.0, 0.0, -19849.0);
		}

		public void PerspectiveMode()
		{
			this.GlMatrixModeProjection();
			this.GlPopMatrix();
			this.GlMatrixModeModelView();
			this.GlPopMatrix();
			GL.DepthRange(0f, 1f);
		}

		public void Connect()
		{
			Compression.Reset();
			this.MainNetClient.Connect(this.Connectdata.Host, this.Connectdata.Port, new Action<ConnectionResult>(this.OnConnectionResult), new Action<Exception>(this.OnDisconnected));
			if (ClientSettings.ForceUdpOverTcp && !this.IsSingleplayer)
			{
				this.FallBackToTcp = true;
			}
			else
			{
				this.UdpNetClient.Connect(this.Connectdata.Host, this.Connectdata.Port);
				UdpNetClient udpClient = this.UdpNetClient as UdpNetClient;
				if (udpClient != null)
				{
					ILogger logger = this.Logger;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 1);
					defaultInterpolatedStringHandler.AppendLiteral("UDP: connected on local endpoint: ");
					defaultInterpolatedStringHandler.AppendFormatted<EndPoint>(udpClient.udpClient.Client.LocalEndPoint);
					logger.Notification(defaultInterpolatedStringHandler.ToStringAndClear());
				}
			}
			this.SendPacketClient(new Packet_Client
			{
				Id = 33
			});
		}

		private void OnDisconnected(Exception caughtException)
		{
			Compression.Reset();
			if (this.exitToDisconnectScreen)
			{
				return;
			}
			this.MainNetClient.Dispose();
			this.UdpNetClient.Dispose();
			Action <>9__1;
			TyronThreadPool.QueueLongDurationTask(delegate
			{
				Thread.Sleep(1000);
				ClientMain <>4__this = this;
				Action action;
				if ((action = <>9__1) == null)
				{
					action = (<>9__1 = delegate
					{
						Compression.Reset();
						if (this.exitToMainMenu || this.exitToDisconnectScreen)
						{
							return;
						}
						this.disconnectReason = "The connection closed unexpectedly: " + caughtException.Message;
						this.Logger.Error("The connection closed unexpectedly.");
						this.Logger.Error(caughtException);
						this.DestroyGameSession(true);
					});
				}
				<>4__this.EnqueueMainThreadTask(action, "disconnect");
			});
		}

		private void OnConnectionResult(ConnectionResult result)
		{
			this.Connectdata.Connected = result.connected;
			this.Connectdata.ErrorMessage = result.errorMessage;
			if (result.exception != null)
			{
				this.Logger.Warning("Error while connecting to server: {0}", new object[] { result.errorMessage });
				this.Logger.Warning(LoggerBase.CleanStackTrace(result.exception.ToString()));
			}
		}

		public void SendRequestJoin()
		{
			this.Logger.VerboseDebug("Sending request to join server");
			this.SendPacketClient(ClientPackets.RequestJoin());
		}

		public void SendLeave(int reason)
		{
			this.SendPacketClient(ClientPackets.Leave(reason));
		}

		public byte[] Serialize(Packet_Client packet)
		{
			CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
			Packet_ClientSerializer.Serialize(citoMemoryStream, packet);
			return citoMemoryStream.ToArray();
		}

		public void SendPacket(byte[] packet)
		{
			if (this.disposed || this.MainNetClient == null)
			{
				return;
			}
			try
			{
				this.MainNetClient.Send(packet);
			}
			catch (ObjectDisposedException e)
			{
				this.OnDisconnected(e);
			}
		}

		public void SendPacketClient(Packet_Client packetClient)
		{
			if (packetClient == null)
			{
				return;
			}
			byte[] packet = this.Serialize(packetClient);
			this.SendPacket(packet);
		}

		public void SendPingReply()
		{
			this.SendPacketClient(ClientPackets.PingReply());
		}

		public void Respawn()
		{
			this.SendPacketClient(ClientPackets.SpecialKeyRespawn());
			this.EntityPlayer.Pos.Motion.Set(0.0, 0.0, 0.0);
		}

		public void SendHandInteraction(int mouseButton, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType, EnumHandInteractNw state, bool firstEvent, EnumItemUseCancelReason cancelReason = EnumItemUseCancelReason.ReleasedMouse)
		{
			if (blockSel == null && entitySel == null)
			{
				this.SendPacketClient(new Packet_Client
				{
					Id = 25,
					HandInteraction = new Packet_ClientHandInteraction
					{
						SlotId = this.player.inventoryMgr.ActiveHotbarSlotNumber,
						MouseButton = mouseButton,
						EnumHandInteract = (int)state,
						UsingCount = this.EntityPlayer.Controls.UsingCount,
						UseType = (int)useType,
						CancelReason = (int)cancelReason,
						FirstEvent = ((firstEvent > false) ? 1 : 0)
					}
				});
				return;
			}
			if (blockSel != null)
			{
				this.SendPacketClient(new Packet_Client
				{
					Id = 25,
					HandInteraction = new Packet_ClientHandInteraction
					{
						SlotId = this.player.inventoryMgr.ActiveHotbarSlotNumber,
						MouseButton = mouseButton,
						X = blockSel.Position.X,
						Y = blockSel.Position.InternalY,
						Z = blockSel.Position.Z,
						HitX = CollectibleNet.SerializeDoublePrecise(blockSel.HitPosition.X),
						HitY = CollectibleNet.SerializeDoublePrecise(blockSel.HitPosition.Y),
						HitZ = CollectibleNet.SerializeDoublePrecise(blockSel.HitPosition.Z),
						OnBlockFace = blockSel.Face.Index,
						SelectionBoxIndex = blockSel.SelectionBoxIndex,
						EnumHandInteract = (int)state,
						UsingCount = this.EntityPlayer.Controls.UsingCount,
						UseType = (int)useType,
						CancelReason = (int)cancelReason,
						FirstEvent = ((firstEvent > false) ? 1 : 0)
					}
				});
				return;
			}
			this.SendPacketClient(new Packet_Client
			{
				Id = 25,
				HandInteraction = new Packet_ClientHandInteraction
				{
					SlotId = this.player.inventoryMgr.ActiveHotbarSlotNumber,
					MouseButton = mouseButton,
					HitX = CollectibleNet.SerializeDoublePrecise(entitySel.HitPosition.X),
					HitY = CollectibleNet.SerializeDoublePrecise(entitySel.HitPosition.Y),
					HitZ = CollectibleNet.SerializeDoublePrecise(entitySel.HitPosition.Z),
					OnBlockFace = entitySel.Face.Index,
					OnEntityId = entitySel.Entity.EntityId,
					SelectionBoxIndex = entitySel.SelectionBoxIndex,
					EnumHandInteract = (int)state,
					UsingCount = this.EntityPlayer.Controls.UsingCount,
					UseType = (int)useType,
					FirstEvent = ((firstEvent > false) ? 1 : 0)
				}
			});
		}

		public bool tryAccess(BlockSelection blockSel, EnumBlockAccessFlags flag)
		{
			string claimant;
			EnumWorldAccessResponse resp = this.WorldMap.TestBlockAccess(this.player, this.BlockSelection, flag, out claimant);
			if (resp != EnumWorldAccessResponse.Granted)
			{
				string code = "noprivilege-" + ((flag == EnumBlockAccessFlags.Use) ? "use" : "buildbreak") + "-" + resp.ToString().ToLowerInvariant();
				if (claimant == null)
				{
					claimant = "?";
				}
				else if (claimant.StartsWithOrdinal("custommessage-"))
				{
					code = "noprivilege-buildbreak-" + claimant.Substring("custommessage-".Length);
				}
				this.api.TriggerIngameError(this, code, Lang.Get("ingameerror-" + code, new object[] { claimant }));
				return false;
			}
			return true;
		}

		public bool OnPlayerTryPlace(BlockSelection blockSelection, ref string failureCode)
		{
			if (!this.WorldMap.IsValidPos(blockSelection.Position))
			{
				failureCode = "outsideworld";
				return false;
			}
			if (!this.tryAccess(blockSelection, EnumBlockAccessFlags.BuildOrBreak))
			{
				return false;
			}
			ItemStack itemstack = this.player.InventoryManager.ActiveHotbarSlot.Itemstack;
			if (itemstack != null && itemstack.Class == EnumItemClass.Block)
			{
				Block oldBlock = this.World.BlockAccessor.GetBlock(blockSelection.Position);
				Block liqBlock = this.World.BlockAccessor.GetBlock(blockSelection.Position, 2);
				if (this.preventPlacementInLava && liqBlock.LiquidCode == "lava" && this.player.worlddata.CurrentGameMode != EnumGameMode.Creative)
				{
					failureCode = "toohottoplacehere";
					return false;
				}
				failureCode = null;
				if (itemstack.Block.TryPlaceBlock(this, this.player, itemstack, blockSelection, ref failureCode))
				{
					this.SendPacketClient(ClientPackets.BlockInteraction(blockSelection, 1, 0));
					ClientEventManager clientEventManager = this.eventManager;
					if (clientEventManager != null)
					{
						clientEventManager.TriggerBlockChanged(this, blockSelection.Position, oldBlock);
					}
					this.TriggerNeighbourBlocksUpdate(this.BlockSelection.Position);
					return true;
				}
				if (failureCode == null)
				{
					failureCode = "generic";
				}
			}
			return false;
		}

		public void OnPlayerTryDestroyBlock(BlockSelection blockSelection)
		{
			if (!this.WorldMap.IsValidPos(blockSelection.Position))
			{
				return;
			}
			if (!this.tryAccess(blockSelection, EnumBlockAccessFlags.BuildOrBreak))
			{
				return;
			}
			ItemSlot hotbarslot = this.player.InventoryManager.ActiveHotbarSlot;
			IItemStack stack = this.player.InventoryManager.ActiveHotbarSlot.Itemstack;
			bool ok = true;
			Block oldBlock = blockSelection.Block ?? this.World.BlockAccessor.GetBlock(blockSelection.Position);
			if (stack != null)
			{
				ok = stack.Collectible.OnBlockBrokenWith(this, this.Player.Entity, hotbarslot, blockSelection, 1f);
			}
			else
			{
				oldBlock.OnBlockBroken(this.World, blockSelection.Position, this.Player, 1f);
			}
			if (!ok)
			{
				return;
			}
			ClientEventManager clientEventManager = this.eventManager;
			if (clientEventManager != null)
			{
				clientEventManager.TriggerBlockChanged(this, blockSelection.Position, oldBlock);
			}
			this.TriggerNeighbourBlocksUpdate(blockSelection.Position);
			this.SendPacketClient(ClientPackets.BlockInteraction(blockSelection, 0, 0));
		}

		public bool MouseGrabbed
		{
			get
			{
				return this.Platform.MouseGrabbed;
			}
			set
			{
				int mouseGrabbed = (this.Platform.MouseGrabbed ? 1 : 0);
				this.Platform.MouseGrabbed = value;
				if (mouseGrabbed == 0 && value && this.DialogsOpened == 0)
				{
					ClientPlayerInventoryManager inventoryMgr = this.player.inventoryMgr;
					if (inventoryMgr != null)
					{
						inventoryMgr.DropMouseSlotItems(true);
					}
					this.OnMouseMove(new MouseEvent(this.Width / 2, this.Height / 2));
				}
			}
		}

		public void OnKeyDown(KeyEvent args)
		{
			if (this.disposed)
			{
				return;
			}
			this.api.eventapi.TriggerKeyDown(args);
			if (args.Handled)
			{
				return;
			}
			int eKey = args.KeyCode;
			this.KeyboardStateRaw[eKey] = true;
			bool handled = ScreenManager.hotkeyManager.TriggerGlobalHotKey(args, this, this.player, false);
			args.Handled = handled;
			if (handled)
			{
				return;
			}
			foreach (ClientSystem system in this.clientSystems)
			{
				if (system.CaptureAllInputs())
				{
					system.OnKeyDown(args);
					return;
				}
			}
			ClientSystem[] array = this.clientSystems;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnKeyDown(args);
				if (args.Handled)
				{
					return;
				}
			}
			this.PreviousKeyboardState[eKey] = this.KeyboardState[eKey];
			this.KeyboardState[eKey] = true;
			handled = ScreenManager.hotkeyManager.TriggerHotKey(args, this, this.player, this.AllowCharacterControl, false);
			args.Handled = handled;
		}

		public void OnKeyUp(KeyEvent args)
		{
			if (this.disposed)
			{
				return;
			}
			int eKey = args.KeyCode;
			this.KeyboardStateRaw[eKey] = false;
			this.PreviousKeyboardState[eKey] = this.KeyboardState[eKey];
			this.KeyboardState[eKey] = false;
			this.api.eventapi.TriggerKeyUp(args);
			if (args.Handled)
			{
				return;
			}
			bool handled = ScreenManager.hotkeyManager.TriggerGlobalHotKey(args, this, this.player, true);
			args.Handled = handled;
			if (handled)
			{
				return;
			}
			foreach (ClientSystem system in this.clientSystems)
			{
				if (system.CaptureAllInputs())
				{
					system.OnKeyUp(args);
					return;
				}
			}
			ClientSystem[] array = this.clientSystems;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnKeyUp(args);
				if (args.Handled)
				{
					return;
				}
			}
			args.Handled = ScreenManager.hotkeyManager.TriggerHotKey(args, this, this.player, this.AllowCharacterControl, true);
		}

		public void OnKeyPress(KeyEvent eventArgs)
		{
			if (this.disposed)
			{
				return;
			}
			foreach (ClientSystem system in this.clientSystems)
			{
				if (system.CaptureAllInputs())
				{
					system.OnKeyPress(eventArgs);
					return;
				}
			}
			ClientSystem[] array = this.clientSystems;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnKeyPress(eventArgs);
				if (eventArgs.Handled)
				{
					return;
				}
			}
		}

		public void OnMouseDownRaw(MouseEvent args)
		{
			if (this.disposed)
			{
				return;
			}
			this.UpdateMouseButtonState(args.Button, this.MouseStateRaw, true);
			foreach (ClientSystem system in this.clientSystems)
			{
				if (system.CaptureRawMouse())
				{
					system.OnMouseDown(args);
					return;
				}
			}
			int eKey = (int)(args.Button + 240);
			this.PreviousKeyboardState[eKey] = this.KeyboardState[eKey];
			this.KeyboardState[eKey] = true;
			ScreenManager.hotkeyManager.OnMouseButton(this, args.Button, args.Modifiers, true);
		}

		private void UpdateMouseButtonState(EnumMouseButton button, MouseButtonState mouseState, bool value)
		{
			if (button == EnumMouseButton.Left)
			{
				mouseState.Left = value;
			}
			if (button == EnumMouseButton.Middle)
			{
				mouseState.Middle = value;
			}
			if (button == EnumMouseButton.Right)
			{
				mouseState.Right = value;
			}
		}

		public bool UpdateMouseButtonState(EnumMouseButton button, bool down)
		{
			MouseEvent args = this.Platform.CreateMouseEvent(button);
			if (down)
			{
				this.api.eventapi.TriggerMouseDown(args);
				if (args.Handled)
				{
					return true;
				}
				foreach (ClientSystem system in this.clientSystems)
				{
					if (system.CaptureAllInputs())
					{
						system.OnMouseDown(args);
						return true;
					}
				}
				ClientSystem[] array = this.clientSystems;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].OnMouseDown(args);
					if (args.Handled)
					{
						return true;
					}
				}
				this.UpdateMouseButtonState(button, this.InWorldMouseState, true);
			}
			else
			{
				this.api.eventapi.TriggerMouseUp(args);
				if (args.Handled)
				{
					return true;
				}
				this.UpdateMouseButtonState(button, this.InWorldMouseState, false);
				foreach (ClientSystem system2 in this.clientSystems)
				{
					if (system2.CaptureAllInputs())
					{
						system2.OnMouseUp(args);
						return true;
					}
				}
				ClientSystem[] array = this.clientSystems;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].OnMouseUp(args);
					if (args.Handled)
					{
						return true;
					}
				}
			}
			return false;
		}

		public void OnMouseUpRaw(MouseEvent args)
		{
			int eKey = (int)(args.Button + 240);
			this.PreviousKeyboardState[eKey] = this.KeyboardState[eKey];
			this.KeyboardState[eKey] = false;
			this.UpdateMouseButtonState(args.Button, this.MouseStateRaw, false);
			foreach (ClientSystem system in this.clientSystems)
			{
				if (system.CaptureRawMouse())
				{
					system.OnMouseUp(args);
					return;
				}
			}
			ScreenManager.hotkeyManager.OnMouseButton(this, args.Button, args.Modifiers, false);
		}

		public void OnMouseWheel(Vintagestory.API.Client.MouseWheelEventArgs args)
		{
			float deltaPrecise = args.deltaPrecise;
			foreach (ClientSystem system in this.clientSystems)
			{
				if (system.CaptureAllInputs())
				{
					system.OnMouseWheel(args);
					return;
				}
			}
			ClientSystem[] array = this.clientSystems;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnMouseWheel(args);
				if (args.IsHandled)
				{
					return;
				}
			}
		}

		public void OnMouseMove(MouseEvent args)
		{
			this.api.eventapi.TriggerMouseMove(args);
			if (args.Handled)
			{
				return;
			}
			this.MouseCurrentX = (this.MouseGrabbed ? (this.Width / 2) : args.X);
			this.MouseCurrentY = (this.MouseGrabbed ? (this.Height / 2) : args.Y);
			this.MouseDeltaX += (double)(args.DeltaX * ClientSettings.MouseSensivity) / 100.0;
			this.MouseDeltaY += (double)(args.DeltaY * ClientSettings.MouseSensivity) / 100.0;
			foreach (ClientSystem system in this.clientSystems)
			{
				if (system.CaptureAllInputs())
				{
					system.OnMouseMove(args);
					return;
				}
			}
			ClientSystem[] array = this.clientSystems;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnMouseMove(args);
				if (args.Handled)
				{
					break;
				}
			}
		}

		public void UpdateFreeMouse()
		{
			int altKey = ScreenManager.hotkeyManager.HotKeys["togglemousecontrol"].CurrentMapping.KeyCode;
			bool isAltKeyDown = this.KeyboardState[altKey];
			bool preferUngrabbedMouse = this.OpenedGuis.Where((GuiDialog gui) => gui.DialogType == EnumDialogType.Dialog).Any((GuiDialog dlg) => dlg.PrefersUngrabbedMouse);
			bool disableMouseGrab = this.OpenedGuis.Any((GuiDialog gui) => gui.DisableMouseGrab);
			bool mouseGrabbed = this.MouseGrabbed;
			this.MouseGrabbed = ScreenManager.Platform.IsFocused && !this.exitToDisconnectScreen && !this.exitToMainMenu && this.BlocksReceivedAndLoaded && !disableMouseGrab && ((this.DialogsOpened == 0 || (ClientSettings.ImmersiveMouseMode && !preferUngrabbedMouse)) ^ isAltKeyDown);
			this.mouseWorldInteractAnyway = !this.MouseGrabbed && !preferUngrabbedMouse;
		}

		public void UpdateCameraYawPitch(float dt)
		{
			if (this.player.worlddata.CurrentGameMode == EnumGameMode.Survival && this.api.renderapi.ShaderUniforms.GlitchStrength > 0.75f && this.Platform.GetWindowState() == WindowState.Normal && this.rand.Value.NextDouble() < 0.01)
			{
				Size2i scsize = this.Platform.ScreenSize;
				Size2i wdsize = this.Platform.WindowSize;
				if (wdsize.Width < scsize.Width && wdsize.Height < scsize.Height)
				{
					int maxx = scsize.Width - wdsize.Width;
					int maxy = scsize.Height - wdsize.Height;
					Vector2i vector2I = ((ClientPlatformWindows)this.Platform).window.ClientSize;
					int x = vector2I.X;
					int y = vector2I.Y;
					if (x > 0 && x < maxx)
					{
						vector2I.X = GameMath.Clamp(x + this.rand.Value.Next(10) - 5, 0, maxx);
					}
					if (y > 0 && y < maxy)
					{
						vector2I.Y = GameMath.Clamp(y + this.rand.Value.Next(10) - 5, 0, maxy);
					}
					((ClientPlatformWindows)this.Platform).window.ClientSize = vector2I;
				}
			}
			double fpsFac = (double)GameMath.Clamp(dt / 0.013333334f, 0f, 3f);
			double fac = GameMath.Clamp((double)((float)ClientSettings.MouseSmoothing / 100f) * fpsFac, 0.009999999776482582, 1.0);
			float mountedSmoothing = 0.5f * (float)fpsFac;
			if (this.player.Entity.Swimming && this.player.CameraMode == EnumCameraMode.FirstPerson && this.swimmingMouseSmoothing > 0f)
			{
				fac = (double)GameMath.Clamp(1f - this.swimmingMouseSmoothing, 0f, 1f);
			}
			double velX = fac * (this.MouseDeltaX - this.DelayedMouseDeltaX);
			double velY = fac * (this.MouseDeltaY - this.DelayedMouseDeltaY);
			this.DelayedMouseDeltaX += velX;
			this.DelayedMouseDeltaY += velY;
			EntityPlayer entityPlayer = this.EntityPlayer;
			bool flag;
			if (entityPlayer == null)
			{
				flag = false;
			}
			else
			{
				IMountableSeat mountedOn = entityPlayer.MountedOn;
				flag = ((mountedOn != null) ? new EnumMountAngleMode?(mountedOn.AngleMode) : null).GetValueOrDefault() == EnumMountAngleMode.FixateYaw;
			}
			if (flag && this.player.CameraMode != EnumCameraMode.FirstPerson)
			{
				float mountyaw = this.EntityPlayer.MountedOn.SeatPosition.Yaw;
				this.EntityPlayer.Pos.Yaw = mountyaw;
				this.EntityPlayer.BodyYaw = mountyaw;
				if (this.MainCamera.CameraMode == EnumCameraMode.FirstPerson)
				{
					this.mouseYaw = mountyaw;
				}
			}
			if (this.AllowCameraControl && this.Platform.IsFocused && this.MouseGrabbed)
			{
				EnumMountAngleMode angleMode = EnumMountAngleMode.Unaffected;
				float? mountyaw2 = null;
				IMountableSeat mount = this.EntityPlayer.MountedOn;
				if (this.EntityPlayer.MountedOn != null)
				{
					angleMode = mount.AngleMode;
					mountyaw2 = new float?(mount.SeatPosition.Yaw);
				}
				if (this.player.CameraMode == EnumCameraMode.Overhead)
				{
					float d = GameMath.AngleRadDistance(this.EntityPlayer.Pos.Yaw, this.EntityPlayer.WalkYaw) * 0.4f;
					this.EntityPlayer.Pos.Yaw += d;
					this.mouseYaw -= (float)(velX * (double)this.rotationspeed * 1.0 / 75.0);
					this.mouseYaw = GameMath.Mod(this.mouseYaw, 6.2831855f);
				}
				else
				{
					this.mouseYaw -= (float)(velX * (double)this.rotationspeed * 1.0 / 75.0);
					if (this.EntityPlayer.HeadYawLimits != null)
					{
						AngleConstraint constr = this.EntityPlayer.HeadYawLimits;
						float range = GameMath.AngleRadDistance(constr.CenterRad, this.mouseYaw);
						this.mouseYaw = constr.CenterRad + GameMath.Clamp(range, -constr.RangeRad, constr.RangeRad);
					}
					this.EntityPlayer.Pos.Yaw = this.mouseYaw;
				}
				bool handRenderMode = this.MainCamera.CameraMode == EnumCameraMode.FirstPerson && mount != null;
				if (angleMode == EnumMountAngleMode.PushYaw || angleMode == EnumMountAngleMode.Push || handRenderMode)
				{
					float diff = -mountedSmoothing * GameMath.AngleRadDistance(mount.SeatPosition.Yaw, this.prevMountAngles.Y);
					this.prevMountAngles.Y += diff;
					if (angleMode == EnumMountAngleMode.Push)
					{
						this.EntityPlayer.Pos.Roll -= GameMath.AngleRadDistance(mount.SeatPosition.Roll, this.prevMountAngles.X);
						this.EntityPlayer.Pos.Pitch -= GameMath.AngleRadDistance(mount.SeatPosition.Pitch, this.prevMountAngles.Z);
					}
					if (this.player.CameraMode == EnumCameraMode.Overhead)
					{
						this.EntityPlayer.WalkYaw += diff;
					}
					else
					{
						this.mouseYaw += diff;
						this.EntityPlayer.Pos.Yaw += diff;
						this.EntityPlayer.BodyYaw += diff;
					}
				}
				if ((angleMode == EnumMountAngleMode.Fixate || angleMode == EnumMountAngleMode.FixateYaw) && !handRenderMode)
				{
					this.EntityPlayer.Pos.Yaw = mountyaw2.Value;
				}
				if (angleMode == EnumMountAngleMode.Fixate)
				{
					this.EntityPlayer.Pos.Pitch = this.EntityPlayer.MountedOn.SeatPosition.Pitch;
				}
				else
				{
					this.EntityPlayer.Pos.Pitch += (float)(velY * (double)this.rotationspeed * 1.0 / 75.0 * (double)(ClientSettings.InvertMouseYAxis ? (-1) : 1));
				}
				if (mount != null)
				{
					this.prevMountAngles.Set(mount.SeatPosition.Roll, this.prevMountAngles.Y, mount.SeatPosition.Pitch);
				}
				this.EntityPlayer.Pos.Pitch = GameMath.Clamp(this.EntityPlayer.Pos.Pitch, 1.5857964f, 4.697389f);
				this.EntityPlayer.Pos.Yaw = GameMath.Mod(this.EntityPlayer.Pos.Yaw, 6.2831855f);
				this.mousePitch = this.EntityPlayer.Pos.Pitch;
			}
		}

		public void OnFocusChanged(bool focus)
		{
			if (this.disposed)
			{
				return;
			}
			ClientEventManager clientEventManager = this.eventManager;
			if (clientEventManager != null)
			{
				clientEventManager.TriggerGameWindowFocus(focus);
			}
			if (!focus)
			{
				this.MouseStateRaw.Clear();
				this.InWorldMouseState.Clear();
			}
		}

		public bool OnFileDrop(string filename)
		{
			return this.api.eventapi.TriggerFileDrop(filename);
		}

		public Item GetItem(int itemId)
		{
			if (itemId >= this.Items.Count)
			{
				throw new ArgumentOutOfRangeException(string.Format("Cannot get item of id {0}, item list count is only until {1}!", itemId, this.Items.Count));
			}
			return this.Items[itemId];
		}

		public Block GetBlock(int blockId)
		{
			if (blockId >= this.Blocks.Count)
			{
				return this.getOrCreateNoBlock(blockId);
			}
			return this.Blocks[blockId];
		}

		private Block getOrCreateNoBlock(int id)
		{
			Block block;
			if (!this.noBlocks.TryGetValue(id, out block))
			{
				block = (this.noBlocks[id] = BlockList.getNoBlock(id, this.api));
			}
			return block;
		}

		public EntityProperties GetEntityType(AssetLocation entityCode)
		{
			EntityProperties eclass;
			this.EntityClassesByCode.TryGetValue(entityCode, out eclass);
			return eclass;
		}

		public void ReloadTextures()
		{
			this.BlockAtlasManager.PauseRegenMipmaps().ReloadTextures();
			this.ItemAtlasManager.PauseRegenMipmaps().ReloadTextures();
			this.EntityAtlasManager.PauseRegenMipmaps().ReloadTextures();
			ClientEventManager clientEventManager = this.eventManager;
			if (clientEventManager != null)
			{
				clientEventManager.TriggerReloadTextures();
			}
			this.BlockAtlasManager.ResumeRegenMipmaps();
			this.ItemAtlasManager.ResumeRegenMipmaps();
			this.EntityAtlasManager.ResumeRegenMipmaps();
			foreach (Block block in this.Blocks)
			{
				block.LoadTextureSubIdForBlockColor();
			}
		}

		public int WhiteTexture()
		{
			if (this.whitetexture == -1)
			{
				BitmapRef bmp = this.Platform.CreateBitmap(1, 1);
				int[] pixels = new int[] { ColorUtil.ToRgba(255, 255, 255, 255) };
				this.Platform.SetBitmapPixelsArgb(bmp, pixels);
				this.whitetexture = this.Platform.LoadTexture(bmp, false, 0, false);
			}
			return this.whitetexture;
		}

		public int GetOrLoadCachedTexture(AssetLocation name)
		{
			name = name.WithPathPrefixOnce("textures/");
			if (this.texturesByLocation.ContainsKey(name))
			{
				return this.texturesByLocation[name].TextureId;
			}
			IAsset asset = this.Platform.AssetManager.TryGet(name, true);
			byte[] assetData = ((asset != null) ? asset.Data : null);
			if (assetData == null)
			{
				return 0;
			}
			BitmapRef bmp = this.Platform.CreateBitmapFromPng(assetData, assetData.Length);
			int textureId = this.Platform.LoadTexture(bmp, false, 0, false);
			this.texturesByLocation[name] = new LoadedTexture(this.api, textureId, bmp.Width, bmp.Height);
			bmp.Dispose();
			return textureId;
		}

		public void GetOrLoadCachedTexture(AssetLocation name, ref LoadedTexture intoTexture)
		{
			intoTexture.IgnoreUndisposed = true;
			if (this.texturesByLocation.ContainsKey(name))
			{
				LoadedTexture cachedTex = this.texturesByLocation[name];
				if (cachedTex.TextureId != intoTexture.TextureId && intoTexture.TextureId != 0)
				{
					intoTexture.Dispose();
				}
				intoTexture.TextureId = cachedTex.TextureId;
				intoTexture.Width = cachedTex.Width;
				intoTexture.Height = cachedTex.Height;
				return;
			}
			IAsset asset = this.Platform.AssetManager.TryGet(name.Clone().WithPathPrefixOnce("textures/"), true);
			byte[] assetData = ((asset != null) ? asset.Data : null);
			if (assetData == null)
			{
				return;
			}
			BitmapRef bmp = this.Platform.CreateBitmapFromPng(assetData, assetData.Length);
			int textureId = this.Platform.LoadTexture(bmp, false, 0, false);
			if (textureId != intoTexture.TextureId && intoTexture.TextureId != 0)
			{
				intoTexture.Dispose();
			}
			intoTexture.TextureId = textureId;
			intoTexture.Width = bmp.Width;
			intoTexture.Height = bmp.Height;
			this.texturesByLocation[name] = new LoadedTexture(this.api, textureId, bmp.Width, bmp.Height);
			bmp.Dispose();
		}

		public void GetOrLoadCachedTexture(AssetLocation name, BitmapRef bmp, ref LoadedTexture intoTexture)
		{
			LoadedTexture cachedTex;
			if (!this.texturesByLocation.TryGetValue(name, out cachedTex))
			{
				cachedTex = (this.texturesByLocation[name] = new LoadedTexture(this.api, this.Platform.LoadTexture(bmp, false, 0, false), bmp.Width, bmp.Height));
			}
			if (cachedTex.TextureId != intoTexture.TextureId && intoTexture.TextureId != 0)
			{
				intoTexture.Dispose();
			}
			intoTexture.TextureId = cachedTex.TextureId;
			intoTexture.Width = cachedTex.Width;
			intoTexture.Height = cachedTex.Height;
		}

		public bool DeleteCachedTexture(AssetLocation name)
		{
			if (name == null || !this.texturesByLocation.ContainsKey(name))
			{
				return false;
			}
			LoadedTexture loadedTexture = this.texturesByLocation[name];
			this.texturesByLocation.Remove(name);
			if (loadedTexture != null)
			{
				loadedTexture.Dispose();
			}
			return true;
		}

		public void PauseGame(bool paused)
		{
			if (!this.IsSingleplayer)
			{
				return;
			}
			if (!this.BlocksReceivedAndLoaded)
			{
				return;
			}
			this.IsPaused = paused;
			this.Platform.SetGamePausedState(paused);
			this.api.eventapi.TriggerPauseResume(paused);
			if (paused)
			{
				this.GameWorldCalendar.watchIngameTime.Stop();
				this.InWorldStopwatch.Stop();
			}
			else
			{
				this.GameWorldCalendar.watchIngameTime.Start();
				this.InWorldStopwatch.Start();
			}
			this.World.Logger.Notification("Client pause state is now {0}", new object[] { paused ? "on" : "off" });
		}

		private void ViewDistanceChanged(int newValue)
		{
			if (newValue != this.player.worlddata.LastApprovedViewDistance)
			{
				this.player.worlddata.RequestNewViewDistance(this);
			}
			this.frustumCuller.UpdateViewDistance(newValue);
		}

		private void OnVsyncChanged(int vsyncmode)
		{
			this.Platform.SetVSync(vsyncmode != 0);
		}

		public void FindCmd(string search)
		{
			ICoreClientAPI capi = this.api;
			List<int> searchIds = new List<int>();
			foreach (Block block in this.Blocks)
			{
				if (block.Code != null && block.Code.Path.Contains(search))
				{
					searchIds.Add(block.BlockId);
				}
			}
			EntityPos defaultSpawnPosition = capi.World.DefaultSpawnPosition;
			int centreX = (int)defaultSpawnPosition.X;
			int centreZ = (int)defaultSpawnPosition.Z;
			StringBuilder sb = new StringBuilder();
			foreach (KeyValuePair<long, ClientChunk> pair in this.WorldMap.chunks)
			{
				pair.Value.Unpack();
				ChunkData data = pair.Value.Data as ChunkData;
				if (data != null)
				{
					BlockPos found = data.FindFirst(searchIds);
					if (!(found == null))
					{
						long key = pair.Key;
						int cx = (int)(key % (long)this.WorldMap.index3dMulX) * 32 + found.X;
						int cy = (int)(key / (long)this.WorldMap.index3dMulX / (long)this.WorldMap.index3dMulZ) * 32 + found.Y;
						int cz = (int)(key / (long)this.WorldMap.index3dMulX % (long)this.WorldMap.index3dMulZ) * 32 + found.Z;
						sb.Append(string.Concat(new string[]
						{
							"\nFound at ",
							(cx - centreX).ToString(),
							",",
							cy.ToString(),
							",",
							(cz - centreZ).ToString()
						}));
					}
				}
			}
			if (sb.Length == 0)
			{
				sb.Append("No block matching '" + search + "' found");
			}
			capi.ShowChatMessage(sb.ToString());
		}

		public float EyesInLavaDepth()
		{
			double eyePos = this.MainCamera.CameraEyePos.Y;
			BlockPos pos = new BlockPos((int)this.MainCamera.CameraEyePos.X, (int)eyePos, (int)this.MainCamera.CameraEyePos.Z);
			AssetLocation code = this.WorldMap.RelaxedBlockAccess.GetBlock(pos).Code;
			if (!(code != null) || !code.PathStartsWith("lava"))
			{
				return 0f;
			}
			float distFromSurface = 1f - ((float)eyePos - (float)((int)eyePos));
			for (;;)
			{
				AssetLocation code2 = this.WorldMap.RelaxedBlockAccess.GetBlock(pos.Up(1)).Code;
				if (code2 == null || !code2.PathStartsWith("lava"))
				{
					break;
				}
				distFromSurface += 1f;
			}
			return distFromSurface;
		}

		public float EyesInWaterDepth()
		{
			double eyePos = this.MainCamera.CameraEyePos.Y;
			BlockPos pos = new BlockPos((int)this.MainCamera.CameraEyePos.X, (int)eyePos, (int)this.MainCamera.CameraEyePos.Z);
			string liquidCode = this.WorldMap.RelaxedBlockAccess.GetBlock(pos, 2).LiquidCode;
			if (!(liquidCode == "water") && !(liquidCode == "seawater") && !(liquidCode == "saltwater"))
			{
				return 0f;
			}
			float distFromSurface = 1f - ((float)eyePos - (float)((int)eyePos));
			liquidCode = this.WorldMap.RelaxedBlockAccess.GetBlock(pos.Up(1), 2).LiquidCode;
			while (liquidCode == "water" || liquidCode == "seawater" || liquidCode == "saltwater")
			{
				distFromSurface += 1f;
				liquidCode = this.WorldMap.RelaxedBlockAccess.GetBlock(pos.Up(1), 2).LiquidCode;
			}
			return distFromSurface;
		}

		public int GetEyesInWaterColorShift()
		{
			double eyePos = this.MainCamera.CameraEyePos.Y;
			string liquidCode = this.WorldMap.RelaxedBlockAccess.GetBlockRaw((int)this.MainCamera.CameraEyePos.X, (int)eyePos, (int)this.MainCamera.CameraEyePos.Z, 2).LiquidCode;
			bool liquid = liquidCode == "water" || liquidCode == "seawater" || liquidCode == "saltwater";
			liquidCode = this.WorldMap.RelaxedBlockAccess.GetBlockRaw((int)this.MainCamera.CameraEyePos.X, (int)(this.MainCamera.CameraEyePos.Y + 1.0), (int)this.MainCamera.CameraEyePos.Z, 2).LiquidCode;
			bool aboveLiquid = liquidCode == "water" || liquidCode == "seawater" || liquidCode == "saltwater";
			if (liquid && aboveLiquid)
			{
				return 100;
			}
			if (!liquid)
			{
				return 0;
			}
			float distFromSurface = (float)eyePos - (float)((int)eyePos);
			return (int)Math.Max(0f, Math.Min(100f, 600f * (1.04f - distFromSurface)));
		}

		public int GetEyesInLavaColorShift()
		{
			double eyePos = this.MainCamera.CameraEyePos.Y;
			AssetLocation code = this.WorldMap.RelaxedBlockAccess.GetBlock((int)this.MainCamera.CameraEyePos.X, (int)eyePos, (int)this.MainCamera.CameraEyePos.Z).Code;
			bool liquid = code != null && code.PathStartsWith("lava");
			code = this.WorldMap.RelaxedBlockAccess.GetBlock((int)this.MainCamera.CameraEyePos.X, (int)(this.MainCamera.CameraEyePos.Y + 1.0), (int)this.MainCamera.CameraEyePos.Z).Code;
			bool aboveLiquid = code != null && code.PathStartsWith("lava");
			if (liquid && aboveLiquid)
			{
				return 100;
			}
			if (!liquid)
			{
				return 0;
			}
			float distFromSurface = (float)eyePos - (float)((int)eyePos);
			return (int)Math.Max(0f, Math.Min(100f, 600f * (1.04f - distFromSurface)));
		}

		public void RedrawAllBlocks()
		{
			this.ShouldRedrawAllBlocks = true;
		}

		public void OnResize()
		{
			this.Platform.GlViewport(0, 0, this.Platform.WindowSize.Width, this.Platform.WindowSize.Height);
			this.Reset3DProjection();
		}

		public void DoReconnect()
		{
			this.doReconnect = true;
		}

		public void ExitAndSwitchServer(MultiplayerServerEntry redirect)
		{
			if (this.IsSingleplayer)
			{
				this.Platform.ExitSinglePlayerServer();
			}
			this.RedirectTo = redirect;
			this.exitToMainMenu = true;
		}

		public MultiplayerServerEntry GetRedirect()
		{
			return this.RedirectTo;
		}

		public void DestroyGameSession(bool gotDisconnected)
		{
			if (this.exitToMainMenu || this.exitToDisconnectScreen)
			{
				return;
			}
			this.Platform.ShaderUniforms = new DefaultShaderUniforms();
			this.Logger.Notification("Destroying game session, waiting up to 200ms for client threads to exit");
			this.api.eventapi.TriggerLeaveWorld();
			this.threadsShouldExit = true;
			int tries = 2;
			while (tries-- > 0)
			{
				bool allThreadsExited = true;
				foreach (Thread thread in this.clientThreads)
				{
					allThreadsExited &= !thread.IsAlive;
				}
				if (allThreadsExited)
				{
					break;
				}
				Thread.Sleep(100);
			}
			this._clientThreadsCts.Cancel();
			if (this.IsSingleplayer && ScreenManager.Platform.IsServerRunning)
			{
				this.Logger.Notification("Stopping single player server");
				this.Platform.ExitSinglePlayerServer();
			}
			this.RedirectTo = null;
			this.exitToMainMenu = !gotDisconnected;
			this.exitToDisconnectScreen = gotDisconnected;
			this.MouseGrabbed = false;
			this.api.eventapi.TriggerLeftWorld();
			this.Dispose();
		}

		private void UpdateResize()
		{
			if (this.lastWidth != this.Platform.WindowSize.Width || this.lastHeight != this.Platform.WindowSize.Height)
			{
				this.lastWidth = this.Platform.WindowSize.Width;
				this.lastHeight = this.Platform.WindowSize.Height;
				this.OnResize();
			}
		}

		public int Width
		{
			get
			{
				return this.Platform.WindowSize.Width;
			}
		}

		public int Height
		{
			get
			{
				return this.Platform.WindowSize.Height;
			}
		}

		public void EnqueueMainThreadTask(Action action, string code)
		{
			object mainThreadTasksLock = this.MainThreadTasksLock;
			lock (mainThreadTasksLock)
			{
				this.MainThreadTasks.Enqueue(new ClientTask
				{
					Action = action,
					Code = code
				});
			}
		}

		public void EnqueueGameLaunchTask(Action action, string code)
		{
			if (!this.disposed)
			{
				this.GameLaunchTasks.Enqueue(new ClientTask
				{
					Action = action,
					Code = code
				});
			}
		}

		public void Dispose()
		{
			this.disposed = true;
			this.api.disposed = true;
			BlockChunkDataLayer.Dispose();
			MeshDataRecycler recycler = MeshData.Recycler;
			if (recycler != null)
			{
				recycler.Dispose();
			}
			CancellationTokenSource musicEngineCts = this.MusicEngineCts;
			if (musicEngineCts != null)
			{
				musicEngineCts.Cancel();
			}
			ClientSystem[] array = this.clientSystems;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Dispose(this);
			}
			Thread.Sleep(100);
			foreach (LoadedTexture loadedTexture in this.texturesByLocation.Values)
			{
				if (loadedTexture != null)
				{
					loadedTexture.Dispose();
				}
			}
			if (this.Blocks != null)
			{
				foreach (Block block in this.Blocks)
				{
					if (block != null)
					{
						block.OnUnloaded(this.api);
					}
				}
			}
			if (this.Items != null)
			{
				foreach (Item item in this.Items)
				{
					if (item != null)
					{
						item.OnUnloaded(this.api);
					}
				}
			}
			PhysicsBehaviorBase.collisionTester = null;
			MeshRef meshRef = this.quadModel;
			if (meshRef != null)
			{
				meshRef.Dispose();
			}
			ClientCoreAPI clientCoreAPI = this.api;
			if (clientCoreAPI != null)
			{
				clientCoreAPI.Dispose();
			}
			this.ItemAtlasManager.Dispose();
			this.BlockAtlasManager.Dispose();
			this.EntityAtlasManager.Dispose();
			this.TesselatorManager.Dispose();
			this.WorldMap.Dispose();
			this._clientThreadsCts.Dispose();
			CancellationTokenSource musicEngineCts2 = this.MusicEngineCts;
			if (musicEngineCts2 != null)
			{
				musicEngineCts2.Dispose();
			}
			this.MusicEngineCts = null;
			this.Platform.ClearOnCrash();
			ClientSettings.Inst.ClearWatchers();
			this.ScreenRunningGame.ScreenManager.registerSettingsWatchers();
			ScreenManager.hotkeyManager.ClearInGameHotKeyHandlers();
			VtmlUtil.TagConverters.Clear();
			ClientEventManager clientEventManager = this.eventManager;
			if (clientEventManager != null)
			{
				clientEventManager.Dispose();
			}
			this.eventManager = null;
			ThreadLocal<Random> threadLocal = this.rand;
			if (threadLocal != null)
			{
				threadLocal.Dispose();
			}
			ClientMain.ClassRegistry = null;
			NetClient mainNetClient = this.MainNetClient;
			if (mainNetClient != null)
			{
				mainNetClient.Dispose();
			}
			UNetClient udpNetClient = this.UdpNetClient;
			if (udpNetClient != null)
			{
				udpNetClient.Dispose();
			}
			this.Platform.Logger.ClearWatchers();
			MeshData.Recycler = null;
			BlockChunkDataLayer.blocksByPaletteIndex = null;
			this.Platform.AssetManager.UnloadExternalAssets(this.Logger);
			this.Platform.AssetManager.CustomModOrigins.Clear();
		}

		public int ApplyColorMapOnRgba(string climateColorMap, string seasonColorMap, int color, int posX, int posY, int posZ, bool flipRb = true)
		{
			return this.WorldMap.ApplyColorMapOnRgba(climateColorMap, seasonColorMap, color, posX, posY, posZ, flipRb);
		}

		public int ApplyColorMapOnRgba(ColorMap climateMap, ColorMap seasonMap, int color, int posX, int posY, int posZ, bool flipRb = true)
		{
			return this.WorldMap.ApplyColorMapOnRgba(climateMap, seasonMap, color, posX, posY, posZ, flipRb);
		}

		public int ApplyColorMapOnRgba(string climateColorMap, string seasonColorMap, int color, int rain, int temp, bool flipRb = true)
		{
			return this.WorldMap.ApplyColorMapOnRgba(climateColorMap, seasonColorMap, color, rain, temp, flipRb, 0f, 0f);
		}

		public void TryAttackEntity(EntitySelection selection)
		{
			if (selection == null)
			{
				return;
			}
			Entity entity = selection.Entity;
			Cuboidd cuboidd = entity.SelectionBox.ToDouble().Translate(entity.Pos.X, entity.Pos.Y, entity.Pos.Z);
			EntityPos pos = this.EntityPlayer.SidedPos;
			IClientPlayer clientPlayer = this.Player;
			ItemStack itemStack;
			if (clientPlayer == null)
			{
				itemStack = null;
			}
			else
			{
				IPlayerInventoryManager inventoryManager = clientPlayer.InventoryManager;
				if (inventoryManager == null)
				{
					itemStack = null;
				}
				else
				{
					ItemSlot activeHotbarSlot = inventoryManager.ActiveHotbarSlot;
					itemStack = ((activeHotbarSlot != null) ? activeHotbarSlot.Itemstack : null);
				}
			}
			ItemStack heldStack = itemStack;
			float attackRange = ((heldStack == null) ? GlobalConstants.DefaultAttackRange : heldStack.Collectible.GetAttackRange(heldStack));
			if (cuboidd.ShortestDistanceFrom(pos.X + this.EntityPlayer.LocalEyePos.X, pos.Y + this.EntityPlayer.LocalEyePos.Y, pos.Z + this.EntityPlayer.LocalEyePos.Z) <= (double)attackRange)
			{
				selection.Entity.OnInteract(this.EntityPlayer, this.player.inventoryMgr.ActiveHotbarSlot, selection.HitPosition, EnumInteractMode.Attack);
				this.SendPacketClient(ClientPackets.EntityInteraction(0, selection.Entity.EntityId, selection.Face, selection.HitPosition, selection.SelectionBoxIndex));
			}
		}

		public void CloneBlockDamage(BlockPos sourcePos, BlockPos targetPos)
		{
			BlockDamage blockdmg;
			if (this.damagedBlocks.TryGetValue(sourcePos, out blockdmg))
			{
				BlockDamage targetDmg;
				if (!this.damagedBlocks.TryGetValue(targetPos, out targetDmg))
				{
					Dictionary<BlockPos, BlockDamage> dictionary = this.damagedBlocks;
					BlockDamage blockDamage = new BlockDamage();
					blockDamage.Position = targetPos;
					blockDamage.Block = this.blockAccessor.GetBlock(targetPos);
					blockDamage.Facing = blockdmg.Facing;
					blockDamage.RemainingResistance = blockdmg.RemainingResistance;
					blockDamage.LastBreakEllapsedMs = blockdmg.LastBreakEllapsedMs;
					blockDamage.BeginBreakEllapsedMs = blockdmg.BeginBreakEllapsedMs;
					blockDamage.ByPlayer = blockdmg.ByPlayer;
					blockDamage.Tool = blockdmg.Tool;
					blockDamage.BreakingCounter = blockdmg.BreakingCounter;
					BlockDamage blockDamage2 = blockDamage;
					dictionary[targetPos] = blockDamage;
					targetDmg = blockDamage2;
				}
				else
				{
					targetDmg.RemainingResistance = blockdmg.RemainingResistance;
					targetDmg.LastBreakEllapsedMs = blockdmg.LastBreakEllapsedMs;
					targetDmg.BeginBreakEllapsedMs = blockdmg.BeginBreakEllapsedMs;
					targetDmg.Tool = blockdmg.Tool;
					targetDmg.BreakingCounter = blockdmg.BreakingCounter;
				}
				ClientEventManager clientEventManager = this.eventManager;
				if (clientEventManager == null)
				{
					return;
				}
				clientEventManager.TriggerBlockBreaking(targetDmg);
			}
		}

		public void IncurBlockDamage(BlockSelection blockSelection, EnumTool? withTool, float damage)
		{
			Block block = this.blockAccessor.GetBlock(blockSelection.Position);
			BlockDamage blockdmg = this.loadOrCreateBlockDamage(blockSelection, block, withTool, null);
			long elapsedMs = this.ElapsedMilliseconds;
			int diff = (int)(elapsedMs - blockdmg.LastBreakEllapsedMs);
			blockdmg.RemainingResistance = block.OnGettingBroken(this.player, blockSelection, this.player.inventoryMgr.ActiveHotbarSlot, blockdmg.RemainingResistance, (float)diff / 1000f, blockdmg.BreakingCounter);
			blockdmg.BreakingCounter++;
			blockdmg.Facing = blockSelection.Face;
			if (blockdmg.Position != blockSelection.Position || blockdmg.Block != block)
			{
				blockdmg.RemainingResistance = block.GetResistance(this.BlockAccessor, blockSelection.Position);
				blockdmg.Block = block;
				blockdmg.Position = blockSelection.Position;
			}
			blockdmg.LastBreakEllapsedMs = elapsedMs;
		}

		public BlockDamage loadOrCreateBlockDamage(BlockSelection blockSelection, Block block, EnumTool? tool, IPlayer byPlayer)
		{
			BlockDamage blockdmg;
			this.damagedBlocks.TryGetValue(blockSelection.Position, out blockdmg);
			if (blockdmg == null)
			{
				blockdmg = new BlockDamage
				{
					Position = blockSelection.Position.Copy(),
					Block = block,
					Facing = blockSelection.Face,
					RemainingResistance = block.GetResistance(this.BlockAccessor, blockSelection.Position),
					LastBreakEllapsedMs = this.ElapsedMilliseconds,
					BeginBreakEllapsedMs = this.ElapsedMilliseconds,
					ByPlayer = byPlayer,
					Tool = tool
				};
				this.damagedBlocks[blockSelection.Position.Copy()] = blockdmg;
			}
			else
			{
				EnumTool? tool2 = blockdmg.Tool;
				EnumTool? enumTool = tool;
				if (!((tool2.GetValueOrDefault() == enumTool.GetValueOrDefault()) & (tool2 != null == (enumTool != null))))
				{
					blockdmg.RemainingResistance = block.GetResistance(this.BlockAccessor, blockSelection.Position);
					blockdmg.Tool = tool;
				}
			}
			return blockdmg;
		}

		public void SetCameraShake(float strength)
		{
			this.MainCamera.CameraShakeStrength = strength * ClientSettings.CameraShakeStrength;
		}

		public void AddCameraShake(float strength)
		{
			this.MainCamera.CameraShakeStrength += strength * ClientSettings.CameraShakeStrength;
		}

		public void ReduceCameraShake(float amount)
		{
			this.MainCamera.CameraShakeStrength = Math.Max(0f, this.MainCamera.CameraShakeStrength - amount * ClientSettings.CameraShakeStrength);
		}

		public IBlockAccessor BlockAccessor
		{
			get
			{
				return this.WorldMap.RelaxedBlockAccess;
			}
		}

		public IBulkBlockAccessor BulkBlockAccessor
		{
			get
			{
				return this.WorldMap.BulkBlockAccess;
			}
		}

		IClassRegistryAPI IWorldAccessor.ClassRegistry
		{
			get
			{
				return this.api.instancerapi;
			}
		}

		public Random Rand
		{
			get
			{
				return this.rand.Value;
			}
		}

		public long ElapsedMilliseconds
		{
			get
			{
				return this.Platform.EllapsedMs;
			}
		}

		public List<EntityProperties> EntityTypes
		{
			get
			{
				List<EntityProperties> list;
				if ((list = this.entityTypesCached) == null)
				{
					list = (this.entityTypesCached = this.EntityClassesByCode.Values.ToList<EntityProperties>());
				}
				return list;
			}
		}

		public List<string> EntityTypeCodes
		{
			get
			{
				List<string> list;
				if ((list = this.entityCodesCached) == null)
				{
					list = (this.entityCodesCached = this.makeEntityCodesCache());
				}
				return list;
			}
		}

		private List<string> makeEntityCodesCache()
		{
			ICollection<AssetLocation> keys = this.EntityClassesByCode.Keys;
			List<string> list = new List<string>(keys.Count);
			foreach (AssetLocation key in keys)
			{
				list.Add(key.ToShortString());
			}
			return list;
		}

		public int DefaultEntityTrackingRange
		{
			get
			{
				return 32;
			}
		}

		public ILogger Logger
		{
			get
			{
				return this.Platform.Logger;
			}
		}

		public IAssetManager AssetManager
		{
			get
			{
				return this.Platform.AssetManager;
			}
		}

		public EnumAppSide Side
		{
			get
			{
				return EnumAppSide.Client;
			}
		}

		List<CollectibleObject> IWorldAccessor.Collectibles
		{
			get
			{
				return this.Collectibles;
			}
		}

		List<GridRecipe> IWorldAccessor.GridRecipes
		{
			get
			{
				return this.GridRecipes;
			}
		}

		IList<Block> IWorldAccessor.Blocks
		{
			get
			{
				return this.Blocks;
			}
		}

		IList<Item> IWorldAccessor.Items
		{
			get
			{
				return this.Items;
			}
		}

		List<EntityProperties> IWorldAccessor.EntityTypes
		{
			get
			{
				return this.EntityTypes;
			}
		}

		public IPlayer[] AllOnlinePlayers
		{
			get
			{
				return this.PlayersByUid.Values.Select((ClientPlayer player) => player).ToArray<ClientPlayer>();
			}
		}

		public IPlayer[] AllPlayers
		{
			get
			{
				return this.PlayersByUid.Values.Select((ClientPlayer player) => player).ToArray<ClientPlayer>();
			}
		}

		float[] IWorldAccessor.BlockLightLevels
		{
			get
			{
				return this.WorldMap.BlockLightLevels;
			}
		}

		float[] IWorldAccessor.SunLightLevels
		{
			get
			{
				return this.WorldMap.SunLightLevels;
			}
		}

		public int SeaLevel
		{
			get
			{
				return ClientWorldMap.seaLevel;
			}
		}

		public int MapSizeY
		{
			get
			{
				return this.WorldMap.MapSizeY;
			}
		}

		int IWorldAccessor.SunBrightness
		{
			get
			{
				return this.WorldMap.SunBrightness;
			}
		}

		public bool EntityDebugMode { get; set; }

		bool IClientWorldAccessor.ForceLiquidSelectable
		{
			get
			{
				return this.forceLiquidSelectable;
			}
			set
			{
				this.forceLiquidSelectable = value;
			}
		}

		public CollisionTester CollisionTester
		{
			get
			{
				return this.collTester;
			}
		}

		Dictionary<long, Entity> IClientWorldAccessor.LoadedEntities
		{
			get
			{
				return this.LoadedEntities;
			}
		}

		public Dictionary<int, IMiniDimension> MiniDimensions
		{
			get
			{
				return ((ClientWorldMap)this.worldmap).dimensions;
			}
		}

		public IMiniDimension GetOrCreateDimension(int dimId, Vec3d pos)
		{
			return ((ClientWorldMap)this.worldmap).GetOrCreateDimension(dimId, pos);
		}

		public bool TryGetMiniDimension(Vec3i origin, out IMiniDimension dimension)
		{
			return this.MiniDimensions.TryGetValue(BlockAccessorMovable.CalcSubDimensionId(origin), out dimension);
		}

		public void SetBlocksPreviewDimension(int dimId)
		{
			Dimensions.BlocksPreviewSubDimension_Client = dimId;
		}

		public void SetChunkColumnVisible(int cx, int cz, int dimension)
		{
			for (int cy = 0; cy < this.worldmap.chunkMapSizeY; cy++)
			{
				long index3d = this.worldmap.ChunkIndex3D(cx, cy + dimension * 1024, cz);
				ClientChunk chunk;
				if (this.WorldMap.chunks.TryGetValue(index3d, out chunk))
				{
					int bufIndex = ClientChunk.bufIndex;
					ClientChunk.bufIndex = 0;
					chunk.SetVisible(true);
					ClientChunk.bufIndex = 1;
					chunk.SetVisible(true);
					ClientChunk.bufIndex = bufIndex;
				}
			}
		}

		public ICoreAPI Api
		{
			get
			{
				return this.api;
			}
		}

		public IChunkProvider ChunkProvider
		{
			get
			{
				return this.WorldMap;
			}
		}

		public ILandClaimAPI Claims
		{
			get
			{
				return this.WorldMap;
			}
		}

		public Entity SpawnItemEntity(ItemStack itemstack, Vec3d position, Vec3d velocity = null)
		{
			return null;
		}

		public Entity SpawnItemEntity(ItemStack itemstack, BlockPos pos, Vec3d velocity = null)
		{
			return null;
		}

		public void SpawnEntity(Entity entity)
		{
		}

		public void SpawnPriorityEntity(Entity entity)
		{
		}

		public IPlayer[] GetPlayersAround(Vec3d position, float horRange, float vertRange, ActionConsumable<IPlayer> matches = null)
		{
			List<IPlayer> players = new List<IPlayer>();
			float horRangeSq = horRange * horRange;
			foreach (ClientPlayer player in this.PlayersByUid.Values)
			{
				if (player.Entity != null && player.Entity.Pos.InRangeOf(position, horRangeSq, vertRange) && (matches == null || matches(player)))
				{
					players.Add(player);
				}
			}
			return players.ToArray();
		}

		public Entity GetNearestEntity(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null)
		{
			return base.GetEntitiesAround(position, horRange, vertRange, matches).MinBy((Entity entity) => entity.Pos.SquareDistanceTo(position));
		}

		public Entity GetEntityById(long entityId)
		{
			Entity entity;
			this.LoadedEntities.TryGetValue(entityId, out entity);
			return entity;
		}

		public override Cuboidf[] GetBlockIntersectionBoxes(BlockPos pos)
		{
			return base.GetBlockIntersectionBoxes(pos, this.LiquidSelectable);
		}

		public override bool IsValidPos(BlockPos pos)
		{
			return this.WorldMap.IsValidPos(pos);
		}

		public override Vec3i MapSize
		{
			get
			{
				return this.WorldMap.MapSize;
			}
		}

		public ITreeAttribute Config
		{
			get
			{
				return this.WorldConfig;
			}
		}

		public void TrySetWorldConfig(byte[] configBytes)
		{
			if (this.WorldConfig != null || configBytes == null)
			{
				return;
			}
			this.WorldConfig = new TreeAttribute();
			this.WorldConfig.FromBytes(configBytes);
		}

		public override IBlockAccessor blockAccessor
		{
			get
			{
				return this.WorldMap.RelaxedBlockAccess;
			}
		}

		public override Block GetBlock(BlockPos pos)
		{
			return this.WorldMap.RelaxedBlockAccess.GetBlock(pos);
		}

		public long RegisterGameTickListener(Action<float> OnGameTick, int millisecondInterval, int initialDelayOffsetMs = 0)
		{
			if (this.eventManager != null)
			{
				return this.eventManager.AddGameTickListener(OnGameTick, millisecondInterval, initialDelayOffsetMs);
			}
			return 0L;
		}

		public long RegisterGameTickListener(Action<float> OnGameTick, Action<Exception> errorHandler, int millisecondInterval, int initialDelayOffsetMs = 0)
		{
			return this.eventManager.AddGameTickListener(OnGameTick, errorHandler, millisecondInterval, initialDelayOffsetMs);
		}

		public long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay)
		{
			return this.RegisterCallback(OnTimePassed, millisecondDelay, false);
		}

		public long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay, bool permittedWhilePaused)
		{
			if (this.IsPaused && !permittedWhilePaused)
			{
				this.Logger.Notification("Call to RegisterCallback while game is paused");
				if (ClientSettings.DeveloperMode && this.extendedDebugInfo)
				{
					throw new Exception("Call to RegisterCallback while game is paused. ExtendedDebug info and developermode is enabled, so will crash on this for reporting reasons.");
				}
			}
			if (this.eventManager != null)
			{
				return this.eventManager.AddDelayedCallback(OnTimePassed, (long)millisecondDelay);
			}
			return 0L;
		}

		public long RegisterGameTickListener(Action<IWorldAccessor, BlockPos, float> OnGameTick, BlockPos pos, int millisecondInterval, int initialDelayOffsetMs = 0)
		{
			if (this.eventManager != null)
			{
				return this.eventManager.AddGameTickListener(OnGameTick, pos, millisecondInterval, initialDelayOffsetMs);
			}
			return 0L;
		}

		public long RegisterCallback(Action<IWorldAccessor, BlockPos, float> OnTimePassed, BlockPos pos, int millisecondDelay)
		{
			if (this.eventManager != null)
			{
				return this.eventManager.AddDelayedCallback(OnTimePassed, pos, (long)millisecondDelay);
			}
			return 0L;
		}

		public long RegisterCallbackUnique(Action<IWorldAccessor, BlockPos, float> OnTimePassed, BlockPos pos, int millisecondDelay)
		{
			if (this.eventManager != null)
			{
				return this.eventManager.AddSingleDelayedCallback(OnTimePassed, pos, (long)millisecondDelay);
			}
			return 0L;
		}

		public void UnregisterCallback(long listenerId)
		{
			ClientEventManager clientEventManager = this.eventManager;
			if (clientEventManager == null)
			{
				return;
			}
			clientEventManager.RemoveDelayedCallback(listenerId);
		}

		public void UnregisterGameTickListener(long listenerId)
		{
			ClientEventManager clientEventManager = this.eventManager;
			if (clientEventManager == null)
			{
				return;
			}
			clientEventManager.RemoveGameTickListener(listenerId);
		}

		public void TriggerNeighbourBlocksUpdate(BlockPos pos)
		{
			foreach (BlockFacing facing in BlockFacing.ALLFACES)
			{
				BlockPos neibPos = pos.AddCopy(facing.Normali.X, facing.Normali.Y, facing.Normali.Z);
				Block block = this.WorldMap.RelaxedBlockAccess.GetBlock(neibPos);
				block.OnNeighbourBlockChange(this, neibPos, pos);
				if (!block.ForFluidsLayer)
				{
					Block liquidBlock = this.WorldMap.RelaxedBlockAccess.GetBlock(neibPos, 2);
					if (liquidBlock.BlockId != 0)
					{
						EnumHandling handled = EnumHandling.PassThrough;
						BlockBehavior[] blockBehaviors = liquidBlock.BlockBehaviors;
						for (int j = 0; j < blockBehaviors.Length; j++)
						{
							blockBehaviors[j].OnNeighbourBlockChange(this, neibPos, pos, ref handled);
							if (handled == EnumHandling.PreventSubsequent)
							{
								break;
							}
						}
					}
				}
			}
		}

		public void SpawnParticles(float quantity, int color, Vec3d minPos, Vec3d maxPos, Vec3f minVelocity, Vec3f maxVelocity, float lifeLength, float gravityEffect, float scale = 1f, EnumParticleModel model = EnumParticleModel.Quad, IPlayer dualCallByPlayer = null)
		{
			this.particleManager.SpawnParticles(quantity, color, minPos, maxPos, minVelocity, maxVelocity, lifeLength, gravityEffect, scale, model);
		}

		public void SpawnParticles(IParticlePropertiesProvider particlePropertiesProvider, IPlayer dualCallByPlayer = null)
		{
			this.particleManager.SpawnParticles(particlePropertiesProvider);
		}

		public void SpawnCubeParticles(Vec3d pos, ItemStack itemstack, float radius, int quantity, float scale = 1f, IPlayer dualCallByPlayer = null, Vec3f velocity = null)
		{
			this.particleManager.SpawnParticles(new StackCubeParticles(pos, itemstack, radius, quantity, scale, velocity));
		}

		public void SpawnCubeParticles(BlockPos blockpos, Vec3d pos, float radius, int quantity, float scale = 1f, IPlayer dualCallByPlayer = null, Vec3f velocity = null)
		{
			this.particleManager.SpawnParticles(new BlockCubeParticles(this, blockpos, pos, radius, quantity, scale, velocity));
		}

		public bool PlayerHasPrivilege(int clientid, string privilege)
		{
			return true;
		}

		public void ShowChatMessage(string message)
		{
			ClientEventManager clientEventManager = this.eventManager;
			if (clientEventManager == null)
			{
				return;
			}
			clientEventManager.TriggerNewServerChatLine(GlobalConstants.CurrentChatGroup, message, EnumChatType.Notification, null);
		}

		public void SendMessageToClient(string message)
		{
			ClientEventManager clientEventManager = this.eventManager;
			if (clientEventManager == null)
			{
				return;
			}
			clientEventManager.TriggerNewModChatLine(GlobalConstants.CurrentChatGroup, message, EnumChatType.OwnMessage, null);
		}

		public void SendArbitraryPacket(byte[] data)
		{
			this.SendPacket(data);
		}

		public void SendBlockEntityPacket(int x, int y, int z, int packetId, byte[] data = null)
		{
			Packet_BlockEntityPacket packet = new Packet_BlockEntityPacket
			{
				Packetid = packetId,
				X = x,
				Y = y,
				Z = z
			};
			packet.SetData(data);
			this.SendPacketClient(new Packet_Client
			{
				Id = 22,
				BlockEntityPacket = packet
			});
		}

		public void SendEntityPacket(long entityId, int packetId, byte[] data = null)
		{
			Packet_EntityPacket packet = new Packet_EntityPacket
			{
				Packetid = packetId,
				EntityId = entityId
			};
			packet.SetData(data);
			this.SendPacketClient(new Packet_Client
			{
				Id = 31,
				EntityPacket = packet
			});
		}

		public IPlayer NearestPlayer(double x, double y, double z)
		{
			IPlayer closestplayer = null;
			float closestSqDistance = -1f;
			foreach (ClientPlayer val in this.PlayersByUid_Threadsafe.Values)
			{
				if (val.Entity != null)
				{
					float distanceSq = val.Entity.Pos.SquareDistanceTo(x, y, z);
					if (closestSqDistance == -1f || distanceSq < closestSqDistance)
					{
						closestSqDistance = distanceSq;
						closestplayer = val;
					}
				}
			}
			return closestplayer;
		}

		public IPlayer PlayerByUid(string playerUid)
		{
			if (playerUid == null)
			{
				return null;
			}
			ClientPlayer plr;
			this.PlayersByUid.TryGetValue(playerUid, out plr);
			return plr;
		}

		public ColorMapData GetColorMapData(Block block, int posX, int posY, int posZ)
		{
			return this.WorldMap.getColorMapData(block, posX, posY, posZ);
		}

		public bool LoadEntity(Entity entity, long fromChunkIndex3d)
		{
			throw new InvalidOperationException("Cannot use LoadEntity on the Client side");
		}

		public AssetLocation ResolveSoundPath(AssetLocation location)
		{
			if (this.SoundConfig == null)
			{
				throw new Exception("soundconfig.json not loaded. Is it missing or are you trying to load sounds before the client has received the level finalize event?");
			}
			AssetLocation[] sounds;
			this.SoundConfig.Soundsets.TryGetValue(location, out sounds);
			if (sounds != null && sounds.Length != 0)
			{
				int pos;
				if (!this.SoundIteration.TryGetValue(location, out pos) || this.Rand.NextDouble() < 0.35)
				{
					pos = this.Rand.Next(sounds.Length);
					this.SoundIteration[location] = pos;
				}
				this.SoundIteration[location] = (this.SoundIteration[location] + 1) % sounds.Length;
				return sounds[pos];
			}
			if (location.EndsWithWildCard)
			{
				int catlen = location.Category.Code.Length + 1;
				string basePath = location.Path.Substring(catlen, location.Path.Length - catlen - 1);
				List<IAsset> assets = this.AssetManager.GetManyInCategory(location.Category.Code, basePath, location.Domain, false);
				if (assets.Count > 0)
				{
					return assets[this.rand.Value.Next(assets.Count)].Location;
				}
			}
			return location;
		}

		public void PlaySound(AssetLocation location, bool randomizePitch = false, float volume = 1f)
		{
			this.PlaySoundAt(location, 0.0, 0.0, 0.0, null, randomizePitch, 32f, volume);
		}

		public void PlaySoundAt(AssetLocation location, IPlayer atPlayer, IPlayer ignorePlayerUid = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
		{
			if (atPlayer == null)
			{
				atPlayer = this.Player;
			}
			this.PlaySoundAt(location, atPlayer.Entity.Pos.X, atPlayer.Entity.Pos.InternalY, atPlayer.Entity.Pos.Z, volume, randomizePitch, range);
		}

		public void PlaySoundAt(AssetLocation location, Entity atEntity, IPlayer dualCallByPlayer = null, float pitch = 1f, float range = 32f, float volume = 1f)
		{
			float yoff = 0f;
			if (atEntity.SelectionBox != null)
			{
				yoff = atEntity.SelectionBox.Y2 / 2f;
			}
			else
			{
				EntityProperties properties = atEntity.Properties;
				if (((properties != null) ? properties.CollisionBoxSize : null) != null)
				{
					yoff = atEntity.Properties.CollisionBoxSize.Y / 2f;
				}
			}
			this.PlaySoundAtInternal(location, atEntity.Pos.X, atEntity.Pos.InternalY + (double)yoff, atEntity.Pos.Z, volume, pitch, range, EnumSoundType.Sound);
		}

		public void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer = null, float pitch = 1f, float range = 32f, float volume = 1f)
		{
			this.PlaySoundAtInternal(location, posx, posy, posz, volume, pitch, range, EnumSoundType.Sound);
		}

		public void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer, EnumSoundType soundType, float pitch = 1f, float range = 32f, float volume = 1f)
		{
			this.PlaySoundAtInternal(location, posx, posy, posz, volume, pitch, range, soundType);
		}

		public void PlaySoundAt(AssetLocation location, Entity atEntity, IPlayer ignorePlayerUid = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
		{
			float yoff = 0f;
			if (atEntity.SelectionBox != null)
			{
				yoff = atEntity.SelectionBox.Y2 / 2f;
			}
			else
			{
				EntityProperties properties = atEntity.Properties;
				if (((properties != null) ? properties.CollisionBoxSize : null) != null)
				{
					yoff = atEntity.Properties.CollisionBoxSize.Y / 2f;
				}
			}
			this.PlaySoundAt(location, atEntity.Pos.X, atEntity.Pos.InternalY + (double)yoff, atEntity.Pos.Z, ignorePlayerUid, randomizePitch, range, volume);
		}

		public void PlaySoundAt(AssetLocation location, double x, double y, double z, IPlayer ignorePlayerUid = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
		{
			this.PlaySoundAt(location, x, y, z, volume, randomizePitch, range);
		}

		public void PlaySoundAt(AssetLocation location, BlockPos pos, double yOffsetFromCenter, IPlayer ignorePlayerUid = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
		{
			this.PlaySoundAt(location, (double)pos.X + 0.5, (double)pos.InternalY + 0.5 + yOffsetFromCenter, (double)pos.Z + 0.5, volume, randomizePitch, range);
		}

		public int PlaySoundAtAndGetDuration(AssetLocation location, double x, double y, double z, IPlayer ignorePlayerUid = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
		{
			return this.PlaySoundAt(location, x, y, z, volume, randomizePitch, range);
		}

		public void PlaySoundFor(AssetLocation location, IPlayer atPlayer, float pitch, float range = 32f, float volume = 1f)
		{
			if (atPlayer == null)
			{
				atPlayer = this.Player;
			}
			this.PlaySoundAtInternal(location, atPlayer.Entity.Pos.X, atPlayer.Entity.Pos.InternalY, atPlayer.Entity.Pos.Z, volume, pitch, range, EnumSoundType.Sound);
		}

		public void PlaySoundFor(AssetLocation location, IPlayer forPlayer, bool randomizePitch = true, float range = 32f, float volume = 1f)
		{
			this.PlaySoundAt(location, forPlayer, null, randomizePitch, range, volume);
		}

		public int PlaySoundAt(AssetLocation location, double x, double y, double z, float volume, bool randomizePitch = true, float range = 32f)
		{
			float pitch = (randomizePitch ? base.RandomPitch() : 1f);
			return this.PlaySoundAtInternal(location, x, y, z, volume, pitch, range, EnumSoundType.Sound);
		}

		private int PlaySoundAtInternal(AssetLocation location, double x, double y, double z, float volume, float pitch = 1f, float range = 32f, EnumSoundType soundType = EnumSoundType.Sound)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException("Cannot call PlaySound outside the main thread, it is not thread safe");
			}
			if (this.ActiveSounds.Count >= 250)
			{
				if (this.ElapsedMilliseconds - this.lastSkipTotalMs > 1000L)
				{
					this.Logger.Notification("Play sound {0} skipped because max concurrent sounds of 250 reached ({1} more skipped)", new object[] { location, this.cntSkip });
					this.cntSkip = 0;
					this.lastSkipTotalMs = this.ElapsedMilliseconds;
				}
				this.cntSkip++;
				return 0;
			}
			if (location == null)
			{
				return 0;
			}
			if (ClientSettings.SoundLevel == 0)
			{
				return 0;
			}
			location = this.ResolveSoundPath(location).Clone().WithPathAppendixOnce(".ogg");
			if (location == null)
			{
				return 0;
			}
			SoundParams sparams;
			if (x == 0.0 && y == 0.0 && z == 0.0)
			{
				sparams = new SoundParams(location)
				{
					RelativePosition = true,
					Range = range,
					SoundType = soundType
				};
			}
			else
			{
				if (this.player.Entity.Pos.SquareDistanceTo(x, y, z) > range * range)
				{
					return 0;
				}
				sparams = new SoundParams(location)
				{
					Position = new Vec3f((float)x, (float)y, (float)z),
					Range = range,
					SoundType = soundType
				};
			}
			sparams.Pitch = pitch;
			sparams.Volume *= volume;
			AudioData audiodata;
			ScreenManager.soundAudioData.TryGetValue(location, out audiodata);
			if (audiodata == null)
			{
				this.Platform.Logger.Warning("Audio File not found: {0}", new object[] { location });
				return 0;
			}
			int result = audiodata.Load_Async(new MainThreadAction(this, () => this.StartPlaying(audiodata, sparams, location), "playSound"));
			if (result >= 0)
			{
				return result;
			}
			return 500;
		}

		private int StartPlaying(AudioData audiodata, SoundParams sparams, AssetLocation location)
		{
			if (this.EyesInWaterDepth() > 0f)
			{
				sparams.LowPassFilter = 0.06f;
			}
			ILoadedSound loadedSound = this.Platform.CreateAudio(sparams, audiodata, this);
			if (audiodata.Loaded == 3)
			{
				return this.StartPlaying(loadedSound, location);
			}
			((AudioMetaData)audiodata).AddOnLoaded(new MainThreadAction(this, () => this.StartPlaying(loadedSound, location), "soundplaying"));
			return 100;
		}

		public int StartPlaying(ILoadedSound loadedSound, AssetLocation location)
		{
			loadedSound.Start();
			if (this.EyesInWaterDepth() > 0f && loadedSound.Params.SoundType != EnumSoundType.Music && loadedSound.Params.SoundType != EnumSoundType.MusicGlitchunaffected)
			{
				loadedSound.SetPitchOffset(-0.15f);
			}
			if (this.EyesInWaterDepth() == 0f && SystemSoundEngine.NowReverbness >= 0.25f && (loadedSound.Params.Position == null || loadedSound.Params.Position == SystemSoundEngine.Zero || SystemSoundEngine.RoomLocation.ContainsOrTouches(loadedSound.Params.Position)))
			{
				loadedSound.SetReverb(SystemSoundEngine.NowReverbness);
			}
			if (ClientSettings.DeveloperMode && loadedSound.Channels > 1 && !loadedSound.Params.RelativePosition)
			{
				this.Platform.Logger.Warning("Audio File {0} is a stereo sound but loaded as a locational sound, will not attenuate correctly.", new object[] { location });
			}
			this.ActiveSounds.Enqueue(loadedSound);
			return (int)(loadedSound.SoundLengthSeconds * 1000f);
		}

		public ILoadedSound LoadSound(SoundParams sound)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException("Cannot call LoadSound outside the main thread, it is not thread safe");
			}
			if (sound.Location != null)
			{
				sound.Location = this.ResolveSoundPath(sound.Location.Clone());
				sound.Location.WithPathAppendixOnce(".ogg");
			}
			AudioData data;
			ScreenManager.soundAudioData.TryGetValue(sound.Location, out data);
			if (data == null)
			{
				this.Platform.Logger.Warning("Audio File not found: {0}", new object[] { sound.Location });
				return null;
			}
			ILoadedSound loadedSound = this.Platform.CreateAudio(sound, data, this);
			if (this.EyesInWaterDepth() > 0f)
			{
				loadedSound.SetPitchOffset(-0.2f);
			}
			if (this.EyesInWaterDepth() == 0f && SystemSoundEngine.NowReverbness >= 0.25f && (loadedSound.Params.Position == null || loadedSound.Params.Position == SystemSoundEngine.Zero || SystemSoundEngine.RoomLocation.ContainsOrTouches(loadedSound.Params.Position)))
			{
				loadedSound.SetReverb(SystemSoundEngine.NowReverbness);
			}
			this.ActiveSounds.Enqueue(loadedSound);
			return loadedSound;
		}

		public IClientGameCalendar Calendar
		{
			get
			{
				return this.GameWorldCalendar;
			}
		}

		IGameCalendar IWorldAccessor.Calendar
		{
			get
			{
				return this.GameWorldCalendar;
			}
		}

		public void UpdateEntityChunk(Entity entity, long newChunkIndex3d)
		{
			IWorldChunk newChunk = this.worldmap.GetChunk(newChunkIndex3d);
			if (newChunk == null)
			{
				return;
			}
			IWorldChunk chunk = this.worldmap.GetChunk(entity.InChunkIndex3d);
			if (chunk != null)
			{
				chunk.RemoveEntity(entity.EntityId);
			}
			newChunk.AddEntity(entity);
			entity.InChunkIndex3d = newChunkIndex3d;
		}

		public void RegisterDialog(params GuiDialog[] dialogs)
		{
			for (int i = 0; i < dialogs.Length; i++)
			{
				GuiDialog dialog = dialogs[i];
				if (!this.LoadedGuis.Contains(dialog))
				{
					int startIndex = this.LoadedGuis.FindIndex((GuiDialog d) => d.InputOrder >= dialog.InputOrder);
					if (startIndex < 0)
					{
						startIndex = this.LoadedGuis.Count;
					}
					int endIndex = this.LoadedGuis.FindIndex(startIndex, (GuiDialog d) => d.InputOrder < dialog.InputOrder);
					if (endIndex < 0)
					{
						endIndex = this.LoadedGuis.Count;
					}
					int index = this.LoadedGuis.FindIndex(startIndex, endIndex - startIndex, (GuiDialog d) => d.DrawOrder < dialog.DrawOrder);
					if (index < 0)
					{
						index = endIndex;
					}
					this.LoadedGuis.Insert(index, dialog);
				}
			}
		}

		public void UnregisterDialog(GuiDialog dialog)
		{
			this.LoadedGuis.Remove(dialog);
			this.OpenedGuis.Remove(dialog);
		}

		public int DialogsOpened
		{
			get
			{
				return this.OpenedGuis.Count((GuiDialog elem) => elem.DialogType == EnumDialogType.Dialog);
			}
		}

		public void HighlightBlocks(IPlayer player, int slotId, List<BlockPos> blocks, List<int> colors, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary, float scale = 1f)
		{
			ClientEventManager clientEventManager = this.eventManager;
			if (clientEventManager == null)
			{
				return;
			}
			clientEventManager.TriggerHighlightBlocks(player, slotId, blocks, colors, mode, shape, scale);
		}

		public void HighlightBlocks(IPlayer player, int slotId, List<BlockPos> blocks, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary)
		{
			ClientEventManager clientEventManager = this.eventManager;
			if (clientEventManager == null)
			{
				return;
			}
			clientEventManager.TriggerHighlightBlocks(player, slotId, blocks, null, mode, shape, 1f);
		}

		public void RemoveEntityRenderer(Entity forEntity)
		{
			EntityRenderer renderer;
			this.EntityRenderers.TryGetValue(forEntity.EntityId, out renderer);
			if (renderer != null)
			{
				renderer.Dispose();
			}
			this.EntityRenderers.Remove(forEntity.EntityId);
			forEntity.Properties.Client.Renderer = null;
		}

		public const int millisecondsToTriggerNewFrame = 60;

		public GuiComposerManager GuiComposers;

		public bool clientPlayingFired;

		public GuiScreenRunningGame ScreenRunningGame;

		public bool[] PreviousKeyboardState;

		public bool[] KeyboardState;

		public bool[] KeyboardStateRaw;

		public bool mouseWorldInteractAnyway;

		public int MouseCurrentX;

		public int MouseCurrentY;

		public int particleLevel;

		public bool BlocksReceivedAndLoaded;

		public bool AssetsReceived;

		public bool DoneColorMaps;

		public bool DoneBlockAndItemShapeLoading;

		public bool AutoJumpEnabled;

		public bool IsPaused;

		public int FrameRateMode;

		public bool LagSimulation;

		public bool Spawned;

		public bool doReconnect;

		public bool exitToMainMenu;

		public bool disposed;

		public string disconnectReason;

		public string disconnectAction;

		public List<string> disconnectMissingMods;

		public string exitReason;

		public bool deleteWorld;

		public bool exitToDisconnectScreen;

		public bool ShouldRender2DOverlays;

		public bool AllowFreemove;

		public bool forceLiquidSelectable;

		public float timelapse;

		public float timelapsedCurrent;

		public float timelapseEnd;

		public Stopwatch InWorldStopwatch = new Stopwatch();

		public SystemMusicEngine MusicEngine;

		public bool AllowCameraControl = true;

		public bool AllowCharacterControl = true;

		public bool StartedConnecting;

		public bool UdpTryConnect;

		public bool FallBackToTcp;

		public bool threadsShouldExit;

		public bool ShouldRedrawAllBlocks;

		public bool doTransparentRenderPass = ClientSettings.TransparentRenderPass;

		public bool extendedDebugInfo;

		public bool HandSetAttackBuild;

		public bool HandSetAttackDestroy;

		public string ServerNetworkVersion;

		public string ServerGameVersion;

		public ServerPacketHandler<Packet_Server>[] PacketHandlers;

		public HandleServerCustomUdpPacket HandleCustomUdpPackets;

		public SystemModHandler modHandler;

		public double[] PerspectiveProjectionMat = new double[16];

		public double[] PerspectiveViewMat = new double[16];

		public const int ClientChunksize = 32;

		public const int ClientChunksizeMask = 31;

		public const int ClientChunksizebits = 5;

		public ClientPlayer player;

		public static ClassRegistry ClassRegistry;

		public Dictionary<string, ClientPlayer> PlayersByUid = new Dictionary<string, ClientPlayer>(10);

		public CachingConcurrentDictionary<string, ClientPlayer> PlayersByUid_Threadsafe = new CachingConcurrentDictionary<string, ClientPlayer>();

		public Dictionary<long, Entity> LoadedEntities = new Dictionary<long, Entity>(100);

		public EntityPos SpawnPosition;

		public ClientSystem[] clientSystems;

		public CancellationTokenSource MusicEngineCts;

		public OrderedDictionary<AssetLocation, EntityProperties> EntityClassesByCode = new OrderedDictionary<AssetLocation, EntityProperties>();

		private List<EntityProperties> entityTypesCached;

		private List<string> entityCodesCached;

		public OrderedDictionary<string, string> DebugScreenInfo;

		public AmbientManager AmbientManager;

		public int[][] FastBlockTextureSubidsByBlockAndFace;

		public int textureSize = 32;

		public ShapeTesselatorManager TesselatorManager;

		public BlockTextureAtlasManager BlockAtlasManager;

		public ItemTextureAtlasManager ItemAtlasManager;

		public EntityTextureAtlasManager EntityAtlasManager;

		private Dictionary<AssetLocation, LoadedTexture> texturesByLocation;

		private List<Thread> clientThreads = new List<Thread>();

		private CancellationTokenSource _clientThreadsCts;

		public ClientEventManager eventManager;

		public MacroManager macroManager;

		public PlayerCamera MainCamera;

		public ClientPlatformAbstract Platform;

		public ClientCoreAPI api;

		public ChunkTesselator TerrainChunkTesselator;

		public bool ShouldTesselateTerrain = true;

		public TerrainIlluminator terrainIlluminator;

		public ClientWorldMap WorldMap;

		public float DeltaTimeLimiter = -1f;

		public MeshRef quadModel;

		public const int DisconnectedIconAfterSeconds = 5;

		public bool Drawblockinfo = ClientSettings.ShowBlockInfoHud;

		public bool offScreenRendering = true;

		public NetClient MainNetClient;

		public UNetClient UdpNetClient;

		public ThreadLocal<Random> rand;

		public long LastReceivedMilliseconds;

		public ServerConnectData Connectdata;

		public bool IsSingleplayer;

		public bool OpenedToLan;

		public bool OpenedToInternet;

		public bool KillNextFrame;

		public FrustumCulling frustumCuller;

		public IColorPresets ColorPreset;

		public DefaultShaderUniforms shUniforms = new DefaultShaderUniforms();

		private ClientSystemStartup csStartup;

		public SystemNetworkProcess networkProc;

		private Queue<ClientTask> reversedQueue = new Queue<ClientTask>();

		private Queue<ClientTask> holdingQueue = new Queue<ClientTask>();

		public ShaderProgramGui guiShaderProg;

		public string tickSummary;

		private LoadedTexture tmpTex;

		private double[] set3DProjectionTempMat4;

		public bool CurrentMatrixModeProjection;

		public StackMatrix4 MvMatrix;

		public StackMatrix4 PMatrix;

		private double[] identityMatrix;

		private double[] glScaleTempVec3;

		private double[] glRotateTempVec3;

		private float[] tmpMatrix = new float[16];

		private double[] tmpMatrixd = new double[16];

		public bool preventPlacementInLava = true;

		public MouseButtonState MouseStateRaw = new MouseButtonState();

		public MouseButtonState InWorldMouseState = new MouseButtonState();

		public bool PickBlock;

		public double MouseDeltaX;

		public double MouseDeltaY;

		public double DelayedMouseDeltaX;

		public double DelayedMouseDeltaY;

		private float rotationspeed;

		private float swimmingMouseSmoothing;

		public float mouseYaw;

		public float mousePitch;

		private Vec3f prevMountAngles = new Vec3f();

		private Dictionary<int, Block> noBlocks = new Dictionary<int, Block>();

		private int whitetexture;

		public MultiplayerServerEntry RedirectTo;

		private int lastWidth;

		private int lastHeight;

		public object MainThreadTasksLock = new object();

		public Queue<ClientTask> GameLaunchTasks;

		public Queue<ClientTask> MainThreadTasks;

		private CollisionTester collTester = new CollisionTester();

		internal SoundConfig SoundConfig;

		internal Dictionary<AssetLocation, int> SoundIteration = new Dictionary<AssetLocation, int>();

		internal Queue<ILoadedSound> ActiveSounds = new Queue<ILoadedSound>();

		internal EnumRenderStage currentRenderStage;

		private long lastSkipTotalMs = -1000L;

		private int cntSkip;

		internal ClientGameCalendar GameWorldCalendar;

		internal bool ignoreServerCalendarUpdates;

		public HashSet<ChunkPos> chunkPositionsForRegenTrav = new HashSet<ChunkPos>();

		public object chunkPositionsLock = new object();

		public Queue<long> compactedClientChunks = new Queue<long>();

		public object compactSyncLock = new object();

		internal bool unbindSamplers;

		public object EntityLoadQueueLock = new object();

		public Stack<Entity> EntityLoadQueue = new Stack<Entity>();

		internal List<ModId> ServerMods;

		internal List<string> ServerModIdBlacklist = new List<string>();

		internal List<string> ServerModIdWhitelist = new List<string>();

		private TreeAttribute WorldConfig;

		internal List<GuiDialog> LoadedGuis = new List<GuiDialog>();

		internal List<GuiDialog> OpenedGuis = new List<GuiDialog>();

		internal Dictionary<int, PlayerGroup> OwnPlayerGroupsById = new Dictionary<int, PlayerGroup>();

		internal Dictionary<int, PlayerGroupMembership> OwnPlayerGroupMemembershipsById = new Dictionary<int, PlayerGroupMembership>();

		public int currentGroupid = GlobalConstants.GeneralChatGroup;

		public Dictionary<int, LimitedList<string>> ChatHistoryByPlayerGroup = new Dictionary<int, LimitedList<string>>();

		internal Dictionary<BlockPos, BlockDamage> damagedBlocks = new Dictionary<BlockPos, BlockDamage>();

		internal PickingRayUtil pickingRayUtil = new PickingRayUtil();

		public TrackedPlayerProperties playerProperties = new TrackedPlayerProperties();

		internal object asyncParticleSpawnersLock = new object();

		internal List<ContinousParticleSpawnTaskDelegate> asyncParticleSpawners = new List<ContinousParticleSpawnTaskDelegate>();

		internal List<IPointLight> pointlights = new List<IPointLight>();

		internal Dictionary<long, EntityRenderer> EntityRenderers = new Dictionary<long, EntityRenderer>();

		internal float[] toShadowMapSpaceMatrixFar = Mat4f.Create();

		internal float[] toShadowMapSpaceMatrixNear = Mat4f.Create();

		internal float[] shadowMvpMatrix = Mat4f.Create();

		internal Vec3f FogColorSky = new Vec3f();

		internal int skyGlowTextureId;

		internal int skyTextureId;

		internal int frameSeed;

		internal ChunkRenderer chunkRenderer;

		internal object dirtyChunksLastLock = new object();

		internal UniqueQueue<long> dirtyChunksLast = new UniqueQueue<long>();

		internal object dirtyChunksLock = new object();

		internal UniqueQueue<long> dirtyChunks = new UniqueQueue<long>();

		internal object dirtyChunksPriorityLock = new object();

		internal UniqueQueue<long> dirtyChunksPriority = new UniqueQueue<long>();

		internal object tesselatedChunksLock = new object();

		internal Queue<long> tesselatedChunks = new Queue<long>();

		internal object tesselatedChunksPriorityLock = new object();

		internal Queue<long> tesselatedChunksPriority = new Queue<long>();

		internal ServerInformation ServerInfo;

		internal bool SuspendMainThreadTasks;

		internal bool AssetLoadingOffThread;

		internal bool ServerReady;

		internal ParticleManager particleManager = new ParticleManager();
	}
}
