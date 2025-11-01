using System;

public class Packet_ItemTypeSerializer
{
	public static Packet_ItemType DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ItemType instance = new Packet_ItemType();
		Packet_ItemTypeSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ItemType DeserializeBuffer(byte[] buffer, int length, Packet_ItemType instance)
	{
		Packet_ItemTypeSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ItemType Deserialize(CitoMemoryStream stream, Packet_ItemType instance)
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
			if (keyInt <= 184)
			{
				if (keyInt <= 90)
				{
					if (keyInt <= 40)
					{
						if (keyInt <= 16)
						{
							if (keyInt == 0)
							{
								goto IL_02E4;
							}
							if (keyInt == 8)
							{
								instance.ItemId = ProtocolParser.ReadUInt32(stream);
								continue;
							}
							if (keyInt == 16)
							{
								instance.MaxStackSize = ProtocolParser.ReadUInt32(stream);
								continue;
							}
						}
						else
						{
							if (keyInt == 26)
							{
								instance.Code = ProtocolParser.ReadString(stream);
								continue;
							}
							if (keyInt == 34)
							{
								instance.CompositeTexturesAdd(Packet_CompositeTextureSerializer.DeserializeLengthDelimitedNew(stream));
								continue;
							}
							if (keyInt == 40)
							{
								instance.Durability = ProtocolParser.ReadUInt32(stream);
								continue;
							}
						}
					}
					else if (keyInt <= 66)
					{
						if (keyInt == 48)
						{
							instance.MiningmaterialAdd(ProtocolParser.ReadUInt32(stream));
							continue;
						}
						if (keyInt == 56)
						{
							instance.DamagedbyAdd(ProtocolParser.ReadUInt32(stream));
							continue;
						}
						if (keyInt == 66)
						{
							instance.CreativeInventoryStacks = ProtocolParser.ReadBytes(stream);
							continue;
						}
					}
					else
					{
						if (keyInt == 74)
						{
							instance.CreativeInventoryTabsAdd(ProtocolParser.ReadString(stream));
							continue;
						}
						if (keyInt != 82)
						{
							if (keyInt == 90)
							{
								if (instance.FpHandTransform == null)
								{
									instance.FpHandTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
									continue;
								}
								Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.FpHandTransform);
								continue;
							}
						}
						else
						{
							if (instance.GuiTransform == null)
							{
								instance.GuiTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
								continue;
							}
							Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.GuiTransform);
							continue;
						}
					}
				}
				else if (keyInt <= 138)
				{
					if (keyInt <= 114)
					{
						if (keyInt != 98)
						{
							if (keyInt == 106)
							{
								instance.Attributes = ProtocolParser.ReadString(stream);
								continue;
							}
							if (keyInt == 114)
							{
								if (instance.CombustibleProps == null)
								{
									instance.CombustibleProps = Packet_CombustiblePropertiesSerializer.DeserializeLengthDelimitedNew(stream);
									continue;
								}
								Packet_CombustiblePropertiesSerializer.DeserializeLengthDelimited(stream, instance.CombustibleProps);
								continue;
							}
						}
						else
						{
							if (instance.TpHandTransform == null)
							{
								instance.TpHandTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
								continue;
							}
							Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.TpHandTransform);
							continue;
						}
					}
					else if (keyInt != 122)
					{
						if (keyInt != 130)
						{
							if (keyInt == 138)
							{
								instance.TextureCodesAdd(ProtocolParser.ReadString(stream));
								continue;
							}
						}
						else
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
						if (instance.NutritionProps == null)
						{
							instance.NutritionProps = Packet_NutritionPropertiesSerializer.DeserializeLengthDelimitedNew(stream);
							continue;
						}
						Packet_NutritionPropertiesSerializer.DeserializeLengthDelimited(stream, instance.NutritionProps);
						continue;
					}
				}
				else if (keyInt <= 160)
				{
					if (keyInt == 146)
					{
						instance.ItemClass = ProtocolParser.ReadString(stream);
						continue;
					}
					if (keyInt == 152)
					{
						instance.Tool = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 160)
					{
						instance.MaterialDensity = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 168)
					{
						instance.AttackPower = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt != 178)
					{
						if (keyInt == 184)
						{
							instance.LiquidSelectable = ProtocolParser.ReadUInt32(stream);
							continue;
						}
					}
					else
					{
						if (instance.GroundTransform == null)
						{
							instance.GroundTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
							continue;
						}
						Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.GroundTransform);
						continue;
					}
				}
			}
			else if (keyInt <= 282)
			{
				if (keyInt <= 234)
				{
					if (keyInt <= 208)
					{
						if (keyInt == 192)
						{
							instance.MiningTier = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 200)
						{
							instance.AttackRange = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 208)
						{
							instance.StorageFlags = ProtocolParser.ReadUInt32(stream);
							continue;
						}
					}
					else
					{
						if (keyInt == 216)
						{
							instance.RenderAlphaTest = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 226)
						{
							instance.HeldTpHitAnimation = ProtocolParser.ReadString(stream);
							continue;
						}
						if (keyInt == 234)
						{
							instance.HeldRightTpIdleAnimation = ProtocolParser.ReadString(stream);
							continue;
						}
					}
				}
				else if (keyInt <= 258)
				{
					if (keyInt == 242)
					{
						instance.HeldTpUseAnimation = ProtocolParser.ReadString(stream);
						continue;
					}
					if (keyInt == 248)
					{
						instance.MiningmaterialspeedAdd(ProtocolParser.ReadUInt32(stream));
						continue;
					}
					if (keyInt == 258)
					{
						if (instance.GrindingProps == null)
						{
							instance.GrindingProps = Packet_GrindingPropertiesSerializer.DeserializeLengthDelimitedNew(stream);
							continue;
						}
						Packet_GrindingPropertiesSerializer.DeserializeLengthDelimited(stream, instance.GrindingProps);
						continue;
					}
				}
				else
				{
					if (keyInt == 264)
					{
						instance.MatterState = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 274)
					{
						instance.HeldLeftTpIdleAnimation = ProtocolParser.ReadString(stream);
						continue;
					}
					if (keyInt == 282)
					{
						instance.VariantAdd(Packet_VariantPartSerializer.DeserializeLengthDelimitedNew(stream));
						continue;
					}
				}
			}
			else if (keyInt <= 328)
			{
				if (keyInt <= 306)
				{
					if (keyInt == 290)
					{
						instance.TransitionablePropsAdd(Packet_TransitionablePropertiesSerializer.DeserializeLengthDelimitedNew(stream));
						continue;
					}
					if (keyInt != 298)
					{
						if (keyInt == 306)
						{
							if (instance.CrushingProps == null)
							{
								instance.CrushingProps = Packet_CrushingPropertiesSerializer.DeserializeLengthDelimitedNew(stream);
								continue;
							}
							Packet_CrushingPropertiesSerializer.DeserializeLengthDelimited(stream, instance.CrushingProps);
							continue;
						}
					}
					else
					{
						if (instance.HeldSounds == null)
						{
							instance.HeldSounds = Packet_HeldSoundSetSerializer.DeserializeLengthDelimitedNew(stream);
							continue;
						}
						Packet_HeldSoundSetSerializer.DeserializeLengthDelimited(stream, instance.HeldSounds);
						continue;
					}
				}
				else
				{
					if (keyInt == 314)
					{
						instance.BehaviorsAdd(Packet_BehaviorSerializer.DeserializeLengthDelimitedNew(stream));
						continue;
					}
					if (keyInt == 320)
					{
						instance.Width = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 328)
					{
						instance.Height = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 352)
			{
				if (keyInt == 336)
				{
					instance.Length = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt != 346)
				{
					if (keyInt == 352)
					{
						instance.LightHsvAdd(ProtocolParser.ReadUInt32(stream));
						continue;
					}
				}
				else
				{
					if (instance.TpOffHandTransform == null)
					{
						instance.TpOffHandTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
						continue;
					}
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.TpOffHandTransform);
					continue;
				}
			}
			else if (keyInt <= 370)
			{
				if (keyInt == 360)
				{
					instance.IsMissing = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 370)
				{
					instance.HeldLeftReadyAnimation = ProtocolParser.ReadString(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 378)
				{
					instance.HeldRightReadyAnimation = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 384)
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
		IL_02E4:
		return null;
	}

	public static Packet_ItemType DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ItemType instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ItemType packet_ItemType = Packet_ItemTypeSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ItemType;
	}

	public static void Serialize(CitoStream stream, Packet_ItemType instance)
	{
		if (instance.ItemId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ItemId);
		}
		if (instance.MaxStackSize != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.MaxStackSize);
		}
		if (instance.Code != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.Code);
		}
		if (instance.Tags != null)
		{
			int[] elems = instance.Tags;
			int elemCount = instance.TagsCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteKey(48, 0);
				ProtocolParser.WriteUInt32(stream, elems[i]);
				i++;
			}
		}
		if (instance.Behaviors != null)
		{
			Packet_Behavior[] elems2 = instance.Behaviors;
			int elemCount2 = instance.BehaviorsCount;
			int j = 0;
			while (j < elems2.Length && j < elemCount2)
			{
				stream.WriteKey(39, 2);
				Packet_BehaviorSerializer.SerializeWithSize(stream, elems2[j]);
				j++;
			}
		}
		if (instance.CompositeTextures != null)
		{
			Packet_CompositeTexture[] elems3 = instance.CompositeTextures;
			int elemCount3 = instance.CompositeTexturesCount;
			int k = 0;
			while (k < elems3.Length && k < elemCount3)
			{
				stream.WriteByte(34);
				Packet_CompositeTextureSerializer.SerializeWithSize(stream, elems3[k]);
				k++;
			}
		}
		if (instance.Durability != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Durability);
		}
		if (instance.Miningmaterial != null)
		{
			int[] elems4 = instance.Miningmaterial;
			int elemCount4 = instance.MiningmaterialCount;
			int l = 0;
			while (l < elems4.Length && l < elemCount4)
			{
				stream.WriteByte(48);
				ProtocolParser.WriteUInt32(stream, elems4[l]);
				l++;
			}
		}
		if (instance.Miningmaterialspeed != null)
		{
			int[] elems5 = instance.Miningmaterialspeed;
			int elemCount5 = instance.MiningmaterialspeedCount;
			int m = 0;
			while (m < elems5.Length && m < elemCount5)
			{
				stream.WriteKey(31, 0);
				ProtocolParser.WriteUInt32(stream, elems5[m]);
				m++;
			}
		}
		if (instance.Damagedby != null)
		{
			int[] elems6 = instance.Damagedby;
			int elemCount6 = instance.DamagedbyCount;
			int n = 0;
			while (n < elems6.Length && n < elemCount6)
			{
				stream.WriteByte(56);
				ProtocolParser.WriteUInt32(stream, elems6[n]);
				n++;
			}
		}
		if (instance.CreativeInventoryStacks != null)
		{
			stream.WriteByte(66);
			ProtocolParser.WriteBytes(stream, instance.CreativeInventoryStacks);
		}
		if (instance.CreativeInventoryTabs != null)
		{
			string[] elems7 = instance.CreativeInventoryTabs;
			int elemCount7 = instance.CreativeInventoryTabsCount;
			int k2 = 0;
			while (k2 < elems7.Length && k2 < elemCount7)
			{
				stream.WriteByte(74);
				ProtocolParser.WriteString(stream, elems7[k2]);
				k2++;
			}
		}
		if (instance.GuiTransform != null)
		{
			stream.WriteByte(82);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.GuiTransform);
		}
		if (instance.FpHandTransform != null)
		{
			stream.WriteByte(90);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.FpHandTransform);
		}
		if (instance.TpHandTransform != null)
		{
			stream.WriteByte(98);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.TpHandTransform);
		}
		if (instance.TpOffHandTransform != null)
		{
			stream.WriteKey(43, 2);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.TpOffHandTransform);
		}
		if (instance.GroundTransform != null)
		{
			stream.WriteKey(22, 2);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.GroundTransform);
		}
		if (instance.Attributes != null)
		{
			stream.WriteByte(106);
			ProtocolParser.WriteString(stream, instance.Attributes);
		}
		if (instance.CombustibleProps != null)
		{
			stream.WriteByte(114);
			Packet_CombustiblePropertiesSerializer.SerializeWithSize(stream, instance.CombustibleProps);
		}
		if (instance.NutritionProps != null)
		{
			stream.WriteByte(122);
			Packet_NutritionPropertiesSerializer.SerializeWithSize(stream, instance.NutritionProps);
		}
		if (instance.GrindingProps != null)
		{
			stream.WriteKey(32, 2);
			Packet_GrindingPropertiesSerializer.SerializeWithSize(stream, instance.GrindingProps);
		}
		if (instance.CrushingProps != null)
		{
			stream.WriteKey(38, 2);
			Packet_CrushingPropertiesSerializer.SerializeWithSize(stream, instance.CrushingProps);
		}
		if (instance.TransitionableProps != null)
		{
			Packet_TransitionableProperties[] elems8 = instance.TransitionableProps;
			int elemCount8 = instance.TransitionablePropsCount;
			int k3 = 0;
			while (k3 < elems8.Length && k3 < elemCount8)
			{
				stream.WriteKey(36, 2);
				Packet_TransitionablePropertiesSerializer.SerializeWithSize(stream, elems8[k3]);
				k3++;
			}
		}
		if (instance.Shape != null)
		{
			stream.WriteKey(16, 2);
			Packet_CompositeShapeSerializer.SerializeWithSize(stream, instance.Shape);
		}
		if (instance.TextureCodes != null)
		{
			string[] elems9 = instance.TextureCodes;
			int elemCount9 = instance.TextureCodesCount;
			int k4 = 0;
			while (k4 < elems9.Length && k4 < elemCount9)
			{
				stream.WriteKey(17, 2);
				ProtocolParser.WriteString(stream, elems9[k4]);
				k4++;
			}
		}
		if (instance.ItemClass != null)
		{
			stream.WriteKey(18, 2);
			ProtocolParser.WriteString(stream, instance.ItemClass);
		}
		if (instance.Tool != 0)
		{
			stream.WriteKey(19, 0);
			ProtocolParser.WriteUInt32(stream, instance.Tool);
		}
		if (instance.MaterialDensity != 0)
		{
			stream.WriteKey(20, 0);
			ProtocolParser.WriteUInt32(stream, instance.MaterialDensity);
		}
		if (instance.AttackPower != 0)
		{
			stream.WriteKey(21, 0);
			ProtocolParser.WriteUInt32(stream, instance.AttackPower);
		}
		if (instance.AttackRange != 0)
		{
			stream.WriteKey(25, 0);
			ProtocolParser.WriteUInt32(stream, instance.AttackRange);
		}
		if (instance.LiquidSelectable != 0)
		{
			stream.WriteKey(23, 0);
			ProtocolParser.WriteUInt32(stream, instance.LiquidSelectable);
		}
		if (instance.MiningTier != 0)
		{
			stream.WriteKey(24, 0);
			ProtocolParser.WriteUInt32(stream, instance.MiningTier);
		}
		if (instance.StorageFlags != 0)
		{
			stream.WriteKey(26, 0);
			ProtocolParser.WriteUInt32(stream, instance.StorageFlags);
		}
		if (instance.RenderAlphaTest != 0)
		{
			stream.WriteKey(27, 0);
			ProtocolParser.WriteUInt32(stream, instance.RenderAlphaTest);
		}
		if (instance.HeldTpHitAnimation != null)
		{
			stream.WriteKey(28, 2);
			ProtocolParser.WriteString(stream, instance.HeldTpHitAnimation);
		}
		if (instance.HeldRightTpIdleAnimation != null)
		{
			stream.WriteKey(29, 2);
			ProtocolParser.WriteString(stream, instance.HeldRightTpIdleAnimation);
		}
		if (instance.HeldLeftTpIdleAnimation != null)
		{
			stream.WriteKey(34, 2);
			ProtocolParser.WriteString(stream, instance.HeldLeftTpIdleAnimation);
		}
		if (instance.HeldTpUseAnimation != null)
		{
			stream.WriteKey(30, 2);
			ProtocolParser.WriteString(stream, instance.HeldTpUseAnimation);
		}
		if (instance.MatterState != 0)
		{
			stream.WriteKey(33, 0);
			ProtocolParser.WriteUInt32(stream, instance.MatterState);
		}
		if (instance.Variant != null)
		{
			Packet_VariantPart[] elems10 = instance.Variant;
			int elemCount10 = instance.VariantCount;
			int k5 = 0;
			while (k5 < elems10.Length && k5 < elemCount10)
			{
				stream.WriteKey(35, 2);
				Packet_VariantPartSerializer.SerializeWithSize(stream, elems10[k5]);
				k5++;
			}
		}
		if (instance.HeldSounds != null)
		{
			stream.WriteKey(37, 2);
			Packet_HeldSoundSetSerializer.SerializeWithSize(stream, instance.HeldSounds);
		}
		if (instance.Width != 0)
		{
			stream.WriteKey(40, 0);
			ProtocolParser.WriteUInt32(stream, instance.Width);
		}
		if (instance.Height != 0)
		{
			stream.WriteKey(41, 0);
			ProtocolParser.WriteUInt32(stream, instance.Height);
		}
		if (instance.Length != 0)
		{
			stream.WriteKey(42, 0);
			ProtocolParser.WriteUInt32(stream, instance.Length);
		}
		if (instance.LightHsv != null)
		{
			int[] elems11 = instance.LightHsv;
			int elemCount11 = instance.LightHsvCount;
			int k6 = 0;
			while (k6 < elems11.Length && k6 < elemCount11)
			{
				stream.WriteKey(44, 0);
				ProtocolParser.WriteUInt32(stream, elems11[k6]);
				k6++;
			}
		}
		if (instance.IsMissing != 0)
		{
			stream.WriteKey(45, 0);
			ProtocolParser.WriteUInt32(stream, instance.IsMissing);
		}
		if (instance.HeldLeftReadyAnimation != null)
		{
			stream.WriteKey(46, 2);
			ProtocolParser.WriteString(stream, instance.HeldLeftReadyAnimation);
		}
		if (instance.HeldRightReadyAnimation != null)
		{
			stream.WriteKey(47, 2);
			ProtocolParser.WriteString(stream, instance.HeldRightReadyAnimation);
		}
	}

	public static int GetSize(Packet_ItemType instance)
	{
		int size = 0;
		if (instance.ItemId != 0)
		{
			size += ProtocolParser.GetSize(instance.ItemId) + 1;
		}
		if (instance.MaxStackSize != 0)
		{
			size += ProtocolParser.GetSize(instance.MaxStackSize) + 1;
		}
		if (instance.Code != null)
		{
			size += ProtocolParser.GetSize(instance.Code) + 1;
		}
		if (instance.Tags != null)
		{
			for (int i = 0; i < instance.TagsCount; i++)
			{
				int i2 = instance.Tags[i];
				size += ProtocolParser.GetSize(i2) + 2;
			}
		}
		if (instance.Behaviors != null)
		{
			for (int j = 0; j < instance.BehaviorsCount; j++)
			{
				int packetlength = Packet_BehaviorSerializer.GetSize(instance.Behaviors[j]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 2;
			}
		}
		if (instance.CompositeTextures != null)
		{
			for (int k = 0; k < instance.CompositeTexturesCount; k++)
			{
				int packetlength2 = Packet_CompositeTextureSerializer.GetSize(instance.CompositeTextures[k]);
				size += packetlength2 + ProtocolParser.GetSize(packetlength2) + 1;
			}
		}
		if (instance.Durability != 0)
		{
			size += ProtocolParser.GetSize(instance.Durability) + 1;
		}
		if (instance.Miningmaterial != null)
		{
			for (int l = 0; l < instance.MiningmaterialCount; l++)
			{
				int i3 = instance.Miningmaterial[l];
				size += ProtocolParser.GetSize(i3) + 1;
			}
		}
		if (instance.Miningmaterialspeed != null)
		{
			for (int m = 0; m < instance.MiningmaterialspeedCount; m++)
			{
				int i4 = instance.Miningmaterialspeed[m];
				size += ProtocolParser.GetSize(i4) + 2;
			}
		}
		if (instance.Damagedby != null)
		{
			for (int n = 0; n < instance.DamagedbyCount; n++)
			{
				int i5 = instance.Damagedby[n];
				size += ProtocolParser.GetSize(i5) + 1;
			}
		}
		if (instance.CreativeInventoryStacks != null)
		{
			size += ProtocolParser.GetSize(instance.CreativeInventoryStacks) + 1;
		}
		if (instance.CreativeInventoryTabs != null)
		{
			for (int k2 = 0; k2 < instance.CreativeInventoryTabsCount; k2++)
			{
				string i6 = instance.CreativeInventoryTabs[k2];
				size += ProtocolParser.GetSize(i6) + 1;
			}
		}
		if (instance.GuiTransform != null)
		{
			int packetlength3 = Packet_ModelTransformSerializer.GetSize(instance.GuiTransform);
			size += packetlength3 + ProtocolParser.GetSize(packetlength3) + 1;
		}
		if (instance.FpHandTransform != null)
		{
			int packetlength4 = Packet_ModelTransformSerializer.GetSize(instance.FpHandTransform);
			size += packetlength4 + ProtocolParser.GetSize(packetlength4) + 1;
		}
		if (instance.TpHandTransform != null)
		{
			int packetlength5 = Packet_ModelTransformSerializer.GetSize(instance.TpHandTransform);
			size += packetlength5 + ProtocolParser.GetSize(packetlength5) + 1;
		}
		if (instance.TpOffHandTransform != null)
		{
			int packetlength6 = Packet_ModelTransformSerializer.GetSize(instance.TpOffHandTransform);
			size += packetlength6 + ProtocolParser.GetSize(packetlength6) + 2;
		}
		if (instance.GroundTransform != null)
		{
			int packetlength7 = Packet_ModelTransformSerializer.GetSize(instance.GroundTransform);
			size += packetlength7 + ProtocolParser.GetSize(packetlength7) + 2;
		}
		if (instance.Attributes != null)
		{
			size += ProtocolParser.GetSize(instance.Attributes) + 1;
		}
		if (instance.CombustibleProps != null)
		{
			int packetlength8 = Packet_CombustiblePropertiesSerializer.GetSize(instance.CombustibleProps);
			size += packetlength8 + ProtocolParser.GetSize(packetlength8) + 1;
		}
		if (instance.NutritionProps != null)
		{
			int packetlength9 = Packet_NutritionPropertiesSerializer.GetSize(instance.NutritionProps);
			size += packetlength9 + ProtocolParser.GetSize(packetlength9) + 1;
		}
		if (instance.GrindingProps != null)
		{
			int packetlength10 = Packet_GrindingPropertiesSerializer.GetSize(instance.GrindingProps);
			size += packetlength10 + ProtocolParser.GetSize(packetlength10) + 2;
		}
		if (instance.CrushingProps != null)
		{
			int packetlength11 = Packet_CrushingPropertiesSerializer.GetSize(instance.CrushingProps);
			size += packetlength11 + ProtocolParser.GetSize(packetlength11) + 2;
		}
		if (instance.TransitionableProps != null)
		{
			for (int k3 = 0; k3 < instance.TransitionablePropsCount; k3++)
			{
				int packetlength12 = Packet_TransitionablePropertiesSerializer.GetSize(instance.TransitionableProps[k3]);
				size += packetlength12 + ProtocolParser.GetSize(packetlength12) + 2;
			}
		}
		if (instance.Shape != null)
		{
			int packetlength13 = Packet_CompositeShapeSerializer.GetSize(instance.Shape);
			size += packetlength13 + ProtocolParser.GetSize(packetlength13) + 2;
		}
		if (instance.TextureCodes != null)
		{
			for (int k4 = 0; k4 < instance.TextureCodesCount; k4++)
			{
				string i7 = instance.TextureCodes[k4];
				size += ProtocolParser.GetSize(i7) + 2;
			}
		}
		if (instance.ItemClass != null)
		{
			size += ProtocolParser.GetSize(instance.ItemClass) + 2;
		}
		if (instance.Tool != 0)
		{
			size += ProtocolParser.GetSize(instance.Tool) + 2;
		}
		if (instance.MaterialDensity != 0)
		{
			size += ProtocolParser.GetSize(instance.MaterialDensity) + 2;
		}
		if (instance.AttackPower != 0)
		{
			size += ProtocolParser.GetSize(instance.AttackPower) + 2;
		}
		if (instance.AttackRange != 0)
		{
			size += ProtocolParser.GetSize(instance.AttackRange) + 2;
		}
		if (instance.LiquidSelectable != 0)
		{
			size += ProtocolParser.GetSize(instance.LiquidSelectable) + 2;
		}
		if (instance.MiningTier != 0)
		{
			size += ProtocolParser.GetSize(instance.MiningTier) + 2;
		}
		if (instance.StorageFlags != 0)
		{
			size += ProtocolParser.GetSize(instance.StorageFlags) + 2;
		}
		if (instance.RenderAlphaTest != 0)
		{
			size += ProtocolParser.GetSize(instance.RenderAlphaTest) + 2;
		}
		if (instance.HeldTpHitAnimation != null)
		{
			size += ProtocolParser.GetSize(instance.HeldTpHitAnimation) + 2;
		}
		if (instance.HeldRightTpIdleAnimation != null)
		{
			size += ProtocolParser.GetSize(instance.HeldRightTpIdleAnimation) + 2;
		}
		if (instance.HeldLeftTpIdleAnimation != null)
		{
			size += ProtocolParser.GetSize(instance.HeldLeftTpIdleAnimation) + 2;
		}
		if (instance.HeldTpUseAnimation != null)
		{
			size += ProtocolParser.GetSize(instance.HeldTpUseAnimation) + 2;
		}
		if (instance.MatterState != 0)
		{
			size += ProtocolParser.GetSize(instance.MatterState) + 2;
		}
		if (instance.Variant != null)
		{
			for (int k5 = 0; k5 < instance.VariantCount; k5++)
			{
				int packetlength14 = Packet_VariantPartSerializer.GetSize(instance.Variant[k5]);
				size += packetlength14 + ProtocolParser.GetSize(packetlength14) + 2;
			}
		}
		if (instance.HeldSounds != null)
		{
			int packetlength15 = Packet_HeldSoundSetSerializer.GetSize(instance.HeldSounds);
			size += packetlength15 + ProtocolParser.GetSize(packetlength15) + 2;
		}
		if (instance.Width != 0)
		{
			size += ProtocolParser.GetSize(instance.Width) + 2;
		}
		if (instance.Height != 0)
		{
			size += ProtocolParser.GetSize(instance.Height) + 2;
		}
		if (instance.Length != 0)
		{
			size += ProtocolParser.GetSize(instance.Length) + 2;
		}
		if (instance.LightHsv != null)
		{
			for (int k6 = 0; k6 < instance.LightHsvCount; k6++)
			{
				int i8 = instance.LightHsv[k6];
				size += ProtocolParser.GetSize(i8) + 2;
			}
		}
		if (instance.IsMissing != 0)
		{
			size += ProtocolParser.GetSize(instance.IsMissing) + 2;
		}
		if (instance.HeldLeftReadyAnimation != null)
		{
			size += ProtocolParser.GetSize(instance.HeldLeftReadyAnimation) + 2;
		}
		if (instance.HeldRightReadyAnimation != null)
		{
			size += ProtocolParser.GetSize(instance.HeldRightReadyAnimation) + 2;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ItemType instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ItemTypeSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ItemType instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ItemTypeSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ItemType instance)
	{
		byte[] data = Packet_ItemTypeSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
