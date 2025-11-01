using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class SystemMusicEngine : ClientSystem, IMusicEngine
	{
		public override string Name
		{
			get
			{
				return "mus";
			}
		}

		public bool CurrentActive
		{
			get
			{
				return this.currentTrack != null && this.currentTrack.IsActive;
			}
		}

		public IMusicTrack CurrentTrack
		{
			get
			{
				return this.currentTrack;
			}
		}

		public IMusicTrack LastPlayedTrack
		{
			get
			{
				return this.lastTrack;
			}
		}

		public long MillisecondsSinceLastTrack
		{
			get
			{
				return this.msSinceLastTrack;
			}
		}

		public SystemMusicEngine(ClientMain game, CancellationToken token)
			: base(game)
		{
			this._token = token;
			game.eventManager.TrackStarter = new TrackStarterDelegate(this.StartTrack);
			game.eventManager.TrackStarterLoaded = new TrackStarterLoadedDelegate(this.StartTrack);
			game.eventManager.CurrentTrackSupplier = () => this.CurrentTrack;
			game.RegisterGameTickListener(new Action<float>(this.OnEverySecond), 1000, 0);
			game.api.ChatCommands.GetOrCreate("debug").BeginSubCommand("music").WithDescription("Show current playing music track")
				.HandleWith(new OnCommandDelegate(this.OnCmdCurrentTrack))
				.BeginSubCommand("sim")
				.WithDescription("Simulate music playing for x days")
				.WithArgs(new ICommandArgumentParser[] { game.api.ChatCommands.Parsers.OptionalInt("days", 5) })
				.HandleWith(new OnCommandDelegate(this.OnCmdSim))
				.EndSubCommand()
				.BeginSubCommand("simstop")
				.WithDescription("Stop music simulation")
				.HandleWith(new OnCommandDelegate(this.OnCmdSimstop))
				.EndSubCommand()
				.BeginSubCommand("stop")
				.WithDescription("Stop current music track")
				.HandleWith(new OnCommandDelegate(this.OnCmdStop))
				.EndSubCommand()
				.EndSubCommand();
		}

		internal void Initialise_SeparateThread()
		{
			this.initialisingThread = new Thread(new ThreadStart(new ThreadMusicInitialise(this, this.game).Process));
			this.initialisingThread.IsBackground = true;
			this.initialisingThread.Start();
		}

		internal void EarlyInitialise()
		{
			ClientSettings.Inst.AddWatcher<int>("musicLevel", new OnSettingsChanged<int>(this.OnMusicLevelChanged));
			Queue<IMusicTrack> tracks = new Queue<IMusicTrack>();
			foreach (IAsset musicConfig in this.game.Platform.AssetManager.GetManyInCategory("music", "musicconfig.json", null, true))
			{
				JsonSerializerSettings settings = new JsonSerializerSettings
				{
					TypeNameHandling = TypeNameHandling.All
				};
				settings.Converters.Add(new AssetLocationJsonParser(musicConfig.Location.Domain));
				this.config = musicConfig.ToObject<MusicConfig>(settings);
				if (this.config.Tracks != null)
				{
					foreach (IMusicTrack track in this.config.Tracks)
					{
						track.Initialize(this.game.Platform.AssetManager, this.game.api, this);
						tracks.Enqueue(track);
					}
				}
			}
			if (this.config == null)
			{
				this.config = new MusicConfig();
			}
			this.shuffledTracks = tracks.ToArray();
			this.config.Tracks = tracks.ToArray();
			this.trackLoader = new Thread(new ThreadStart(this.ProcessTrackQueue));
			this.trackLoader.IsBackground = true;
			this.trackLoader.Start();
		}

		public override void OnBlockTexturesLoaded()
		{
			while (this.initialisingThread.IsAlive)
			{
				Thread.Sleep(10);
			}
			this.game.Logger.Notification("Initialized Music Engine");
		}

		public MusicTrack StartTrack(AssetLocation soundLocation, float priority, EnumSoundType soundType, Action<ILoadedSound> onLoaded = null)
		{
			if (this.CurrentTrack != null && this.CurrentTrack.Priority > priority)
			{
				return null;
			}
			IMusicTrack musicTrack = this.CurrentTrack;
			if (musicTrack != null)
			{
				musicTrack.FadeOut(2f, null);
			}
			MusicTrack track = new MusicTrack
			{
				Location = soundLocation,
				Priority = priority
			};
			track.Initialize(this.game.AssetManager, this.game.api, this);
			track.loading = true;
			this.currentTrack = track;
			this.TracksToLoad.Enqueue(new TrackToLoad
			{
				ByTrack = track,
				SoundType = soundType,
				Location = track.Location,
				OnLoaded = delegate(ILoadedSound sound)
				{
					if (onLoaded == null)
					{
						sound.Start();
					}
					else
					{
						onLoaded(sound);
					}
					if (!track.loading)
					{
						if (sound != null)
						{
							sound.Stop();
						}
						if (!track.ManualDispose && sound != null)
						{
							sound.Dispose();
						}
					}
					else
					{
						track.Sound = sound;
					}
					track.loading = false;
				}
			});
			return track;
		}

		public void StartTrack(MusicTrack track, float priority, EnumSoundType soundType, bool playNow = true)
		{
			if ((this.CurrentTrack != null && this.CurrentTrack.Priority > priority) || track == this.currentTrack)
			{
				return;
			}
			IMusicTrack musicTrack = this.CurrentTrack;
			if (musicTrack != null)
			{
				musicTrack.FadeOut(2f, null);
			}
			this.currentTrack = track;
			if (playNow)
			{
				track.Sound.Start();
			}
		}

		private void ProcessTrackQueue()
		{
			try
			{
				while (!this._token.IsCancellationRequested)
				{
					TrackToLoad trackToLoad;
					if (this.TracksToLoad.TryDequeue(out trackToLoad))
					{
						IAsset music = this.game.Platform.AssetManager.TryGet(trackToLoad.Location, true);
						if (music != null)
						{
							(ScreenManager.LoadMusicTrack(music) as AudioMetaData).AutoUnload = true;
							this.game.EnqueueMainThreadTask(delegate
							{
								ILoadedSound loadedsound = this.game.LoadSound(new SoundParams
								{
									Location = trackToLoad.Location,
									SoundType = trackToLoad.SoundType,
									Volume = trackToLoad.volume,
									Pitch = trackToLoad.pitch,
									DisposeOnFinish = false
								});
								trackToLoad.OnLoaded(loadedsound);
							}, "loadtrack");
						}
						else
						{
							this.game.Logger.Warning("Music File not found: {0}", new object[] { trackToLoad.Location });
							this.game.EnqueueMainThreadTask(delegate
							{
								trackToLoad.OnLoaded(null);
							}, "loadtrack");
						}
					}
					if (this.debugSimulation)
					{
						break;
					}
					Thread.Sleep(75);
				}
			}
			catch (TaskCanceledException)
			{
			}
		}

		private TextCommandResult OnCmdSimstop(TextCommandCallingArgs args)
		{
			this.totalHoursStop = 0.0;
			return TextCommandResult.Success("Ok, sim stopped", null);
		}

		private TextCommandResult OnCmdStop(TextCommandCallingArgs args)
		{
			IMusicTrack musicTrack = this.currentTrack;
			if (musicTrack != null)
			{
				musicTrack.FadeOut(1f, null);
			}
			return TextCommandResult.Success("Ok, track stopped", null);
		}

		private TextCommandResult OnCmdSim(TextCommandCallingArgs args)
		{
			int days = (int)args[0];
			if (ClientSettings.MusicLevel > 0)
			{
				return TextCommandResult.Error("Set music level to 0 first", "");
			}
			this.debugSimulation = true;
			while (this.trackLoader.IsAlive)
			{
				Thread.Sleep(100);
			}
			JsonSerializerSettings settings = new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.All
			};
			this.game.Platform.AssetManager.Reload(AssetCategory.music);
			IAsset asset = this.game.Platform.AssetManager.TryGet("music/musicconfig.json", true);
			this.config = ((asset != null) ? asset.ToObject<MusicConfig>(settings) : null);
			if (this.config == null)
			{
				this.config = new MusicConfig();
			}
			if (this.config.Tracks != null)
			{
				this.shuffledTracks = (IMusicTrack[])this.config.Tracks.Clone();
				IMusicTrack[] tracks = this.config.Tracks;
				for (int i = 0; i < tracks.Length; i++)
				{
					tracks[i].Initialize(this.game.Platform.AssetManager, this.game.api, this);
				}
			}
			this.game.ignoreServerCalendarUpdates = true;
			this.totalHoursStop = this.game.GameWorldCalendar.TotalHours + (double)((float)days * this.game.GameWorldCalendar.HoursPerDay);
			this.trackPlayCount.Clear();
			this.listenerId = this.game.RegisterGameTickListener(new Action<float>(this.DebugSimTick), 20, 0);
			return TextCommandResult.Success("Ok, sim started", null);
		}

		private TextCommandResult OnCmdCurrentTrack(TextCommandCallingArgs args)
		{
			IMusicTrack musicTrack = this.currentTrack;
			if (musicTrack != null && musicTrack.IsActive)
			{
				return TextCommandResult.Success("Currently playing: " + this.currentTrack.Name, null);
			}
			return TextCommandResult.Success((!this.TracksToLoad.IsEmpty) ? "Loading track(s)... " : "Searching for fitting track... ", null);
		}

		private void OnEverySecond(float dt)
		{
			if ((ScreenManager.IntroMusic != null && !ScreenManager.IntroMusic.HasStopped) || ClientSettings.MusicLevel <= 0)
			{
				return;
			}
			IMusicTrack musicTrack = this.currentTrack;
			if (musicTrack != null && !musicTrack.ContinuePlay(dt, this.game.playerProperties))
			{
				this.lastTrack = this.currentTrack;
				this.currentTrack = null;
				this.msSinceLastTrack = this.game.ElapsedMilliseconds;
			}
			if (this.shuffledTracks == null)
			{
				return;
			}
			GameMath.Shuffle<IMusicTrack>(this.rand, this.shuffledTracks);
			IMusicTrack[] array = this.shuffledTracks;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].BeginSort();
			}
			BlockPos pos = this.game.player.Entity.Pos.AsBlockPos;
			ClimateCondition conds = this.game.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.NowValues, 0.0);
			foreach (IMusicTrack track in this.shuffledTracks.OrderBy((IMusicTrack t) => t.StartPriority).Reverse<IMusicTrack>())
			{
				this.currentlyCheckedTrack = track;
				if (this.CurrentActive && this.currentTrack != track)
				{
					this.tracksOnCooldown[track] = this.game.ElapsedMilliseconds + 8000L;
					if (track.Priority <= this.currentTrack.Priority)
					{
						break;
					}
				}
				if (this.currentTrack != track && track.ShouldPlay(this.game.playerProperties, conds, pos))
				{
					if (this.currentTrack != null)
					{
						this.game.Logger.Notification("Current track {0} got replaced by a higher priority one ({1}). Fading out.", new object[]
						{
							this.currentTrack.Name,
							track.Name
						});
						this.currentTrack.FadeOut(5f, null);
						this.msSinceLastTrack = this.game.ElapsedMilliseconds;
					}
					this.game.Logger.Notification("Track {0} now started", new object[] { track.Name });
					this.currentTrack = track;
					this.currentTrack.BeginPlay(this.game.playerProperties);
					break;
				}
			}
		}

		public void LoadTrack(AssetLocation location, Action<ILoadedSound> onLoaded, float volume = 1f, float pitch = 1f)
		{
			this.TracksToLoad.Enqueue(new TrackToLoad
			{
				ByTrack = this.currentlyCheckedTrack,
				Location = location,
				volume = volume,
				pitch = pitch,
				OnLoaded = onLoaded
			});
			if (this.debugSimulation)
			{
				this.ProcessTrackQueue();
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Audio;
		}

		private void OnMusicLevelChanged(int newValue)
		{
			if (this.currentTrack != null)
			{
				this.currentTrack.UpdateVolume();
			}
		}

		public void StopTrack(IMusicTrack musicTrack)
		{
			if (this.CurrentTrack == musicTrack)
			{
				this.currentTrack = null;
			}
		}

		private void DebugSimTick(float dt)
		{
			float irlsecondsper5minutes = 10f;
			double totalHours = this.game.GameWorldCalendar.TotalHours;
			totalHours += 0.0833333358168602;
			BlockPos pos = this.game.player.Entity.Pos.AsBlockPos;
			ClimateCondition conds = this.game.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.ForSuppliedDateValues, totalHours / (double)this.game.GameWorldCalendar.HoursPerDay);
			if (totalHours > this.totalHoursStop)
			{
				this.game.UnregisterGameTickListener(this.listenerId);
				this.debugSimulation = false;
				this.game.ignoreServerCalendarUpdates = false;
				this.game.Logger.Notification("Simulation executed. Results");
				foreach (KeyValuePair<string, int> val in this.trackPlayCount)
				{
					this.game.Logger.Notification("{0}: {1}", new object[] { val.Key, val.Value });
				}
				return;
			}
			this.game.GameWorldCalendar.Add(0.083333336f);
			this.game.GameWorldCalendar.Tick();
			IMusicTrack musicTrack = this.currentTrack;
			if (musicTrack != null && !musicTrack.ContinuePlay(dt, this.game.playerProperties))
			{
				this.lastTrack = this.currentTrack;
				this.currentTrack = null;
				this.msSinceLastTrack = this.game.ElapsedMilliseconds;
			}
			if (this.currentTrack != null && this.currentTrack.IsActive)
			{
				this.currentTrack.FastForward(irlsecondsper5minutes);
				this.game.Logger.Notification("{0}", new object[] { this.currentTrack.PositionString });
			}
			else
			{
				SurfaceMusicTrack.globalCooldownUntilMs -= (long)((int)irlsecondsper5minutes * 1000);
				foreach (string text in new List<string>(SurfaceMusicTrack.tracksCooldownUntilMs.Keys))
				{
					Dictionary<string, long> tracksCooldownUntilMs = SurfaceMusicTrack.tracksCooldownUntilMs;
					string text2 = text;
					tracksCooldownUntilMs[text2] -= (long)((int)irlsecondsper5minutes * 1000);
				}
			}
			GameMath.Shuffle<IMusicTrack>(this.rand, this.shuffledTracks);
			IMusicTrack[] array = this.shuffledTracks;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].BeginSort();
			}
			this.shuffledTracks.OrderBy((IMusicTrack t) => t.StartPriority).Reverse<IMusicTrack>();
			foreach (IMusicTrack track in this.shuffledTracks.OrderBy((IMusicTrack t) => t.Priority).Reverse<IMusicTrack>())
			{
				this.currentlyCheckedTrack = track;
				if (this.CurrentActive && this.currentTrack != track)
				{
					this.tracksOnCooldown[track] = this.game.ElapsedMilliseconds + 8000L;
					if (track.Priority <= this.currentTrack.Priority)
					{
						break;
					}
				}
				if (this.currentTrack != track && track.ShouldPlay(this.game.playerProperties, conds, pos))
				{
					if (this.currentTrack != null && this.currentTrack.IsActive)
					{
						this.game.Logger.Notification("Current track {0} got replaced by a higher priority one ({1}). Fading out.", new object[]
						{
							this.currentTrack.Name,
							track.Name
						});
						this.currentTrack.FadeOut(5f, null);
						this.msSinceLastTrack = this.game.ElapsedMilliseconds;
					}
					this.game.Logger.Notification("Track {0} now started", new object[] { track.Name });
					this.currentTrack = track;
					this.currentTrack.BeginPlay(this.game.playerProperties);
					if (this.trackPlayCount.ContainsKey(track.Name))
					{
						OrderedDictionary<string, int> orderedDictionary = this.trackPlayCount;
						string text2 = track.Name;
						int i = orderedDictionary[text2];
						orderedDictionary[text2] = i + 1;
						break;
					}
					this.trackPlayCount[track.Name] = 1;
					break;
				}
			}
		}

		public ConcurrentQueue<TrackToLoad> TracksToLoad = new ConcurrentQueue<TrackToLoad>();

		private Thread trackLoader;

		private readonly CancellationToken _token;

		private IMusicTrack currentlyCheckedTrack;

		private ConcurrentDictionary<IMusicTrack, long> tracksOnCooldown = new ConcurrentDictionary<IMusicTrack, long>();

		private MusicConfig config;

		private IMusicTrack currentTrack;

		private IMusicTrack lastTrack;

		private IMusicTrack[] shuffledTracks;

		private long msSinceLastTrack;

		private bool debugSimulation;

		private Thread initialisingThread;

		private Random rand = new Random();

		private double totalHoursStop;

		private long listenerId;

		private OrderedDictionary<string, int> trackPlayCount = new OrderedDictionary<string, int>();
	}
}
