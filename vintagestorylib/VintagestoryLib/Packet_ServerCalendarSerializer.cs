using System;

public class Packet_ServerCalendarSerializer
{
	public static Packet_ServerCalendar DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerCalendar instance = new Packet_ServerCalendar();
		Packet_ServerCalendarSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerCalendar DeserializeBuffer(byte[] buffer, int length, Packet_ServerCalendar instance)
	{
		Packet_ServerCalendarSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerCalendar Deserialize(CitoMemoryStream stream, Packet_ServerCalendar instance)
	{
		instance.InitializeValues();
		int keyInt;
		for (;;)
		{
			keyInt = stream.ReadByte();
			if ((keyInt & 128) != 0)
			{
				keyInt = ProtocolParser.ReadKeyAsInt(keyInt, stream);
				if ((keyInt & 16384) != 0)
				{
					break;
				}
			}
			if (keyInt <= 32)
			{
				if (keyInt <= 8)
				{
					if (keyInt == 0)
					{
						goto IL_0087;
					}
					if (keyInt == 8)
					{
						instance.TotalSeconds = ProtocolParser.ReadUInt64(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 18)
					{
						instance.TimeSpeedModifierNamesAdd(ProtocolParser.ReadString(stream));
						continue;
					}
					if (keyInt == 24)
					{
						instance.TimeSpeedModifierSpeedsAdd(ProtocolParser.ReadUInt32(stream));
						continue;
					}
					if (keyInt == 32)
					{
						instance.MoonOrbitDays = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 48)
			{
				if (keyInt == 40)
				{
					instance.HoursPerDay = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 48)
				{
					instance.Running = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 56)
				{
					instance.CalendarSpeedMul = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 64)
				{
					instance.DaysPerMonth = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 72)
				{
					instance.TotalSecondsStart = ProtocolParser.ReadUInt64(stream);
					continue;
				}
			}
			ProtocolParser.SkipKey(stream, Key.Create(keyInt));
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
		IL_0087:
		return null;
	}

	public static Packet_ServerCalendar DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerCalendar instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerCalendar packet_ServerCalendar = Packet_ServerCalendarSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerCalendar;
	}

	public static void Serialize(CitoStream stream, Packet_ServerCalendar instance)
	{
		if (instance.TotalSeconds != 0L)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt64(stream, instance.TotalSeconds);
		}
		if (instance.TimeSpeedModifierNames != null)
		{
			string[] elems = instance.TimeSpeedModifierNames;
			int elemCount = instance.TimeSpeedModifierNamesCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(18);
				ProtocolParser.WriteString(stream, elems[i]);
				i++;
			}
		}
		if (instance.TimeSpeedModifierSpeeds != null)
		{
			int[] elems2 = instance.TimeSpeedModifierSpeeds;
			int elemCount2 = instance.TimeSpeedModifierSpeedsCount;
			int j = 0;
			while (j < elems2.Length && j < elemCount2)
			{
				stream.WriteByte(24);
				ProtocolParser.WriteUInt32(stream, elems2[j]);
				j++;
			}
		}
		if (instance.MoonOrbitDays != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.MoonOrbitDays);
		}
		if (instance.HoursPerDay != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.HoursPerDay);
		}
		if (instance.Running != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Running);
		}
		if (instance.CalendarSpeedMul != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.CalendarSpeedMul);
		}
		if (instance.DaysPerMonth != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.DaysPerMonth);
		}
		if (instance.TotalSecondsStart != 0L)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt64(stream, instance.TotalSecondsStart);
		}
	}

	public static int GetSize(Packet_ServerCalendar instance)
	{
		int size = 0;
		if (instance.TotalSeconds != 0L)
		{
			size += ProtocolParser.GetSize(instance.TotalSeconds) + 1;
		}
		if (instance.TimeSpeedModifierNames != null)
		{
			for (int i = 0; i < instance.TimeSpeedModifierNamesCount; i++)
			{
				string i2 = instance.TimeSpeedModifierNames[i];
				size += ProtocolParser.GetSize(i2) + 1;
			}
		}
		if (instance.TimeSpeedModifierSpeeds != null)
		{
			for (int j = 0; j < instance.TimeSpeedModifierSpeedsCount; j++)
			{
				int i3 = instance.TimeSpeedModifierSpeeds[j];
				size += ProtocolParser.GetSize(i3) + 1;
			}
		}
		if (instance.MoonOrbitDays != 0)
		{
			size += ProtocolParser.GetSize(instance.MoonOrbitDays) + 1;
		}
		if (instance.HoursPerDay != 0)
		{
			size += ProtocolParser.GetSize(instance.HoursPerDay) + 1;
		}
		if (instance.Running != 0)
		{
			size += ProtocolParser.GetSize(instance.Running) + 1;
		}
		if (instance.CalendarSpeedMul != 0)
		{
			size += ProtocolParser.GetSize(instance.CalendarSpeedMul) + 1;
		}
		if (instance.DaysPerMonth != 0)
		{
			size += ProtocolParser.GetSize(instance.DaysPerMonth) + 1;
		}
		if (instance.TotalSecondsStart != 0L)
		{
			size += ProtocolParser.GetSize(instance.TotalSecondsStart) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerCalendar instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerCalendarSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerCalendar instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerCalendarSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerCalendar instance)
	{
		byte[] data = Packet_ServerCalendarSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
