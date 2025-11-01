using System;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	public class ServerSystemAutoSaveGame : ServerSystem
	{
		public ServerSystemAutoSaveGame(ServerMain server)
			: base(server)
		{
			server.api.ChatCommands.Create("autosavenow").RequiresPrivilege(Privilege.controlserver).HandleWith(new OnCommandDelegate(this.CmdAutosaveNow));
		}

		public override int GetUpdateInterval()
		{
			return 500;
		}

		public override void OnServerTick(float dt)
		{
			if (MagicNum.ServerAutoSave <= 0L)
			{
				return;
			}
			if ((this.millisecondsSinceStart - this.milliSecondsSinceSave) / 1000L > MagicNum.ServerAutoSave && this.server.RunPhase == EnumServerRunPhase.RunGame)
			{
				if (!this.server.readyToAutoSave)
				{
					return;
				}
				this.doAutoSave();
			}
		}

		private TextCommandResult CmdAutosaveNow(TextCommandCallingArgs args)
		{
			if (!this.server.readyToAutoSave)
			{
				return TextCommandResult.Success("Server not ready to autosave now, try again.", null);
			}
			if (this.server.chunkThread.BackupInProgress)
			{
				return TextCommandResult.Success("Server is currently doing a backup. Will ignore autosave.", null);
			}
			this.doAutoSave();
			return TextCommandResult.Success("Autosave completed", null);
		}

		private void doAutoSave()
		{
			if (this.server.chunkThread.BackupInProgress)
			{
				return;
			}
			if (this.server.chunkThread.runOffThreadSaveNow)
			{
				ServerMain.Logger.Warning("Call to autosave, but server is already saving. May indicate a disk i/o bottleneck. Reduce autosave interval or improve file i/o. Will ignore this autosave call.");
				return;
			}
			ServerMain.FrameProfiler.Mark("autosave - preparing for autosave");
			if (Monitor.TryEnter(this.server.suspendLock, 5000))
			{
				try
				{
					ServerMain.FrameProfiler.Mark("autosave - obtaining lock");
					if (!this.server.Suspend(true, 3000))
					{
						ServerMain.Logger.Notification("Unable to autosave, was not able to pause the server");
						this.server.Suspend(false, 60000);
					}
					else
					{
						if (!this.server.Saving)
						{
							this.server.Saving = true;
							ServerMain.FrameProfiler.Mark("autosave - pausing server");
							ServerMain.Logger.Notification("Autosaving game world. Notifying mods, then systems of save...");
							this.server.SendMessageToGroup(GlobalConstants.ServerInfoChatGroup, "Saving game world....", EnumChatType.Notification, null, null);
							ServerMain.FrameProfiler.Mark("autosave - notifying players");
							this.server.EventManager.TriggerGameWorldBeingSaved();
							this.server.Saving = false;
							this.milliSecondsSinceSave = this.millisecondsSinceStart;
						}
						this.server.Suspend(false, 60000);
						ServerMain.FrameProfiler.Mark("autosave");
					}
				}
				finally
				{
					Monitor.Exit(this.server.suspendLock);
				}
			}
		}

		private long milliSecondsSinceSave;
	}
}
