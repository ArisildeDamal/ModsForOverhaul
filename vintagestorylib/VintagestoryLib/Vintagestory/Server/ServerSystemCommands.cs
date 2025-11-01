using System;

namespace Vintagestory.Server
{
	internal class ServerSystemCommands : ServerSystem
	{
		public ServerSystemCommands(ServerMain server)
			: base(server)
		{
		}

		public override void OnBeginConfiguration()
		{
			new CmdKickBan(this.server);
			new CmdAnnounce(this.server);
			new CmdTp(this.server);
			new CmdLand(this.server);
			new CmdGlobalList(this.server);
			new CmdHelp(this.server);
			new CmdServerConfig(this.server);
			new CmdWorldConfig(this.server);
			new CmdWorldConfigCreate(this.server);
			new CmdEntity(this.server);
			new CmdGive(this.server);
			new CmdDebug(this.server);
			new CmdStop(this.server);
			new CmdStats(this.server);
			new CmdInfo(this.server);
			new CmdChunk(this.server);
			new CmdModDBUtil(this.server);
			if (!this.server.IsDedicatedServer)
			{
				new CmdToggleAllowLan(this.server);
			}
			new CmdSetBlock(this.server);
			new CmdExecuteAs(this.server);
			new CmdActivate(this.server);
			new CmdTime(this.server);
		}
	}
}
