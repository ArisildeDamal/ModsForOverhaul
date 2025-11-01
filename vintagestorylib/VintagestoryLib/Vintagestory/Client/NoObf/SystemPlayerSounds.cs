using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class SystemPlayerSounds : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "plso";
			}
		}

		public SystemPlayerSounds(ClientMain game)
			: base(game)
		{
			game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.FallSpeed, new OnPlayerPropertyChanged(this.OnFallSpeedChange));
			game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.EyesInWaterDepth, new OnPlayerPropertyChanged(this.OnSwimDepthChange));
			game.RegisterGameTickListener(new Action<float>(this.OnGameTick), 20, 0);
			game.eventManager.OnAmbientSoundsScanComplete = new OnAmbientSoundScanCompleteDelegate(this.OnAmbientSoundScan);
			game.eventManager.RegisterRenderer(new Action<float>(this.Render3D), EnumRenderStage.Opaque, "playersoundswireframe", 0.9);
			this.wireframes = new WireframeCube[]
			{
				WireframeCube.CreateUnitCube(game.api, ColorUtil.ToRgba(128, 255, 255, 0)),
				WireframeCube.CreateUnitCube(game.api, ColorUtil.ToRgba(128, 255, 0, 0)),
				WireframeCube.CreateUnitCube(game.api, ColorUtil.ToRgba(128, 0, 255, 0)),
				WireframeCube.CreateUnitCube(game.api, ColorUtil.ToRgba(128, 0, 0, 255)),
				WireframeCube.CreateUnitCube(game.api, ColorUtil.ToRgba(128, 0, 255, 255)),
				WireframeCube.CreateUnitCube(game.api, ColorUtil.ToRgba(128, 255, 0, 255))
			};
		}

		public override void OnBlockTexturesLoaded()
		{
			this.FlySound = this.game.LoadSound(new SoundParams
			{
				Location = new AssetLocation("sounds/environment/wind.ogg"),
				ShouldLoop = true,
				RelativePosition = true,
				DisposeOnFinish = false
			});
			this.UnderWaterSound = this.game.LoadSound(new SoundParams
			{
				Location = new AssetLocation("sounds/environment/underwater.ogg"),
				ShouldLoop = true,
				RelativePosition = true,
				DisposeOnFinish = false
			});
		}

		private void Render3D(float dt)
		{
			this.updateFlySound(dt);
			if (!this.game.api.renderapi.WireframeDebugRender.AmbientSounds)
			{
				return;
			}
			int i = 0;
			foreach (AmbientSound ambientSound in this.ambientSounds.Keys)
			{
				ambientSound.RenderWireFrame(this.game, this.wireframes[i % this.wireframes.Length]);
				i++;
			}
		}

		private void updateFlySound(float dt)
		{
			bool nowActive = Math.Abs(this.flySpeed) - 0.05000000074505806 > 0.2;
			if (nowActive && !this.fallActive && !this.FlySound.IsPlaying)
			{
				this.FlySound.Start();
			}
			if (!nowActive && (double)this.curVolume < 0.08 && this.FlySound.IsPlaying)
			{
				this.FlySound.Stop();
			}
			if (this.FlySound.IsPlaying)
			{
				this.targetVolume = (nowActive ? Math.Min(1f, Math.Abs((float)this.flySpeed)) : 0f);
				this.curVolume = GameMath.Clamp(this.curVolume + (this.targetVolume - this.curVolume) * dt * (float)(nowActive ? 1 : 5), 0f, 1f);
				this.FlySound.SetVolume(this.curVolume);
			}
			this.fallActive = nowActive;
		}

		private void OnAmbientSoundScan(List<AmbientSound> newAmbientSounds)
		{
			HashSet<AmbientSound> soundsToFadeout = new HashSet<AmbientSound>(this.ambientSounds.Keys);
			if (ClientSettings.AmbientSoundLevel > 0)
			{
				foreach (AmbientSound newambsound in newAmbientSounds)
				{
					soundsToFadeout.Remove(newambsound);
					if (this.ambientSounds.ContainsKey(newambsound))
					{
						AmbientSound ambientSound = this.ambientSounds[newambsound];
						ambientSound.QuantityNearbyBlocks = newambsound.QuantityNearbyBlocks;
						ambientSound.BoundingBoxes = newambsound.BoundingBoxes;
						ambientSound.VolumeMul = newambsound.VolumeMul;
						ambientSound.FadeToNewVolumne();
					}
					else
					{
						this.ambientSounds[newambsound] = newambsound;
						newambsound.Sound = this.game.LoadSound(new SoundParams
						{
							Location = newambsound.AssetLoc,
							ShouldLoop = true,
							RelativePosition = false,
							DisposeOnFinish = false,
							Volume = 0.01f,
							Position = new Vec3f(),
							Range = 40f,
							SoundType = newambsound.SoundType
						});
						newambsound.updatePosition(this.game.EntityPlayer.Pos);
						newambsound.Sound.Start();
						newambsound.Sound.PlaybackPosition = (float)this.game.Rand.NextDouble() * newambsound.Sound.SoundLengthSeconds;
						newambsound.FadeToNewVolumne();
					}
				}
			}
			foreach (AmbientSound ambsound in soundsToFadeout)
			{
				ambsound.Sound.FadeOut(1f, delegate(ILoadedSound loadedsound)
				{
					loadedsound.Stop();
					loadedsound.Dispose();
				});
				this.ambientSounds.Remove(ambsound);
			}
		}

		public void OnGameTick(float dt)
		{
			foreach (KeyValuePair<AmbientSound, AmbientSound> val in this.ambientSounds)
			{
				val.Value.updatePosition(this.game.EntityPlayer.Pos);
			}
		}

		internal int GetSoundCount(string[] soundwalk)
		{
			int count = 0;
			if (soundwalk == null)
			{
				return 0;
			}
			for (int i = 0; i < soundwalk.Length; i++)
			{
				if (soundwalk[i] != null)
				{
					count++;
				}
			}
			return count;
		}

		private void OnSwimDepthChange(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
		{
			bool nowActive = Math.Abs(newValues.EyesInWaterDepth) > 0f;
			if (nowActive && !this.underwaterActive)
			{
				this.UnderWaterSound.Start();
			}
			if (!nowActive && this.underwaterActive)
			{
				this.UnderWaterSound.Stop();
			}
			if (nowActive)
			{
				this.UnderWaterSound.SetVolume(Math.Min(0.1f, newValues.EyesInWaterDepth / 2f));
			}
			this.underwaterActive = nowActive;
		}

		private void OnFallSpeedChange(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
		{
			this.flySpeed = newValues.FallSpeed;
		}

		public override void Dispose(ClientMain game)
		{
			foreach (AmbientSound ambientSound in this.ambientSounds.Keys)
			{
				ILoadedSound sound = ambientSound.Sound;
				if (sound != null)
				{
					sound.Dispose();
				}
			}
			ILoadedSound flySound = this.FlySound;
			if (flySound != null)
			{
				flySound.Dispose();
			}
			ILoadedSound underWaterSound = this.UnderWaterSound;
			if (underWaterSound != null)
			{
				underWaterSound.Dispose();
			}
			if (this.wireframes != null)
			{
				foreach (WireframeCube wireframeCube in this.wireframes)
				{
					if (wireframeCube != null)
					{
						wireframeCube.Dispose();
					}
				}
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		private ILoadedSound FlySound;

		private ILoadedSound UnderWaterSound;

		private Dictionary<AmbientSound, AmbientSound> ambientSounds = new Dictionary<AmbientSound, AmbientSound>();

		private WireframeCube[] wireframes;

		private bool fallActive;

		private bool underwaterActive;

		private float targetVolume;

		private float curVolume;

		private double flySpeed;
	}
}
