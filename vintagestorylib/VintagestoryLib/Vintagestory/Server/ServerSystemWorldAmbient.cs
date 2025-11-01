using System;
using System.IO;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	public class ServerSystemWorldAmbient : ServerSystem
	{
		public ServerSystemWorldAmbient(ServerMain server)
			: base(server)
		{
			this.serverSettings = new AmbientModifier().EnsurePopulated();
			server.EventManager.OnGameWorldBeingSaved += this.OnSaving;
			server.api.ChatCommands.Create("setambient").WithDescription("Sets the server controlled ambient for everyone. Json format.").RequiresPrivilege(Privilege.controlserver)
				.WithArgs(new ICommandArgumentParser[] { server.api.ChatCommands.Parsers.All("json_code") })
				.HandleWith(new OnCommandDelegate(this.OnSetAmbient));
		}

		private TextCommandResult OnSetAmbient(TextCommandCallingArgs args)
		{
			try
			{
				this.serverSettings = JsonConvert.DeserializeObject<AmbientModifier>(args[0] as string).EnsurePopulated();
				this.server.BroadcastPacket(this.GetAmbientPacket(), Array.Empty<IServerPlayer>());
			}
			catch
			{
				return TextCommandResult.Success("Failed parsing the json", null);
			}
			return TextCommandResult.Success("", null);
		}

		public override void OnPlayerJoin(ServerPlayer player)
		{
			this.server.SendPacket(player.ClientId, this.GetAmbientPacket());
		}

		private Packet_Server GetAmbientPacket()
		{
			Packet_Server p = new Packet_Server
			{
				Id = 65,
				Ambient = new Packet_Ambient()
			};
			using (MemoryStream ms = new MemoryStream())
			{
				this.serverSettings.ToBytes(new BinaryWriter(ms));
				p.Ambient.SetData(ms.ToArray());
			}
			return p;
		}

		private void OnSaving()
		{
			using (MemoryStream ms = new MemoryStream())
			{
				this.serverSettings.ToBytes(new BinaryWriter(ms));
				this.server.SaveGameData.ModData["ambient"] = ms.ToArray();
			}
		}

		public override void OnBeginGameReady(SaveGame savegame)
		{
			byte[] data;
			if (savegame.ModData.TryGetValue("ambient", out data))
			{
				try
				{
					using (MemoryStream ms = new MemoryStream(data))
					{
						this.serverSettings.FromBytes(new BinaryReader(ms));
					}
					this.serverSettings.EnsurePopulated();
				}
				catch
				{
				}
			}
			base.OnBeginGameReady(savegame);
			if (savegame.IsNewWorld)
			{
				this.serverSettings = AmbientModifier.DefaultAmbient;
				float newWeight = 0f;
				this.serverSettings.AmbientColor.Weight = 0f;
				this.serverSettings.FogColor.Weight = newWeight;
				this.serverSettings.FogDensity.Weight = newWeight;
				this.serverSettings.FogMin.Weight = newWeight;
				this.serverSettings.CloudBrightness.Weight = newWeight;
				this.serverSettings.CloudDensity.Weight = newWeight;
			}
		}

		private AmbientModifier serverSettings;
	}
}
