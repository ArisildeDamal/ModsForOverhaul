using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class SystemPlayerEnvAwarenessTracker : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "pltr";
			}
		}

		private TrackedPlayerProperties latestProperties
		{
			get
			{
				return this.game.playerProperties;
			}
		}

		public SystemPlayerEnvAwarenessTracker(ClientMain game)
			: base(game)
		{
			game.RegisterGameTickListener(new Action<float>(this.OnGameTick), 20, 0);
			game.RegisterGameTickListener(new Action<float>(this.OnGameTick1s), 1000, 0);
		}

		private void OnGameTick1s(float dt)
		{
			GlobalConstants.CurrentDistanceToRainfallClient = (float)this.game.blockAccessor.GetDistanceToRainFall(this.game.EntityPlayer.Pos.AsBlockPos, 12, 4);
		}

		public override void OnOwnPlayerDataReceived()
		{
			base.OnOwnPlayerDataReceived();
			BlockPos pos = this.game.EntityPlayer.Pos.AsBlockPos;
			this.currentProperties.PlayerChunkPos.X = pos.X / this.game.WorldMap.ClientChunkSize;
			this.currentProperties.PlayerChunkPos.Y = pos.InternalY / this.game.WorldMap.ClientChunkSize;
			this.currentProperties.PlayerChunkPos.Z = pos.Z / this.game.WorldMap.ClientChunkSize;
		}

		public void OnGameTick(float dt)
		{
			this.latestProperties.EyesInWaterColorShift = this.game.GetEyesInWaterColorShift();
			this.latestProperties.EyesInWaterDepth = this.game.EyesInWaterDepth();
			this.latestProperties.EyesInLavaColorShift = this.game.GetEyesInLavaColorShift();
			this.latestProperties.EyesInLavaDepth = this.game.EyesInLavaDepth();
			this.latestProperties.DayLight = this.game.GameWorldCalendar.DayLightStrength;
			this.latestProperties.MoonLight = this.game.GameWorldCalendar.MoonLightStrength;
			BlockPos pos = this.game.EntityPlayer.Pos.AsBlockPos;
			this.latestProperties.PlayerChunkPos.X = pos.X / this.game.WorldMap.ClientChunkSize;
			this.latestProperties.PlayerChunkPos.Y = pos.InternalY / this.game.WorldMap.ClientChunkSize;
			this.latestProperties.PlayerChunkPos.Z = pos.Z / this.game.WorldMap.ClientChunkSize;
			this.latestProperties.PlayerPosDiv8.X = pos.X / 8;
			this.latestProperties.PlayerPosDiv8.Y = pos.InternalY / 8;
			this.latestProperties.PlayerPosDiv8.Z = pos.Z / 8;
			this.latestProperties.FallSpeed = this.game.EntityPlayer.Pos.Motion.Length();
			this.latestProperties.DistanceToSpawnPoint = (float)((int)this.game.EntityPlayer.Pos.DistanceTo(this.game.player.SpawnPosition.ToVec3d()));
			double y = this.game.EntityPlayer.Pos.Y;
			this.currentProperties.posY = (this.latestProperties.posY = (((double)this.game.SeaLevel < y) ? ((float)(y / (double)this.game.SeaLevel)) : ((float)((y - (double)this.game.SeaLevel) / (double)(this.game.WorldMap.MapSizeY - this.game.SeaLevel)))));
			this.currentProperties.sunSlight = (this.latestProperties.sunSlight = (float)this.game.WorldMap.RelaxedBlockAccess.GetLightLevel(pos, EnumLightLevelType.OnlySunLight));
			this.currentProperties.Playstyle = (this.latestProperties.Playstyle = this.game.ServerInfo.Playstyle);
			this.currentProperties.PlayListCode = (this.latestProperties.PlayListCode = this.game.ServerInfo.PlayListCode);
			if (Math.Abs(this.latestProperties.FallSpeed - this.currentProperties.FallSpeed) > 0.005)
			{
				this.Trigger(EnumProperty.FallSpeed);
			}
			if (Math.Abs(this.latestProperties.EyesInWaterDepth - this.currentProperties.EyesInWaterDepth) > 0.005f || this.currentProperties.EyesInWaterDepth == 0f)
			{
				this.Trigger(EnumProperty.EyesInWaterDepth);
			}
			if (this.latestProperties.EyesInWaterColorShift != this.currentProperties.EyesInWaterColorShift)
			{
				this.Trigger(EnumProperty.EyesInWaterColorShift);
			}
			if (Math.Abs(this.latestProperties.EyesInLavaDepth - this.currentProperties.EyesInLavaDepth) > 0.005f)
			{
				this.Trigger(EnumProperty.EyesInLavaDepth);
			}
			if (this.latestProperties.EyesInLavaColorShift != this.currentProperties.EyesInLavaColorShift)
			{
				this.Trigger(EnumProperty.EyesInLavaColorShift);
			}
			if (this.latestProperties.DayLight != this.currentProperties.DayLight)
			{
				this.Trigger(EnumProperty.DayLight);
			}
			if (this.latestProperties.MoonLight != this.currentProperties.MoonLight)
			{
				this.Trigger(EnumProperty.MoonLight);
			}
			if (!this.latestProperties.PlayerChunkPos.Equals(this.currentProperties.PlayerChunkPos))
			{
				this.Trigger(EnumProperty.PlayerChunkPos);
			}
			if (!this.latestProperties.PlayerPosDiv8.Equals(this.currentProperties.PlayerPosDiv8))
			{
				this.Trigger(EnumProperty.PlayerPosDiv8);
			}
			this.currentProperties.EyesInWaterColorShift = this.latestProperties.EyesInWaterColorShift;
			this.currentProperties.EyesInWaterDepth = this.latestProperties.EyesInWaterDepth;
			this.currentProperties.EyesInLavaColorShift = this.latestProperties.EyesInLavaColorShift;
			this.currentProperties.EyesInLavaDepth = this.latestProperties.EyesInLavaDepth;
			this.currentProperties.DayLight = this.latestProperties.DayLight;
			this.currentProperties.PlayerChunkPos.Set(this.latestProperties.PlayerChunkPos);
			this.currentProperties.PlayerPosDiv8.Set(this.latestProperties.PlayerPosDiv8);
			this.currentProperties.FallSpeed = this.latestProperties.FallSpeed;
		}

		public void Trigger(EnumProperty property)
		{
			List<OnPlayerPropertyChanged> watchers = null;
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager != null)
			{
				eventManager.OnPlayerPropertyChanged.TryGetValue(property, out watchers);
			}
			if (watchers != null)
			{
				foreach (OnPlayerPropertyChanged onPlayerPropertyChanged in watchers)
				{
					onPlayerPropertyChanged(this.currentProperties, this.latestProperties);
				}
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		private TrackedPlayerProperties currentProperties = new TrackedPlayerProperties();
	}
}
