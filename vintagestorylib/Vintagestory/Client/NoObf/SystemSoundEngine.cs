using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class SystemSoundEngine : ClientSystem, IRenderer, IDisposable
	{
		public override string Name
		{
			get
			{
				return "soen";
			}
		}

		public double RenderOrder
		{
			get
			{
				return 1.0;
			}
		}

		public int RenderRange
		{
			get
			{
				return 1;
			}
		}

		public SystemSoundEngine(ClientMain game)
			: base(game)
		{
			game.eventManager.RegisterRenderer(this, EnumRenderStage.Before, "updateAudioListener");
			game.RegisterGameTickListener(new Action<float>(this.OnGameTick100ms), 100, 0);
			game.RegisterGameTickListener(new Action<float>(this.OnGameTick500ms), 500, 0);
			ClientSettings.Inst.AddWatcher<int>("soundLevel", new OnSettingsChanged<int>(this.OnSoundLevelChanged));
			ClientSettings.Inst.AddWatcher<int>("entitySoundLevel", new OnSettingsChanged<int>(this.OnSoundLevelChanged));
			ClientSettings.Inst.AddWatcher<int>("ambientSoundLevel", new OnSettingsChanged<int>(this.OnSoundLevelChanged));
			ClientSettings.Inst.AddWatcher<int>("weatherSoundLevel", new OnSettingsChanged<int>(this.OnSoundLevelChanged));
			game.api.ChatCommands.GetOrCreate("debug").BeginSub("sound").WithDescription("sound")
				.BeginSub("list")
				.WithDescription("list")
				.HandleWith(new OnCommandDelegate(this.onListSounds))
				.EndSub()
				.EndSub();
			this.intersectionTester = new AABBIntersectionTest(new OffthreadBaSupplier(game));
		}

		private TextCommandResult onListSounds(TextCommandCallingArgs args)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Active sounds: ");
			sb.AppendLine("IsPlaying | Location | Sound path");
			foreach (ILoadedSound val in this.game.ActiveSounds)
			{
				if (!val.IsDisposed)
				{
					sb.AppendLine(string.Format("{0} | {1} | {2}", val.IsPlaying, val.Params.Position, val.Params.Location.ToShortString()));
				}
			}
			this.game.Logger.Notification(sb.ToString());
			return TextCommandResult.Success("Active sounds printed to client-main.txt", null);
		}

		public override void OnBlockTexturesLoaded()
		{
			ClientMain game = this.game;
			IAsset asset = this.game.Platform.AssetManager.TryGet("sounds/soundconfig.json", true);
			game.SoundConfig = ((asset != null) ? asset.ToObject<SoundConfig>(null) : null);
			if (this.game.SoundConfig == null)
			{
				this.game.SoundConfig = new SoundConfig();
			}
			for (int i = 0; i < this.game.Blocks.Count; i++)
			{
				Block block = this.game.Blocks[i];
				if (block != null && !(block.Code == null))
				{
					if (block.Sounds == null)
					{
						block.Sounds = new BlockSounds();
					}
					if (block.Sounds.Walk == null)
					{
						block.Sounds.Walk = this.game.SoundConfig.defaultBlockSounds.Walk;
					}
					if (block.Sounds.Place == null)
					{
						block.Sounds.Place = this.game.SoundConfig.defaultBlockSounds.Place;
					}
					if (block.Sounds.Hit == null)
					{
						block.Sounds.Hit = this.game.SoundConfig.defaultBlockSounds.Hit;
					}
					if (block.Sounds.Break == null)
					{
						block.Sounds.Break = this.game.SoundConfig.defaultBlockSounds.Break;
					}
				}
			}
		}

		private void OnGameTick500ms(float dt)
		{
			if (this.game.IsPaused)
			{
				return;
			}
			int count = this.game.ActiveSounds.Count;
			while (count-- > 0)
			{
				ILoadedSound sound = this.game.ActiveSounds.Dequeue();
				if (sound == null)
				{
					this.game.Logger.Error("Found a null sound in the ActiveSounds queue, something is incorrectly programmed. Skipping over it.");
				}
				else
				{
					SoundParams @params = sound.Params;
					if (@params != null && @params.DisposeOnFinish && sound.HasStopped && sound.HasReverbStopped(this.game.ElapsedMilliseconds))
					{
						sound.Dispose();
					}
					else if (!sound.IsDisposed)
					{
						this.game.ActiveSounds.Enqueue(sound);
					}
				}
			}
			if (!this.scanning)
			{
				TyronThreadPool.QueueLongDurationTask(new Action(this.scanReverbnessOffthread));
			}
		}

		private void scanReverbnessOffthread()
		{
			this.scanning = true;
			EntityPos entitypos = this.game.player.Entity.Pos.Copy().Add(this.game.player.Entity.LocalEyePos.X, this.game.player.Entity.LocalEyePos.Y, this.game.player.Entity.LocalEyePos.Z);
			Vec3d plrpos = this.game.player.Entity.Pos.XYZ.Add(this.game.player.Entity.LocalEyePos);
			Vec3d minpos = plrpos.Clone();
			Vec3d maxpos = plrpos.Clone();
			BlockSelection blocksel = new BlockSelection();
			new EntitySelection();
			double nowreverbness = 0.0;
			IBlockAccessor blockAccessor = this.game.World.BlockAccessor;
			for (float yaw = 0f; yaw < 360f; yaw += 45f)
			{
				for (float pitch = -90f; pitch <= 90f; pitch += 45f)
				{
					int faceIndex;
					if (pitch <= -45f)
					{
						faceIndex = BlockFacing.UP.Index;
					}
					else if (pitch >= 45f)
					{
						faceIndex = BlockFacing.DOWN.Index;
					}
					else
					{
						faceIndex = BlockFacing.HorizontalFromYaw(yaw).Opposite.Index;
					}
					Ray ray = Ray.FromAngles(plrpos, pitch * 0.017453292f, yaw * 0.017453292f, 35f);
					this.intersectionTester.LoadRayAndPos(ray);
					float range = (float)ray.Length;
					blocksel = this.intersectionTester.GetSelectedBlock(range, (BlockPos pos, Block block) => true, true);
					Block block2 = ((blocksel != null) ? blocksel.Block : null);
					if (block2 != null && (block2.BlockMaterial == EnumBlockMaterial.Metal || block2.BlockMaterial == EnumBlockMaterial.Ore || block2.BlockMaterial == EnumBlockMaterial.Mantle || block2.BlockMaterial == EnumBlockMaterial.Ice || block2.BlockMaterial == EnumBlockMaterial.Ceramic || block2.BlockMaterial == EnumBlockMaterial.Brick || block2.BlockMaterial == EnumBlockMaterial.Stone) && block2.SideIsSolid(blocksel.Position, faceIndex))
					{
						Vec3d pos2 = blocksel.FullPosition;
						float distance = pos2.DistanceTo(plrpos);
						nowreverbness += (Math.Log((double)(distance + 1f)) / 18.0 - 0.07) * 3.0;
						minpos.Set(Math.Min(minpos.X, pos2.X), Math.Min(minpos.Y, pos2.Y), Math.Min(minpos.Z, pos2.Z));
						maxpos.Set(Math.Max(maxpos.X, pos2.X), Math.Max(maxpos.Y, pos2.Y), Math.Max(maxpos.Z, pos2.Z));
					}
					else
					{
						nowreverbness -= 0.2;
						entitypos.Yaw = yaw;
						entitypos.Pitch = pitch;
						entitypos.AheadCopy(35.0);
						minpos.Set(Math.Min(minpos.X, entitypos.X), Math.Min(minpos.Y, entitypos.InternalY), Math.Min(minpos.Z, entitypos.Z));
						maxpos.Set(Math.Max(maxpos.X, entitypos.X), Math.Max(maxpos.Y, entitypos.InternalY), Math.Max(maxpos.Z, entitypos.Z));
					}
				}
			}
			SystemSoundEngine.TargetReverbness = (float)nowreverbness;
			SystemSoundEngine.RoomLocation = new Cuboidi(minpos.AsBlockPos, maxpos.AsBlockPos).GrowBy(10, 10, 10);
			this.scanning = false;
		}

		public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
		{
			Vec3f look = this.game.EntityPlayer.Pos.GetViewVector();
			Vec3d eyesPos = this.game.EntityPlayer.Pos.XYZ.Add(this.game.EntityPlayer.LocalEyePos);
			this.game.Platform.UpdateAudioListener((float)eyesPos.X, (float)eyesPos.Y, (float)eyesPos.Z, look.X, 0f, look.Z);
			SystemSoundEngine.NowReverbness += (SystemSoundEngine.TargetReverbness - SystemSoundEngine.NowReverbness) * deltaTime / 1.5f;
		}

		private void OnGameTick100ms(float dt)
		{
			if (this.game.api.renderapi.ShaderUniforms.GlitchStrength > 0.5f)
			{
				this.glitchActive = true;
				float str = GameMath.Clamp(this.game.api.renderapi.ShaderUniforms.GlitchStrength * 2f, 0f, 1f);
				using (Queue<ILoadedSound>.Enumerator enumerator = this.game.ActiveSounds.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						ILoadedSound val = enumerator.Current;
						if (val.Params.SoundType != EnumSoundType.SoundGlitchunaffected && val.Params.SoundType != EnumSoundType.AmbientGlitchunaffected && val.Params.SoundType != EnumSoundType.MusicGlitchunaffected)
						{
							float rnd = (float)this.game.Rand.NextDouble() * 0.75f;
							int dir = this.game.Rand.Next(2) * 2 - 1;
							val.SetPitchOffset(GameMath.Mix(0f, rnd * (float)dir - 0.2f, str));
						}
					}
					goto IL_0187;
				}
			}
			if (this.glitchActive)
			{
				this.glitchActive = false;
				foreach (ILoadedSound val2 in this.game.ActiveSounds)
				{
					if (val2.Params.SoundType != EnumSoundType.SoundGlitchunaffected && val2.Params.SoundType != EnumSoundType.AmbientGlitchunaffected && val2.Params.SoundType != EnumSoundType.MusicGlitchunaffected)
					{
						val2.SetPitchOffset(0f);
					}
				}
			}
			IL_0187:
			if (this.submerged() && !this.prevSubmerged)
			{
				this.prevSubmerged = true;
				using (Queue<ILoadedSound>.Enumerator enumerator = this.game.ActiveSounds.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						ILoadedSound val3 = enumerator.Current;
						if (!val3.IsDisposed)
						{
							val3.SetLowPassfiltering(0.06f);
							if (!this.glitchActive && val3.Params.SoundType != EnumSoundType.Music && val3.Params.SoundType != EnumSoundType.MusicGlitchunaffected)
							{
								val3.SetPitchOffset(-0.15f);
							}
						}
					}
					goto IL_0298;
				}
			}
			if (this.prevSubmerged && !this.submerged())
			{
				this.prevSubmerged = false;
				foreach (ILoadedSound val4 in this.game.ActiveSounds)
				{
					if (!val4.IsDisposed)
					{
						val4.SetLowPassfiltering(1f);
						if (!this.glitchActive)
						{
							val4.SetPitchOffset(0f);
						}
					}
				}
			}
			IL_0298:
			if (this.prevReverbKey != this.reverbKey())
			{
				this.prevReverbKey = this.reverbKey();
				foreach (ILoadedSound val5 in this.game.ActiveSounds)
				{
					if (!val5.IsDisposed && val5.IsReady)
					{
						if (val5.Params.Position == null || val5.Params.Position == SystemSoundEngine.Zero || SystemSoundEngine.RoomLocation.ContainsOrTouches(val5.Params.Position))
						{
							val5.SetReverb(Math.Max(0f, SystemSoundEngine.NowReverbness));
						}
						else
						{
							val5.SetReverb(0f);
						}
					}
				}
			}
		}

		private int reverbKey()
		{
			if (!this.submerged())
			{
				return (int)(SystemSoundEngine.NowReverbness * 10f);
			}
			return 0;
		}

		private bool submerged()
		{
			return this.game.EyesInWaterDepth() > 0f || this.game.EyesInLavaDepth() > 0f;
		}

		public override void Dispose(ClientMain game)
		{
			while (game.ActiveSounds.Count > 0)
			{
				ILoadedSound loadedSound = game.ActiveSounds.Dequeue();
				if (loadedSound != null)
				{
					loadedSound.Stop();
				}
				if (loadedSound != null)
				{
					loadedSound.Dispose();
				}
			}
		}

		private void OnSoundLevelChanged(int newValue)
		{
			int count = this.game.ActiveSounds.Count;
			while (count-- > 0)
			{
				ILoadedSound sound = this.game.ActiveSounds.Dequeue();
				sound.SetVolume();
				this.game.ActiveSounds.Enqueue(sound);
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		public void Dispose()
		{
		}

		public static Vec3f Zero = new Vec3f();

		public static float NowReverbness = 0f;

		public static float TargetReverbness = 0f;

		private bool scanning;

		public static Cuboidi RoomLocation = new Cuboidi();

		private AABBIntersectionTest intersectionTester;

		private bool glitchActive;

		private bool prevSubmerged;

		private int prevReverbKey = -999;
	}
}
