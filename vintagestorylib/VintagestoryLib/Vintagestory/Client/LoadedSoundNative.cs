using System;
using System.Collections.Generic;
using System.Threading;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client
{
	public class LoadedSoundNative : ILoadedSound, IDisposable
	{
		public SoundParams Params
		{
			get
			{
				return this.soundParams;
			}
		}

		public bool IsDisposed
		{
			get
			{
				return this.disposed;
			}
		}

		public int Channels
		{
			get
			{
				return this.sample.Channels;
			}
		}

		public bool IsPlaying
		{
			get
			{
				int state;
				AL.GetSource(this.sourceId, ALGetSourcei.SourceState, out state);
				return !this.disposed && state == 4114;
			}
		}

		public bool IsPaused
		{
			get
			{
				int state;
				AL.GetSource(this.sourceId, ALGetSourcei.SourceState, out state);
				return !this.disposed && state == 4115;
			}
		}

		public bool IsReady
		{
			get
			{
				return this.sample.Loaded == 3;
			}
		}

		public bool HasStopped
		{
			get
			{
				if (this.disposed)
				{
					return true;
				}
				int state;
				AL.GetSource(this.sourceId, ALGetSourcei.SourceState, out state);
				return state != 4114 && state != 4115;
			}
		}

		public bool HasReverbStopped(long elapsedMilliseconds)
		{
			if (this.Params.ReverbDecayTime <= 0f)
			{
				return true;
			}
			if (!this.didStop && this.HasStopped)
			{
				this.didStop = true;
				this.stoppedMsAgo = elapsedMilliseconds;
			}
			return (float)(elapsedMilliseconds - this.stoppedMsAgo) > this.Params.ReverbDecayTime * 1000f;
		}

		public float PlaybackPosition
		{
			get
			{
				if (!this.disposed)
				{
					AL.GetSource(this.sourceId, ALSourcef.SecOffset, out this.playbackPosition);
					this.testError("get playback pos");
				}
				return this.playbackPosition;
			}
			set
			{
				this.playbackPosition = value;
				if (this.disposed)
				{
					return;
				}
				AL.Source(this.sourceId, ALSourcef.SecOffset, value);
				this.testError("set playback pos");
			}
		}

		public float SoundLengthSeconds
		{
			get
			{
				return this.soundLengthSeconds;
			}
		}

		private float GlobalVolume
		{
			get
			{
				if (this.soundParams.SoundType == EnumSoundType.Music || this.soundParams.SoundType == EnumSoundType.MusicGlitchunaffected)
				{
					return (float)ClientSettings.MusicLevel / 100f;
				}
				if (this.soundParams.SoundType == EnumSoundType.Weather)
				{
					return (float)ClientSettings.WeatherSoundLevel / 100f;
				}
				if (this.soundParams.SoundType == EnumSoundType.Ambient || this.soundParams.SoundType == EnumSoundType.AmbientGlitchunaffected)
				{
					return (float)ClientSettings.AmbientSoundLevel / 100f;
				}
				if (this.soundParams.SoundType == EnumSoundType.Entity)
				{
					return (float)ClientSettings.EntitySoundLevel / 100f;
				}
				return (float)ClientSettings.SoundLevel / 100f;
			}
		}

		public bool IsFadingIn
		{
			get
			{
				return this.fadeState == 1;
			}
		}

		public bool IsFadingOut
		{
			get
			{
				return this.fadeState == 2;
			}
		}

		public static void ChangeOutputDevice(Action changeCallback)
		{
			object obj = LoadedSoundNative.loadedSoundsLock;
			lock (obj)
			{
				foreach (LoadedSoundNative loadedSoundNative in LoadedSoundNative.loadedSounds)
				{
					loadedSoundNative.disposeSoundSource();
				}
			}
			try
			{
				changeCallback();
			}
			finally
			{
				obj = LoadedSoundNative.loadedSoundsLock;
				lock (obj)
				{
					foreach (LoadedSoundNative loadedSoundNative2 in LoadedSoundNative.loadedSounds)
					{
						loadedSoundNative2.createSoundSource();
					}
				}
			}
		}

		public LoadedSoundNative(SoundParams soundParams, AudioMetaData sample, ClientMain game)
		{
			this.sample = sample;
			this.soundParams = soundParams;
			this.testError("construction before");
			if (RuntimeEnv.DebugSoundDispose)
			{
				this.trace = Environment.StackTrace;
			}
			object obj = LoadedSoundNative.loadedSoundsLock;
			lock (obj)
			{
				LoadedSoundNative.loadedSounds.Add(this);
			}
			switch (sample.Loaded)
			{
			case 0:
				sample.Load();
				this.createSoundSource();
				return;
			case 1:
				sample.AddOnLoaded(new MainThreadAction(game, new Func<int>(this.createSoundSource), "soundloading"));
				return;
			}
			this.createSoundSource();
		}

		public LoadedSoundNative(SoundParams soundParams, AudioMetaData sample)
		{
			this.sample = sample;
			this.soundParams = soundParams;
			this.testError("construction before");
			if (RuntimeEnv.DebugSoundDispose)
			{
				this.trace = Environment.StackTrace;
			}
			object obj = LoadedSoundNative.loadedSoundsLock;
			lock (obj)
			{
				LoadedSoundNative.loadedSounds.Add(this);
			}
			if (sample.Loaded == 0)
			{
				sample.Load();
			}
			int timeout = 64;
			while (sample.Loaded < 2 && timeout-- > 0)
			{
				Thread.Sleep(15);
			}
			this.createSoundSource();
		}

		private unsafe int createSoundSource()
		{
			try
			{
				this.sourceId = AL.GenSource();
				if (this.sourceId == 0)
				{
					throw new Exception("Unable to get sourceId");
				}
				this.bufferId = AL.GenBuffer();
				ALFormat soundFormat = AudioOpenAl.GetSoundFormat(this.sample.Channels, this.sample.BitsPerSample);
				try
				{
					byte[] array;
					byte* p;
					if ((array = this.sample.Pcm) == null || array.Length == 0)
					{
						p = null;
					}
					else
					{
						p = &array[0];
					}
					AL.BufferData(this.bufferId, soundFormat, (void*)p, this.sample.Pcm.Length, this.sample.Rate);
				}
				finally
				{
					byte[] array = null;
				}
				int bufferSize;
				AL.GetBuffer(this.bufferId, ALGetBufferi.Size, out bufferSize);
				int channels;
				AL.GetBuffer(this.bufferId, ALGetBufferi.Channels, out channels);
				this.soundLengthSeconds = 1f * (float)bufferSize / (float)(this.sample.Rate * this.sample.BitsPerSample / 8) / (float)channels;
				float rf = -(float)(Math.Log(0.009999999776482582) / Math.Log((double)this.soundParams.Range));
				float refdist = (float)Math.Max(3.0, Math.Pow((double)this.soundParams.Range, 0.5) - 2.0);
				AL.DistanceModel(ALDistanceModel.ExponentDistanceClamped);
				AL.Source(this.sourceId, ALSourcef.RolloffFactor, rf);
				AL.Source(this.sourceId, ALSourcef.ReferenceDistance, (this.soundParams.ReferenceDistance != 3f) ? this.soundParams.ReferenceDistance : refdist);
				AL.Source(this.sourceId, ALSourcef.MaxDistance, 9999f);
				AL.Source(this.sourceId, ALSourcei.Buffer, this.bufferId);
				AL.Source(this.sourceId, ALSourcef.Gain, this.soundParams.Volume * this.GlobalVolume);
				AL.Source(this.sourceId, ALSourcef.Pitch, GameMath.Clamp(this.soundParams.Pitch + this.pitchOffset, 0.1f, 3f));
				bool flag = soundFormat - ALFormat.Stereo8 <= 1;
				if (flag && AudioOpenAl.UseHrtf)
				{
					AL.Source(this.sourceId, (ALSourcei)4147, 2);
				}
				if (this.soundParams.Position != null)
				{
					Vector3 vec = new Vector3(this.soundParams.Position.X, this.soundParams.Position.Y, this.soundParams.Position.Z);
					AL.Source(this.sourceId, ALSource3f.Position, ref vec);
				}
				AL.Source(this.sourceId, ALSourceb.SourceRelative, this.soundParams.RelativePosition);
				AL.Source(this.sourceId, ALSourceb.Looping, this.soundParams.ShouldLoop);
				if (this.playbackPosition > 0f)
				{
					AL.Source(this.sourceId, ALSourcef.SecOffset, this.playbackPosition);
				}
				this.testError("setup");
				this.SetReverb(this.Params.ReverbDecayTime);
				this.SetLowPassfiltering(this.Params.LowPassFilter);
				this.testError("filter");
				ALSourceState alsourceState = this.sourceState;
				if (alsourceState != ALSourceState.Playing)
				{
					if (alsourceState == ALSourceState.Paused)
					{
						AL.SourcePause(this.sourceId);
					}
				}
				else
				{
					AL.SourcePlay(this.sourceId);
				}
				this.sample.Loaded = 3;
				this.disposed = false;
			}
			catch (Exception e)
			{
				ILogger logger = ScreenManager.Platform.Logger;
				string text = "Could not load sound ";
				SoundParams soundParams = this.soundParams;
				logger.Error(text + ((soundParams != null) ? soundParams.Location : null));
				ScreenManager.Platform.Logger.Error(e);
				this.disposed = true;
			}
			this.testError("construction");
			return 0;
		}

		public void SetReverb(float reverbDecayTime)
		{
			this.Params.ReverbDecayTime = reverbDecayTime;
			if (!AudioOpenAl.HasEffectsExtension)
			{
				return;
			}
			if (this.Params.ReverbDecayTime > 0f)
			{
				ReverbEffect reverbConfig = AudioOpenAl.GetOrCreateReverbEffect(reverbDecayTime);
				AL.Source(this.sourceId, (ALSource3i)131078, reverbConfig.ReverbEffectSlot, 0, 0);
			}
			else
			{
				AL.Source(this.sourceId, (ALSource3i)131078, 0, 0, 0);
			}
			this.testError("SetReverb");
		}

		private void DisposeReverb()
		{
			if (AudioOpenAl.HasEffectsExtension)
			{
				AL.Source(this.sourceId, (ALSource3i)131078, 0, 0, 0);
				this.testError("disposereverb");
			}
		}

		public void SetLowPassfiltering(float value)
		{
			this.Params.LowPassFilter = value;
			if (!AudioOpenAl.HasEffectsExtension)
			{
				return;
			}
			if (this.Params.LowPassFilter < 1f)
			{
				if (AudioOpenAl.EchoFilterId == 0)
				{
					AudioOpenAl.EchoFilterId = ALC.EFX.GenFilter();
					ALC.EFX.Filter(AudioOpenAl.EchoFilterId, FilterInteger.FilterType, 1);
					ALC.EFX.Filter(AudioOpenAl.EchoFilterId, FilterFloat.LowpassGain, 1f);
					ALC.EFX.Filter(AudioOpenAl.EchoFilterId, FilterFloat.LowpassGainHF, this.Params.LowPassFilter);
				}
				AL.Source(this.sourceId, ALSourcei.EfxDirectFilter, AudioOpenAl.EchoFilterId);
			}
			else
			{
				AL.Source(this.sourceId, ALSourcei.EfxDirectFilter, 0);
			}
			this.testError("SetLowPassfiltering");
		}

		private void DisposeLowPassfilter()
		{
			if (AudioOpenAl.HasEffectsExtension)
			{
				AL.Source(this.sourceId, ALSourcei.EfxDirectFilter, 0);
				this.testError("disposeLowPassFilter");
			}
		}

		private void disposeSoundSource()
		{
			if (this.sourceId == 0)
			{
				this.disposed = true;
				return;
			}
			AL.GetSource(this.sourceId, ALSourcef.SecOffset, out this.playbackPosition);
			AL.SourceStop(this.sourceId);
			AL.Source(this.sourceId, ALSourcei.Buffer, 0);
			this.testError("disposestop");
			this.DisposeLowPassfilter();
			this.DisposeReverb();
			AL.DeleteSource(this.sourceId);
			AL.DeleteBuffer(this.bufferId);
			this.testError("dispose");
			this.sourceId = 0;
			this.bufferId = 0;
			this.disposed = true;
		}

		public void SetPosition(float x, float y, float z)
		{
			if (this.soundParams.Position == null)
			{
				this.soundParams.Position = new Vec3f(x, y, z);
				this.soundParams.RelativePosition = false;
				if (!this.disposed)
				{
					AL.Source(this.sourceId, ALSourceb.SourceRelative, false);
				}
			}
			else
			{
				this.soundParams.Position.Set(x, y, z);
			}
			if (this.sourceId == 0)
			{
				return;
			}
			if (!this.disposed)
			{
				Vector3 vec = new Vector3(x, y, z);
				AL.Source(this.sourceId, ALSource3f.Position, ref vec);
			}
			this.testError("setposition x/y/z");
		}

		public void SetPosition(Vec3f position)
		{
			this.testError("before setposition vec3");
			if (this.soundParams.Position == null)
			{
				this.soundParams.Position = position.Clone();
				this.soundParams.RelativePosition = false;
				if (!this.disposed)
				{
					AL.Source(this.sourceId, ALSourceb.SourceRelative, false);
				}
			}
			else
			{
				this.soundParams.Position.Set(position.X, position.Y, position.Z);
			}
			if (this.sourceId == 0)
			{
				return;
			}
			if (!this.disposed)
			{
				Vector3 vec = new Vector3(position.X, position.Y, position.Z);
				AL.Source(this.sourceId, ALSource3f.Position, ref vec);
			}
			this.testError("setposition vec3");
		}

		public void SetPitch(float val)
		{
			this.soundParams.Pitch = val;
			if (this.sourceId == 0)
			{
				return;
			}
			if (this.disposed)
			{
				return;
			}
			AL.Source(this.sourceId, ALSourcef.Pitch, GameMath.Clamp(this.soundParams.Pitch + this.pitchOffset, 0.1f, 3f));
			this.testError("SetPitch");
		}

		public void SetPitchOffset(float val)
		{
			this.pitchOffset = val;
			if (this.sourceId == 0)
			{
				return;
			}
			if (this.disposed)
			{
				return;
			}
			AL.Source(this.sourceId, ALSourcef.Pitch, GameMath.Clamp(this.soundParams.Pitch + this.pitchOffset, 0.1f, 3f));
			this.testError("SetPitchOffset");
		}

		public void SetVolume()
		{
			if (this.sourceId == 0)
			{
				return;
			}
			if (this.disposed)
			{
				return;
			}
			AL.Source(this.sourceId, ALSourcef.Gain, this.soundParams.Volume * this.GlobalVolume);
			this.testError("setvolume");
		}

		public void SetVolume(float val)
		{
			this.soundParams.Volume = val;
			if (this.sourceId == 0)
			{
				return;
			}
			if (this.disposed)
			{
				return;
			}
			AL.Source(this.sourceId, ALSourcef.Gain, val * this.GlobalVolume);
			this.testError("setvolume(val)");
		}

		public void Toggle(bool on)
		{
			if (on)
			{
				this.Start();
				return;
			}
			this.Stop();
		}

		public void Start()
		{
			this.sourceState = ALSourceState.Playing;
			if (this.disposed)
			{
				return;
			}
			AL.SourcePlay(this.sourceId);
			this.testError("start");
		}

		public void Stop()
		{
			this.sourceState = ALSourceState.Stopped;
			if (this.disposed)
			{
				return;
			}
			AL.SourceStop(this.sourceId);
			this.testError("stop");
		}

		public void Pause()
		{
			this.sourceState = ALSourceState.Paused;
			if (this.disposed)
			{
				return;
			}
			AL.SourcePause(this.sourceId);
			this.testError("pause");
		}

		public void Dispose()
		{
			if (this.disposed)
			{
				return;
			}
			object obj = LoadedSoundNative.loadedSoundsLock;
			lock (obj)
			{
				this.disposeSoundSource();
				LoadedSoundNative.loadedSounds.Remove(this);
			}
			if (ClientSettings.OptimizeRamMode == 2 && this.sample.AutoUnload)
			{
				AudioMetaData audioMetaData = this.sample;
				if (audioMetaData == null)
				{
					return;
				}
				audioMetaData.Unload();
			}
		}

		private void testError(string during)
		{
			ALError err = AL.GetError();
			if (err != ALError.NoError)
			{
				ILogger logger = ScreenManager.Platform.Logger;
				string text = "OpenAL Error during {0} of sound {1}: {2}";
				object[] array = new object[3];
				array[0] = during;
				int num = 1;
				SoundParams @params = this.Params;
				array[num] = ((@params != null) ? @params.Location : null);
				array[2] = err;
				logger.Warning(text, array);
			}
			if (err == ALError.OutOfMemory && RuntimeEnv.DebugSoundDispose)
			{
				ScreenManager.Platform.Logger.Warning("OutOfMemory error detected. Sound dispose debug enabled, printing all active sources ({0}):", new object[] { LoadedSoundNative.loadedSounds.Count });
				object obj = LoadedSoundNative.loadedSoundsLock;
				lock (obj)
				{
					foreach (LoadedSoundNative val in LoadedSoundNative.loadedSounds)
					{
						ILogger logger2 = ScreenManager.Platform.Logger;
						string text2 = val.sourceId.ToString();
						string text3 = ": ";
						SoundParams soundParams = val.soundParams;
						logger2.Notification(text2 + text3 + ((soundParams != null) ? soundParams.Location.ToShortString() : null));
					}
				}
				throw new Exception("Sound dispose debug enabled, killing game. More debug info see client-main.log");
			}
		}

		protected override void Finalize()
		{
			try
			{
				if (!this.disposed && !ScreenManager.Platform.IsShuttingDown)
				{
					object obj = LoadedSoundNative.loadedSoundsLock;
					lock (obj)
					{
						LoadedSoundNative.loadedSounds.Remove(this);
					}
					if (this.trace == null)
					{
						ScreenManager.Platform.Logger.Debug("Loaded sound {0} is leaking memory, missing call to Dispose()", new object[] { this.Params.Location });
					}
					else
					{
						ScreenManager.Platform.Logger.Debug("Loaded sound {0} is leaking memory, missing call to Dispose(). Allocated at {1}", new object[]
						{
							this.Params.Location,
							this.trace
						});
					}
				}
			}
			finally
			{
				base.Finalize();
			}
		}

		public void FadeTo(double newVolume, float duration, Action<ILoadedSound> onFaded)
		{
			duration = Math.Max(0.02f, duration);
			newVolume = GameMath.Clamp(newVolume, 0.01, 1.0);
			double curVolume = (double)this.Params.Volume;
			this.fadeState = 0;
			if (newVolume > curVolume)
			{
				this.fadeState = 1;
			}
			if (newVolume < curVolume)
			{
				this.fadeState = 2;
			}
			double percStep = 0.019999999552965164;
			double factor = 1.0 - percStep;
			if (newVolume > curVolume)
			{
				factor = 1.0 + percStep;
				if (curVolume <= 0.0)
				{
					curVolume = 0.01;
				}
			}
			double stepsPerSecond = Math.Ceiling((Math.Log(newVolume) - Math.Log(curVolume)) / Math.Log(factor)) / (double)duration;
			double stepsPer10ms = stepsPerSecond / 100.0;
			int sleepMs = 10;
			if (RuntimeEnv.OS == OS.Windows && duration <= 0.1f)
			{
				sleepMs = 0;
			}
			Action <>9__1;
			TyronThreadPool.QueueLongDurationTask(delegate
			{
				double stepsToDo = 0.0;
				LoadedSoundNative <>4__this = this;
				int num = this.curFadingIter + 1;
				<>4__this.curFadingIter = num;
				int fadingIter = num;
				while ((factor > 1.0) ? (newVolume - curVolume > 0.01) : (curVolume - newVolume > 0.01))
				{
					Thread.Sleep(sleepMs);
					for (stepsToDo += stepsPer10ms; stepsToDo > 1.0; stepsToDo -= 1.0)
					{
						this.SetVolume((float)(curVolume *= factor));
					}
					if (fadingIter != this.curFadingIter)
					{
						return;
					}
				}
				this.Params.Volume = (float)curVolume;
				this.fadeState = 0;
				if (onFaded != null && fadingIter == this.curFadingIter)
				{
					Action action;
					if ((action = <>9__1) == null)
					{
						action = (<>9__1 = delegate
						{
							onFaded(this);
						});
					}
					ScreenManager.EnqueueMainThreadTask(action);
				}
			});
		}

		public void FadeOut(float duration, Action<ILoadedSound> onFadedOut)
		{
			this.FadeTo(0.0, duration, onFadedOut);
		}

		public void FadeIn(float duration, Action<ILoadedSound> onFadedIn)
		{
			this.FadeTo(1.0, duration, onFadedIn);
		}

		public void FadeOutAndStop(float duration)
		{
			this.FadeTo(0.0, duration, delegate(ILoadedSound sound)
			{
				this.Stop();
			});
		}

		public void SetLooping(bool on)
		{
			this.Params.ShouldLoop = on;
			AL.Source(this.sourceId, ALSourceb.Looping, on);
			this.testError("set looping");
		}

		public static void DisposeAllSounds()
		{
			object obj = LoadedSoundNative.loadedSoundsLock;
			lock (obj)
			{
				foreach (LoadedSoundNative loadedSoundNative in LoadedSoundNative.loadedSounds)
				{
					loadedSoundNative.disposeSoundSource();
				}
			}
		}

		private static object loadedSoundsLock = new object();

		private static readonly List<LoadedSoundNative> loadedSounds = new List<LoadedSoundNative>();

		private SoundParams soundParams;

		private AudioMetaData sample;

		private int sourceId;

		private int bufferId;

		private int fadeState;

		private bool disposed;

		private string trace;

		private float pitchOffset;

		private float playbackPosition;

		private ALSourceState sourceState = ALSourceState.Stopped;

		private float soundLengthSeconds;

		private bool didStop;

		private long stoppedMsAgo;

		private int curFadingIter;
	}
}
