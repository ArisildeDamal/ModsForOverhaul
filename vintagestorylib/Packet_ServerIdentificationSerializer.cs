using System;

public class Packet_ServerIdentificationSerializer
{
	public static Packet_ServerIdentification DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerIdentification instance = new Packet_ServerIdentification();
		Packet_ServerIdentificationSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerIdentification DeserializeBuffer(byte[] buffer, int length, Packet_ServerIdentification instance)
	{
		Packet_ServerIdentificationSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerIdentification Deserialize(CitoMemoryStream stream, Packet_ServerIdentification instance)
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
			if (keyInt <= 130)
			{
				if (keyInt <= 64)
				{
					if (keyInt <= 10)
					{
						if (keyInt == 0)
						{
							goto IL_0151;
						}
						if (keyInt == 10)
						{
							instance.NetworkVersion = ProtocolParser.ReadString(stream);
							continue;
						}
					}
					else
					{
						if (keyInt == 26)
						{
							instance.ServerName = ProtocolParser.ReadString(stream);
							continue;
						}
						if (keyInt == 56)
						{
							instance.MapSizeX = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 64)
						{
							instance.MapSizeY = ProtocolParser.ReadUInt32(stream);
							continue;
						}
					}
				}
				else if (keyInt <= 88)
				{
					if (keyInt == 72)
					{
						instance.MapSizeZ = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 88)
					{
						instance.DisableShadows = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 96)
					{
						instance.PlayerAreaSize = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 104)
					{
						instance.Seed = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 130)
					{
						instance.PlayStyle = ProtocolParser.ReadString(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 168)
			{
				if (keyInt <= 144)
				{
					if (keyInt == 138)
					{
						instance.GameVersion = ProtocolParser.ReadString(stream);
						continue;
					}
					if (keyInt == 144)
					{
						instance.RequireRemapping = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 154)
					{
						instance.ModsAdd(Packet_ModIdSerializer.DeserializeLengthDelimitedNew(stream));
						continue;
					}
					if (keyInt == 162)
					{
						instance.WorldConfiguration = ProtocolParser.ReadBytes(stream);
						continue;
					}
					if (keyInt == 168)
					{
						instance.RegionMapSizeX = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 194)
			{
				if (keyInt == 176)
				{
					instance.RegionMapSizeY = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 184)
				{
					instance.RegionMapSizeZ = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 194)
				{
					instance.SavegameIdentifier = ProtocolParser.ReadString(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 202)
				{
					instance.PlayListCode = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 210)
				{
					instance.ServerModIdBlackListAdd(ProtocolParser.ReadString(stream));
					continue;
				}
				if (keyInt == 218)
				{
					instance.ServerModIdWhiteListAdd(ProtocolParser.ReadString(stream));
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
		IL_0151:
		return null;
	}

	public static Packet_ServerIdentification DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerIdentification instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerIdentification packet_ServerIdentification = Packet_ServerIdentificationSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerIdentification;
	}

	public static void Serialize(CitoStream stream, Packet_ServerIdentification instance)
	{
		if (instance.NetworkVersion != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.NetworkVersion);
		}
		if (instance.GameVersion != null)
		{
			stream.WriteKey(17, 2);
			ProtocolParser.WriteString(stream, instance.GameVersion);
		}
		if (instance.ServerName != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.ServerName);
		}
		if (instance.MapSizeX != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.MapSizeX);
		}
		if (instance.MapSizeY != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.MapSizeY);
		}
		if (instance.MapSizeZ != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.MapSizeZ);
		}
		if (instance.RegionMapSizeX != 0)
		{
			stream.WriteKey(21, 0);
			ProtocolParser.WriteUInt32(stream, instance.RegionMapSizeX);
		}
		if (instance.RegionMapSizeY != 0)
		{
			stream.WriteKey(22, 0);
			ProtocolParser.WriteUInt32(stream, instance.RegionMapSizeY);
		}
		if (instance.RegionMapSizeZ != 0)
		{
			stream.WriteKey(23, 0);
			ProtocolParser.WriteUInt32(stream, instance.RegionMapSizeZ);
		}
		if (instance.DisableShadows != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.DisableShadows);
		}
		if (instance.PlayerAreaSize != 0)
		{
			stream.WriteByte(96);
			ProtocolParser.WriteUInt32(stream, instance.PlayerAreaSize);
		}
		if (instance.Seed != 0)
		{
			stream.WriteByte(104);
			ProtocolParser.WriteUInt32(stream, instance.Seed);
		}
		if (instance.PlayStyle != null)
		{
			stream.WriteKey(16, 2);
			ProtocolParser.WriteString(stream, instance.PlayStyle);
		}
		if (instance.RequireRemapping != 0)
		{
			stream.WriteKey(18, 0);
			ProtocolParser.WriteUInt32(stream, instance.RequireRemapping);
		}
		if (instance.Mods != null)
		{
			Packet_ModId[] elems = instance.Mods;
			int elemCount = instance.ModsCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteKey(19, 2);
				Packet_ModIdSerializer.SerializeWithSize(stream, elems[i]);
				i++;
			}
		}
		if (instance.WorldConfiguration != null)
		{
			stream.WriteKey(20, 2);
			ProtocolParser.WriteBytes(stream, instance.WorldConfiguration);
		}
		if (instance.SavegameIdentifier != null)
		{
			stream.WriteKey(24, 2);
			ProtocolParser.WriteString(stream, instance.SavegameIdentifier);
		}
		if (instance.PlayListCode != null)
		{
			stream.WriteKey(25, 2);
			ProtocolParser.WriteString(stream, instance.PlayListCode);
		}
		if (instance.ServerModIdBlackList != null)
		{
			string[] elems2 = instance.ServerModIdBlackList;
			int elemCount2 = instance.ServerModIdBlackListCount;
			int j = 0;
			while (j < elems2.Length && j < elemCount2)
			{
				stream.WriteKey(26, 2);
				ProtocolParser.WriteString(stream, elems2[j]);
				j++;
			}
		}
		if (instance.ServerModIdWhiteList != null)
		{
			string[] elems3 = instance.ServerModIdWhiteList;
			int elemCount3 = instance.ServerModIdWhiteListCount;
			int k = 0;
			while (k < elems3.Length && k < elemCount3)
			{
				stream.WriteKey(27, 2);
				ProtocolParser.WriteString(stream, elems3[k]);
				k++;
			}
		}
	}

	public static int GetSize(Packet_ServerIdentification instance)
	{
		int size = 0;
		if (instance.NetworkVersion != null)
		{
			size += ProtocolParser.GetSize(instance.NetworkVersion) + 1;
		}
		if (instance.GameVersion != null)
		{
			size += ProtocolParser.GetSize(instance.GameVersion) + 2;
		}
		if (instance.ServerName != null)
		{
			size += ProtocolParser.GetSize(instance.ServerName) + 1;
		}
		if (instance.MapSizeX != 0)
		{
			size += ProtocolParser.GetSize(instance.MapSizeX) + 1;
		}
		if (instance.MapSizeY != 0)
		{
			size += ProtocolParser.GetSize(instance.MapSizeY) + 1;
		}
		if (instance.MapSizeZ != 0)
		{
			size += ProtocolParser.GetSize(instance.MapSizeZ) + 1;
		}
		if (instance.RegionMapSizeX != 0)
		{
			size += ProtocolParser.GetSize(instance.RegionMapSizeX) + 2;
		}
		if (instance.RegionMapSizeY != 0)
		{
			size += ProtocolParser.GetSize(instance.RegionMapSizeY) + 2;
		}
		if (instance.RegionMapSizeZ != 0)
		{
			size += ProtocolParser.GetSize(instance.RegionMapSizeZ) + 2;
		}
		if (instance.DisableShadows != 0)
		{
			size += ProtocolParser.GetSize(instance.DisableShadows) + 1;
		}
		if (instance.PlayerAreaSize != 0)
		{
			size += ProtocolParser.GetSize(instance.PlayerAreaSize) + 1;
		}
		if (instance.Seed != 0)
		{
			size += ProtocolParser.GetSize(instance.Seed) + 1;
		}
		if (instance.PlayStyle != null)
		{
			size += ProtocolParser.GetSize(instance.PlayStyle) + 2;
		}
		if (instance.RequireRemapping != 0)
		{
			size += ProtocolParser.GetSize(instance.RequireRemapping) + 2;
		}
		if (instance.Mods != null)
		{
			for (int i = 0; i < instance.ModsCount; i++)
			{
				int packetlength = Packet_ModIdSerializer.GetSize(instance.Mods[i]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 2;
			}
		}
		if (instance.WorldConfiguration != null)
		{
			size += ProtocolParser.GetSize(instance.WorldConfiguration) + 2;
		}
		if (instance.SavegameIdentifier != null)
		{
			size += ProtocolParser.GetSize(instance.SavegameIdentifier) + 2;
		}
		if (instance.PlayListCode != null)
		{
			size += ProtocolParser.GetSize(instance.PlayListCode) + 2;
		}
		if (instance.ServerModIdBlackList != null)
		{
			for (int j = 0; j < instance.ServerModIdBlackListCount; j++)
			{
				string i2 = instance.ServerModIdBlackList[j];
				size += ProtocolParser.GetSize(i2) + 2;
			}
		}
		if (instance.ServerModIdWhiteList != null)
		{
			for (int k = 0; k < instance.ServerModIdWhiteListCount; k++)
			{
				string i3 = instance.ServerModIdWhiteList[k];
				size += ProtocolParser.GetSize(i3) + 2;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerIdentification instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerIdentificationSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerIdentification instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerIdentificationSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerIdentification instance)
	{
		byte[] data = Packet_ServerIdentificationSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
