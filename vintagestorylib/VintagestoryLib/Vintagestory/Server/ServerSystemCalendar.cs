using System;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	internal class ServerSystemCalendar : ServerSystem
	{
		public ServerSystemCalendar(ServerMain server)
			: base(server)
		{
			server.EventManager.OnGameWorldBeingSaved += this.OnWorldBeingSaved;
			server.EventManager.OnPlayerNowPlaying += this.EventManager_OnPlayerNowPlaying;
		}

		private void EventManager_OnPlayerNowPlaying(IServerPlayer byPlayer)
		{
			this.updateGameWorldCalendarRunningState();
		}

		public override void OnBeginModsAndConfigReady()
		{
			ITreeAttribute worldConfig = this.server.SaveGameData.WorldConfiguration;
			int days = Math.Max(1, worldConfig.GetAsInt("daysPerMonth", 12));
			this.server.GameWorldCalendar = new GameCalendar(this.server.AssetManager.Get("textures/environment/sunlight.png"), this.server.SaveGameData.Seed, (long)this.server.SaveGameData.GetTotalGameSecondsStart());
			this.server.GameWorldCalendar.DaysPerMonth = days;
		}

		public override void OnServerPause()
		{
			this.serverPause = true;
			this.updateGameWorldCalendarRunningState();
		}

		public override void OnServerResume()
		{
			this.serverPause = false;
			this.updateGameWorldCalendarRunningState();
		}

		public override int GetUpdateInterval()
		{
			return 200;
		}

		public override void OnBeginGameReady(SaveGame savegame)
		{
			if (savegame.TotalGameSeconds < 0L)
			{
				ServerMain.Logger.Warning("TotalGameSeconds was negative. Did you accidently set a negative time? This will cause undefined behavior. Clamping back to 0.");
				savegame.TotalGameSeconds = 0L;
			}
			this.server.GameWorldCalendar.SetTotalSeconds(savegame.TotalGameSeconds, savegame.TotalGameSecondsStart);
			this.server.GameWorldCalendar.TimeSpeedModifiers = savegame.TimeSpeedModifiers;
			this.server.GameWorldCalendar.HoursPerDay = savegame.HoursPerDay;
			this.server.GameWorldCalendar.CalendarSpeedMul = savegame.CalendarSpeedMul;
			this.server.GameWorldCalendar.Start();
			this.server.GameWorldCalendar.Tick();
		}

		public void OnWorldBeingSaved()
		{
			if (this.server.GameWorldCalendar != null)
			{
				this.server.SaveGameData.TotalGameSeconds = this.server.GameWorldCalendar.TotalSeconds;
				this.server.SaveGameData.TimeSpeedModifiers = this.server.GameWorldCalendar.TimeSpeedModifiers;
				this.server.SaveGameData.HoursPerDay = this.server.GameWorldCalendar.HoursPerDay;
				this.server.SaveGameData.CalendarSpeedMul = this.server.GameWorldCalendar.CalendarSpeedMul;
			}
		}

		public override void OnPlayerJoin(ServerPlayer player)
		{
			this.updateGameWorldCalendarRunningState();
			this.server.SendPacket(player.ClientId, this.server.GameWorldCalendar.ToPacket());
		}

		public override void OnServerTick(float dt)
		{
			this.updateGameWorldCalendarRunningState();
			this.server.GameWorldCalendar.Tick();
			this.server.SaveGameData.TotalGameSeconds = this.server.GameWorldCalendar.TotalSeconds;
			if (this.server.totalUnpausedTime.ElapsedMilliseconds - this.server.lastUpdateSentToClient > (long)(1000 * MagicNum.CalendarPacketSecondInterval))
			{
				this.server.BroadcastPacket(this.server.GameWorldCalendar.ToPacket(), Array.Empty<IServerPlayer>());
				this.server.lastUpdateSentToClient = this.server.totalUnpausedTime.ElapsedMilliseconds;
			}
		}

		private void updateGameWorldCalendarRunningState()
		{
			if (this.serverPause)
			{
				GameCalendar gameWorldCalendar = this.server.GameWorldCalendar;
				if (gameWorldCalendar == null)
				{
					return;
				}
				gameWorldCalendar.Stop();
				return;
			}
			else
			{
				if (this.server.Config.PassTimeWhenEmpty)
				{
					if (!this.server.GameWorldCalendar.IsRunning)
					{
						ServerMain.Logger.Notification("Server configured to always pass time, resuming game calendar.");
					}
					this.server.GameWorldCalendar.Start();
					return;
				}
				if (this.server.GetPlayingClients() == 0)
				{
					if (this.server.GameWorldCalendar.IsRunning)
					{
						ServerMain.Logger.Notification("All clients disconnected, pausing game calendar.");
					}
					this.server.GameWorldCalendar.Stop();
					return;
				}
				if (!this.server.GameWorldCalendar.IsRunning)
				{
					ServerMain.Logger.Notification("A client reconnected, resuming game calendar.");
				}
				this.server.GameWorldCalendar.Start();
				return;
			}
		}

		private bool serverPause;
	}
}
