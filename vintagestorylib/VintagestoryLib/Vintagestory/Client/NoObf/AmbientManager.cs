using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class AmbientManager : IAmbientManager
	{
		public Vec4f BlendedFogColor { get; set; }

		public Vec3f BlendedAmbientColor { get; set; }

		public float BlendedFogDensity { get; set; }

		public float BlendedFogMin { get; set; }

		public float BlendedFlatFogDensity { get; set; }

		public float BlendedFlatFogYOffset { get; set; }

		public float BlendedCloudBrightness { get; set; }

		public float BlendedCloudDensity { get; set; }

		public float BlendedCloudYPos { get; set; }

		public float BlendedFlatFogYPosForShader { get; set; }

		public float BlendedSceneBrightness { get; set; }

		public float BlendedFogBrightness { get; set; }

		public OrderedDictionary<string, AmbientModifier> CurrentModifiers
		{
			get
			{
				return this.ambientModifiers;
			}
		}

		public float ViewDistance
		{
			get
			{
				return (float)ClientSettings.ViewDistance;
			}
		}

		public AmbientModifier Base
		{
			get
			{
				return this.BaseModifier;
			}
		}

		public AmbientManager(ClientMain game)
		{
			this.BlendedFogColor = new Vec4f(1f, 1f, 1f, 1f);
			this.BlendedAmbientColor = new Vec3f();
			this.game = game;
			game.eventManager.RegisterRenderer(new Action<float>(this.UpdateAmbient), EnumRenderStage.Before, "ambientmanager", 0.0);
			this.ambientModifiers = new OrderedDictionary<string, AmbientModifier>();
			this.ambientModifiers["sunglow"] = (this.Sunglow = new AmbientModifier
			{
				FogColor = WeightedFloatArray.New(new float[] { 0.8f, 0.8f, 0.8f }, 0f),
				AmbientColor = WeightedFloatArray.New(new float[] { 1f, 1f, 1f }, 0.9f),
				FogDensity = WeightedFloat.New(0f, 0f)
			}.EnsurePopulated());
			this.ambientModifiers["serverambient"] = new AmbientModifier().EnsurePopulated();
			this.ambientModifiers["night"] = new AmbientModifier().EnsurePopulated();
			this.ambientModifiers["water"] = new AmbientModifier
			{
				FogColor = WeightedFloatArray.New(new float[] { 0.18f, 0.74f, 1f }, 0f),
				FogDensity = WeightedFloat.New(0.07f, 0f),
				FogMin = WeightedFloat.New(0.03f, 0f),
				AmbientColor = WeightedFloatArray.New(new float[] { 0.18f, 0.74f, 1f }, 0f)
			}.EnsurePopulated();
			this.ambientModifiers["lava"] = new AmbientModifier
			{
				FogColor = WeightedFloatArray.New(new float[] { 1f, 0.92156863f, 0.31764707f }, 0f),
				FogDensity = WeightedFloat.New(0.3f, 0f),
				FogMin = WeightedFloat.New(0.5f, 0f),
				AmbientColor = WeightedFloatArray.New(new float[] { 1f, 0.92156863f, 0.31764707f }, 0f)
			}.EnsurePopulated();
			this.ambientModifiers["deepwater"] = new AmbientModifier
			{
				FogColor = WeightedFloatArray.New(new float[] { 0f, 0f, 0.07f }, 0f),
				FogMin = WeightedFloat.New(0.1f, 0f),
				FogDensity = WeightedFloat.New(0.1f, 0f),
				AmbientColor = WeightedFloatArray.New(new float[] { 0f, 0f, 0.07f }, 0f)
			}.EnsurePopulated();
			this.ambientModifiers["blackfogincaves"] = new AmbientModifier
			{
				FogColor = WeightedFloatArray.New(new float[3], 0f)
			}.EnsurePopulated();
			ClientSettings.Inst.AddWatcher<int>("viewDistance", new OnSettingsChanged<int>(this.OnViewDistanceChanged));
			this.ShadowQuality = ClientSettings.ShadowMapQuality;
			ClientSettings.Inst.AddWatcher<int>("shadowMapQuality", delegate(int b)
			{
				this.ShadowQuality = ClientSettings.ShadowMapQuality;
			});
			game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.DayLight, new OnPlayerPropertyChanged(this.OnDayLightChanged));
			game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.EyesInWaterColorShift, new OnPlayerPropertyChanged(this.OnPlayerSightBeingChangedByWater));
			game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.EyesInLavaColorShift, new OnPlayerPropertyChanged(this.OnPlayerSightBeingChangedByLava));
			game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.EyesInWaterDepth, new OnPlayerPropertyChanged(this.OnPlayerUnderWater));
		}

		public void LateInit()
		{
			this.game.api.eventapi.PlayerDimensionChanged += this.Eventapi_PlayerDimensionChanged;
		}

		private void Eventapi_PlayerDimensionChanged(IPlayer byPlayer)
		{
			this.UpdateAmbient(0f);
		}

		private void OnPlayerUnderWater(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
		{
			float depthFactor = GameMath.Clamp(newValues.EyesInWaterDepth / 70f, 0f, 1f);
			AmbientModifier ambientModifier = this.ambientModifiers["deepwater"];
			ambientModifier.FogColor.Weight = 0.95f * depthFactor;
			ambientModifier.AmbientColor.Weight = 0.85f * depthFactor;
		}

		private void UpdateDaylight(float dt)
		{
			if (this.smoothedLightLevel < 0f)
			{
				this.smoothedLightLevel = (float)this.game.BlockAccessor.GetLightLevel(this.game.Player.Entity.Pos.AsBlockPos, EnumLightLevelType.OnlySunLight);
			}
			AmbientModifier ambientModifier = this.ambientModifiers["night"];
			float t = Math.Min(0.6f, -this.game.Calendar.SunPositionNormalized.Y) - 0.75f * Math.Min(0.33f, this.game.Calendar.MoonLightStrength);
			ambientModifier.FogBrightness.Weight = GameMath.Clamp(1f - this.game.Calendar.DayLightStrength + GameMath.Clamp(t, 0f, 0.5f) * 0.85f, 0f, 0.88f);
			float p = GameMath.Clamp(1.5f * this.game.Calendar.DayLightStrength - 0.2f, 0.1f, 1f);
			ambientModifier.SceneBrightness.Weight = GameMath.Clamp(1f - p, 0f, 0.65f);
			BlockPos plrPos = this.game.player.Entity.Pos.AsBlockPos;
			int lightlevel = Math.Max(this.game.BlockAccessor.GetLightLevel(plrPos, EnumLightLevelType.OnlySunLight), this.game.BlockAccessor.GetLightLevel(plrPos.Up(1), EnumLightLevelType.OnlySunLight));
			this.smoothedLightLevel += ((float)lightlevel - this.smoothedLightLevel) * dt;
			float fogMultiplier = GameMath.Clamp(3f * this.smoothedLightLevel / 20f, 0f, 1f);
			float fac = (float)GameMath.Clamp(this.game.Player.Entity.Pos.Y / (double)this.game.SeaLevel, 0.0, 1.0);
			fac *= fac;
			fogMultiplier *= fac;
			this.ambientModifiers["blackfogincaves"].FogColor.Weight = GameMath.Clamp(1f - fogMultiplier, 0f, 1f);
		}

		private void OnDayLightChanged(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
		{
			AmbientModifier ambientModifier = this.ambientModifiers["night"];
			ambientModifier.FogBrightness.Value = 0f;
			ambientModifier.SceneBrightness.Value = 0f;
			this.OnPlayerSightBeingChangedByWater(oldValues, newValues);
		}

		private void OnPlayerSightBeingChangedByWater(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
		{
			AmbientModifier ambientModifier = this.ambientModifiers["water"];
			ambientModifier.FogColor.Weight = (float)(newValues.EyesInWaterColorShift * newValues.EyesInWaterColorShift) / 10000f;
			ambientModifier.AmbientColor.Weight = 0.75f * (float)newValues.EyesInWaterColorShift / 100f;
			ambientModifier.FogDensity.Weight = (float)newValues.EyesInWaterColorShift / 100f;
			ambientModifier.FogMin.Weight = (float)newValues.EyesInWaterColorShift / 100f;
			this.game.api.Render.ShaderUniforms.CameraUnderwater = (float)newValues.EyesInWaterColorShift / 100f;
			this.setWaterColors();
		}

		private void setWaterColors()
		{
			AmbientModifier ambientModifier = this.ambientModifiers["water"];
			float daylight = Math.Max(0.2f, this.game.Calendar.DayLightStrength);
			int waterTint = this.game.WorldMap.ApplyColorMapOnRgba("climateWaterTint", null, -1, (int)this.game.EntityPlayer.Pos.X, (int)this.game.EntityPlayer.Pos.Y, (int)this.game.EntityPlayer.Pos.Z, false);
			int[] hsv = ColorUtil.RgbToHsvInts(waterTint & 255, (waterTint >> 8) & 255, (waterTint >> 16) & 255);
			hsv[2] /= 2;
			hsv[2] = (int)((float)hsv[2] * daylight);
			int[] rgbInt = ColorUtil.Hsv2RgbInts(hsv[0], hsv[1], hsv[2]);
			float[] fogColor = ambientModifier.FogColor.Value;
			fogColor[0] = (float)rgbInt[0] / 255f;
			fogColor[1] = (float)rgbInt[1] / 255f;
			fogColor[2] = (float)rgbInt[2] / 255f;
			ambientModifier.AmbientColor.Value[0] = fogColor[0] * 2f;
			ambientModifier.AmbientColor.Value[1] = fogColor[1] * 2f;
			ambientModifier.AmbientColor.Value[2] = fogColor[2] * 2f;
			float wdepth = this.game.EyesInWaterDepth();
			ambientModifier.AmbientColor.Weight = ((wdepth > 0f) ? GameMath.Clamp(this.game.EyesInWaterDepth() / 30f, 0f, 1f) : 0f);
			hsv[1] /= 2;
			rgbInt = ColorUtil.Hsv2RgbInts(hsv[0], hsv[1], hsv[2]);
			this.game.api.Render.ShaderUniforms.WaterMurkColor = new Vec4f((float)rgbInt[0] / 255f, (float)rgbInt[1] / 255f, (float)rgbInt[2] / 255f, 1f);
		}

		private void OnPlayerSightBeingChangedByLava(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
		{
			AmbientModifier ambientModifier = this.ambientModifiers["lava"];
			ambientModifier.FogColor.Weight = (float)(newValues.EyesInLavaColorShift * newValues.EyesInLavaColorShift) / 10000f;
			ambientModifier.AmbientColor.Weight = 0.5f * (float)newValues.EyesInLavaColorShift / 100f;
			ambientModifier.FogDensity.Weight = (float)newValues.EyesInLavaColorShift / 100f;
			ambientModifier.FogMin.Weight = (float)newValues.EyesInLavaColorShift / 100f;
		}

		private void OnViewDistanceChanged(int newValue)
		{
		}

		public void SetFogRange(float density, float min)
		{
			this.BaseModifier.FogDensity.Value = density;
			this.BaseModifier.FogMin.Value = min;
		}

		public void UpdateAmbient(float dt)
		{
			this.setWaterColors();
			this.updateColorGradingValues(dt);
			float[] mixedFogColor = new float[]
			{
				this.BaseModifier.FogColor.Value[0],
				this.BaseModifier.FogColor.Value[1],
				this.BaseModifier.FogColor.Value[2],
				1f
			};
			float[] mixedAmbientColor = new float[]
			{
				this.BaseModifier.AmbientColor.Value[0],
				this.BaseModifier.AmbientColor.Value[1],
				this.BaseModifier.AmbientColor.Value[2]
			};
			this.BlendedFogDensity = this.BaseModifier.FogDensity.Value;
			this.BlendedFogMin = this.BaseModifier.FogMin.Value;
			this.BlendedFlatFogDensity = this.BaseModifier.FlatFogDensity.Value;
			this.BlendedFlatFogYOffset = this.BaseModifier.FlatFogYPos.Value;
			this.BlendedCloudBrightness = this.BaseModifier.CloudBrightness.Value;
			this.BlendedCloudDensity = this.BaseModifier.CloudDensity.Value;
			this.BlendedSceneBrightness = this.BaseModifier.SceneBrightness.Value;
			this.BlendedFogBrightness = this.BaseModifier.FogBrightness.Value;
			this.UpdateDaylight(dt);
			foreach (KeyValuePair<string, AmbientModifier> val in this.ambientModifiers)
			{
				AmbientModifier modifier = val.Value;
				float w = modifier.FogColor.Weight;
				mixedFogColor[0] = w * modifier.FogColor.Value[0] + (1f - w) * mixedFogColor[0];
				mixedFogColor[1] = w * modifier.FogColor.Value[1] + (1f - w) * mixedFogColor[1];
				mixedFogColor[2] = w * modifier.FogColor.Value[2] + (1f - w) * mixedFogColor[2];
				w = modifier.AmbientColor.Weight;
				mixedAmbientColor[0] = w * modifier.AmbientColor.Value[0] + (1f - w) * mixedAmbientColor[0];
				mixedAmbientColor[1] = w * modifier.AmbientColor.Value[1] + (1f - w) * mixedAmbientColor[1];
				mixedAmbientColor[2] = w * modifier.AmbientColor.Value[2] + (1f - w) * mixedAmbientColor[2];
				w = modifier.FogDensity.Weight;
				this.BlendedFogDensity = w * w * modifier.FogDensity.Value + (1f - w) * (1f - w) * this.BlendedFogDensity;
				w = modifier.FlatFogDensity.Weight;
				this.BlendedFlatFogDensity = w * modifier.FlatFogDensity.Value + (1f - w) * this.BlendedFlatFogDensity;
				w = modifier.FogMin.Weight;
				this.BlendedFogMin = w * modifier.FogMin.Value + (1f - w) * this.BlendedFogMin;
				w = modifier.FlatFogYPos.Weight;
				this.BlendedFlatFogYOffset = w * modifier.FlatFogYPos.Value + (1f - w) * this.BlendedFlatFogYOffset;
				w = modifier.CloudBrightness.Weight;
				this.BlendedCloudBrightness = w * modifier.CloudBrightness.Value + (1f - w) * this.BlendedCloudBrightness;
				w = modifier.CloudDensity.Weight;
				this.BlendedCloudDensity = w * modifier.CloudDensity.Value + (1f - w) * this.BlendedCloudDensity;
				w = modifier.SceneBrightness.Weight;
				this.BlendedSceneBrightness = w * modifier.SceneBrightness.Value + (1f - w) * this.BlendedSceneBrightness;
				w = modifier.FogBrightness.Weight;
				this.BlendedFogBrightness = w * modifier.FogBrightness.Value + (1f - w) * this.BlendedFogBrightness;
			}
			mixedFogColor[0] *= this.BlendedSceneBrightness * this.BlendedFogBrightness;
			mixedFogColor[1] *= this.BlendedSceneBrightness * this.BlendedFogBrightness;
			mixedFogColor[2] *= this.BlendedSceneBrightness * this.BlendedFogBrightness;
			this.BlendedFogColor.Set(mixedFogColor);
			mixedAmbientColor[0] *= this.BlendedSceneBrightness;
			mixedAmbientColor[1] *= this.BlendedSceneBrightness;
			mixedAmbientColor[2] *= this.BlendedSceneBrightness;
			this.BlendedAmbientColor.Set(mixedAmbientColor);
			this.BlendedFlatFogYPosForShader = this.BlendedFlatFogYOffset + (float)this.game.SeaLevel;
			double playerHeightFactor = Math.Max(0.0, (this.game.Player.Entity.Pos.Y - (double)this.game.SeaLevel - 5000.0) / 10000.0);
			this.BlendedFogMin = Math.Max(0f, this.BlendedFogMin - (float)playerHeightFactor);
			this.BlendedFogDensity = Math.Max(0f, this.BlendedFogDensity - (float)playerHeightFactor);
			if (float.IsNaN(this.BlendedFlatFogDensity))
			{
				this.BlendedFlatFogDensity = 0f;
				return;
			}
			this.BlendedFlatFogDensity = (float)((double)Math.Sign(this.BlendedFlatFogDensity) * Math.Max(0.0, (double)Math.Abs(this.BlendedFlatFogDensity) - playerHeightFactor));
		}

		private void updateColorGradingValues(float dt)
		{
			DefaultShaderUniforms ShaderUniforms = this.game.Platform.ShaderUniforms;
			if (!ClientSettings.DynamicColorGrading)
			{
				this.prevDynamicColourGrading = false;
				ShaderUniforms.ExtraContrastLevel = ClientSettings.ExtraContrastLevel;
				ShaderUniforms.SepiaLevel = ClientSettings.SepiaLevel;
				return;
			}
			if (!this.prevDynamicColourGrading)
			{
				this.prevDynamicColourGrading = true;
				ShaderUniforms.ExtraContrastLevel = ClientSettings.ExtraContrastLevel;
				ShaderUniforms.SepiaLevel = ClientSettings.SepiaLevel;
			}
			dt = Math.Min(0.2f, dt);
			BlockPos plrPos = this.game.player.Entity.Pos.XYZ.AsBlockPos;
			plrPos.Y = this.game.SeaLevel;
			ClimateCondition nowConds = this.game.World.BlockAccessor.GetClimateAt(plrPos, EnumGetClimateMode.NowValues, 0.0);
			if (nowConds == null)
			{
				return;
			}
			if (float.IsNaN(nowConds.Temperature) || float.IsNaN(nowConds.WorldgenRainfall))
			{
				this.game.Logger.Warning("Color grading: Temperature/Rainfall at {0} is {1}/{2}. Will ignore.", new object[] { nowConds.Temperature, nowConds.WorldgenRainfall });
				return;
			}
			float contrastSub = this.game.api.renderapi.ShaderUniforms.GlitchStrength;
			this.targetExtraContrastLevel = GameMath.Clamp((Math.Abs(nowConds.Temperature) / 4f + nowConds.WorldgenRainfall * 20f) / 50f - contrastSub, 0f, 0.4f);
			this.targetSepiaLevel = GameMath.Clamp((nowConds.Temperature - nowConds.WorldgenRainfall * 25f + 5f) / 35f, 0f, 1f) * 0.5f;
			float contrastChange = this.targetExtraContrastLevel - ShaderUniforms.ExtraContrastLevel;
			ShaderUniforms.ExtraContrastLevel += contrastChange * dt;
			float sepiaChange = this.targetSepiaLevel - ShaderUniforms.SepiaLevel;
			ShaderUniforms.SepiaLevel += sepiaChange * dt;
			float extraBloom = Math.Max(0f, nowConds.Temperature / 20f) * Math.Max(0f, nowConds.WorldgenRainfall - 0.4f);
			this.game.api.Render.ShaderUniforms.AmbientBloomLevelAdd[0] = GameMath.Clamp(extraBloom, 0f, 2f);
		}

		private OrderedDictionary<string, AmbientModifier> ambientModifiers;

		private ClientMain game;

		internal float DropShadowIntensity;

		public int ShadowQuality;

		internal AmbientModifier BaseModifier = AmbientModifier.DefaultAmbient;

		internal AmbientModifier Sunglow;

		public bool prevDynamicColourGrading;

		private float targetExtraContrastLevel;

		private float targetSepiaLevel;

		private float smoothedLightLevel = -1f;
	}
}
