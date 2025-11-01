using System;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class SystemCalendar : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "cal";
			}
		}

		public SystemCalendar(ClientMain game)
			: base(game)
		{
			game.PacketHandlers[13] = new ServerPacketHandler<Packet_Server>(this.HandleCalendarPacket);
			game.api.ChatCommands.Create("time").WithDescription("Show the the current client time and speed").WithArgs(new ICommandArgumentParser[] { game.api.ChatCommands.Parsers.OptionalBool("speed", "on") })
				.HandleWith(new OnCommandDelegate(this.OnTimeCommand));
			game.RegisterGameTickListener(new Action<float>(this.OnGameTick), 20, 0);
		}

		public override void OnBlockTexturesLoaded()
		{
			this.game.GameWorldCalendar = new ClientGameCalendar(this.game, ScreenManager.Platform.AssetManager.Get("textures/environment/sunlight.png"), this.game.Seed, 28000L);
			this.game.api.eventapi.LevelFinalize += this.Eventapi_LevelFinalize;
		}

		private void Eventapi_LevelFinalize()
		{
			this.game.GameWorldCalendar.Update();
			this.game.GameWorldCalendar.Update();
		}

		private void OnGameTick(float dt)
		{
			this.game.GameWorldCalendar.Tick();
		}

		private void HandleCalendarPacket(Packet_Server packet)
		{
			if (this.game.ignoreServerCalendarUpdates || this.game.GameWorldCalendar == null)
			{
				return;
			}
			if (!this.started)
			{
				this.game.GameWorldCalendar.Start();
				this.started = true;
			}
			int drift = (int)(packet.Calendar.TotalSeconds - this.game.GameWorldCalendar.TotalSeconds);
			if (Math.Abs(drift) > 900 && this.game.GameWorldCalendar.TotalSeconds > 28000L)
			{
				ScreenManager.Platform.Logger.Notification("Wow, client daytime drifted off significantly from server daytime ({0} mins)", new object[] { Math.Round((double)((float)drift / 60f), 1) });
			}
			this.game.GameWorldCalendar.UpdateFromPacket(packet);
		}

		private TextCommandResult OnTimeCommand(TextCommandCallingArgs args)
		{
			GameCalendar cal = this.game.GameWorldCalendar;
			this.game.ShowChatMessage("Client time: " + cal.PrettyDate());
			if ((bool)args[0])
			{
				this.game.ShowChatMessage("Game speed: " + Math.Round((double)(this.game.GameWorldCalendar.DayLengthInRealLifeSeconds / 60f), 1).ToString() + " IRL minutes");
			}
			return TextCommandResult.Success("", null);
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		private bool started;
	}
}
