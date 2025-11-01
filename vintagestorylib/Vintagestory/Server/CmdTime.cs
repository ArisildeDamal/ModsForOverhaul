using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server
{
	internal class CmdTime
	{
		public CmdTime(ServerMain server)
		{
			this.server = server;
			IChatCommandApi cmdapi = server.api.ChatCommands;
			CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
			cmdapi.Create("time").RequiresPrivilege(Privilege.time).HandleWith(new OnCommandDelegate(this.cmdGetTime))
				.WithDescription("Get or set world time or time speed")
				.BeginSub("stop")
				.WithDesc("Stop passage of time and time affected processes")
				.HandleWith(new OnCommandDelegate(this.cmdStopTime))
				.EndSub()
				.BeginSub("resume")
				.WithDesc("Resume passage of time and time affected processes")
				.HandleWith(new OnCommandDelegate(this.cmdResumeTime))
				.EndSub()
				.BeginSub("speed")
				.WithDesc("Get/Set speed of time passage. Not recommended for normal gameplay! If you want longer days, use /time calendarspeedmul")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalFloat("speed", 60f) })
				.HandleWith(new OnCommandDelegate(this.cmdTimeSpeed))
				.EndSub()
				.BeginSub("set")
				.WithDesc("Fast forward to a time of day")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("24 hour format or word", new string[]
				{
					"lunch", "day", "night", "latenight", "morning", "latemorning", "sunrise", "sunset", "afternoon", "midnight",
					"witchinghour"
				}) })
				.HandleWith(new OnCommandDelegate(this.cmdTimeSet))
				.EndSub()
				.BeginSub("setmonth")
				.WithDesc("Fast forward to a given month")
				.WithArgs(new ICommandArgumentParser[] { parsers.WordRange("month", new string[]
				{
					"jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct",
					"nov", "dec"
				}) })
				.HandleWith(new OnCommandDelegate(this.cmdTimeSetMonth))
				.EndSub()
				.BeginSub("add")
				.WithDesc("Fast forward by given time span")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Float("amount"),
					parsers.OptionalWordRange("span", new string[] { "minute", "minutes", "hour", "hours", "day", "days", "month", "months", "year", "years" })
				})
				.HandleWith(new OnCommandDelegate(this.cmdTimeAdd))
				.EndSub()
				.BeginSub("calendarspeedmul")
				.WithAlias(new string[] { "csm" })
				.WithDesc("Determines the relationship between in-game time and real-world time. A value of 1 means one in-game minute is 1 real world second. A value of 0.5 means one in-game minute is 2 real world second")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalFloat("value", 0.5f) })
				.HandleWith(new OnCommandDelegate(this.cmdCalendarSpeedMul))
				.EndSub()
				.BeginSub("hoursperday")
				.WithDesc("Determines how many hours a day has.")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalFloat("value", 24f) })
				.HandleWith(new OnCommandDelegate(this.cmdHoursPerDay))
				.EndSub();
			cmdapi.GetOrCreate("debug").BeginSub("time").BeginSub("nexteclipse")
				.HandleWith(new OnCommandDelegate(this.handleCmdNextEclipse))
				.EndSub()
				.EndSub();
		}

		private TextCommandResult handleCmdNextEclipse(TextCommandCallingArgs args)
		{
			Vec3d pos = args.Caller.Pos;
			double peakDotResult = -1.0;
			double lastEclipseTotalDays = -99.0;
			StringBuilder sb = new StringBuilder();
			double timeNow = this.server.GameWorldCalendar.TotalDays;
			for (double daysdelta = 0.0; daysdelta <= 500.0; daysdelta += 0.008333333333333333)
			{
				double totalDays = timeNow + daysdelta;
				Vec3f vec3f = this.server.GameWorldCalendar.GetSunPosition(pos, totalDays).Normalize();
				Vec3f moonPos = this.server.GameWorldCalendar.GetMoonPosition(pos, totalDays);
				float hereDotResult = vec3f.Dot(moonPos);
				if ((double)hereDotResult < peakDotResult)
				{
					if (peakDotResult > 0.9996 && totalDays - lastEclipseTotalDays > 1.0)
					{
						double hourOfDay = (totalDays - 0.008333333333333333) % 1.0 * (double)this.server.GameWorldCalendar.HoursPerDay;
						if (hourOfDay > 6.0 && hourOfDay < 17.0)
						{
							sb.AppendLine(string.Format("Eclipse will happen in {0:0} days, will get within {1:0.#} degrees of the sun, at {2:00}:{3:00}.", new object[]
							{
								daysdelta - 0.08333333333333333,
								Math.Acos(peakDotResult) * 57.2957763671875,
								(int)hourOfDay,
								(hourOfDay - (double)((int)hourOfDay)) * 60.0
							}));
							lastEclipseTotalDays = totalDays;
							peakDotResult = 0.0;
						}
					}
				}
				else
				{
					peakDotResult = (double)hereDotResult;
				}
			}
			if (sb.Length > 0)
			{
				return TextCommandResult.Success(sb.ToString(), null);
			}
			return TextCommandResult.Success("No eclipse found in next 500 days", null);
		}

		private TextCommandResult cmdGetTime(TextCommandCallingArgs args)
		{
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-gettime", new object[]
			{
				this.server.GameWorldCalendar.PrettyDate(),
				Math.Round((double)(this.server.GameWorldCalendar.DayLengthInRealLifeSeconds / 60f), 1)
			}), null);
		}

		private TextCommandResult cmdHoursPerDay(TextCommandCallingArgs args)
		{
			if (args.Parsers[0].IsMissing)
			{
				return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-hoursperday", new object[] { this.server.GameWorldCalendar.HoursPerDay }), null);
			}
			float hpd = (float)args[0];
			if ((double)hpd < 0.1)
			{
				return TextCommandResult.Error("Cannot be less than 0.1", "");
			}
			this.server.GameWorldCalendar.HoursPerDay = hpd;
			this.resendTimePacket();
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-hoursperdayset", new object[] { hpd }), null);
		}

		private TextCommandResult cmdCalendarSpeedMul(TextCommandCallingArgs args)
		{
			if (args.Parsers[0].IsMissing)
			{
				return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-calendarspeedmul", new object[] { this.server.GameWorldCalendar.CalendarSpeedMul }), null);
			}
			this.server.GameWorldCalendar.CalendarSpeedMul = (float)args[0];
			this.resendTimePacket();
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-calendarspeedmulset", new object[]
			{
				this.server.GameWorldCalendar.CalendarSpeedMul,
				Math.Round((double)(this.server.GameWorldCalendar.DayLengthInRealLifeSeconds / 60f), 1)
			}), null);
		}

		private TextCommandResult cmdTimeAdd(TextCommandCallingArgs args)
		{
			float amount = (float)args[0];
			string type = (string)args[1];
			if (amount < 0f)
			{
				return TextCommandResult.Error("Only positive values are allowed", "");
			}
			if (args.Parsers[1].IsMissing)
			{
				type = "hour";
			}
			if (type.Last<char>().Equals('s'))
			{
				type = type.Substring(0, type.Length - 1);
			}
			if (!(type == "minute"))
			{
				if (!(type == "hour"))
				{
					if (!(type == "day"))
					{
						if (!(type == "month"))
						{
							if (!(type == "year"))
							{
								return TextCommandResult.Error("Invalid time span type", "");
							}
							this.server.GameWorldCalendar.Add(amount * this.server.GameWorldCalendar.HoursPerDay * (float)this.server.GameWorldCalendar.DaysPerYear);
						}
						else
						{
							this.server.GameWorldCalendar.Add(amount * this.server.GameWorldCalendar.HoursPerDay * (float)this.server.GameWorldCalendar.DaysPerMonth);
						}
					}
					else
					{
						this.server.GameWorldCalendar.Add(amount * this.server.GameWorldCalendar.HoursPerDay);
					}
				}
				else
				{
					this.server.GameWorldCalendar.Add(amount);
				}
			}
			else
			{
				this.server.GameWorldCalendar.Add(amount / 60f);
			}
			this.resendTimePacket();
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-timeadd-" + type, new object[]
			{
				amount,
				this.server.GameWorldCalendar.PrettyDate()
			}), null);
		}

		private TextCommandResult cmdTimeSetMonth(TextCommandCallingArgs args)
		{
			int month = args.Parsers[0].GetValidRange(args.RawArgs).IndexOf((string)args[0]);
			this.server.GameWorldCalendar.SetMonth((float)month);
			this.resendTimePacket();
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-timeset", new object[] { this.server.GameWorldCalendar.PrettyDate() }), null);
		}

		private TextCommandResult cmdTimeSet(TextCommandCallingArgs args)
		{
			float? hour = null;
			string strValue = (string)args[0];
			if (strValue != null)
			{
				switch (strValue.Length)
				{
				case 3:
					if (strValue == "day")
					{
						hour = new float?(12f);
					}
					break;
				case 5:
				{
					char c = strValue[0];
					if (c != 'l')
					{
						if (c == 'n')
						{
							if (strValue == "night")
							{
								hour = new float?(20f);
							}
						}
					}
					else if (strValue == "lunch")
					{
						hour = new float?(12f);
					}
					break;
				}
				case 6:
					if (strValue == "sunset")
					{
						hour = new float?(17.5f);
					}
					break;
				case 7:
				{
					char c = strValue[0];
					if (c != 'm')
					{
						if (c == 's')
						{
							if (strValue == "sunrise")
							{
								hour = new float?(6.5f);
							}
						}
					}
					else if (strValue == "morning")
					{
						hour = new float?(8f);
					}
					break;
				}
				case 8:
					if (strValue == "midnight")
					{
						hour = new float?(0f);
					}
					break;
				case 9:
				{
					char c = strValue[0];
					if (c != 'a')
					{
						if (c == 'l')
						{
							if (strValue == "latenight")
							{
								hour = new float?(22f);
							}
						}
					}
					else if (strValue == "afternoon")
					{
						hour = new float?(14f);
					}
					break;
				}
				case 11:
					if (strValue == "latemorning")
					{
						hour = new float?(10f);
					}
					break;
				case 12:
					if (strValue == "witchinghour")
					{
						hour = new float?(3f);
					}
					break;
				}
			}
			if (hour != null)
			{
				this.server.GameWorldCalendar.SetDayTime(hour.Value / 24f * this.server.GameWorldCalendar.HoursPerDay);
				this.resendTimePacket();
				return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-timeset", new object[] { this.server.GameWorldCalendar.PrettyDate() }), null);
			}
			float hours;
			if (!this.ParseTimeSpan(strValue, out hours))
			{
				return TextCommandResult.Error(Lang.GetL(args.LanguageCode, "command-time-invalidtimespan", new object[] { strValue }), "");
			}
			if (hours < 0f)
			{
				return TextCommandResult.Error(Lang.GetL(args.LanguageCode, "command-time-negativeerror", Array.Empty<object>()), "");
			}
			if (hours > this.server.GameWorldCalendar.HoursPerDay)
			{
				return TextCommandResult.Error(Lang.GetL(args.LanguageCode, "command-invalidtimeset", new object[] { this.server.GameWorldCalendar.HoursPerDay }), "");
			}
			this.server.GameWorldCalendar.SetDayTime(hours);
			this.resendTimePacket();
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-timeset", new object[] { this.server.GameWorldCalendar.PrettyDate() }), null);
		}

		private TextCommandResult cmdTimeSpeed(TextCommandCallingArgs args)
		{
			if (args.Parsers[0].IsMissing)
			{
				return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-time-speed", new object[] { this.server.GameWorldCalendar.TimeSpeedModifiers["baseline"] }), null);
			}
			float speed = (float)args[0];
			this.server.GameWorldCalendar.SetTimeSpeedModifier("baseline", speed);
			this.resendTimePacket();
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, (speed == 0f) ? "command-time-speed0set" : "command-time-speedset", new object[]
			{
				speed,
				Math.Round((double)(this.server.GameWorldCalendar.DayLengthInRealLifeSeconds / 60f), 1)
			}), null);
		}

		private TextCommandResult cmdResumeTime(TextCommandCallingArgs args)
		{
			this.server.GameWorldCalendar.SetTimeSpeedModifier("baseline", 60f);
			this.resendTimePacket();
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-time-resumed", Array.Empty<object>()), null);
		}

		private TextCommandResult cmdStopTime(TextCommandCallingArgs args)
		{
			this.server.GameWorldCalendar.SetTimeSpeedModifier("baseline", 0f);
			this.resendTimePacket();
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-time-stopped", Array.Empty<object>()), null);
		}

		private bool ParseTimeSpan(string timespan, out float hours)
		{
			int minutes = 0;
			int hoursi;
			bool valid;
			if (timespan.Contains(":"))
			{
				string[] parts = timespan.Split(':', StringSplitOptions.None);
				valid = int.TryParse(parts[0], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out hoursi);
				valid &= int.TryParse(parts[1], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out minutes);
			}
			else
			{
				valid = int.TryParse(timespan, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out hoursi);
			}
			hours = (float)hoursi + (float)minutes / 60f;
			return valid;
		}

		private void resendTimePacket()
		{
			this.server.lastUpdateSentToClient = (long)(-1000 * MagicNum.CalendarPacketSecondInterval);
		}

		private ServerMain server;
	}
}
