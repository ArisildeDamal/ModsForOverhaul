using System;

public class Packet_ServerCalendar
{
	public void SetTotalSeconds(long value)
	{
		this.TotalSeconds = value;
	}

	public string[] GetTimeSpeedModifierNames()
	{
		return this.TimeSpeedModifierNames;
	}

	public void SetTimeSpeedModifierNames(string[] value, int count, int length)
	{
		this.TimeSpeedModifierNames = value;
		this.TimeSpeedModifierNamesCount = count;
		this.TimeSpeedModifierNamesLength = length;
	}

	public void SetTimeSpeedModifierNames(string[] value)
	{
		this.TimeSpeedModifierNames = value;
		this.TimeSpeedModifierNamesCount = value.Length;
		this.TimeSpeedModifierNamesLength = value.Length;
	}

	public int GetTimeSpeedModifierNamesCount()
	{
		return this.TimeSpeedModifierNamesCount;
	}

	public void TimeSpeedModifierNamesAdd(string value)
	{
		if (this.TimeSpeedModifierNamesCount >= this.TimeSpeedModifierNamesLength)
		{
			if ((this.TimeSpeedModifierNamesLength *= 2) == 0)
			{
				this.TimeSpeedModifierNamesLength = 1;
			}
			string[] newArray = new string[this.TimeSpeedModifierNamesLength];
			for (int i = 0; i < this.TimeSpeedModifierNamesCount; i++)
			{
				newArray[i] = this.TimeSpeedModifierNames[i];
			}
			this.TimeSpeedModifierNames = newArray;
		}
		string[] timeSpeedModifierNames = this.TimeSpeedModifierNames;
		int timeSpeedModifierNamesCount = this.TimeSpeedModifierNamesCount;
		this.TimeSpeedModifierNamesCount = timeSpeedModifierNamesCount + 1;
		timeSpeedModifierNames[timeSpeedModifierNamesCount] = value;
	}

	public int[] GetTimeSpeedModifierSpeeds()
	{
		return this.TimeSpeedModifierSpeeds;
	}

	public void SetTimeSpeedModifierSpeeds(int[] value, int count, int length)
	{
		this.TimeSpeedModifierSpeeds = value;
		this.TimeSpeedModifierSpeedsCount = count;
		this.TimeSpeedModifierSpeedsLength = length;
	}

	public void SetTimeSpeedModifierSpeeds(int[] value)
	{
		this.TimeSpeedModifierSpeeds = value;
		this.TimeSpeedModifierSpeedsCount = value.Length;
		this.TimeSpeedModifierSpeedsLength = value.Length;
	}

	public int GetTimeSpeedModifierSpeedsCount()
	{
		return this.TimeSpeedModifierSpeedsCount;
	}

	public void TimeSpeedModifierSpeedsAdd(int value)
	{
		if (this.TimeSpeedModifierSpeedsCount >= this.TimeSpeedModifierSpeedsLength)
		{
			if ((this.TimeSpeedModifierSpeedsLength *= 2) == 0)
			{
				this.TimeSpeedModifierSpeedsLength = 1;
			}
			int[] newArray = new int[this.TimeSpeedModifierSpeedsLength];
			for (int i = 0; i < this.TimeSpeedModifierSpeedsCount; i++)
			{
				newArray[i] = this.TimeSpeedModifierSpeeds[i];
			}
			this.TimeSpeedModifierSpeeds = newArray;
		}
		int[] timeSpeedModifierSpeeds = this.TimeSpeedModifierSpeeds;
		int timeSpeedModifierSpeedsCount = this.TimeSpeedModifierSpeedsCount;
		this.TimeSpeedModifierSpeedsCount = timeSpeedModifierSpeedsCount + 1;
		timeSpeedModifierSpeeds[timeSpeedModifierSpeedsCount] = value;
	}

	public void SetMoonOrbitDays(int value)
	{
		this.MoonOrbitDays = value;
	}

	public void SetHoursPerDay(int value)
	{
		this.HoursPerDay = value;
	}

	public void SetRunning(int value)
	{
		this.Running = value;
	}

	public void SetCalendarSpeedMul(int value)
	{
		this.CalendarSpeedMul = value;
	}

	public void SetDaysPerMonth(int value)
	{
		this.DaysPerMonth = value;
	}

	public void SetTotalSecondsStart(long value)
	{
		this.TotalSecondsStart = value;
	}

	internal void InitializeValues()
	{
	}

	public long TotalSeconds;

	public string[] TimeSpeedModifierNames;

	public int TimeSpeedModifierNamesCount;

	public int TimeSpeedModifierNamesLength;

	public int[] TimeSpeedModifierSpeeds;

	public int TimeSpeedModifierSpeedsCount;

	public int TimeSpeedModifierSpeedsLength;

	public int MoonOrbitDays;

	public int HoursPerDay;

	public int Running;

	public int CalendarSpeedMul;

	public int DaysPerMonth;

	public long TotalSecondsStart;

	public const int TotalSecondsFieldID = 1;

	public const int TimeSpeedModifierNamesFieldID = 2;

	public const int TimeSpeedModifierSpeedsFieldID = 3;

	public const int MoonOrbitDaysFieldID = 4;

	public const int HoursPerDayFieldID = 5;

	public const int RunningFieldID = 6;

	public const int CalendarSpeedMulFieldID = 7;

	public const int DaysPerMonthFieldID = 8;

	public const int TotalSecondsStartFieldID = 9;

	public int size;
}
