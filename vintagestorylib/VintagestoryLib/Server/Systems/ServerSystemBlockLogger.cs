using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace VintagestoryLib.Server.Systems
{
	public class ServerSystemBlockLogger : ServerSystem
	{
		public ServerSystemBlockLogger(ServerMain server)
			: base(server)
		{
			this.sapi = (ICoreServerAPI)server.Api;
			this.sapi.Event.ServerRunPhase(EnumServerRunPhase.LoadGamePre, new Action(this.OnConfigReady));
		}

		private void OnConfigReady()
		{
			if (this.sapi.Server.Config.LogBlockBreakPlace)
			{
				this.sapi.Event.DidBreakBlock += this.DidBreakBock;
				this.sapi.Event.DidPlaceBlock += this.DidPlaceBLock;
			}
		}

		private void DidPlaceBLock(IServerPlayer byplayer, int oldblockid, BlockSelection blocksel, ItemStack withitemstack)
		{
			if (oldblockid != 0)
			{
				string oldBlock = this.sapi.World.GetBlock(oldblockid).Code.ToString();
				this.sapi.Logger.Build("{0} placed {1} [pre: {2}] at {3}", new object[]
				{
					byplayer.PlayerName,
					withitemstack.Collectible.Code.ToString(),
					oldBlock,
					blocksel.Position
				});
				return;
			}
			this.sapi.Logger.Build("{0} placed {1} at {2}", new object[]
			{
				byplayer.PlayerName,
				withitemstack.Collectible.Code.ToString(),
				blocksel.Position
			});
		}

		private void DidBreakBock(IServerPlayer byplayer, int oldblockid, BlockSelection blocksel)
		{
			string oldBlock = ((oldblockid != 0) ? this.sapi.World.GetBlock(oldblockid).Code.ToString() : "Air");
			this.sapi.Logger.Build("{0} removed {1} at {2}", new object[] { byplayer.PlayerName, oldBlock, blocksel.Position });
		}

		public override void Dispose()
		{
			if (this.sapi.Server.Config.LogBlockBreakPlace)
			{
				this.sapi.Event.DidBreakBlock -= this.DidBreakBock;
				this.sapi.Event.DidPlaceBlock -= this.DidPlaceBLock;
			}
		}

		private ICoreServerAPI sapi;
	}
}
