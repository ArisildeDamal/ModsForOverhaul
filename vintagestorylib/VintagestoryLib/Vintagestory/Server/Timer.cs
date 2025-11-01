using System;

namespace Vintagestory.Server
{
	public class Timer
	{
		public double Interval
		{
			get
			{
				return this.interval;
			}
			set
			{
				this.interval = value;
			}
		}

		public double MaxDeltaTime
		{
			get
			{
				return this.maxDeltaTime;
			}
			set
			{
				this.maxDeltaTime = value;
			}
		}

		public Timer()
		{
			this.Reset();
		}

		public void Reset()
		{
			this.starttime = Timer.Gettime();
		}

		public void Update(Timer.Tick tick)
		{
			double currenttime = Timer.Gettime() - this.starttime;
			double deltaTime = currenttime - this.oldtime;
			this.Accumulator += deltaTime;
			double dt = this.Interval;
			if (this.MaxDeltaTime != double.PositiveInfinity && this.Accumulator > this.MaxDeltaTime)
			{
				this.Accumulator = this.MaxDeltaTime;
			}
			while (this.Accumulator >= dt)
			{
				tick();
				this.Accumulator -= dt;
			}
			this.oldtime = currenttime;
		}

		private static double Gettime()
		{
			return (double)DateTime.UtcNow.Ticks / 10000000.0;
		}

		private double interval = 1.0;

		private double maxDeltaTime = double.PositiveInfinity;

		private double starttime;

		private double oldtime;

		public double Accumulator;

		public delegate void Tick();
	}
}
