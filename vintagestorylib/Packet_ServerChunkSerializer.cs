using System;

public class Packet_ServerChunkSerializer
{
	public static Packet_ServerChunk DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerChunk instance = new Packet_ServerChunk();
		Packet_ServerChunkSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerChunk DeserializeBuffer(byte[] buffer, int length, Packet_ServerChunk instance)
	{
		Packet_ServerChunkSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerChunk Deserialize(CitoMemoryStream stream, Packet_ServerChunk instance)
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
			if (keyInt <= 58)
			{
				if (keyInt <= 26)
				{
					if (keyInt <= 10)
					{
						if (keyInt == 0)
						{
							goto IL_00F6;
						}
						if (keyInt == 10)
						{
							instance.Blocks = ProtocolParser.ReadBytes(stream);
							continue;
						}
					}
					else
					{
						if (keyInt == 18)
						{
							instance.Light = ProtocolParser.ReadBytes(stream);
							continue;
						}
						if (keyInt == 26)
						{
							instance.LightSat = ProtocolParser.ReadBytes(stream);
							continue;
						}
					}
				}
				else if (keyInt <= 40)
				{
					if (keyInt == 32)
					{
						instance.X = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 40)
					{
						instance.Y = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 48)
					{
						instance.Z = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 58)
					{
						instance.EntitiesAdd(Packet_EntitySerializer.DeserializeLengthDelimitedNew(stream));
						continue;
					}
				}
			}
			else if (keyInt <= 88)
			{
				if (keyInt <= 72)
				{
					if (keyInt == 66)
					{
						instance.BlockEntitiesAdd(Packet_BlockEntitySerializer.DeserializeLengthDelimitedNew(stream));
						continue;
					}
					if (keyInt == 72)
					{
						instance.LightPositionsAdd(ProtocolParser.ReadUInt32(stream));
						continue;
					}
				}
				else
				{
					if (keyInt == 82)
					{
						instance.Moddata = ProtocolParser.ReadBytes(stream);
						continue;
					}
					if (keyInt == 88)
					{
						instance.Empty = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 104)
			{
				if (keyInt == 96)
				{
					instance.DecorsPosAdd(ProtocolParser.ReadUInt32(stream));
					continue;
				}
				if (keyInt == 104)
				{
					instance.DecorsIdsAdd(ProtocolParser.ReadUInt32(stream));
					continue;
				}
			}
			else
			{
				if (keyInt == 112)
				{
					instance.Compver = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 122)
				{
					instance.Liquids = ProtocolParser.ReadBytes(stream);
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
		IL_00F6:
		return null;
	}

	public static Packet_ServerChunk DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerChunk instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerChunk packet_ServerChunk = Packet_ServerChunkSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerChunk;
	}

	public static void Serialize(CitoStream stream, Packet_ServerChunk instance)
	{
		if (instance.Blocks != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, instance.Blocks);
		}
		if (instance.Light != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteBytes(stream, instance.Light);
		}
		if (instance.LightSat != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, instance.LightSat);
		}
		if (instance.Liquids != null)
		{
			stream.WriteByte(122);
			ProtocolParser.WriteBytes(stream, instance.Liquids);
		}
		if (instance.LightPositions != null)
		{
			int[] elems = instance.LightPositions;
			int elemCount = instance.LightPositionsCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(72);
				ProtocolParser.WriteUInt32(stream, elems[i]);
				i++;
			}
		}
		if (instance.X != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.X);
		}
		if (instance.Y != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Y);
		}
		if (instance.Z != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Z);
		}
		if (instance.Entities != null)
		{
			Packet_Entity[] elems2 = instance.Entities;
			int elemCount2 = instance.EntitiesCount;
			int j = 0;
			while (j < elems2.Length && j < elemCount2)
			{
				stream.WriteByte(58);
				Packet_EntitySerializer.SerializeWithSize(stream, elems2[j]);
				j++;
			}
		}
		if (instance.BlockEntities != null)
		{
			Packet_BlockEntity[] elems3 = instance.BlockEntities;
			int elemCount3 = instance.BlockEntitiesCount;
			int k = 0;
			while (k < elems3.Length && k < elemCount3)
			{
				stream.WriteByte(66);
				Packet_BlockEntitySerializer.SerializeWithSize(stream, elems3[k]);
				k++;
			}
		}
		if (instance.Moddata != null)
		{
			stream.WriteByte(82);
			ProtocolParser.WriteBytes(stream, instance.Moddata);
		}
		if (instance.Empty != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.Empty);
		}
		if (instance.DecorsPos != null)
		{
			int[] elems4 = instance.DecorsPos;
			int elemCount4 = instance.DecorsPosCount;
			int l = 0;
			while (l < elems4.Length && l < elemCount4)
			{
				stream.WriteByte(96);
				ProtocolParser.WriteUInt32(stream, elems4[l]);
				l++;
			}
		}
		if (instance.DecorsIds != null)
		{
			int[] elems5 = instance.DecorsIds;
			int elemCount5 = instance.DecorsIdsCount;
			int m = 0;
			while (m < elems5.Length && m < elemCount5)
			{
				stream.WriteByte(104);
				ProtocolParser.WriteUInt32(stream, elems5[m]);
				m++;
			}
		}
		if (instance.Compver != 0)
		{
			stream.WriteByte(112);
			ProtocolParser.WriteUInt32(stream, instance.Compver);
		}
	}

	public static int GetSize(Packet_ServerChunk instance)
	{
		int size = 0;
		if (instance.Blocks != null)
		{
			size += ProtocolParser.GetSize(instance.Blocks) + 1;
		}
		if (instance.Light != null)
		{
			size += ProtocolParser.GetSize(instance.Light) + 1;
		}
		if (instance.LightSat != null)
		{
			size += ProtocolParser.GetSize(instance.LightSat) + 1;
		}
		if (instance.Liquids != null)
		{
			size += ProtocolParser.GetSize(instance.Liquids) + 1;
		}
		if (instance.LightPositions != null)
		{
			for (int i = 0; i < instance.LightPositionsCount; i++)
			{
				int i2 = instance.LightPositions[i];
				size += ProtocolParser.GetSize(i2) + 1;
			}
		}
		if (instance.X != 0)
		{
			size += ProtocolParser.GetSize(instance.X) + 1;
		}
		if (instance.Y != 0)
		{
			size += ProtocolParser.GetSize(instance.Y) + 1;
		}
		if (instance.Z != 0)
		{
			size += ProtocolParser.GetSize(instance.Z) + 1;
		}
		if (instance.Entities != null)
		{
			for (int j = 0; j < instance.EntitiesCount; j++)
			{
				int packetlength = Packet_EntitySerializer.GetSize(instance.Entities[j]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
			}
		}
		if (instance.BlockEntities != null)
		{
			for (int k = 0; k < instance.BlockEntitiesCount; k++)
			{
				int packetlength2 = Packet_BlockEntitySerializer.GetSize(instance.BlockEntities[k]);
				size += packetlength2 + ProtocolParser.GetSize(packetlength2) + 1;
			}
		}
		if (instance.Moddata != null)
		{
			size += ProtocolParser.GetSize(instance.Moddata) + 1;
		}
		if (instance.Empty != 0)
		{
			size += ProtocolParser.GetSize(instance.Empty) + 1;
		}
		if (instance.DecorsPos != null)
		{
			for (int l = 0; l < instance.DecorsPosCount; l++)
			{
				int i3 = instance.DecorsPos[l];
				size += ProtocolParser.GetSize(i3) + 1;
			}
		}
		if (instance.DecorsIds != null)
		{
			for (int m = 0; m < instance.DecorsIdsCount; m++)
			{
				int i4 = instance.DecorsIds[m];
				size += ProtocolParser.GetSize(i4) + 1;
			}
		}
		if (instance.Compver != 0)
		{
			size += ProtocolParser.GetSize(instance.Compver) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerChunk instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerChunkSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerChunk instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerChunkSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerChunk instance)
	{
		byte[] data = Packet_ServerChunkSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
