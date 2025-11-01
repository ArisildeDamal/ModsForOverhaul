using System;

public class Packet_ServerAssetsSerializer
{
	public static Packet_ServerAssets DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerAssets instance = new Packet_ServerAssets();
		Packet_ServerAssetsSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerAssets DeserializeBuffer(byte[] buffer, int length, Packet_ServerAssets instance)
	{
		Packet_ServerAssetsSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerAssets Deserialize(CitoMemoryStream stream, Packet_ServerAssets instance)
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
					goto IL_0055;
				}
				if (keyInt == 10)
				{
					instance.BlocksAdd(Packet_BlockTypeSerializer.DeserializeLengthDelimitedNew(stream));
					continue;
				}
				if (keyInt == 18)
				{
					instance.ItemsAdd(Packet_ItemTypeSerializer.DeserializeLengthDelimitedNew(stream));
					continue;
				}
			}
			else
			{
				if (keyInt == 26)
				{
					instance.EntitiesAdd(Packet_EntityTypeSerializer.DeserializeLengthDelimitedNew(stream));
					continue;
				}
				if (keyInt == 34)
				{
					instance.RecipesAdd(Packet_RecipesSerializer.DeserializeLengthDelimitedNew(stream));
					continue;
				}
				if (keyInt == 42)
				{
					if (instance.Tags == null)
					{
						instance.Tags = Packet_TagsSerializer.DeserializeLengthDelimitedNew(stream);
						continue;
					}
					Packet_TagsSerializer.DeserializeLengthDelimited(stream, instance.Tags);
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
		IL_0055:
		return null;
	}

	public static Packet_ServerAssets DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerAssets instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerAssets packet_ServerAssets = Packet_ServerAssetsSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerAssets;
	}

	public static void Serialize(CitoStream stream, Packet_ServerAssets instance)
	{
		if (instance.Blocks != null)
		{
			Packet_BlockType[] elems = instance.Blocks;
			int elemCount = instance.BlocksCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(10);
				Packet_BlockTypeSerializer.SerializeWithSize(stream, elems[i]);
				i++;
			}
		}
		if (instance.Items != null)
		{
			Packet_ItemType[] elems2 = instance.Items;
			int elemCount2 = instance.ItemsCount;
			int j = 0;
			while (j < elems2.Length && j < elemCount2)
			{
				stream.WriteByte(18);
				Packet_ItemTypeSerializer.SerializeWithSize(stream, elems2[j]);
				j++;
			}
		}
		if (instance.Entities != null)
		{
			Packet_EntityType[] elems3 = instance.Entities;
			int elemCount3 = instance.EntitiesCount;
			int k = 0;
			while (k < elems3.Length && k < elemCount3)
			{
				stream.WriteByte(26);
				Packet_EntityTypeSerializer.SerializeWithSize(stream, elems3[k]);
				k++;
			}
		}
		if (instance.Recipes != null)
		{
			Packet_Recipes[] elems4 = instance.Recipes;
			int elemCount4 = instance.RecipesCount;
			int l = 0;
			while (l < elems4.Length && l < elemCount4)
			{
				stream.WriteByte(34);
				Packet_RecipesSerializer.SerializeWithSize(stream, elems4[l]);
				l++;
			}
		}
		if (instance.Tags != null)
		{
			stream.WriteByte(42);
			Packet_TagsSerializer.SerializeWithSize(stream, instance.Tags);
		}
	}

	public static int GetSize(Packet_ServerAssets instance)
	{
		int size = 0;
		if (instance.Blocks != null)
		{
			for (int i = 0; i < instance.BlocksCount; i++)
			{
				int packetlength = Packet_BlockTypeSerializer.GetSize(instance.Blocks[i]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
			}
		}
		if (instance.Items != null)
		{
			for (int j = 0; j < instance.ItemsCount; j++)
			{
				int packetlength2 = Packet_ItemTypeSerializer.GetSize(instance.Items[j]);
				size += packetlength2 + ProtocolParser.GetSize(packetlength2) + 1;
			}
		}
		if (instance.Entities != null)
		{
			for (int k = 0; k < instance.EntitiesCount; k++)
			{
				int packetlength3 = Packet_EntityTypeSerializer.GetSize(instance.Entities[k]);
				size += packetlength3 + ProtocolParser.GetSize(packetlength3) + 1;
			}
		}
		if (instance.Recipes != null)
		{
			for (int l = 0; l < instance.RecipesCount; l++)
			{
				int packetlength4 = Packet_RecipesSerializer.GetSize(instance.Recipes[l]);
				size += packetlength4 + ProtocolParser.GetSize(packetlength4) + 1;
			}
		}
		if (instance.Tags != null)
		{
			int packetlength5 = Packet_TagsSerializer.GetSize(instance.Tags);
			size += packetlength5 + ProtocolParser.GetSize(packetlength5) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerAssets instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerAssetsSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerAssets instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerAssetsSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerAssets instance)
	{
		byte[] data = Packet_ServerAssetsSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
