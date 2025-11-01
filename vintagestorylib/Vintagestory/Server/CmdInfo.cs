using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	public class CmdInfo
	{
		public CmdInfo(ServerMain server)
		{
			server.api.commandapi.Create("info").RequiresPrivilege(Privilege.controlserver).WithDesc("Server information")
				.BeginSub("ident")
				.WithDesc("Get save game identifier")
				.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(server.SaveGameData.SavegameIdentifier, null))
				.EndSub()
				.BeginSub("seed")
				.WithDesc("Get world seed")
				.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(server.SaveGameData.Seed.ToString() ?? "", null))
				.EndSub()
				.BeginSub("createdversion")
				.WithDesc("Get game version on which this save game was created on")
				.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(server.SaveGameData.CreatedGameVersion, null))
				.EndSub()
				.BeginSub("mapsize")
				.WithDesc("Get world map size")
				.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(string.Concat(new string[]
				{
					server.SaveGameData.MapSizeX.ToString(),
					"x",
					server.SaveGameData.MapSizeY.ToString(),
					"x",
					server.SaveGameData.MapSizeZ.ToString()
				}), null))
				.EndSub();
		}
	}
}
