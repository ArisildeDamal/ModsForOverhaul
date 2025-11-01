using System;
using System.Drawing;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common
{
	public class ClientGameCalendar : GameCalendar, IClientGameCalendar, IGameCalendar
	{
		public float DayLightStrength
		{
			get
			{
				return this.dayLight;
			}
			set
			{
				this.dayLight = value;
			}
		}

		public float MoonLightStrength { get; set; }

		public float SunLightStrength { get; set; }

		public Vec3f SunColor
		{
			get
			{
				return this.sunColor;
			}
		}

		public Vec3f ReflectColor
		{
			get
			{
				return this.reflectColor;
			}
		}

		public Color SunLightColor
		{
			get
			{
				float sunIntensity = (GameMath.Clamp(this.SunPositionNormalized.Y * 1.5f, -1f, 1f) + 1f) / 2f;
				return base.getSunlightPixelRel(sunIntensity, 0.01f);
			}
		}

		Vec3f IClientGameCalendar.SunPositionNormalized
		{
			get
			{
				return this.SunPositionNormalized;
			}
		}

		Vec3f IClientGameCalendar.SunPosition
		{
			get
			{
				return this.SunPosition;
			}
		}

		Vec3f IClientGameCalendar.MoonPosition
		{
			get
			{
				return this.MoonPosition;
			}
		}

		internal ClientGameCalendar(IClientWorldAccessor cworld, IAsset sunlightTexture, int worldSeed, long totalSecondsStart = 28000L)
			: base(sunlightTexture, worldSeed, totalSecondsStart)
		{
			this.cworld = cworld;
		}

		public override void Update()
		{
			base.Update();
			Vec3d plrpos = this.cworld.Player.Entity.Pos.XYZ;
			this.SunPositionNormalized.Set(base.GetSunPosition(plrpos, base.TotalDays));
			this.SunPosition.Set(this.SunPositionNormalized).Mul(50f);
			Vec3f MoonPositionNormalized = base.GetMoonPosition(plrpos.Z);
			this.MoonPosition.Set(MoonPositionNormalized).Mul(50f);
			float sunIntensity = (GameMath.Clamp(this.SunPositionNormalized.Y * 1.4f + 0.2f, -1f, 1f) + 1f) / 2f;
			float moonYRel = MoonPositionNormalized.Y;
			float moonPhaseBrightness = base.MoonPhaseBrightness;
			float bright = GameMath.Clamp(moonPhaseBrightness * (0.66f + 0.66f * moonYRel), -0.2f, moonPhaseBrightness);
			float sunExtraIntensity = Math.Max(0f, (this.SunPositionNormalized.Y - 0.4f) / 7.5f);
			this.MoonLightStrength = GameMath.Lerp(0f, bright, GameMath.Clamp(moonYRel * 20f, 0f, 1f));
			this.SunLightStrength = (this.sunColor.R + this.sunColor.G + this.sunColor.B) / 3f + sunExtraIntensity;
			this.DayLightStrength = Math.Max(this.MoonLightStrength, this.SunLightStrength);
			float eclipseDarkening = GameMath.Clamp((this.SunPositionNormalized.Dot(MoonPositionNormalized) - 0.99955f) * 2500f, 0f, this.DayLightStrength * 0.6f);
			this.DayLightStrength = Math.Max(0f, this.DayLightStrength - eclipseDarkening);
			this.DayLightStrength = Math.Min(1.5f, this.DayLightStrength + (float)Math.Max(0.0, (plrpos.Y - (double)this.cworld.SeaLevel - 1000.0) / 30000.0));
			double transitionDays = base.TotalDays + 0.020833333333333332;
			float targetSunsetMod = GameMath.Clamp(((float)this.sunsetModNoise.Noise(0.0, (double)((int)transitionDays)) - 0.65f) / 1.8f, -0.1f, 0.3f);
			float dt = GameMath.Clamp((float)((transitionDays - this.transitionDaysLast) * 6.0), 4.1666666E-05f, 1f);
			this.transitionDaysLast = transitionDays;
			base.SunsetMod += (targetSunsetMod - base.SunsetMod) * dt;
			Color colSun = base.getSunlightPixelRel(GameMath.Clamp(sunIntensity + base.SunsetMod, 0f, 1f), 0.01f);
			this.sunColor.Set((float)colSun.R / 255f, (float)colSun.G / 255f, (float)colSun.B / 255f);
			Color colRefle = base.getSunlightPixelRel(GameMath.Clamp(sunIntensity - base.SunsetMod, 0f, 1f), 0.01f);
			this.reflectColor.Set((float)colRefle.R / 255f, (float)colRefle.G / 255f, (float)colRefle.B / 255f);
			if (this.SunPosition.Y < -0.1f)
			{
				float darkness = -this.SunPosition.Y / 10f - 0.3f;
				this.reflectColor.R = Math.Max(this.reflectColor.R - darkness, ClientGameCalendar.nightColor[0]);
				this.reflectColor.G = Math.Max(this.reflectColor.G - darkness, ClientGameCalendar.nightColor[1]);
				this.reflectColor.B = Math.Max(this.reflectColor.B - darkness, ClientGameCalendar.nightColor[2]);
			}
		}

		public const long ClientCalendarStartingSeconds = 28000L;

		public Vec3f SunPosition = new Vec3f();

		public Vec3f MoonPosition = new Vec3f();

		public Vec3f SunPositionNormalized = new Vec3f();

		internal float dayLight;

		private Vec3f sunColor = new Vec3f();

		private Vec3f reflectColor = new Vec3f();

		private IClientWorldAccessor cworld;

		public static Vec3f nightColor = new Vec3f(0f, 0.0627451f, 0.13333334f);

		private double transitionDaysLast;
	}
}
