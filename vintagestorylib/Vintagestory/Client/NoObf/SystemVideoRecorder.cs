using System;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf
{
	public class SystemVideoRecorder : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "vrec";
			}
		}

		public SystemVideoRecorder(ClientMain game)
			: base(game)
		{
			CommandArgumentParsers parsers = game.api.ChatCommands.Parsers;
			IChatCommand chatCommand = game.api.ChatCommands.Create("vrec").WithDescription("Video Recorder Tools").BeginSubCommand("start")
				.WithDescription("start")
				.HandleWith(new OnCommandDelegate(this.VrecCmdStart))
				.EndSubCommand()
				.BeginSubCommand("stop")
				.WithDescription("stop")
				.HandleWith(new OnCommandDelegate(this.VrecCmdStop))
				.EndSubCommand()
				.BeginSubCommand("toggle")
				.WithDescription("toggle")
				.HandleWith(new OnCommandDelegate(this.VrecCmdToggle))
				.EndSubCommand()
				.BeginSubCommand("codec")
				.WithDescription("codec");
			ICommandArgumentParser[] array = new ICommandArgumentParser[1];
			array[0] = parsers.Word("codec", (from c in game.Platform.GetAvailableCodecs()
				select c.Code).ToArray<string>());
			chatCommand.WithArgs(array).HandleWith(new OnCommandDelegate(this.VrecCmdCodec)).EndSubCommand()
				.BeginSubCommand("videofps")
				.WithDescription("videofps")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("videofps", 0) })
				.HandleWith(new OnCommandDelegate(this.VrecCmdVideofps))
				.EndSubCommand()
				.BeginSubCommand("tickfps")
				.WithDescription("tickfps")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("tickfps", 0) })
				.HandleWith(new OnCommandDelegate(this.VrecCmdTickfps))
				.EndSubCommand()
				.BeginSubCommand("filetarget")
				.WithDescription("filetarget")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalWord("file") })
				.HandleWith(new OnCommandDelegate(this.VrecCmdFiletarget))
				.EndSubCommand();
			game.eventManager.RegisterRenderer(new Action<float>(this.OnFinalizeFrame), EnumRenderStage.Done, this.Name, 1.0);
		}

		private TextCommandResult VrecCmdFiletarget(TextCommandCallingArgs args)
		{
			if (args.Parsers[0].IsMissing)
			{
				string target = ClientSettings.VideoFileTarget;
				if (target == null)
				{
					target = GamePaths.Videos;
				}
				this.game.ShowChatMessage("Current file target: " + target);
				return TextCommandResult.Success("Use file target '-' to reset to default target (=Videos folder)", null);
			}
			string target2 = args[0] as string;
			if (target2 == "-")
			{
				target2 = null;
			}
			ClientSettings.VideoFileTarget = target2;
			if (target2 == null)
			{
				target2 = GamePaths.Videos;
			}
			return TextCommandResult.Success("Video File Target set to " + target2, null);
		}

		private TextCommandResult VrecCmdVideofps(TextCommandCallingArgs args)
		{
			if (args.Parsers[0].IsMissing)
			{
				return TextCommandResult.Success("Current Framerate: " + ClientSettings.RecordingFrameRate.ToString(), null);
			}
			ClientSettings.RecordingFrameRate = (float)((int)args[0]);
			return TextCommandResult.Success("Framerate: " + ClientSettings.RecordingFrameRate.ToString() + " set.", null);
		}

		private TextCommandResult VrecCmdTickfps(TextCommandCallingArgs args)
		{
			if (args.Parsers[0].IsMissing)
			{
				return TextCommandResult.Success("Current Game Tick Framerate: " + ClientSettings.GameTickFrameRate.ToString(), null);
			}
			ClientSettings.GameTickFrameRate = (float)((int)args[0]);
			return TextCommandResult.Success("Current Game Tick Framerate: " + ClientSettings.GameTickFrameRate.ToString() + " set.", null);
		}

		private TextCommandResult VrecCmdCodec(TextCommandCallingArgs args)
		{
			AvailableCodec[] codecs = null;
			try
			{
				codecs = this.game.Platform.GetAvailableCodecs();
			}
			catch (Exception e)
			{
				this.game.Logger.Error("Failed retrieving codecs:");
				this.game.Logger.Error(e);
				return TextCommandResult.Success("Could not retrieve codecs. Check log files.", null);
			}
			if (args.Parsers[0].IsMissing)
			{
				StringBuilder text = new StringBuilder();
				text.AppendLine("List of available codecs:");
				for (int i = 0; i < codecs.Length; i++)
				{
					text.AppendLine(codecs[i].Code + " =&gt; " + codecs[i].Name);
				}
				this.game.ShowChatMessage(text.ToString());
				if (ClientSettings.RecordingCodec != null)
				{
					this.game.ShowChatMessage("Currently selected codec: " + ClientSettings.RecordingCodec);
				}
				return TextCommandResult.Success("", null);
			}
			string code = args[0] as string;
			for (int j = 0; j < codecs.Length; j++)
			{
				if (code == codecs[j].Code)
				{
					ClientSettings.RecordingCodec = code;
					return TextCommandResult.Success(string.Concat(new string[]
					{
						"Codec ",
						code,
						" (",
						codecs[j].Name,
						") set."
					}), null);
				}
			}
			return TextCommandResult.Success("No such video codec supported.", null);
		}

		private TextCommandResult VrecCmdToggle(TextCommandCallingArgs args)
		{
			if (!this.Recording)
			{
				return this.VrecCmdStart(args);
			}
			return this.VrecCmdStop(args);
		}

		private TextCommandResult VrecCmdStop(TextCommandCallingArgs args)
		{
			if (!this.Recording)
			{
				return TextCommandResult.Success("", null);
			}
			this.Recording = false;
			if (this.avi != null)
			{
				this.avi.Close();
				this.avi = null;
			}
			string path = ((ClientSettings.VideoFileTarget == null) ? GamePaths.Videos : ClientSettings.VideoFileTarget);
			this.game.ShowChatMessage(this.videoFileName + " saved to " + path);
			this.game.DeltaTimeLimiter = -1f;
			return TextCommandResult.Success("Ok, Video Recording stopped", null);
		}

		private TextCommandResult VrecCmdStart(TextCommandCallingArgs args)
		{
			if (this.Recording)
			{
				return TextCommandResult.Success("", null);
			}
			this.Recording = true;
			string path = ((ClientSettings.VideoFileTarget == null) ? GamePaths.Videos : ClientSettings.VideoFileTarget);
			try
			{
				if (new DirectoryInfo(path).Parent != null && !Directory.Exists(path))
				{
					Directory.CreateDirectory(path);
				}
				this.avi = this.game.Platform.CreateAviWriter(ClientSettings.RecordingFrameRate, ClientSettings.RecordingCodec);
				string time = string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now);
				this.avi.Open(Path.Combine(path, this.videoFileName = string.Format("{0}.avi", time)), this.game.Width, this.game.Height);
			}
			catch (Exception e)
			{
				this.Recording = false;
				return TextCommandResult.Success("Could not start recorder: " + e.Message, null);
			}
			if (ClientSettings.GameTickFrameRate > 0f)
			{
				this.game.DeltaTimeLimiter = 1f / ClientSettings.GameTickFrameRate;
			}
			return TextCommandResult.Success("Ok, Video Recording now", null);
		}

		public void OnFinalizeFrame(float dt)
		{
			if (!this.Recording || this.avi == null)
			{
				return;
			}
			if (!this.firstFrameDone)
			{
				this.firstFrameDone = true;
				return;
			}
			this.writeAccum += dt;
			float frameRate = ClientSettings.RecordingFrameRate;
			if (this.writeAccum >= 1f / frameRate)
			{
				this.writeAccum -= 1f / frameRate;
				this.avi.AddFrame();
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		public bool Recording;

		public IAviWriter avi;

		public float writeAccum;

		public bool firstFrameDone;

		public string videoFileName;
	}
}
