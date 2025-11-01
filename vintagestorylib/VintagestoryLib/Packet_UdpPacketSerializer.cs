using System;

public class Packet_UdpPacketSerializer
{
	public static Packet_UdpPacket DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_UdpPacket instance = new Packet_UdpPacket();
		Packet_UdpPacketSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_UdpPacket DeserializeBuffer(byte[] buffer, int length, Packet_UdpPacket instance)
	{
		Packet_UdpPacketSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_UdpPacket Deserialize(CitoMemoryStream stream, Packet_UdpPacket instance)
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
			if (keyInt <= 18)
			{
				if (keyInt == 0)
				{
					goto IL_0072;
				}
				if (keyInt == 8)
				{
					instance.Id = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 18)
				{
					if (instance.EntityPosition == null)
					{
						instance.EntityPosition = Packet_EntityPositionSerializer.DeserializeLengthDelimitedNew(stream);
						continue;
					}
					Packet_EntityPositionSerializer.DeserializeLengthDelimited(stream, instance.EntityPosition);
					continue;
				}
			}
			else if (keyInt <= 34)
			{
				if (keyInt != 26)
				{
					if (keyInt == 34)
					{
						if (instance.ChannelPacket == null)
						{
							instance.ChannelPacket = Packet_CustomPacketSerializer.DeserializeLengthDelimitedNew(stream);
							continue;
						}
						Packet_CustomPacketSerializer.DeserializeLengthDelimited(stream, instance.ChannelPacket);
						continue;
					}
				}
				else
				{
					if (instance.BulkPositions == null)
					{
						instance.BulkPositions = Packet_BulkEntityPositionSerializer.DeserializeLengthDelimitedNew(stream);
						continue;
					}
					Packet_BulkEntityPositionSerializer.DeserializeLengthDelimited(stream, instance.BulkPositions);
					continue;
				}
			}
			else if (keyInt != 42)
			{
				if (keyInt == 48)
				{
					instance.Length = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (instance.ConnectionPacket == null)
				{
					instance.ConnectionPacket = Packet_ConnectionPacketSerializer.DeserializeLengthDelimitedNew(stream);
					continue;
				}
				Packet_ConnectionPacketSerializer.DeserializeLengthDelimited(stream, instance.ConnectionPacket);
				continue;
			}
			ProtocolParser.SkipKey(stream, Key.Create(keyInt));
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
		IL_0072:
		return null;
	}

	public static Packet_UdpPacket DeserializeLengthDelimited(CitoMemoryStream stream, Packet_UdpPacket instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_UdpPacket packet_UdpPacket = Packet_UdpPacketSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_UdpPacket;
	}

	public static void Serialize(CitoStream stream, Packet_UdpPacket instance)
	{
		stream.WriteByte(8);
		ProtocolParser.WriteUInt32(stream, instance.Id);
		if (instance.EntityPosition != null)
		{
			stream.WriteByte(18);
			Packet_EntityPosition i2 = instance.EntityPosition;
			Packet_EntityPositionSerializer.GetSize(i2);
			Packet_EntityPositionSerializer.SerializeWithSize(stream, i2);
		}
		if (instance.BulkPositions != null)
		{
			stream.WriteByte(26);
			Packet_BulkEntityPosition i3 = instance.BulkPositions;
			Packet_BulkEntityPositionSerializer.GetSize(i3);
			Packet_BulkEntityPositionSerializer.SerializeWithSize(stream, i3);
		}
		if (instance.ChannelPacket != null)
		{
			stream.WriteByte(34);
			Packet_CustomPacket i4 = instance.ChannelPacket;
			Packet_CustomPacketSerializer.GetSize(i4);
			Packet_CustomPacketSerializer.SerializeWithSize(stream, i4);
		}
		if (instance.ConnectionPacket != null)
		{
			stream.WriteByte(42);
			Packet_ConnectionPacket i5 = instance.ConnectionPacket;
			Packet_ConnectionPacketSerializer.GetSize(i5);
			Packet_ConnectionPacketSerializer.SerializeWithSize(stream, i5);
		}
		if (instance.Length != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Length);
		}
	}

	public static int GetSize(Packet_UdpPacket instance)
	{
		int size = 0;
		size += ProtocolParser.GetSize(instance.Id) + 1;
		if (instance.EntityPosition != null)
		{
			int packetlength = Packet_EntityPositionSerializer.GetSize(instance.EntityPosition);
			size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
		}
		if (instance.BulkPositions != null)
		{
			int packetlength2 = Packet_BulkEntityPositionSerializer.GetSize(instance.BulkPositions);
			size += packetlength2 + ProtocolParser.GetSize(packetlength2) + 1;
		}
		if (instance.ChannelPacket != null)
		{
			int packetlength3 = Packet_CustomPacketSerializer.GetSize(instance.ChannelPacket);
			size += packetlength3 + ProtocolParser.GetSize(packetlength3) + 1;
		}
		if (instance.ConnectionPacket != null)
		{
			int packetlength4 = Packet_ConnectionPacketSerializer.GetSize(instance.ConnectionPacket);
			size += packetlength4 + ProtocolParser.GetSize(packetlength4) + 1;
		}
		if (instance.Length != 0)
		{
			size += ProtocolParser.GetSize(instance.Length) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_UdpPacket instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_UdpPacketSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_UdpPacket instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_UdpPacketSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_UdpPacket instance)
	{
		byte[] data = Packet_UdpPacketSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
