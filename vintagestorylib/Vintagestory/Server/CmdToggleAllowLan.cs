using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Server.Network;

namespace Vintagestory.Server
{
	internal class CmdToggleAllowLan
	{
		public CmdToggleAllowLan(ServerMain server)
		{
			this.server = server;
			server.api.ChatCommands.Create("allowlan").RequiresPrivilege(Privilege.controlserver).WithDescription("Whether or not to allow external LAN connections to the server")
				.WithAdditionalInformation("(this is a temporary runtime setting for non dedicated servers, i.e. single player games)")
				.WithArgs(new ICommandArgumentParser[] { server.api.ChatCommands.Parsers.OptionalBool("state", "on") })
				.HandleWith(new OnCommandDelegate(this.handle));
		}

		private TextCommandResult handle(TextCommandCallingArgs args)
		{
			if (args.Parsers[0].IsMissing)
			{
				return TextCommandResult.Success("LAN connections are currently " + ((this.server.MainSockets[1] == null) ? "disabled" : "enabled"), null);
			}
			if ((bool)args[0])
			{
				if (this.server.MainSockets[1] == null)
				{
					this.server.MainSockets[1] = new TcpNetServer();
					this.server.MainSockets[1].SetIpAndPort(this.server.CurrentIp, this.server.CurrentPort);
					this.server.MainSockets[1].Start();
					this.server.UdpSockets[1] = new UdpNetServer(this.server.Clients);
					this.server.UdpSockets[1].SetIpAndPort(this.server.CurrentIp, this.server.CurrentPort);
					this.server.UdpSockets[1].Start();
					if (this.server.CurrentIp != null)
					{
						string currentIp = this.server.CurrentIp;
					}
					else
					{
						RuntimeEnv.GetLocalIpAddress();
					}
					return TextCommandResult.Success(Lang.Get("LAN connections enabled, players in the local network can now connect", Array.Empty<object>()), null);
				}
				return TextCommandResult.Success("LAN connections was already enabled", null);
			}
			else
			{
				if (this.server.MainSockets[1] == null)
				{
					return TextCommandResult.Success("LAN connections was already disabled", null);
				}
				this.server.MainSockets[1].Dispose();
				this.server.MainSockets[1] = null;
				this.server.UdpSockets[1].Dispose();
				this.server.UdpSockets[1] = null;
				return TextCommandResult.Success("LAN connections disabled", null);
			}
		}

		private ServerMain server;
	}
}
