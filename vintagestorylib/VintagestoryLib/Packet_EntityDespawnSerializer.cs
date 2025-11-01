using System;

public class Packet_EntityDespawnSerializer
{
	public static Packet_EntityDespawn DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityDespawn instance = new Packet_EntityDespawn();
		Packet_EntityDespawnSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_EntityDespawn DeserializeBuffer(byte[] buffer, int length, Packet_EntityDespawn instance)
	{
		Packet_EntityDespawnSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityDespawn Deserialize(CitoMemoryStream stream, Packet_EntityDespawn instance)
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
			if (keyInt <= 8)
			{
				if (keyInt == 0)
				{
					goto IL_004B;
				}
				if (keyInt == 8)
				{
					instance.EntityIdAdd(ProtocolParser.ReadUInt64(stream));
					continue;
				}
			}
			else
			{
				if (keyInt == 16)
				{
					instance.DespawnReasonAdd(ProtocolParser.ReadUInt32(stream));
					continue;
				}
				if (keyInt == 24)
				{
					instance.DeathDamageSourceAdd(ProtocolParser.ReadUInt32(stream));
					continue;
				}
				if (keyInt == 32)
				{
					instance.ByEntityIdAdd(ProtocolParser.ReadUInt64(stream));
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
		IL_004B:
		return null;
	}

	public static Packet_EntityDespawn DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityDespawn instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_EntityDespawn packet_EntityDespawn = Packet_EntityDespawnSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_EntityDespawn;
	}

	public static void Serialize(CitoStream stream, Packet_EntityDespawn instance)
	{
		if (instance.EntityId != null)
		{
			long[] elems = instance.EntityId;
			int elemCount = instance.EntityIdCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(8);
				ProtocolParser.WriteUInt64(stream, elems[i]);
				i++;
			}
		}
		if (instance.DespawnReason != null)
		{
			int[] elems2 = instance.DespawnReason;
			int elemCount2 = instance.DespawnReasonCount;
			int j = 0;
			while (j < elems2.Length && j < elemCount2)
			{
				stream.WriteByte(16);
				ProtocolParser.WriteUInt32(stream, elems2[j]);
				j++;
			}
		}
		if (instance.DeathDamageSource != null)
		{
			int[] elems3 = instance.DeathDamageSource;
			int elemCount3 = instance.DeathDamageSourceCount;
			int k = 0;
			while (k < elems3.Length && k < elemCount3)
			{
				stream.WriteByte(24);
				ProtocolParser.WriteUInt32(stream, elems3[k]);
				k++;
			}
		}
		if (instance.ByEntityId != null)
		{
			long[] elems4 = instance.ByEntityId;
			int elemCount4 = instance.ByEntityIdCount;
			int l = 0;
			while (l < elems4.Length && l < elemCount4)
			{
				stream.WriteByte(32);
				ProtocolParser.WriteUInt64(stream, elems4[l]);
				l++;
			}
		}
	}

	public static int GetSize(Packet_EntityDespawn instance)
	{
		int size = 0;
		if (instance.EntityId != null)
		{
			for (int i = 0; i < instance.EntityIdCount; i++)
			{
				long i2 = instance.EntityId[i];
				size += ProtocolParser.GetSize(i2) + 1;
			}
		}
		if (instance.DespawnReason != null)
		{
			for (int j = 0; j < instance.DespawnReasonCount; j++)
			{
				int i3 = instance.DespawnReason[j];
				size += ProtocolParser.GetSize(i3) + 1;
			}
		}
		if (instance.DeathDamageSource != null)
		{
			for (int k = 0; k < instance.DeathDamageSourceCount; k++)
			{
				int i4 = instance.DeathDamageSource[k];
				size += ProtocolParser.GetSize(i4) + 1;
			}
		}
		if (instance.ByEntityId != null)
		{
			for (int l = 0; l < instance.ByEntityIdCount; l++)
			{
				long i5 = instance.ByEntityId[l];
				size += ProtocolParser.GetSize(i5) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_EntityDespawn instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_EntityDespawnSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_EntityDespawn instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_EntityDespawnSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityDespawn instance)
	{
		byte[] data = Packet_EntityDespawnSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
