using System;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.ModDb;

namespace Vintagestory.Server
{
	public class ServerSystemModHandler : ServerSystem
	{
		public ServerSystemModHandler(ServerMain server)
			: base(server)
		{
			server.api = new ServerCoreAPI(server);
		}

		public override void OnBeginInitialization()
		{
			this.server.api.eventapi.OnServerStage(EnumServerRunPhase.Initialization);
		}

		public override void OnLoadAssets()
		{
			List<string> modSearchPaths = new List<string>(this.server.Config.ModPaths);
			if (this.server.progArgs.AddModPath != null)
			{
				modSearchPaths.AddRange(this.server.progArgs.AddModPath);
			}
			this.loader = new ModLoader(this.server.api, modSearchPaths, this.server.progArgs.TraceLog);
			this.server.api.modLoader = this.loader;
			if (!this.server.Config.DisableModSafetyCheck)
			{
				while (ModDbUtil.ModBlockList == null)
				{
					Thread.Sleep(20);
				}
			}
			this.loader.LoadMods(this.server.Config.WorldConfig.DisabledMods);
			if (this.server.SaveGameData.IsNewWorld)
			{
				SaveGame.SetNewWorldConfig(this.server.SaveGameData, this.server);
				this.server.SaveGameData.WillSave(new FastMemoryStream());
			}
			this.loader.RunModPhase(ModRunPhase.Pre);
			ServerMain.Logger.VerboseDebug("Searching file system (including mods) for asset files");
			this.server.AssetManager.AddExternalAssets(ServerMain.Logger, this.loader);
			ServerMain.Logger.VerboseDebug("Finished building index of asset files");
			foreach (KeyValuePair<string, ITranslationService> locale in Lang.AvailableLanguages)
			{
				locale.Value.Invalidate();
			}
			Lang.Load(ServerMain.Logger, this.server.AssetManager, this.server.Config.ServerLanguage);
			ServerMain.Logger.Notification("Reloaded lang file with mod assets");
			ServerMain.Logger.VerboseDebug("Reloaded lang file with mod assets");
			this.loader.RunModPhase(ModRunPhase.Start);
			ServerMain.Logger.VerboseDebug("Started mods");
			this.loader.RunModPhase(ModRunPhase.AssetsLoaded);
		}

		public override void OnFinalizeAssets()
		{
			this.loader.RunModPhase(ModRunPhase.AssetsFinalize);
		}

		public override void OnBeginConfiguration()
		{
			if (!this.server.Config.DisableModSafetyCheck)
			{
				TyronThreadPool.QueueTask(async delegate
				{
					await ModDbUtil.GetBlockedModsAsync(ServerMain.Logger);
				});
			}
			this.server.api.eventapi.OnServerStage(EnumServerRunPhase.Configuration);
		}

		public override void OnBeginModsAndConfigReady()
		{
			this.loader.RunModPhase(ModRunPhase.Normal);
			this.server.api.eventapi.OnServerStage(EnumServerRunPhase.LoadGamePre);
		}

		public override void OnBeginWorldReady()
		{
			this.server.api.eventapi.OnServerStage(EnumServerRunPhase.WorldReady);
		}

		public override void OnBeginGameReady(SaveGame savegame)
		{
			this.server.api.eventapi.OnServerStage(EnumServerRunPhase.GameReady);
		}

		public override void OnBeginRunGame()
		{
			this.server.api.eventapi.OnServerStage(EnumServerRunPhase.RunGame);
		}

		public override void OnBeginShutdown()
		{
			this.server.api.eventapi.OnServerStage(EnumServerRunPhase.Shutdown);
			ModLoader modLoader = this.loader;
			if (modLoader == null)
			{
				return;
			}
			modLoader.Dispose();
		}

		private ModLoader loader;
	}
}
