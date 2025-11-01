using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class ServerSystemLoadConfig : ServerSystem
	{
		public ServerSystemLoadConfig(ServerMain server)
			: base(server)
		{
			server.EventManager.OnSaveGameLoaded += this.OnSaveGameLoaded;
		}

		public override int GetUpdateInterval()
		{
			return 100;
		}

		public override void OnServerTick(float dt)
		{
			if (this.server.ConfigNeedsSaving)
			{
				this.server.ConfigNeedsSaving = false;
				ServerSystemLoadConfig.SaveConfig(this.server);
			}
		}

		public override void OnBeginConfiguration()
		{
			ServerSystemLoadConfig.EnsureConfigExists(this.server);
			ServerSystemLoadConfig.LoadConfig(this.server);
			if (this.server.Standalone)
			{
				this.server.Config.ApplyStartServerArgs(this.server.Config.WorldConfig);
			}
			else
			{
				this.server.Config.ApplyStartServerArgs(this.server.serverStartArgs);
			}
			if (this.server.Config.Roles == null || this.server.Config.Roles.Count == 0)
			{
				this.server.Config.InitializeRoles();
			}
			if (this.server.Config.LoadedConfigVersion == "1.0")
			{
				this.server.Config.InitializeRoles();
				ServerSystemLoadConfig.SaveConfig(this.server);
			}
		}

		public static void EnsureConfigExists(ServerMain server)
		{
			string filename = "serverconfig.json";
			if (!File.Exists(Path.Combine(GamePaths.Config, filename)))
			{
				ServerMain.Logger.Notification("serverconfig.json not found, creating new one");
				ServerSystemLoadConfig.GenerateConfig(server);
				ServerSystemLoadConfig.SaveConfig(server);
			}
		}

		private void OnSaveGameLoaded()
		{
			ServerConfig config = this.server.Config;
			ServerWorldMap worldmap = this.server.WorldMap;
			this.server.AddChunkColumnToForceLoadedList(worldmap.MapChunkIndex2D(worldmap.ChunkMapSizeX / 2, worldmap.ChunkMapSizeZ / 2));
			PlayerSpawnPos plrSpawn = this.server.SaveGameData.DefaultSpawn;
			if (plrSpawn != null)
			{
				this.server.AddChunkColumnToForceLoadedList(worldmap.MapChunkIndex2D(plrSpawn.x / 32, plrSpawn.z / 32));
			}
			foreach (PlayerRole role in config.RolesByCode.Values)
			{
				if (role.DefaultSpawn != null)
				{
					this.server.AddChunkColumnToForceLoadedList(worldmap.MapChunkIndex2D(role.DefaultSpawn.x / 32, role.DefaultSpawn.z / 32));
				}
				if (role.ForcedSpawn != null)
				{
					this.server.AddChunkColumnToForceLoadedList(worldmap.MapChunkIndex2D(role.ForcedSpawn.x / 32, role.ForcedSpawn.z / 32));
				}
			}
		}

		public override void OnBeginRunGame()
		{
			base.OnBeginRunGame();
			if (this.server.Config.StartupCommands != null)
			{
				ServerMain.Logger.Notification("Running startup commands");
				foreach (string line in this.server.Config.StartupCommands.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
				{
					this.server.ReceiveServerConsole(line);
				}
			}
		}

		public static void GenerateConfig(ServerMain server)
		{
			server.Config = new ServerConfig();
			server.Config.InitializeRoles();
			if (server.Standalone)
			{
				server.Config.ApplyStartServerArgs(server.Config.WorldConfig);
				return;
			}
			server.Config.ApplyStartServerArgs(server.serverStartArgs);
		}

		public static void LoadConfig(ServerMain server)
		{
			string filename = "serverconfig.json";
			try
			{
				string config = File.ReadAllText(Path.Combine(GamePaths.Config, filename));
				server.Config = JsonConvert.DeserializeObject<ServerConfig>(config);
			}
			catch (JsonReaderException e)
			{
				ServerMain.Logger.Error("Failed to read serverconfig.json");
				ServerMain.Logger.Error(e);
				ServerMain.Logger.StoryEvent("Failed to read serverconfig.json. Did you modify it? See server-main.log for the affected line. Will stop the server.");
				server.Config = new ServerConfig();
				server.Stop("serverconfig.json read error", null, EnumLogType.Notification);
				return;
			}
			if (server.Config == null)
			{
				Logger logger = ServerMain.Logger;
				if (logger != null)
				{
					logger.Notification("The deserialized serverconfig.json was null? Creating new one.");
				}
				server.Config = new ServerConfig();
				server.Config.InitializeRoles();
				ServerSystemLoadConfig.SaveConfig(server);
			}
			if (server.progArgs.WithConfig != null)
			{
				JObject fileConfig;
				using (TextReader textReader = new StreamReader(Path.Combine(GamePaths.Config, filename)))
				{
					fileConfig = JToken.Parse(textReader.ReadToEnd()) as JObject;
					textReader.Close();
				}
				JObject runtimeConfig = JToken.Parse(server.progArgs.WithConfig) as JObject;
				fileConfig.Merge(runtimeConfig);
				server.Config = fileConfig.ToObject<ServerConfig>();
				ServerSystemLoadConfig.SaveConfig(server);
			}
			Logger.LogFileSplitAfterLine = server.Config.LogFileSplitAfterLine;
			int maxClients;
			if (server.progArgs.MaxClients != null && int.TryParse(server.progArgs.MaxClients, out maxClients))
			{
				server.Config.MaxClientsProgArgs = maxClients;
			}
		}

		public static void SaveConfig(ServerMain server)
		{
			if (server.Standalone)
			{
				server.Config.FileEditWarning = "";
			}
			else
			{
				server.Config.FileEditWarning = "PLEASE NOTE: This file is also loaded when you start a single player world. If you want to run a dedicated server without affecting single player, we recommend you install the game into a different folder and run the server from there.";
			}
			server.Config.Save();
		}
	}
}
