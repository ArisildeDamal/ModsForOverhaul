using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class ServerCoreAPI : APIBase, ICoreServerAPI, ICoreAPI, ICoreAPICommon
	{
		public override ClassRegistry ClassRegistryNative
		{
			get
			{
				return this.server.ClassRegistryInt;
			}
		}

		public string[] CmdlArguments
		{
			get
			{
				return this.server.RawCmdLineArgs;
			}
		}

		public IChatCommandApi ChatCommands
		{
			get
			{
				return this.commandapi;
			}
		}

		public EnumAppSide Side
		{
			get
			{
				return EnumAppSide.Server;
			}
		}

		IEventAPI ICoreAPI.Event
		{
			get
			{
				return this.eventapi;
			}
		}

		IWorldAccessor ICoreAPI.World
		{
			get
			{
				return this.server;
			}
		}

		public IClassRegistryAPI ClassRegistry
		{
			get
			{
				return this.classregistryapi;
			}
		}

		public IAssetManager Assets
		{
			get
			{
				return this.server.AssetManager;
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
				return this.server.TagRegistry;
			}
		}

		public IPermissionManager Permissions
		{
			get
			{
				return this.server.PlayerDataManager;
			}
		}

		public IGroupManager Groups
		{
			get
			{
				return this.server.PlayerDataManager;
			}
		}

		public IPlayerDataManager PlayerData
		{
			get
			{
				return this.server.PlayerDataManager;
			}
		}

		public IServerEventAPI Event
		{
			get
			{
				return this.eventapi;
			}
		}

		public IWorldManagerAPI WorldManager
		{
			get
			{
				return this.worldapi;
			}
		}

		public IServerAPI Server
		{
			get
			{
				return this.serverapi;
			}
		}

		public IServerNetworkAPI Network
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

		public IServerWorldAccessor World
		{
			get
			{
				return this.server;
			}
		}

		public ILogger Logger
		{
			get
			{
				return ServerMain.Logger;
			}
		}

		public ServerCoreAPI(ServerMain server)
			: base(server)
		{
			this.server = server;
			this.eventapi = new ServerEventAPI(server);
			this.serverapi = new ServerAPI(server);
			this.worldapi = new WorldAPI(server);
			this.classregistryapi = new ClassRegistryAPI(server, ServerMain.ClassRegistry);
			this.networkapi = new NetworkAPI(server);
			this.commandapi = new ChatCommandApi(this);
		}

		public void RegisterEntityClass(string entityClassName, EntityProperties config)
		{
			this.server.EntityTypesByCode[config.Code] = config;
			config.Id = this.server.EntityTypesByCode.Count;
			this.server.entityTypesCached = null;
			this.server.entityCodesCached = null;
		}

		public void SendIngameError(IServerPlayer player, string code, string message = null, params object[] langparams)
		{
			this.server.SendIngameError(player, code, message, langparams);
		}

		public void SendIngameDiscovery(IServerPlayer player, string code, string message = null, params object[] langparams)
		{
			this.server.SendIngameDiscovery(player, code, message, langparams);
		}

		public void SendMessage(IPlayer player, int groupid, string message, EnumChatType chatType, string data = null)
		{
			this.server.SendMessage((IServerPlayer)player, groupid, message, chatType, data);
		}

		public void SendMessageToGroup(int groupid, string message, EnumChatType chatType, string data = null)
		{
			this.server.SendMessageToGroup(groupid, message, chatType, null, data);
		}

		public void BroadcastMessageToAllGroups(string message, EnumChatType chatType, string data = null)
		{
			this.server.BroadcastMessageToAllGroups(message, chatType, data);
		}

		public void RegisterItem(Item item)
		{
			this.server.RegisterItem(item);
		}

		public void RegisterBlock(Block block)
		{
			this.server.RegisterBlock(block);
		}

		public void RegisterCraftingRecipe(GridRecipe recipe)
		{
			this.server.GridRecipes.Add(recipe);
		}

		public void RegisterTreeGenerator(AssetLocation generatorCode, ITreeGenerator gen)
		{
			this.server.TreeGeneratorsByTreeCode[generatorCode] = gen;
		}

		public void RegisterTreeGenerator(AssetLocation generatorCode, GrowTreeDelegate dele)
		{
			this.server.TreeGeneratorsByTreeCode[generatorCode] = new ServerCoreAPI.TreeGenWrapper
			{
				dele = dele
			};
		}

		public override void RegisterColorMap(ColorMap map)
		{
			this.server.ColorMaps[map.Code] = map;
		}

		public bool RegisterCommand(ServerChatCommand chatcommand)
		{
			return this.commandapi.RegisterCommand(chatcommand);
		}

		[Obsolete("Better to directly use new ChatCommands api instead")]
		public bool RegisterCommand(string command, string descriptionMsg, string syntaxMsg, ServerChatCommandDelegate handler, string requiredPrivilege = null)
		{
			return this.commandapi.RegisterCommand(command, descriptionMsg, syntaxMsg, handler, requiredPrivilege);
		}

		public void HandleCommand(IServerPlayer player, string message)
		{
			string command = message.Split(new char[] { ' ' })[0].Replace("/", "");
			command = command.ToLowerInvariant();
			string argument = ((message.IndexOf(' ') < 0) ? "" : message.Substring(message.IndexOf(' ') + 1));
			this.commandapi.Execute(command, player, GlobalConstants.CurrentChatGroup, argument, null);
		}

		public void InjectConsole(string message)
		{
			this.server.ReceiveServerConsole(message);
		}

		public void TriggerOnAssetsFirstLoaded()
		{
			this.server.ModEventManager.OnAssetsFirstLoaded();
		}

		internal ServerEventAPI eventapi;

		internal ServerAPI serverapi;

		internal WorldAPI worldapi;

		internal ClassRegistryAPI classregistryapi;

		internal NetworkAPI networkapi;

		internal ModLoader modLoader;

		internal ChatCommandApi commandapi;

		private ServerMain server;

		public class TreeGenWrapper : ITreeGenerator
		{
			public void GrowTree(IBlockAccessor blockAccessor, BlockPos pos, TreeGenParams treegenParams, IRandom random)
			{
				this.dele(blockAccessor, pos, treegenParams);
			}

			public GrowTreeDelegate dele;
		}
	}
}
