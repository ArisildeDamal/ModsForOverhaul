using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client
{
	public class AudioOpenAl : IDisposable
	{
		public IList<string> Devices
		{
			get
			{
				return ALC.GetString(AlcGetStringList.AllDevicesSpecifier).ToList<string>();
			}
		}

		public string CurrentDevice
		{
			get
			{
				return ALC.GetString(this.Device, AlcGetString.DeviceSpecifier);
			}
		}

		public static ReverbEffect GetOrCreateReverbEffect(float reverbness)
		{
			if (reverbness < 0.25f)
			{
				return AudioOpenAl.NoReverb;
			}
			float reverbMin = 0.5f;
			float range = 7f - reverbMin;
			int key = Math.Min(Math.Max(0, (int)((reverbness - reverbMin) / range * 24f)), 23);
			ReverbEffect effe = AudioOpenAl.reverbEffectsByReverbness[key];
			if (effe == null && AudioOpenAl.HasEffectsExtension)
			{
				int reverbEffectSlot = ALC.EFX.GenAuxiliaryEffectSlot();
				int reverbEffectId = ALC.EFX.GenEffect();
				ALC.EFX.Effect(reverbEffectId, EffectInteger.EffectType, 1);
				ALC.EFX.Effect(reverbEffectId, EffectFloat.ReverbDecayTime, (float)key / 23f * range + reverbMin);
				ALC.EFX.AuxiliaryEffectSlot(reverbEffectSlot, EffectSlotInteger.Effect, reverbEffectId);
				ReverbEffect[] array = AudioOpenAl.reverbEffectsByReverbness;
				int num = key;
				ReverbEffect reverbEffect = new ReverbEffect();
				reverbEffect.reverbEffectId = reverbEffectId;
				reverbEffect.ReverbEffectSlot = reverbEffectSlot;
				ReverbEffect reverbEffect2 = reverbEffect;
				array[num] = reverbEffect;
				effe = reverbEffect2;
			}
			return effe;
		}

		public AudioOpenAl(ILogger logger)
		{
			this.initContext(logger);
		}

		~AudioOpenAl()
		{
			this.Dispose(false);
		}

		public float MasterSoundLevel
		{
			get
			{
				return AL.GetListener(ALListenerf.Gain);
			}
			set
			{
				AL.Listener(ALListenerf.Gain, value);
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			LoadedSoundNative.DisposeAllSounds();
			for (int i = 0; i < AudioOpenAl.reverbEffectsByReverbness.Length; i++)
			{
				ReverbEffect val = AudioOpenAl.reverbEffectsByReverbness[i];
				if (val != null)
				{
					if (AudioOpenAl.HasEffectsExtension)
					{
						ALC.EFX.DeleteEffect(val.reverbEffectId);
						ALC.EFX.DeleteAuxiliaryEffectSlot(val.ReverbEffectSlot);
					}
					AudioOpenAl.reverbEffectsByReverbness[i] = null;
				}
			}
			if (AudioOpenAl.HasEffectsExtension)
			{
				ALC.EFX.DeleteFilter(AudioOpenAl.EchoFilterId);
				AudioOpenAl.EchoFilterId = 0;
			}
			if (this.Device != ALDevice.Null)
			{
				ALC.MakeContextCurrent(ALContext.Null);
				ALC.DestroyContext(this.Context);
				ALC.CloseDevice(this.Device);
				this.Device = ALDevice.Null;
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void initContext(ILogger logger)
		{
			try
			{
				if (this.Device != ALDevice.Null)
				{
					ALC.MakeContextCurrent(ALContext.Null);
					ALC.DestroyContext(this.Context);
					ALC.CloseDevice(this.Device);
				}
				string desiredDevice = ClientSettings.AudioDevice;
				if (!this.Devices.Any((string d) => d.Equals(desiredDevice)))
				{
					desiredDevice = null;
					ClientSettings.AudioDevice = null;
				}
				this.Device = ALC.OpenDevice(desiredDevice);
				AudioOpenAl.UseHrtf = ClientSettings.UseHRTFAudio;
				int[] hrtfSettings;
				if (!ClientSettings.AllowSettingHRTFAudio)
				{
					hrtfSettings = Array.Empty<int>();
					AudioOpenAl.UseHrtf = false;
				}
				else if (AudioOpenAl.UseHrtf)
				{
					int[] array;
					if (!ClientSettings.Force48kHzHRTFAudio)
					{
						RuntimeHelpers.InitializeArray(array = new int[4], fieldof(global::<PrivateImplementationDetails>.FB952A0E23AB78F0986F2082EE2A1BD09CCFA3AB622A3AFDFCBE0922AAD19576).FieldHandle);
					}
					else
					{
						RuntimeHelpers.InitializeArray(array = new int[6], fieldof(global::<PrivateImplementationDetails>.CD61B50FF7909DC7917D957586AC7DA7F4A30CB6968DA41E470E45F124FDABC6).FieldHandle);
					}
					hrtfSettings = array;
				}
				else
				{
					hrtfSettings = new int[] { 6546, 0, 6572, 6574 };
				}
				this.Context = ALC.CreateContext(this.Device, hrtfSettings);
				ALC.MakeContextCurrent(this.Context);
				AudioOpenAl.CheckALError(logger, "Start");
				AL.Listener(ALListener3f.Velocity, 0f, 0f, 0f);
			}
			catch (Exception ex)
			{
				logger.Error("Failed creating audio context");
				logger.Error(ex);
				return;
			}
			ALContextAttributes alContextAttributes = ALC.GetContextAttributes(this.Device);
			logger.Notification("OpenAL Initialized. Available Mono/Stereo Sources: {0}/{1}", new object[] { alContextAttributes.MonoSources, alContextAttributes.StereoSources });
			AudioOpenAl.HasEffectsExtension = ALC.EFX.IsExtensionPresent(this.Device);
			if (!AudioOpenAl.HasEffectsExtension)
			{
				logger.Notification("OpenAL Effects Extension not found. Disabling extra sound effects now.");
			}
		}

		public static void CheckALError(ILogger logger, string str)
		{
			ALError error = AL.GetError();
			if (error != ALError.NoError)
			{
				logger.Warning("ALError at '" + str + "': " + AL.GetErrorString(error));
			}
		}

		internal void RecreateContext(Logger logger)
		{
			this.Dispose(true);
			this.initContext(logger);
		}

		public static byte[] LoadWave(Stream stream, out int channels, out int bits, out int rate)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			byte[] array;
			using (BinaryReader reader = new BinaryReader(stream))
			{
				if (new string(reader.ReadChars(4)) != "RIFF")
				{
					throw new NotSupportedException("Specified stream is not a wave file.");
				}
				reader.ReadInt32();
				if (new string(reader.ReadChars(4)) != "WAVE")
				{
					throw new NotSupportedException("Specified stream is not a wave file.");
				}
				if (new string(reader.ReadChars(4)) != "fmt ")
				{
					throw new NotSupportedException("Specified wave file is not supported.");
				}
				reader.ReadInt32();
				reader.ReadInt16();
				int num_channels = (int)reader.ReadInt16();
				int sample_rate = reader.ReadInt32();
				reader.ReadInt32();
				reader.ReadInt16();
				int bits_per_sample = (int)reader.ReadInt16();
				if (new string(reader.ReadChars(4)) != "data")
				{
					throw new NotSupportedException("Specified wave file is not supported.");
				}
				reader.ReadInt32();
				channels = num_channels;
				bits = bits_per_sample;
				rate = sample_rate;
				array = reader.ReadBytes((int)reader.BaseStream.Length);
			}
			return array;
		}

		public static ALFormat GetSoundFormat(int channels, int bits)
		{
			if (channels != 1)
			{
				if (channels != 2)
				{
					throw new NotSupportedException("The specified sound format is not supported (channels: " + channels.ToString() + ").");
				}
				if (bits != 8)
				{
					return ALFormat.Stereo16;
				}
				return ALFormat.Stereo8;
			}
			else
			{
				if (bits != 8)
				{
					return ALFormat.Mono16;
				}
				return ALFormat.Mono8;
			}
		}

		public AudioMetaData GetSampleFromArray(IAsset asset)
		{
			Stream stream = new MemoryStream(asset.Data);
			if (stream.ReadByte() == 82 && stream.ReadByte() == 73 && stream.ReadByte() == 70 && stream.ReadByte() == 70)
			{
				stream.Position = 0L;
				int channels;
				int bits_per_sample;
				int sample_rate;
				byte[] sound_data = AudioOpenAl.LoadWave(stream, out channels, out bits_per_sample, out sample_rate);
				return new AudioMetaData(asset)
				{
					Pcm = sound_data,
					BitsPerSample = bits_per_sample,
					Channels = channels,
					Rate = sample_rate,
					Loaded = 1
				};
			}
			stream.Position = 0L;
			return new OggDecoder().OggToWav(stream, asset);
		}

		public void UpdateListener(Vector3 position, Vector3 orientation)
		{
			try
			{
				AL.Listener(ALListener3f.Position, position.X, position.Y, position.Z);
				Vector3 up = Vector3.UnitY;
				AL.Listener(ALListenerfv.Orientation, ref orientation, ref up);
			}
			catch
			{
			}
		}

		private ALContext Context = ALContext.Null;

		private ALDevice Device;

		public static bool UseHrtf;

		public static bool HasEffectsExtension;

		private const int ALC_HRTF_SOFT = 6546;

		private const int ALC_OUTPUT_MODE_SOFT = 6572;

		private const int HrtfEnabled = 5377;

		private const int HrtfDisabled = 6574;

		private const int ALC_FREQUENCY = 4103;

		public const int AL_REMIX_UNMATCHED_SOFT = 2;

		public const int AL_DIRECT_CHANNELS_SOFT = 4147;

		private static ReverbEffect[] reverbEffectsByReverbness = new ReverbEffect[24];

		private static ReverbEffect NoReverb = new ReverbEffect();

		public static int EchoFilterId;
	}
}
