using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common
{
	public class GameCalendar : IGameCalendar
	{
		public Dictionary<string, float> TimeSpeedModifiers
		{
			get
			{
				return this.timeSpeedModifiers;
			}
			set
			{
				this.timeSpeedModifiers = value;
				this.CalculateCurrentTimeSpeed();
			}
		}

		public float SpeedOfTime
		{
			get
			{
				return this.currentSpeedOfTime;
			}
		}

		public float CalendarSpeedMul
		{
			get
			{
				return this.calendarSpeedMul;
			}
			set
			{
				this.calendarSpeedMul = value;
				this.CalculateCurrentTimeSpeed();
			}
		}

		public float Timelapse
		{
			get
			{
				return this.timelapse;
			}
			set
			{
				this.timelapse = value;
			}
		}

		public int DaysPerMonth { get; set; } = 9;

		public int DaysPerYear
		{
			get
			{
				return this.DaysPerMonth * 12;
			}
		}

		public int DayOfMonth
		{
			get
			{
				return (int)(this.TotalDays % (double)this.DaysPerMonth) + 1;
			}
		}

		public int MonthsPerYear
		{
			get
			{
				return this.DaysPerYear / this.DaysPerMonth;
			}
		}

		public int FullHourOfDay
		{
			get
			{
				return (int)(this.timespan.TotalHours % (double)this.HoursPerDay);
			}
		}

		public float HourOfDay
		{
			get
			{
				return (float)(this.timespan.TotalHours % (double)this.HoursPerDay);
			}
		}

		public long ElapsedSeconds
		{
			get
			{
				return (long)(this.timespan.TotalSeconds - (double)this.totalSecondsStart);
			}
		}

		public double ElapsedHours
		{
			get
			{
				return (double)this.ElapsedSeconds / 60.0 / 60.0;
			}
		}

		public double ElapsedDays
		{
			get
			{
				return this.ElapsedHours / (double)this.HoursPerDay;
			}
		}

		public long TotalSeconds
		{
			get
			{
				return (long)this.timespan.TotalSeconds;
			}
		}

		public double TotalHours
		{
			get
			{
				return this.timespan.TotalHours;
			}
		}

		public double TotalDays
		{
			get
			{
				return this.timespan.TotalHours / (double)this.HoursPerDay + (double)this.timelapse;
			}
		}

		public int DayOfYear
		{
			get
			{
				return (int)(this.TotalDays % (double)this.DaysPerYear);
			}
		}

		public float DayOfYearf
		{
			get
			{
				return (float)(this.TotalDays % (double)this.DaysPerYear);
			}
		}

		public int Year
		{
			get
			{
				return (int)(this.TotalDays / (double)this.DaysPerYear);
			}
		}

		public int Month
		{
			get
			{
				return (int)Math.Ceiling((double)(this.YearRel * (float)this.MonthsPerYear));
			}
		}

		public float Monthf
		{
			get
			{
				return this.YearRel * (float)this.MonthsPerYear;
			}
		}

		public EnumMonth MonthName
		{
			get
			{
				return (EnumMonth)this.Month;
			}
		}

		public float YearRel
		{
			get
			{
				return (float)(GameMath.Mod(this.TotalDays, (double)this.DaysPerYear) / (double)this.DaysPerYear);
			}
		}

		int IGameCalendar.DaysPerYear
		{
			get
			{
				return this.DaysPerYear;
			}
		}

		float IGameCalendar.HoursPerDay
		{
			get
			{
				return this.HoursPerDay;
			}
		}

		public EnumMoonPhase MoonPhase
		{
			get
			{
				return (EnumMoonPhase)((int)this.MoonPhaseExact % this.MoonOrbitDays);
			}
		}

		public double MoonPhaseExact
		{
			get
			{
				return this.moonPhaseCached;
			}
		}

		public bool Dusk
		{
			get
			{
				return (double)(this.HourOfDay / this.HoursPerDay) > 0.5;
			}
		}

		public float MoonPhaseBrightness
		{
			get
			{
				double ph = this.MoonPhaseExact;
				float i = (float)ph - (float)((int)ph);
				float num = GameCalendar.MoonBrightnesByPhase[(int)ph];
				float b = GameCalendar.MoonBrightnesByPhase[(int)(ph + 1.0) % this.MoonOrbitDays];
				return num * (1f - i) + b * i;
			}
		}

		public float MoonSize
		{
			get
			{
				double ph = this.MoonPhaseExact;
				float i = (float)ph - (float)((int)ph);
				float num = GameCalendar.MoonSizeByPhase[(int)ph];
				float b = GameCalendar.MoonSizeByPhase[(int)(ph + 1.0) % this.MoonOrbitDays];
				return (num * (1f - i) + b * i) * this.superMoonSize;
			}
		}

		public float SunsetMod { get; protected set; }

		public bool IsRunning
		{
			get
			{
				return this.watchIngameTime.IsRunning;
			}
		}

		public SolarSphericalCoordsDelegate OnGetSolarSphericalCoords { get; set; }

		public HemisphereDelegate OnGetHemisphere { get; set; }

		public GetLatitudeDelegate OnGetLatitude { get; set; }

		public float? SeasonOverride { get; set; }

		public GameCalendar(IAsset sunlightTexture, int worldSeed, long totalSecondsStart = 4176000L)
		{
			this.OnGetSolarSphericalCoords = (double posX, double posZ, float yearRel, float dayRel) => new SolarSphericalCoords(6.2831855f * GameMath.Mod(this.HourOfDay / this.HoursPerDay, 1f) - 3.1415927f, 0f);
			this.OnGetLatitude = (double posZ) => 0.5;
			this.watchIngameTime = new Stopwatch();
			this.totalSecondsStart = totalSecondsStart;
			this.timespan = TimeSpan.FromSeconds((double)totalSecondsStart);
			this.timeSpeedModifiers["baseline"] = 60f;
			BitmapRef bmp = this.BitmapCreateFromPng(sunlightTexture);
			this.sunLightTexture = bmp.Pixels;
			this.sunLightTextureSize = new Size2i(bmp.Width, bmp.Height);
			bmp.Dispose();
			this.sunsetModNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 1.0, 0.9, (long)worldSeed);
			this.superMoonNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 1.0, 0.9, (long)(worldSeed + 12098));
		}

		public double GetMoonPhase(double totaldays)
		{
			double raSun = this.GetSunAscension(totaldays);
			double num;
			return ((double)(GameMath.Mod((float)(this.GetMoonAscensionDeclination(totaldays, out num) - raSun) / 6.2831855f, 1f) * (float)this.MoonOrbitDays) + 0.5) % (double)this.MoonOrbitDays;
		}

		public double GetSunAscension(double totaldays)
		{
			double yearRel = GameMath.Mod(totaldays, (double)this.DaysPerYear) / (double)this.DaysPerYear;
			double dayRel = GameMath.Mod(totaldays, 1.0);
			return (GameMath.Mod(totaldays - yearRel, 1.0) - (dayRel - 0.5)) * 6.2831854820251465;
		}

		public SolarSphericalCoords GetCelestialAngles(double x, double z, double totalDays)
		{
			float yearRel = (float)(GameMath.Mod(totalDays, (double)this.DaysPerYear) / (double)this.DaysPerYear);
			float dayRel = (float)GameMath.Mod(totalDays, 1.0);
			return this.OnGetSolarSphericalCoords(x, z, yearRel, dayRel);
		}

		public Vec3f GetSunPosition(Vec3d pos, double totalDays)
		{
			SolarSphericalCoords celestialAngles = this.GetCelestialAngles(pos.X, pos.Z, totalDays);
			float phi = celestialAngles.AzimuthAngle;
			float theta = celestialAngles.ZenithAngle;
			float sinTheta = GameMath.Sin(theta);
			return new Vec3f(sinTheta * GameMath.Sin(phi), GameMath.Cos(theta), sinTheta * GameMath.Cos(phi));
		}

		public Vec3f GetMoonPosition(Vec3d position, double totalDays)
		{
			return this.GetMoonPosition(position.Z, totalDays);
		}

		protected Vec3f GetMoonPosition(double posZ, double totalDays)
		{
			double sinh;
			double az = this.GetMoonCelestialAngles(posZ, totalDays, out sinh);
			double cosh = Math.Sqrt(1.0 - sinh * sinh);
			return new Vec3f((float)(cosh * Math.Sin(az)), (float)sinh, (float)(cosh * Math.Cos(az)));
		}

		protected Vec3f GetMoonPosition(double posZ)
		{
			double sinh;
			double az = this.GetMoonCelestialAnglesFromCache(posZ, out sinh);
			double cosh = Math.Sqrt(1.0 - sinh * sinh);
			return new Vec3f((float)(cosh * Math.Sin(az)), (float)sinh, (float)(cosh * Math.Cos(az)));
		}

		private double GetMoonAscensionDeclination(double totalDays, out double sinDelta)
		{
			double T = 1386.0 + (totalDays - 3.0) / (double)this.DaysPerYear;
			double num = GameMath.Mod(218.3164477 + 4812.6788123421 * T, 360.0);
			double M = GameMath.Mod(134.9634114 + 4771.988675605 * T, 360.0) * 0.017453292519943295;
			double F = GameMath.Mod(93.2720906 + 4832.020175273 * T, 360.0) * 0.017453292519943295;
			double D = (297.8501921 + 4452.671114034 * T) * 0.017453292519943295;
			double eL = num + 6.289 * Math.Sin(M) - 1.274 * Math.Sin(M - 2.0 * D) + 0.658 * Math.Sin(2.0 * D);
			double num2 = 5.128 * Math.Sin(F) + 0.28 * Math.Sin(F + M) - 0.28 * Math.Sin(F - M);
			eL *= 0.017453292519943295;
			double num3 = num2 * 0.017453292519943295;
			double cosB = Math.Cos(num3);
			double sinB = Math.Sin(num3);
			double CosBSinL = cosB * Math.Sin(eL);
			double X = cosB * Math.Cos(eL);
			double num4 = 0.9174771 * CosBSinL - 0.3977885 * sinB;
			sinDelta = 0.3977885 * CosBSinL + 0.9174771 * sinB;
			return Math.Atan2(num4, X);
		}

		public double GetMoonCelestialAngles(double z, double totalDays, out double sinh)
		{
			double sinDelta;
			double RA = this.GetMoonAscensionDeclination(totalDays, out sinDelta);
			double cosDelta = Math.Sqrt(1.0 - sinDelta * sinDelta);
			double num = GameMath.Mod(totalDays - GameMath.Mod(totalDays, (double)this.DaysPerYear) / (double)this.DaysPerYear, 1.0) * 6.2831854820251465 - RA;
			double num2 = this.OnGetLatitude(z) * 1.5707963705062866;
			double sinLat = Math.Sin(num2);
			double cosLat = Math.Cos(num2);
			double cosTau = Math.Cos(num);
			sinh = sinLat * sinDelta + cosLat * cosDelta * cosTau;
			return -Math.Atan2(Math.Sin(num), sinLat * cosTau - cosLat * sinDelta / cosDelta);
		}

		protected double GetMoonCelestialAnglesFromCache(double z, out double sinh)
		{
			double sinDelta = this.moonSinDeltaCached;
			double cosDelta = Math.Sqrt(1.0 - sinDelta * sinDelta);
			double num = this.moonTauCached;
			double num2 = this.OnGetLatitude(z) * 1.5707963705062866;
			double sinLat = Math.Sin(num2);
			double cosLat = Math.Cos(num2);
			double cosTau = Math.Cos(num);
			sinh = sinLat * sinDelta + cosLat * cosDelta * cosTau;
			return -Math.Atan2(Math.Sin(num), sinLat * cosTau - cosLat * sinDelta / cosDelta);
		}

		protected void CacheMoonCelestialAngles()
		{
			double totalDays = this.TotalDays;
			double sinDelta;
			double RA = this.GetMoonAscensionDeclination(totalDays, out sinDelta);
			this.moonTauCached = GameMath.Mod(totalDays - GameMath.Mod(totalDays, (double)this.DaysPerYear) / (double)this.DaysPerYear, 1.0) * 6.2831854820251465 - RA;
			this.moonSinDeltaCached = sinDelta;
		}

		public float RealSecondsToGameSeconds(float seconds)
		{
			return seconds * this.currentSpeedOfTime * this.CalendarSpeedMul;
		}

		public void SetSeasonOverride(float? seasonRel)
		{
			this.SeasonOverride = seasonRel;
		}

		public void SetTimeSpeedModifier(string name, float speed)
		{
			this.timeSpeedModifiers[name] = speed;
			this.CalculateCurrentTimeSpeed();
		}

		public void RemoveTimeSpeedModifier(string name)
		{
			this.timeSpeedModifiers.Remove(name);
			this.CalculateCurrentTimeSpeed();
		}

		private void CalculateCurrentTimeSpeed()
		{
			float totalSpeed = 0f;
			foreach (float speed in this.timeSpeedModifiers.Values)
			{
				totalSpeed += speed;
			}
			this.currentSpeedOfTime = totalSpeed;
			this.DayLengthInRealLifeSeconds = ((this.currentSpeedOfTime == 0f) ? float.MaxValue : (3600f * this.HoursPerDay / this.currentSpeedOfTime / this.CalendarSpeedMul));
		}

		public void SetTotalSeconds(long totalSecondsNow, long totalSecondsStart)
		{
			this.timespan = TimeSpan.FromSeconds((double)totalSecondsNow);
			this.totalSecondsStart = totalSecondsStart;
		}

		public void Start()
		{
			if (!this.watchIngameTime.IsRunning)
			{
				this.watchIngameTime.Start();
			}
		}

		public void Stop()
		{
			if (this.watchIngameTime.IsRunning)
			{
				this.watchIngameTime.Stop();
			}
		}

		public virtual void Tick()
		{
			if (!this.watchIngameTime.IsRunning)
			{
				return;
			}
			double elapsedGameSeconds = (double)this.watchIngameTime.ElapsedMilliseconds / 1000.0 * (double)this.SpeedOfTime * (double)this.CalendarSpeedMul;
			this.timespan += TimeSpan.FromSeconds(elapsedGameSeconds);
			this.watchIngameTime.Restart();
			this.Update();
		}

		public virtual void Update()
		{
			float sub = Math.Max(0f, 1.15f - this.MoonSize);
			double noiseval = this.superMoonNoise.Noise(0.0, this.TotalDays / 8.0);
			this.superMoonSize = (float)GameMath.Clamp((noiseval - 0.74 - (double)sub) * 50.0, 1.0, 2.5);
			this.moonPhaseCached = this.GetMoonPhase(this.TotalDays);
			this.CacheMoonCelestialAngles();
		}

		public void SetDayTime(float wantHourOfDay)
		{
			float hoursToAdd;
			if (this.HourOfDay > wantHourOfDay)
			{
				hoursToAdd = wantHourOfDay + (this.HoursPerDay - this.HourOfDay);
			}
			else
			{
				hoursToAdd = wantHourOfDay - this.HourOfDay;
			}
			this.Add(hoursToAdd);
		}

		public void SetMonth(float month)
		{
			float hoursToAdd;
			if (this.Monthf > month)
			{
				hoursToAdd = (month + ((float)this.MonthsPerYear - this.Monthf)) * this.HoursPerDay * (float)this.DaysPerMonth + 12f;
			}
			else
			{
				hoursToAdd = (month - this.Monthf) * this.HoursPerDay * (float)this.DaysPerMonth + 12f;
			}
			this.Add(hoursToAdd);
		}

		public void Add(float hours)
		{
			TimeSpan toadd = TimeSpan.FromHours((double)hours);
			this.timespan = this.timespan.Add(toadd);
		}

		public float GetDayLightStrength(double x, double z)
		{
			double totalDays = this.TotalDays;
			SolarSphericalCoords celestialAngles = this.GetCelestialAngles(x, z, totalDays);
			float phi = celestialAngles.AzimuthAngle;
			float theta = celestialAngles.ZenithAngle;
			float sinTheta = GameMath.Sin(theta);
			Vec3f sunPos = new Vec3f(sinTheta * GameMath.Sin(phi), GameMath.Cos(theta), sinTheta * GameMath.Cos(phi));
			Vec3f moonPos = this.GetMoonPosition(z);
			float moonPhaseBrightness = this.MoonPhaseBrightness;
			float sunIntensity = (GameMath.Clamp(sunPos.Y * 1.4f + 0.2f, -1f, 1f) + 1f) / 2f;
			float moonYRel = moonPos.Y;
			float moonBright = GameMath.Clamp(moonPhaseBrightness * (0.66f + 0.33f * moonYRel), -0.2f, moonPhaseBrightness);
			float eclipseDarkening = GameMath.Clamp((sunPos.Dot(moonPos) - 0.99955f) * 2500f, 0f, sunIntensity * 0.6f);
			sunIntensity = Math.Max(0f, sunIntensity - eclipseDarkening);
			moonBright = Math.Max(0f, moonBright - eclipseDarkening);
			float num = GameMath.Lerp(0f, moonBright, GameMath.Clamp(moonYRel * 20f, 0f, 1f));
			float sunExtraIntensity = Math.Max(0f, (sunPos.Y - 0.4f) / 7.5f);
			Color colSun = this.getSunlightPixelRel(GameMath.Clamp(sunIntensity + this.SunsetMod, 0f, 1f), 0.01f);
			return Math.Max(num, (float)(colSun.R + colSun.G + colSun.B) / 3f / 255f + sunExtraIntensity);
		}

		public float GetDayLightStrength(BlockPos pos)
		{
			return this.GetDayLightStrength((double)pos.X, (double)pos.Z);
		}

		public EnumSeason GetSeason(BlockPos pos)
		{
			float val = GameMath.Mod(this.GetSeasonRel(pos) - 0.21916668f, 1f);
			return (EnumSeason)(4f * val);
		}

		public float GetSeasonRel(BlockPos pos)
		{
			if (this.SeasonOverride != null)
			{
				return this.SeasonOverride.Value;
			}
			if (this.GetHemisphere(pos) != EnumHemisphere.North)
			{
				return (this.YearRel + 0.5f) % 1f;
			}
			return this.YearRel;
		}

		public EnumHemisphere GetHemisphere(BlockPos pos)
		{
			if (this.OnGetHemisphere != null)
			{
				return this.OnGetHemisphere((double)pos.X, (double)pos.Z);
			}
			return EnumHemisphere.North;
		}

		public Color getSunlightPixelRel(float relx, float rely)
		{
			float num = Math.Min((float)(this.sunLightTextureSize.Width - 1), relx * (float)this.sunLightTextureSize.Width);
			int x = (int)num;
			int y = (int)Math.Min((float)(this.sunLightTextureSize.Height - 1), rely * (float)this.sunLightTextureSize.Height);
			float num2 = num - (float)x;
			int col = this.sunLightTexture[y * this.sunLightTextureSize.Width + x];
			int col2 = this.sunLightTexture[y * this.sunLightTextureSize.Width + Math.Min(this.sunLightTextureSize.Width - 1, x + 1)];
			return Color.FromArgb(GameMath.LerpRgbaColor(num2, col, col2));
		}

		public Packet_Server ToPacket()
		{
			string[] names = new string[this.timeSpeedModifiers.Count];
			int[] speeds = new int[this.timeSpeedModifiers.Count];
			int i = 0;
			foreach (KeyValuePair<string, float> val in this.timeSpeedModifiers)
			{
				names[i] = val.Key;
				speeds[i] = CollectibleNet.SerializeFloatPrecise(val.Value);
				i++;
			}
			Packet_ServerCalendar p = new Packet_ServerCalendar
			{
				TotalSeconds = (long)this.timespan.TotalSeconds,
				TotalSecondsStart = this.totalSecondsStart,
				MoonOrbitDays = this.MoonOrbitDays,
				DaysPerMonth = this.DaysPerMonth,
				HoursPerDay = CollectibleNet.SerializeFloatVeryPrecise(this.HoursPerDay),
				CalendarSpeedMul = CollectibleNet.SerializeFloatVeryPrecise(this.calendarSpeedMul)
			};
			p.SetTimeSpeedModifierNames(names);
			p.SetTimeSpeedModifierSpeeds(speeds);
			p.Running = ((this.watchIngameTime.IsRunning > false) ? 1 : 0);
			return new Packet_Server
			{
				Id = 13,
				Calendar = p
			};
		}

		public string PrettyDate()
		{
			float hourOfDay = this.HourOfDay;
			int hour = (int)hourOfDay;
			int minute = (int)((hourOfDay - (float)hour) * 60f);
			return Lang.Get("dateformat", new object[]
			{
				this.DayOfMonth,
				Lang.Get("month-" + this.MonthName.ToString(), Array.Empty<object>()),
				this.Year.ToString("0"),
				hour.ToString("00"),
				minute.ToString("00")
			});
		}

		public void UpdateFromPacket(Packet_Server packet)
		{
			Packet_ServerCalendar calpacket = packet.Calendar;
			this.SetTotalSeconds(calpacket.TotalSeconds, calpacket.TotalSecondsStart);
			this.timeSpeedModifiers.Clear();
			for (int i = 0; i < calpacket.TimeSpeedModifierNamesCount; i++)
			{
				this.timeSpeedModifiers[calpacket.TimeSpeedModifierNames[i]] = CollectibleNet.DeserializeFloatPrecise(calpacket.TimeSpeedModifierSpeeds[i]);
			}
			this.MoonOrbitDays = calpacket.MoonOrbitDays;
			this.HoursPerDay = CollectibleNet.DeserializeFloatVeryPrecise(calpacket.HoursPerDay);
			this.calendarSpeedMul = CollectibleNet.DeserializeFloatVeryPrecise(calpacket.CalendarSpeedMul);
			this.DaysPerMonth = calpacket.DaysPerMonth;
			if (this.HoursPerDay == 0f)
			{
				throw new ArgumentException("Trying to set 0 hours per day.");
			}
			if (calpacket.Running > 0)
			{
				if (!this.watchIngameTime.IsRunning)
				{
					this.watchIngameTime.Start();
				}
			}
			else if (this.watchIngameTime.IsRunning)
			{
				this.watchIngameTime.Stop();
			}
			this.CalculateCurrentTimeSpeed();
		}

		public BitmapRef BitmapCreateFromPng(IAsset asset)
		{
			return new BitmapExternal(new MemoryStream(asset.Data), null);
		}

		protected float currentSpeedOfTime = 60f;

		public float HoursPerDay = 24f;

		public int MoonOrbitDays = 8;

		public float DayLengthInRealLifeSeconds;

		internal Stopwatch watchIngameTime;

		protected TimeSpan timespan = TimeSpan.Zero;

		protected long totalSecondsStart;

		public Size2i sunLightTextureSize;

		public int[] sunLightTexture;

		protected Dictionary<string, float> timeSpeedModifiers = new Dictionary<string, float>();

		protected NormalizedSimplexNoise sunsetModNoise;

		protected NormalizedSimplexNoise superMoonNoise;

		protected float superMoonSize;

		protected double moonPhaseCached;

		private double moonTauCached;

		private double moonSinDeltaCached;

		protected float timelapse;

		private float calendarSpeedMul = 0.5f;

		public static float[] MoonBrightnesByPhase = new float[] { -0.1f, 0.1f, 0.2f, 0.26f, 0.33f, 0.26f, 0.2f, 0.1f };

		public static float[] MoonSizeByPhase = new float[] { 0.8f, 0.9f, 1f, 1.1f, 1.2f, 1.1f, 1f, 0.9f };
	}
}
