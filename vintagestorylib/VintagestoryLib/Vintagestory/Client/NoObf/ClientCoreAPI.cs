using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class ClientCoreAPI : APIBase, ICoreClientAPI, ICoreAPI, ICoreAPICommon
	{
		public override ClassRegistry ClassRegistryNative
		{
			get
			{
				return this.instancerapi.registry;
			}
		}

		public Dictionary<string, Tag2RichTextDelegate> TagConverters
		{
			get
			{
				return VtmlUtil.TagConverters;
			}
		}

		public string[] CmdlArguments
		{
			get
			{
				return ScreenManager.RawCmdLineArgs;
			}
		}

		public EnumAppSide Side
		{
			get
			{
				return EnumAppSide.Client;
			}
		}

		IEventAPI ICoreAPI.Event
		{
			get
			{
				return this.eventapi;
			}
		}

		public IChatCommandApi ChatCommands
		{
			get
			{
				return this.chatcommandapi;
			}
		}

		IWorldAccessor ICoreAPI.World
		{
			get
			{
				return this.game;
			}
		}

		public IClassRegistryAPI ClassRegistry
		{
			get
			{
				return this.instancerapi;
			}
		}

		public IAssetManager Assets
		{
			get
			{
				return this.game.Platform.AssetManager;
			}
		}

		public IXPlatformInterface Forms
		{
			get
			{
				return this.game.Platform.XPlatInterface;
			}
		}

		public IModLoader ModLoader
		{
			get
			{
				return this.modLoader;
			}
		}

		public ITagRegistry TagRegistry
		{
			get
			{
				return this.game.TagRegistry;
			}
		}

		public ILogger Logger
		{
			get
			{
				return this.game.Logger;
			}
		}

		public bool IsShuttingDown
		{
			get
			{
				return this.game.Platform.IsShuttingDown || this.game.disposed;
			}
		}

		public bool IsGamePaused
		{
			get
			{
				return this.game.IsPaused;
			}
		}

		public long ElapsedMilliseconds
		{
			get
			{
				return this.game.Platform.EllapsedMs;
			}
		}

		public long InWorldEllapsedMilliseconds
		{
			get
			{
				return this.game.InWorldEllapsedMs;
			}
		}

		public bool HideGuis
		{
			get
			{
				return !this.game.ShouldRender2DOverlays;
			}
		}

		public IClientEventAPI Event
		{
			get
			{
				return this.eventapi;
			}
		}

		public IAmbientManager Ambient
		{
			get
			{
				return this.game.AmbientManager;
			}
		}

		public IRenderAPI Render
		{
			get
			{
				return this.renderapi;
			}
		}

		public IShaderAPI Shader
		{
			get
			{
				return this.shaderapi;
			}
		}

		public IGuiAPI Gui
		{
			get
			{
				return this.guiapi;
			}
		}

		public IInputAPI Input
		{
			get
			{
				return this.inputapi;
			}
		}

		public IColorPresets ColorPreset
		{
			get
			{
				return this.game.ColorPreset;
			}
		}

		public IMacroManager MacroManager
		{
			get
			{
				return this.game.macroManager;
			}
		}

		public ITesselatorManager TesselatorManager
		{
			get
			{
				return this.game.TesselatorManager;
			}
		}

		public ITesselatorAPI Tesselator
		{
			get
			{
				return this.game.TesselatorManager.Tesselator;
			}
		}

		public IBlockTextureAtlasAPI BlockTextureAtlas
		{
			get
			{
				return this.game.BlockAtlasManager;
			}
		}

		public IItemTextureAtlasAPI ItemTextureAtlas
		{
			get
			{
				return this.game.ItemAtlasManager;
			}
		}

		public ITextureAtlasAPI EntityTextureAtlas
		{
			get
			{
				return this.game.EntityAtlasManager;
			}
		}

		public IClientNetworkAPI Network
		{
			get
			{
				return this.networkapi;
			}
		}

		INetworkAPI ICoreAPI.Network
		{
			get
			{
				return this.networkapi;
			}
		}

		public IClientWorldAccessor World
		{
			get
			{
				return this.game;
			}
		}

		public ClientCoreAPI(ClientMain game)
			: base(game)
		{
			this.game = game;
			this.instancerapi = new ClassRegistryAPI(game, ClientMain.ClassRegistry);
			this.eventapi = new ClientEventAPI(game);
			this.renderapi = new RenderAPIGame(this, game);
			this.networkapi = new NetworkAPI(game);
			this.shaderapi = new ShaderAPI(game);
			this.inputapi = new InputAPI(game);
			this.guiapi = new GuiAPI(game, this);
			this.chatcommandapi = new ChatCommandApi(this);
		}

		internal void Dispose()
		{
			this.renderapi.Dispose();
		}

		public void RegisterEntityClass(string entityClassName, EntityProperties config)
		{
		}

		public void RegisterLinkProtocol(string protocolname, Action<LinkTextComponent> onLinkClicked)
		{
			this.linkProtocols[protocolname] = onLinkClicked;
		}

		public IEnumerable<object> OpenedGuis
		{
			get
			{
				return this.game.OpenedGuis;
			}
		}

		public ISettings Settings
		{
			get
			{
				return ClientSettings.Inst;
			}
		}

		public IMusicTrack CurrentMusicTrack
		{
			get
			{
				ClientEventManager eventManager = this.game.eventManager;
				if (eventManager == null)
				{
					return null;
				}
				return eventManager.CurrentTrackSupplier();
			}
		}

		public Dictionary<string, Action<LinkTextComponent>> LinkProtocols
		{
			get
			{
				return this.linkProtocols;
			}
		}

		public bool IsSinglePlayer
		{
			get
			{
				return this.game.IsSingleplayer;
			}
		}

		public bool OpenedToLan
		{
			get
			{
				return this.game.OpenedToLan;
			}
		}

		public bool PlayerReadyFired
		{
			get
			{
				return this.game.clientPlayingFired;
			}
		}

		public void SendPacketClient(object packet)
		{
			this.game.SendPacketClient((Packet_Client)packet);
		}

		public bool RegisterCommand(ClientChatCommand chatcommand)
		{
			return this.chatcommandapi.RegisterCommand(chatcommand);
		}

		public bool RegisterCommand(string command, string descriptionMsg, string syntaxMsg, ClientChatCommandDelegate handler)
		{
			return this.chatcommandapi.RegisterCommand(command, descriptionMsg, syntaxMsg, handler, null);
		}

		public void TriggerIngameError(object sender, string errorCode, string text)
		{
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TriggerIngameError(sender, errorCode, text);
		}

		public void TriggerIngameDiscovery(object sender, string errorCode, string text)
		{
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TriggerIngameDiscovery(sender, errorCode, text);
		}

		public void RegisterEntityRendererClass(string className, Type rendererType)
		{
			ClientMain.ClassRegistry.RegisterEntityRendererType(className, rendererType);
		}

		public void ShowChatMessage(string message)
		{
			this.game.ShowChatMessage(message);
		}

		public void TriggerChatMessage(string message)
		{
			this.game.SendMessageToClient(message);
		}

		public void SendChatMessage(string message, int groupId, string data = null)
		{
			this.game.SendPacketClient(ClientPackets.Chat(groupId, message, data));
		}

		public void SendChatMessage(string message, string data = null)
		{
			this.game.SendPacketClient(ClientPackets.Chat(this.game.currentGroupid, message, data));
		}

		public void ShowChatNotification(string message)
		{
			this.game.ShowChatMessage(message);
		}

		public MusicTrack StartTrack(AssetLocation soundLocation, float priority, EnumSoundType soundType, Action<ILoadedSound> onLoaded = null)
		{
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return null;
			}
			return eventManager.TrackStarter(soundLocation, priority, soundType, onLoaded);
		}

		public void StartTrack(MusicTrack track, float priority, EnumSoundType soundType, bool playNow = true)
		{
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TrackStarterLoaded(track, priority, soundType, playNow);
		}

		public override void RegisterColorMap(ColorMap map)
		{
			this.game.ColorMaps[map.Code] = map;
		}

		public void PauseGame(bool paused)
		{
			this.game.PauseGame(paused);
		}

		internal ClassRegistryAPI instancerapi;

		internal ClientEventAPI eventapi;

		internal RenderAPIGame renderapi;

		internal NetworkAPI networkapi;

		internal ShaderAPI shaderapi;

		internal InputAPI inputapi;

		internal GuiAPI guiapi;

		internal ModLoader modLoader;

		internal ChatCommandApi chatcommandapi;

		public Dictionary<string, Action<LinkTextComponent>> linkProtocols = new Dictionary<string, Action<LinkTextComponent>>();

		private ClientMain game;

		public bool disposed;
	}
}
