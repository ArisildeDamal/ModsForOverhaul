using System;

public class Packet_EntityTypeSerializer
{
	public static Packet_EntityType DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityType instance = new Packet_EntityType();
		Packet_EntityTypeSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_EntityType DeserializeBuffer(byte[] buffer, int length, Packet_EntityType instance)
	{
		Packet_EntityTypeSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityType Deserialize(CitoMemoryStream stream, Packet_EntityType instance)
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
			if (keyInt <= 152)
			{
				if (keyInt <= 74)
				{
					if (keyInt <= 32)
					{
						if (keyInt <= 10)
						{
							if (keyInt == 0)
							{
								goto IL_027A;
							}
							if (keyInt == 10)
							{
								instance.Code = ProtocolParser.ReadString(stream);
								continue;
							}
						}
						else
						{
							if (keyInt == 18)
							{
								instance.Class = ProtocolParser.ReadString(stream);
								continue;
							}
							if (keyInt == 26)
							{
								instance.Renderer = ProtocolParser.ReadString(stream);
								continue;
							}
							if (keyInt == 32)
							{
								instance.Habitat = ProtocolParser.ReadUInt32(stream);
								continue;
							}
						}
					}
					else if (keyInt <= 48)
					{
						if (keyInt == 42)
						{
							instance.BehaviorsAdd(Packet_BehaviorSerializer.DeserializeLengthDelimitedNew(stream));
							continue;
						}
						if (keyInt == 48)
						{
							instance.CollisionBoxLength = ProtocolParser.ReadUInt32(stream);
							continue;
						}
					}
					else
					{
						if (keyInt == 56)
						{
							instance.CollisionBoxHeight = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 66)
						{
							instance.Attributes = ProtocolParser.ReadString(stream);
							continue;
						}
						if (keyInt == 74)
						{
							instance.SoundKeysAdd(ProtocolParser.ReadString(stream));
							continue;
						}
					}
				}
				else if (keyInt <= 112)
				{
					if (keyInt <= 90)
					{
						if (keyInt == 82)
						{
							instance.SoundNamesAdd(ProtocolParser.ReadString(stream));
							continue;
						}
						if (keyInt == 90)
						{
							if (instance.Shape == null)
							{
								instance.Shape = Packet_CompositeShapeSerializer.DeserializeLengthDelimitedNew(stream);
								continue;
							}
							Packet_CompositeShapeSerializer.DeserializeLengthDelimited(stream, instance.Shape);
							continue;
						}
					}
					else
					{
						if (keyInt == 98)
						{
							instance.TextureCodesAdd(ProtocolParser.ReadString(stream));
							continue;
						}
						if (keyInt == 106)
						{
							instance.CompositeTexturesAdd(Packet_CompositeTextureSerializer.DeserializeLengthDelimitedNew(stream));
							continue;
						}
						if (keyInt == 112)
						{
							instance.IdleSoundChance = ProtocolParser.ReadUInt32(stream);
							continue;
						}
					}
				}
				else if (keyInt <= 128)
				{
					if (keyInt == 120)
					{
						instance.Size = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 128)
					{
						instance.EyeHeight = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 136)
					{
						instance.CanClimb = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 146)
					{
						instance.AnimationMetaData = ProtocolParser.ReadBytes(stream);
						continue;
					}
					if (keyInt == 152)
					{
						instance.KnockbackResistance = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 232)
			{
				if (keyInt <= 192)
				{
					if (keyInt <= 168)
					{
						if (keyInt == 160)
						{
							instance.GlowLevel = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 168)
						{
							instance.CanClimbAnywhere = ProtocolParser.ReadUInt32(stream);
							continue;
						}
					}
					else
					{
						if (keyInt == 176)
						{
							instance.ClimbTouchDistance = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 184)
						{
							instance.RotateModelOnClimb = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 192)
						{
							instance.FallDamage = ProtocolParser.ReadUInt32(stream);
							continue;
						}
					}
				}
				else if (keyInt <= 208)
				{
					if (keyInt == 202)
					{
						instance.Drops = ProtocolParser.ReadBytes(stream);
						continue;
					}
					if (keyInt == 208)
					{
						instance.DeadCollisionBoxLength = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 216)
					{
						instance.DeadCollisionBoxHeight = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 226)
					{
						instance.VariantAdd(Packet_VariantPartSerializer.DeserializeLengthDelimitedNew(stream));
						continue;
					}
					if (keyInt == 232)
					{
						instance.Weight = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 272)
			{
				if (keyInt <= 248)
				{
					if (keyInt == 240)
					{
						instance.SizeGrowthFactor = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 248)
					{
						instance.PitchStep = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 256)
					{
						instance.SelectionBoxLength = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 264)
					{
						instance.SelectionBoxHeight = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 272)
					{
						instance.DeadSelectionBoxLength = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 296)
			{
				if (keyInt == 280)
				{
					instance.DeadSelectionBoxHeight = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 288)
				{
					instance.SwimmingEyeHeight = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 296)
				{
					instance.IdleSoundRange = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 306)
				{
					instance.Color = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 312)
				{
					instance.FallDamageMultiplier = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 320)
				{
					instance.TagsAdd(ProtocolParser.ReadUInt32(stream));
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
		IL_027A:
		return null;
	}

	public static Packet_EntityType DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityType instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_EntityType packet_EntityType = Packet_EntityTypeSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_EntityType;
	}

	public static void Serialize(CitoStream stream, Packet_EntityType instance)
	{
		if (instance.Code != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Code);
		}
		if (instance.Class != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Class);
		}
		if (instance.Tags != null)
		{
			int[] elems = instance.Tags;
			int elemCount = instance.TagsCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteKey(40, 0);
				ProtocolParser.WriteUInt32(stream, elems[i]);
				i++;
			}
		}
		if (instance.Renderer != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.Renderer);
		}
		if (instance.Habitat != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Habitat);
		}
		if (instance.Drops != null)
		{
			stream.WriteKey(25, 2);
			ProtocolParser.WriteBytes(stream, instance.Drops);
		}
		if (instance.Shape != null)
		{
			stream.WriteByte(90);
			Packet_CompositeShapeSerializer.SerializeWithSize(stream, instance.Shape);
		}
		if (instance.Behaviors != null)
		{
			Packet_Behavior[] elems2 = instance.Behaviors;
			int elemCount2 = instance.BehaviorsCount;
			int j = 0;
			while (j < elems2.Length && j < elemCount2)
			{
				stream.WriteByte(42);
				Packet_BehaviorSerializer.SerializeWithSize(stream, elems2[j]);
				j++;
			}
		}
		if (instance.CollisionBoxLength != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.CollisionBoxLength);
		}
		if (instance.CollisionBoxHeight != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.CollisionBoxHeight);
		}
		if (instance.DeadCollisionBoxLength != 0)
		{
			stream.WriteKey(26, 0);
			ProtocolParser.WriteUInt32(stream, instance.DeadCollisionBoxLength);
		}
		if (instance.DeadCollisionBoxHeight != 0)
		{
			stream.WriteKey(27, 0);
			ProtocolParser.WriteUInt32(stream, instance.DeadCollisionBoxHeight);
		}
		if (instance.SelectionBoxLength != 0)
		{
			stream.WriteKey(32, 0);
			ProtocolParser.WriteUInt32(stream, instance.SelectionBoxLength);
		}
		if (instance.SelectionBoxHeight != 0)
		{
			stream.WriteKey(33, 0);
			ProtocolParser.WriteUInt32(stream, instance.SelectionBoxHeight);
		}
		if (instance.DeadSelectionBoxLength != 0)
		{
			stream.WriteKey(34, 0);
			ProtocolParser.WriteUInt32(stream, instance.DeadSelectionBoxLength);
		}
		if (instance.DeadSelectionBoxHeight != 0)
		{
			stream.WriteKey(35, 0);
			ProtocolParser.WriteUInt32(stream, instance.DeadSelectionBoxHeight);
		}
		if (instance.Attributes != null)
		{
			stream.WriteByte(66);
			ProtocolParser.WriteString(stream, instance.Attributes);
		}
		if (instance.SoundKeys != null)
		{
			string[] elems3 = instance.SoundKeys;
			int elemCount3 = instance.SoundKeysCount;
			int k = 0;
			while (k < elems3.Length && k < elemCount3)
			{
				stream.WriteByte(74);
				ProtocolParser.WriteString(stream, elems3[k]);
				k++;
			}
		}
		if (instance.SoundNames != null)
		{
			string[] elems4 = instance.SoundNames;
			int elemCount4 = instance.SoundNamesCount;
			int l = 0;
			while (l < elems4.Length && l < elemCount4)
			{
				stream.WriteByte(82);
				ProtocolParser.WriteString(stream, elems4[l]);
				l++;
			}
		}
		if (instance.IdleSoundChance != 0)
		{
			stream.WriteByte(112);
			ProtocolParser.WriteUInt32(stream, instance.IdleSoundChance);
		}
		if (instance.IdleSoundRange != 0)
		{
			stream.WriteKey(37, 0);
			ProtocolParser.WriteUInt32(stream, instance.IdleSoundRange);
		}
		if (instance.TextureCodes != null)
		{
			string[] elems5 = instance.TextureCodes;
			int elemCount5 = instance.TextureCodesCount;
			int m = 0;
			while (m < elems5.Length && m < elemCount5)
			{
				stream.WriteByte(98);
				ProtocolParser.WriteString(stream, elems5[m]);
				m++;
			}
		}
		if (instance.CompositeTextures != null)
		{
			Packet_CompositeTexture[] elems6 = instance.CompositeTextures;
			int elemCount6 = instance.CompositeTexturesCount;
			int n = 0;
			while (n < elems6.Length && n < elemCount6)
			{
				stream.WriteByte(106);
				Packet_CompositeTextureSerializer.SerializeWithSize(stream, elems6[n]);
				n++;
			}
		}
		if (instance.Size != 0)
		{
			stream.WriteByte(120);
			ProtocolParser.WriteUInt32(stream, instance.Size);
		}
		if (instance.EyeHeight != 0)
		{
			stream.WriteKey(16, 0);
			ProtocolParser.WriteUInt32(stream, instance.EyeHeight);
		}
		if (instance.SwimmingEyeHeight != 0)
		{
			stream.WriteKey(36, 0);
			ProtocolParser.WriteUInt32(stream, instance.SwimmingEyeHeight);
		}
		if (instance.Weight != 0)
		{
			stream.WriteKey(29, 0);
			ProtocolParser.WriteUInt32(stream, instance.Weight);
		}
		if (instance.CanClimb != 0)
		{
			stream.WriteKey(17, 0);
			ProtocolParser.WriteUInt32(stream, instance.CanClimb);
		}
		if (instance.AnimationMetaData != null)
		{
			stream.WriteKey(18, 2);
			ProtocolParser.WriteBytes(stream, instance.AnimationMetaData);
		}
		if (instance.KnockbackResistance != 0)
		{
			stream.WriteKey(19, 0);
			ProtocolParser.WriteUInt32(stream, instance.KnockbackResistance);
		}
		if (instance.GlowLevel != 0)
		{
			stream.WriteKey(20, 0);
			ProtocolParser.WriteUInt32(stream, instance.GlowLevel);
		}
		if (instance.CanClimbAnywhere != 0)
		{
			stream.WriteKey(21, 0);
			ProtocolParser.WriteUInt32(stream, instance.CanClimbAnywhere);
		}
		if (instance.ClimbTouchDistance != 0)
		{
			stream.WriteKey(22, 0);
			ProtocolParser.WriteUInt32(stream, instance.ClimbTouchDistance);
		}
		if (instance.RotateModelOnClimb != 0)
		{
			stream.WriteKey(23, 0);
			ProtocolParser.WriteUInt32(stream, instance.RotateModelOnClimb);
		}
		if (instance.FallDamage != 0)
		{
			stream.WriteKey(24, 0);
			ProtocolParser.WriteUInt32(stream, instance.FallDamage);
		}
		if (instance.FallDamageMultiplier != 0)
		{
			stream.WriteKey(39, 0);
			ProtocolParser.WriteUInt32(stream, instance.FallDamageMultiplier);
		}
		if (instance.Variant != null)
		{
			Packet_VariantPart[] elems7 = instance.Variant;
			int elemCount7 = instance.VariantCount;
			int k2 = 0;
			while (k2 < elems7.Length && k2 < elemCount7)
			{
				stream.WriteKey(28, 2);
				Packet_VariantPartSerializer.SerializeWithSize(stream, elems7[k2]);
				k2++;
			}
		}
		if (instance.SizeGrowthFactor != 0)
		{
			stream.WriteKey(30, 0);
			ProtocolParser.WriteUInt32(stream, instance.SizeGrowthFactor);
		}
		if (instance.PitchStep != 0)
		{
			stream.WriteKey(31, 0);
			ProtocolParser.WriteUInt32(stream, instance.PitchStep);
		}
		if (instance.Color != null)
		{
			stream.WriteKey(38, 2);
			ProtocolParser.WriteString(stream, instance.Color);
		}
	}

	public static int GetSize(Packet_EntityType instance)
	{
		int size = 0;
		if (instance.Code != null)
		{
			size += ProtocolParser.GetSize(instance.Code) + 1;
		}
		if (instance.Class != null)
		{
			size += ProtocolParser.GetSize(instance.Class) + 1;
		}
		if (instance.Tags != null)
		{
			for (int i = 0; i < instance.TagsCount; i++)
			{
				int i2 = instance.Tags[i];
				size += ProtocolParser.GetSize(i2) + 2;
			}
		}
		if (instance.Renderer != null)
		{
			size += ProtocolParser.GetSize(instance.Renderer) + 1;
		}
		if (instance.Habitat != 0)
		{
			size += ProtocolParser.GetSize(instance.Habitat) + 1;
		}
		if (instance.Drops != null)
		{
			size += ProtocolParser.GetSize(instance.Drops) + 2;
		}
		if (instance.Shape != null)
		{
			int packetlength = Packet_CompositeShapeSerializer.GetSize(instance.Shape);
			size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
		}
		if (instance.Behaviors != null)
		{
			for (int j = 0; j < instance.BehaviorsCount; j++)
			{
				int packetlength2 = Packet_BehaviorSerializer.GetSize(instance.Behaviors[j]);
				size += packetlength2 + ProtocolParser.GetSize(packetlength2) + 1;
			}
		}
		if (instance.CollisionBoxLength != 0)
		{
			size += ProtocolParser.GetSize(instance.CollisionBoxLength) + 1;
		}
		if (instance.CollisionBoxHeight != 0)
		{
			size += ProtocolParser.GetSize(instance.CollisionBoxHeight) + 1;
		}
		if (instance.DeadCollisionBoxLength != 0)
		{
			size += ProtocolParser.GetSize(instance.DeadCollisionBoxLength) + 2;
		}
		if (instance.DeadCollisionBoxHeight != 0)
		{
			size += ProtocolParser.GetSize(instance.DeadCollisionBoxHeight) + 2;
		}
		if (instance.SelectionBoxLength != 0)
		{
			size += ProtocolParser.GetSize(instance.SelectionBoxLength) + 2;
		}
		if (instance.SelectionBoxHeight != 0)
		{
			size += ProtocolParser.GetSize(instance.SelectionBoxHeight) + 2;
		}
		if (instance.DeadSelectionBoxLength != 0)
		{
			size += ProtocolParser.GetSize(instance.DeadSelectionBoxLength) + 2;
		}
		if (instance.DeadSelectionBoxHeight != 0)
		{
			size += ProtocolParser.GetSize(instance.DeadSelectionBoxHeight) + 2;
		}
		if (instance.Attributes != null)
		{
			size += ProtocolParser.GetSize(instance.Attributes) + 1;
		}
		if (instance.SoundKeys != null)
		{
			for (int k = 0; k < instance.SoundKeysCount; k++)
			{
				string i3 = instance.SoundKeys[k];
				size += ProtocolParser.GetSize(i3) + 1;
			}
		}
		if (instance.SoundNames != null)
		{
			for (int l = 0; l < instance.SoundNamesCount; l++)
			{
				string i4 = instance.SoundNames[l];
				size += ProtocolParser.GetSize(i4) + 1;
			}
		}
		if (instance.IdleSoundChance != 0)
		{
			size += ProtocolParser.GetSize(instance.IdleSoundChance) + 1;
		}
		if (instance.IdleSoundRange != 0)
		{
			size += ProtocolParser.GetSize(instance.IdleSoundRange) + 2;
		}
		if (instance.TextureCodes != null)
		{
			for (int m = 0; m < instance.TextureCodesCount; m++)
			{
				string i5 = instance.TextureCodes[m];
				size += ProtocolParser.GetSize(i5) + 1;
			}
		}
		if (instance.CompositeTextures != null)
		{
			for (int n = 0; n < instance.CompositeTexturesCount; n++)
			{
				int packetlength3 = Packet_CompositeTextureSerializer.GetSize(instance.CompositeTextures[n]);
				size += packetlength3 + ProtocolParser.GetSize(packetlength3) + 1;
			}
		}
		if (instance.Size != 0)
		{
			size += ProtocolParser.GetSize(instance.Size) + 1;
		}
		if (instance.EyeHeight != 0)
		{
			size += ProtocolParser.GetSize(instance.EyeHeight) + 2;
		}
		if (instance.SwimmingEyeHeight != 0)
		{
			size += ProtocolParser.GetSize(instance.SwimmingEyeHeight) + 2;
		}
		if (instance.Weight != 0)
		{
			size += ProtocolParser.GetSize(instance.Weight) + 2;
		}
		if (instance.CanClimb != 0)
		{
			size += ProtocolParser.GetSize(instance.CanClimb) + 2;
		}
		if (instance.AnimationMetaData != null)
		{
			size += ProtocolParser.GetSize(instance.AnimationMetaData) + 2;
		}
		if (instance.KnockbackResistance != 0)
		{
			size += ProtocolParser.GetSize(instance.KnockbackResistance) + 2;
		}
		if (instance.GlowLevel != 0)
		{
			size += ProtocolParser.GetSize(instance.GlowLevel) + 2;
		}
		if (instance.CanClimbAnywhere != 0)
		{
			size += ProtocolParser.GetSize(instance.CanClimbAnywhere) + 2;
		}
		if (instance.ClimbTouchDistance != 0)
		{
			size += ProtocolParser.GetSize(instance.ClimbTouchDistance) + 2;
		}
		if (instance.RotateModelOnClimb != 0)
		{
			size += ProtocolParser.GetSize(instance.RotateModelOnClimb) + 2;
		}
		if (instance.FallDamage != 0)
		{
			size += ProtocolParser.GetSize(instance.FallDamage) + 2;
		}
		if (instance.FallDamageMultiplier != 0)
		{
			size += ProtocolParser.GetSize(instance.FallDamageMultiplier) + 2;
		}
		if (instance.Variant != null)
		{
			for (int k2 = 0; k2 < instance.VariantCount; k2++)
			{
				int packetlength4 = Packet_VariantPartSerializer.GetSize(instance.Variant[k2]);
				size += packetlength4 + ProtocolParser.GetSize(packetlength4) + 2;
			}
		}
		if (instance.SizeGrowthFactor != 0)
		{
			size += ProtocolParser.GetSize(instance.SizeGrowthFactor) + 2;
		}
		if (instance.PitchStep != 0)
		{
			size += ProtocolParser.GetSize(instance.PitchStep) + 2;
		}
		if (instance.Color != null)
		{
			size += ProtocolParser.GetSize(instance.Color) + 2;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_EntityType instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_EntityTypeSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_EntityType instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_EntityTypeSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityType instance)
	{
		byte[] data = Packet_EntityTypeSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
