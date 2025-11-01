using System;

public class Packet_BlockTypeSerializer
{
	public static Packet_BlockType DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlockType instance = new Packet_BlockType();
		Packet_BlockTypeSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_BlockType DeserializeBuffer(byte[] buffer, int length, Packet_BlockType instance)
	{
		Packet_BlockTypeSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlockType Deserialize(CitoMemoryStream stream, Packet_BlockType instance)
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
			if (keyInt <= 442)
			{
				if (keyInt <= 232)
				{
					if (keyInt <= 88)
					{
						if (keyInt <= 40)
						{
							if (keyInt <= 18)
							{
								if (keyInt == 0)
								{
									goto IL_05B9;
								}
								if (keyInt == 10)
								{
									instance.TextureCodesAdd(ProtocolParser.ReadString(stream));
									continue;
								}
								if (keyInt == 18)
								{
									instance.CompositeTexturesAdd(Packet_CompositeTextureSerializer.DeserializeLengthDelimitedNew(stream));
									continue;
								}
							}
							else
							{
								if (keyInt == 26)
								{
									instance.InventoryTextureCodesAdd(ProtocolParser.ReadString(stream));
									continue;
								}
								if (keyInt == 34)
								{
									instance.InventoryCompositeTexturesAdd(Packet_CompositeTextureSerializer.DeserializeLengthDelimitedNew(stream));
									continue;
								}
								if (keyInt == 40)
								{
									instance.BlockId = ProtocolParser.ReadUInt32(stream);
									continue;
								}
							}
						}
						else if (keyInt <= 64)
						{
							if (keyInt == 50)
							{
								instance.Code = ProtocolParser.ReadString(stream);
								continue;
							}
							if (keyInt == 58)
							{
								instance.BehaviorsAdd(Packet_BehaviorSerializer.DeserializeLengthDelimitedNew(stream));
								continue;
							}
							if (keyInt == 64)
							{
								instance.RenderPass = ProtocolParser.ReadUInt32(stream);
								continue;
							}
						}
						else
						{
							if (keyInt == 72)
							{
								instance.DrawType = ProtocolParser.ReadUInt32(stream);
								continue;
							}
							if (keyInt == 80)
							{
								instance.MatterState = ProtocolParser.ReadUInt32(stream);
								continue;
							}
							if (keyInt == 88)
							{
								instance.WalkSpeedFloat = ProtocolParser.ReadUInt32(stream);
								continue;
							}
						}
					}
					else if (keyInt <= 138)
					{
						if (keyInt <= 112)
						{
							if (keyInt == 96)
							{
								instance.IsSlipperyWalk = ProtocolParser.ReadBool(stream);
								continue;
							}
							if (keyInt != 106)
							{
								if (keyInt == 112)
								{
									instance.LightHsvAdd(ProtocolParser.ReadUInt32(stream));
									continue;
								}
							}
							else
							{
								if (instance.Sounds == null)
								{
									instance.Sounds = Packet_BlockSoundSetSerializer.DeserializeLengthDelimitedNew(stream);
									continue;
								}
								Packet_BlockSoundSetSerializer.DeserializeLengthDelimited(stream, instance.Sounds);
								continue;
							}
						}
						else
						{
							if (keyInt == 120)
							{
								instance.Climbable = ProtocolParser.ReadUInt32(stream);
								continue;
							}
							if (keyInt == 130)
							{
								instance.CreativeInventoryTabsAdd(ProtocolParser.ReadString(stream));
								continue;
							}
							if (keyInt == 138)
							{
								instance.CreativeInventoryStacks = ProtocolParser.ReadBytes(stream);
								continue;
							}
						}
					}
					else if (keyInt <= 202)
					{
						if (keyInt == 184)
						{
							instance.FaceCullMode = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 192)
						{
							instance.SideOpaqueFlagsAdd(ProtocolParser.ReadUInt32(stream));
							continue;
						}
						if (keyInt == 202)
						{
							instance.SeasonColorMap = ProtocolParser.ReadString(stream);
							continue;
						}
					}
					else
					{
						if (keyInt == 208)
						{
							instance.CullFaces = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 216)
						{
							instance.Replacable = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 232)
						{
							instance.LightAbsorption = ProtocolParser.ReadUInt32(stream);
							continue;
						}
					}
				}
				else if (keyInt <= 346)
				{
					if (keyInt <= 282)
					{
						if (keyInt <= 256)
						{
							if (keyInt == 240)
							{
								instance.HardnessLevel = ProtocolParser.ReadUInt32(stream);
								continue;
							}
							if (keyInt == 248)
							{
								instance.Resistance = ProtocolParser.ReadUInt32(stream);
								continue;
							}
							if (keyInt == 256)
							{
								instance.BlockMaterial = ProtocolParser.ReadUInt32(stream);
								continue;
							}
						}
						else
						{
							if (keyInt == 266)
							{
								instance.Moddata = ProtocolParser.ReadBytes(stream);
								continue;
							}
							if (keyInt != 274)
							{
								if (keyInt == 282)
								{
									if (instance.ShapeInventory == null)
									{
										instance.ShapeInventory = Packet_CompositeShapeSerializer.DeserializeLengthDelimitedNew(stream);
										continue;
									}
									Packet_CompositeShapeSerializer.DeserializeLengthDelimited(stream, instance.ShapeInventory);
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
					}
					else if (keyInt <= 322)
					{
						if (keyInt == 304)
						{
							instance.Ambientocclusion = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 314)
						{
							instance.CollisionBoxesAdd(Packet_CubeSerializer.DeserializeLengthDelimitedNew(stream));
							continue;
						}
						if (keyInt == 322)
						{
							instance.SelectionBoxesAdd(Packet_CubeSerializer.DeserializeLengthDelimitedNew(stream));
							continue;
						}
					}
					else
					{
						if (keyInt == 330)
						{
							instance.Blockclass = ProtocolParser.ReadString(stream);
							continue;
						}
						if (keyInt != 338)
						{
							if (keyInt == 346)
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
				else if (keyInt <= 392)
				{
					if (keyInt <= 368)
					{
						if (keyInt != 354)
						{
							if (keyInt != 362)
							{
								if (keyInt == 368)
								{
									instance.SideSolidFlagsAdd(ProtocolParser.ReadUInt32(stream));
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
					else
					{
						if (keyInt == 376)
						{
							instance.Fertility = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 386)
						{
							instance.ParticleProperties = ProtocolParser.ReadBytes(stream);
							continue;
						}
						if (keyInt == 392)
						{
							instance.ParticlePropertiesQuantity = ProtocolParser.ReadUInt32(stream);
							continue;
						}
					}
				}
				else if (keyInt <= 418)
				{
					if (keyInt == 400)
					{
						instance.RandomDrawOffset = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 408)
					{
						instance.VertexFlags = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 418)
					{
						instance.DropsAdd(Packet_BlockDropSerializer.DeserializeLengthDelimitedNew(stream));
						continue;
					}
				}
				else
				{
					if (keyInt == 424)
					{
						instance.LiquidLevel = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 434)
					{
						instance.Attributes = ProtocolParser.ReadString(stream);
						continue;
					}
					if (keyInt == 442)
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
			}
			else if (keyInt <= 642)
			{
				if (keyInt <= 544)
				{
					if (keyInt <= 496)
					{
						if (keyInt <= 474)
						{
							if (keyInt == 456)
							{
								instance.SideAoAdd(ProtocolParser.ReadUInt32(stream));
								continue;
							}
							if (keyInt == 466)
							{
								instance.EntityClass = ProtocolParser.ReadString(stream);
								continue;
							}
							if (keyInt == 474)
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
						else
						{
							if (keyInt == 480)
							{
								instance.MaxStackSize = ProtocolParser.ReadUInt32(stream);
								continue;
							}
							if (keyInt == 490)
							{
								instance.CropProps = ProtocolParser.ReadBytes(stream);
								continue;
							}
							if (keyInt == 496)
							{
								instance.MaterialDensity = ProtocolParser.ReadUInt32(stream);
								continue;
							}
						}
					}
					else if (keyInt <= 520)
					{
						if (keyInt == 504)
						{
							instance.AttackPower = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 512)
						{
							instance.LiquidSelectable = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 520)
						{
							instance.MiningTier = ProtocolParser.ReadUInt32(stream);
							continue;
						}
					}
					else
					{
						if (keyInt == 528)
						{
							instance.RequiredMiningTier = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 536)
						{
							instance.MiningmaterialAdd(ProtocolParser.ReadUInt32(stream));
							continue;
						}
						if (keyInt == 544)
						{
							instance.DragMultiplierFloat = ProtocolParser.ReadUInt32(stream);
							continue;
						}
					}
				}
				else if (keyInt <= 594)
				{
					if (keyInt <= 568)
					{
						if (keyInt == 552)
						{
							instance.RandomizeAxes = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 560)
						{
							instance.AttackRange = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 568)
						{
							instance.StorageFlags = ProtocolParser.ReadUInt32(stream);
							continue;
						}
					}
					else
					{
						if (keyInt == 576)
						{
							instance.RenderAlphaTest = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 586)
						{
							instance.HeldTpHitAnimation = ProtocolParser.ReadString(stream);
							continue;
						}
						if (keyInt == 594)
						{
							instance.HeldRightTpIdleAnimation = ProtocolParser.ReadString(stream);
							continue;
						}
					}
				}
				else if (keyInt <= 618)
				{
					if (keyInt == 602)
					{
						instance.HeldTpUseAnimation = ProtocolParser.ReadString(stream);
						continue;
					}
					if (keyInt == 608)
					{
						instance.MiningmaterialspeedAdd(ProtocolParser.ReadUInt32(stream));
						continue;
					}
					if (keyInt == 618)
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
					if (keyInt == 624)
					{
						instance.RainPermeable = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 632)
					{
						instance.NeighbourSideAo = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 642)
					{
						instance.HeldLeftTpIdleAnimation = ProtocolParser.ReadString(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 738)
			{
				if (keyInt <= 690)
				{
					if (keyInt <= 666)
					{
						if (keyInt == 650)
						{
							instance.LiquidCode = ProtocolParser.ReadString(stream);
							continue;
						}
						if (keyInt == 658)
						{
							instance.VariantAdd(Packet_VariantPartSerializer.DeserializeLengthDelimitedNew(stream));
							continue;
						}
						if (keyInt == 666)
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
						if (keyInt == 674)
						{
							instance.EntityBehaviors = ProtocolParser.ReadString(stream);
							continue;
						}
						if (keyInt == 682)
						{
							instance.TransitionablePropsAdd(Packet_TransitionablePropertiesSerializer.DeserializeLengthDelimitedNew(stream));
							continue;
						}
						if (keyInt == 690)
						{
							if (instance.Lod0shape == null)
							{
								instance.Lod0shape = Packet_CompositeShapeSerializer.DeserializeLengthDelimitedNew(stream);
								continue;
							}
							Packet_CompositeShapeSerializer.DeserializeLengthDelimited(stream, instance.Lod0shape);
							continue;
						}
					}
				}
				else if (keyInt <= 712)
				{
					if (keyInt == 696)
					{
						instance.RandomizeRotations = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 706)
					{
						instance.ClimateColorMap = ProtocolParser.ReadString(stream);
						continue;
					}
					if (keyInt == 712)
					{
						instance.Frostable = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 722)
					{
						instance.CropPropBehaviorsAdd(ProtocolParser.ReadString(stream));
						continue;
					}
					if (keyInt == 730)
					{
						instance.ParticleCollisionBoxesAdd(Packet_CubeSerializer.DeserializeLengthDelimitedNew(stream));
						continue;
					}
					if (keyInt == 738)
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
			}
			else if (keyInt <= 784)
			{
				if (keyInt <= 760)
				{
					if (keyInt == 744)
					{
						instance.RandomSizeAdjust = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt != 754)
					{
						if (keyInt == 760)
						{
							instance.DoNotRenderAtLod2 = ProtocolParser.ReadUInt32(stream);
							continue;
						}
					}
					else
					{
						if (instance.Lod2shape == null)
						{
							instance.Lod2shape = Packet_CompositeShapeSerializer.DeserializeLengthDelimitedNew(stream);
							continue;
						}
						Packet_CompositeShapeSerializer.DeserializeLengthDelimited(stream, instance.Lod2shape);
						continue;
					}
				}
				else
				{
					if (keyInt == 768)
					{
						instance.Width = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 776)
					{
						instance.Height = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 784)
					{
						instance.Length = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 808)
			{
				if (keyInt != 794)
				{
					if (keyInt == 800)
					{
						instance.IsMissing = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 808)
					{
						instance.Durability = ProtocolParser.ReadUInt32(stream);
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
			else
			{
				if (keyInt == 818)
				{
					instance.HeldLeftReadyAnimation = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 826)
				{
					instance.HeldRightReadyAnimation = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 832)
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
		IL_05B9:
		return null;
	}

	public static Packet_BlockType DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlockType instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_BlockType packet_BlockType = Packet_BlockTypeSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_BlockType;
	}

	public static void Serialize(CitoStream stream, Packet_BlockType instance)
	{
		if (instance.TextureCodes != null)
		{
			string[] elems = instance.TextureCodes;
			int elemCount = instance.TextureCodesCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(10);
				ProtocolParser.WriteString(stream, elems[i]);
				i++;
			}
		}
		if (instance.CompositeTextures != null)
		{
			Packet_CompositeTexture[] elems2 = instance.CompositeTextures;
			int elemCount2 = instance.CompositeTexturesCount;
			int j = 0;
			while (j < elems2.Length && j < elemCount2)
			{
				stream.WriteByte(18);
				Packet_CompositeTextureSerializer.SerializeWithSize(stream, elems2[j]);
				j++;
			}
		}
		if (instance.InventoryTextureCodes != null)
		{
			string[] elems3 = instance.InventoryTextureCodes;
			int elemCount3 = instance.InventoryTextureCodesCount;
			int k = 0;
			while (k < elems3.Length && k < elemCount3)
			{
				stream.WriteByte(26);
				ProtocolParser.WriteString(stream, elems3[k]);
				k++;
			}
		}
		if (instance.InventoryCompositeTextures != null)
		{
			Packet_CompositeTexture[] elems4 = instance.InventoryCompositeTextures;
			int elemCount4 = instance.InventoryCompositeTexturesCount;
			int l = 0;
			while (l < elems4.Length && l < elemCount4)
			{
				stream.WriteByte(34);
				Packet_CompositeTextureSerializer.SerializeWithSize(stream, elems4[l]);
				l++;
			}
		}
		if (instance.BlockId != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.BlockId);
		}
		if (instance.Code != null)
		{
			stream.WriteByte(50);
			ProtocolParser.WriteString(stream, instance.Code);
		}
		if (instance.EntityClass != null)
		{
			stream.WriteKey(58, 2);
			ProtocolParser.WriteString(stream, instance.EntityClass);
		}
		if (instance.Tags != null)
		{
			int[] elems5 = instance.Tags;
			int elemCount5 = instance.TagsCount;
			int m = 0;
			while (m < elems5.Length && m < elemCount5)
			{
				stream.WriteKey(104, 0);
				ProtocolParser.WriteUInt32(stream, elems5[m]);
				m++;
			}
		}
		if (instance.Behaviors != null)
		{
			Packet_Behavior[] elems6 = instance.Behaviors;
			int elemCount6 = instance.BehaviorsCount;
			int n = 0;
			while (n < elems6.Length && n < elemCount6)
			{
				stream.WriteByte(58);
				Packet_BehaviorSerializer.SerializeWithSize(stream, elems6[n]);
				n++;
			}
		}
		if (instance.EntityBehaviors != null)
		{
			stream.WriteKey(84, 2);
			ProtocolParser.WriteString(stream, instance.EntityBehaviors);
		}
		if (instance.RenderPass != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.RenderPass);
		}
		if (instance.DrawType != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.DrawType);
		}
		if (instance.MatterState != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.MatterState);
		}
		if (instance.WalkSpeedFloat != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.WalkSpeedFloat);
		}
		if (instance.IsSlipperyWalk)
		{
			stream.WriteByte(96);
			ProtocolParser.WriteBool(stream, instance.IsSlipperyWalk);
		}
		if (instance.Sounds != null)
		{
			stream.WriteByte(106);
			Packet_BlockSoundSetSerializer.SerializeWithSize(stream, instance.Sounds);
		}
		if (instance.HeldSounds != null)
		{
			stream.WriteKey(83, 2);
			Packet_HeldSoundSetSerializer.SerializeWithSize(stream, instance.HeldSounds);
		}
		if (instance.LightHsv != null)
		{
			int[] elems7 = instance.LightHsv;
			int elemCount7 = instance.LightHsvCount;
			int k2 = 0;
			while (k2 < elems7.Length && k2 < elemCount7)
			{
				stream.WriteByte(112);
				ProtocolParser.WriteUInt32(stream, elems7[k2]);
				k2++;
			}
		}
		if (instance.VertexFlags != 0)
		{
			stream.WriteKey(51, 0);
			ProtocolParser.WriteUInt32(stream, instance.VertexFlags);
		}
		if (instance.Climbable != 0)
		{
			stream.WriteByte(120);
			ProtocolParser.WriteUInt32(stream, instance.Climbable);
		}
		if (instance.CreativeInventoryTabs != null)
		{
			string[] elems8 = instance.CreativeInventoryTabs;
			int elemCount8 = instance.CreativeInventoryTabsCount;
			int k3 = 0;
			while (k3 < elems8.Length && k3 < elemCount8)
			{
				stream.WriteKey(16, 2);
				ProtocolParser.WriteString(stream, elems8[k3]);
				k3++;
			}
		}
		if (instance.CreativeInventoryStacks != null)
		{
			stream.WriteKey(17, 2);
			ProtocolParser.WriteBytes(stream, instance.CreativeInventoryStacks);
		}
		if (instance.SideOpaqueFlags != null)
		{
			int[] elems9 = instance.SideOpaqueFlags;
			int elemCount9 = instance.SideOpaqueFlagsCount;
			int k4 = 0;
			while (k4 < elems9.Length && k4 < elemCount9)
			{
				stream.WriteKey(24, 0);
				ProtocolParser.WriteUInt32(stream, elems9[k4]);
				k4++;
			}
		}
		if (instance.FaceCullMode != 0)
		{
			stream.WriteKey(23, 0);
			ProtocolParser.WriteUInt32(stream, instance.FaceCullMode);
		}
		if (instance.SideSolidFlags != null)
		{
			int[] elems10 = instance.SideSolidFlags;
			int elemCount10 = instance.SideSolidFlagsCount;
			int k5 = 0;
			while (k5 < elems10.Length && k5 < elemCount10)
			{
				stream.WriteKey(46, 0);
				ProtocolParser.WriteUInt32(stream, elems10[k5]);
				k5++;
			}
		}
		if (instance.SeasonColorMap != null)
		{
			stream.WriteKey(25, 2);
			ProtocolParser.WriteString(stream, instance.SeasonColorMap);
		}
		if (instance.ClimateColorMap != null)
		{
			stream.WriteKey(88, 2);
			ProtocolParser.WriteString(stream, instance.ClimateColorMap);
		}
		if (instance.CullFaces != 0)
		{
			stream.WriteKey(26, 0);
			ProtocolParser.WriteUInt32(stream, instance.CullFaces);
		}
		if (instance.Replacable != 0)
		{
			stream.WriteKey(27, 0);
			ProtocolParser.WriteUInt32(stream, instance.Replacable);
		}
		if (instance.LightAbsorption != 0)
		{
			stream.WriteKey(29, 0);
			ProtocolParser.WriteUInt32(stream, instance.LightAbsorption);
		}
		if (instance.HardnessLevel != 0)
		{
			stream.WriteKey(30, 0);
			ProtocolParser.WriteUInt32(stream, instance.HardnessLevel);
		}
		if (instance.Resistance != 0)
		{
			stream.WriteKey(31, 0);
			ProtocolParser.WriteUInt32(stream, instance.Resistance);
		}
		if (instance.BlockMaterial != 0)
		{
			stream.WriteKey(32, 0);
			ProtocolParser.WriteUInt32(stream, instance.BlockMaterial);
		}
		if (instance.Moddata != null)
		{
			stream.WriteKey(33, 2);
			ProtocolParser.WriteBytes(stream, instance.Moddata);
		}
		if (instance.Shape != null)
		{
			stream.WriteKey(34, 2);
			Packet_CompositeShapeSerializer.SerializeWithSize(stream, instance.Shape);
		}
		if (instance.ShapeInventory != null)
		{
			stream.WriteKey(35, 2);
			Packet_CompositeShapeSerializer.SerializeWithSize(stream, instance.ShapeInventory);
		}
		if (instance.Ambientocclusion != 0)
		{
			stream.WriteKey(38, 0);
			ProtocolParser.WriteUInt32(stream, instance.Ambientocclusion);
		}
		if (instance.CollisionBoxes != null)
		{
			Packet_Cube[] elems11 = instance.CollisionBoxes;
			int elemCount11 = instance.CollisionBoxesCount;
			int k6 = 0;
			while (k6 < elems11.Length && k6 < elemCount11)
			{
				stream.WriteKey(39, 2);
				Packet_CubeSerializer.SerializeWithSize(stream, elems11[k6]);
				k6++;
			}
		}
		if (instance.SelectionBoxes != null)
		{
			Packet_Cube[] elems12 = instance.SelectionBoxes;
			int elemCount12 = instance.SelectionBoxesCount;
			int k7 = 0;
			while (k7 < elems12.Length && k7 < elemCount12)
			{
				stream.WriteKey(40, 2);
				Packet_CubeSerializer.SerializeWithSize(stream, elems12[k7]);
				k7++;
			}
		}
		if (instance.ParticleCollisionBoxes != null)
		{
			Packet_Cube[] elems13 = instance.ParticleCollisionBoxes;
			int elemCount13 = instance.ParticleCollisionBoxesCount;
			int k8 = 0;
			while (k8 < elems13.Length && k8 < elemCount13)
			{
				stream.WriteKey(91, 2);
				Packet_CubeSerializer.SerializeWithSize(stream, elems13[k8]);
				k8++;
			}
		}
		if (instance.Blockclass != null)
		{
			stream.WriteKey(41, 2);
			ProtocolParser.WriteString(stream, instance.Blockclass);
		}
		if (instance.GuiTransform != null)
		{
			stream.WriteKey(42, 2);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.GuiTransform);
		}
		if (instance.FpHandTransform != null)
		{
			stream.WriteKey(43, 2);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.FpHandTransform);
		}
		if (instance.TpHandTransform != null)
		{
			stream.WriteKey(44, 2);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.TpHandTransform);
		}
		if (instance.TpOffHandTransform != null)
		{
			stream.WriteKey(99, 2);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.TpOffHandTransform);
		}
		if (instance.GroundTransform != null)
		{
			stream.WriteKey(45, 2);
			Packet_ModelTransformSerializer.SerializeWithSize(stream, instance.GroundTransform);
		}
		if (instance.Fertility != 0)
		{
			stream.WriteKey(47, 0);
			ProtocolParser.WriteUInt32(stream, instance.Fertility);
		}
		if (instance.ParticleProperties != null)
		{
			stream.WriteKey(48, 2);
			ProtocolParser.WriteBytes(stream, instance.ParticleProperties);
		}
		if (instance.ParticlePropertiesQuantity != 0)
		{
			stream.WriteKey(49, 0);
			ProtocolParser.WriteUInt32(stream, instance.ParticlePropertiesQuantity);
		}
		if (instance.RandomDrawOffset != 0)
		{
			stream.WriteKey(50, 0);
			ProtocolParser.WriteUInt32(stream, instance.RandomDrawOffset);
		}
		if (instance.RandomizeAxes != 0)
		{
			stream.WriteKey(69, 0);
			ProtocolParser.WriteUInt32(stream, instance.RandomizeAxes);
		}
		if (instance.RandomizeRotations != 0)
		{
			stream.WriteKey(87, 0);
			ProtocolParser.WriteUInt32(stream, instance.RandomizeRotations);
		}
		if (instance.Drops != null)
		{
			Packet_BlockDrop[] elems14 = instance.Drops;
			int elemCount14 = instance.DropsCount;
			int k9 = 0;
			while (k9 < elems14.Length && k9 < elemCount14)
			{
				stream.WriteKey(52, 2);
				Packet_BlockDropSerializer.SerializeWithSize(stream, elems14[k9]);
				k9++;
			}
		}
		if (instance.LiquidLevel != 0)
		{
			stream.WriteKey(53, 0);
			ProtocolParser.WriteUInt32(stream, instance.LiquidLevel);
		}
		if (instance.Attributes != null)
		{
			stream.WriteKey(54, 2);
			ProtocolParser.WriteString(stream, instance.Attributes);
		}
		if (instance.CombustibleProps != null)
		{
			stream.WriteKey(55, 2);
			Packet_CombustiblePropertiesSerializer.SerializeWithSize(stream, instance.CombustibleProps);
		}
		if (instance.SideAo != null)
		{
			int[] elems15 = instance.SideAo;
			int elemCount15 = instance.SideAoCount;
			int k10 = 0;
			while (k10 < elems15.Length && k10 < elemCount15)
			{
				stream.WriteKey(57, 0);
				ProtocolParser.WriteUInt32(stream, elems15[k10]);
				k10++;
			}
		}
		if (instance.NeighbourSideAo != 0)
		{
			stream.WriteKey(79, 0);
			ProtocolParser.WriteUInt32(stream, instance.NeighbourSideAo);
		}
		if (instance.GrindingProps != null)
		{
			stream.WriteKey(77, 2);
			Packet_GrindingPropertiesSerializer.SerializeWithSize(stream, instance.GrindingProps);
		}
		if (instance.NutritionProps != null)
		{
			stream.WriteKey(59, 2);
			Packet_NutritionPropertiesSerializer.SerializeWithSize(stream, instance.NutritionProps);
		}
		if (instance.TransitionableProps != null)
		{
			Packet_TransitionableProperties[] elems16 = instance.TransitionableProps;
			int elemCount16 = instance.TransitionablePropsCount;
			int k11 = 0;
			while (k11 < elems16.Length && k11 < elemCount16)
			{
				stream.WriteKey(85, 2);
				Packet_TransitionablePropertiesSerializer.SerializeWithSize(stream, elems16[k11]);
				k11++;
			}
		}
		if (instance.MaxStackSize != 0)
		{
			stream.WriteKey(60, 0);
			ProtocolParser.WriteUInt32(stream, instance.MaxStackSize);
		}
		if (instance.CropProps != null)
		{
			stream.WriteKey(61, 2);
			ProtocolParser.WriteBytes(stream, instance.CropProps);
		}
		if (instance.CropPropBehaviors != null)
		{
			string[] elems17 = instance.CropPropBehaviors;
			int elemCount17 = instance.CropPropBehaviorsCount;
			int k12 = 0;
			while (k12 < elems17.Length && k12 < elemCount17)
			{
				stream.WriteKey(90, 2);
				ProtocolParser.WriteString(stream, elems17[k12]);
				k12++;
			}
		}
		if (instance.MaterialDensity != 0)
		{
			stream.WriteKey(62, 0);
			ProtocolParser.WriteUInt32(stream, instance.MaterialDensity);
		}
		if (instance.AttackPower != 0)
		{
			stream.WriteKey(63, 0);
			ProtocolParser.WriteUInt32(stream, instance.AttackPower);
		}
		if (instance.AttackRange != 0)
		{
			stream.WriteKey(70, 0);
			ProtocolParser.WriteUInt32(stream, instance.AttackRange);
		}
		if (instance.LiquidSelectable != 0)
		{
			stream.WriteKey(64, 0);
			ProtocolParser.WriteUInt32(stream, instance.LiquidSelectable);
		}
		if (instance.MiningTier != 0)
		{
			stream.WriteKey(65, 0);
			ProtocolParser.WriteUInt32(stream, instance.MiningTier);
		}
		if (instance.RequiredMiningTier != 0)
		{
			stream.WriteKey(66, 0);
			ProtocolParser.WriteUInt32(stream, instance.RequiredMiningTier);
		}
		if (instance.Miningmaterial != null)
		{
			int[] elems18 = instance.Miningmaterial;
			int elemCount18 = instance.MiningmaterialCount;
			int k13 = 0;
			while (k13 < elems18.Length && k13 < elemCount18)
			{
				stream.WriteKey(67, 0);
				ProtocolParser.WriteUInt32(stream, elems18[k13]);
				k13++;
			}
		}
		if (instance.Miningmaterialspeed != null)
		{
			int[] elems19 = instance.Miningmaterialspeed;
			int elemCount19 = instance.MiningmaterialspeedCount;
			int k14 = 0;
			while (k14 < elems19.Length && k14 < elemCount19)
			{
				stream.WriteKey(76, 0);
				ProtocolParser.WriteUInt32(stream, elems19[k14]);
				k14++;
			}
		}
		if (instance.DragMultiplierFloat != 0)
		{
			stream.WriteKey(68, 0);
			ProtocolParser.WriteUInt32(stream, instance.DragMultiplierFloat);
		}
		if (instance.StorageFlags != 0)
		{
			stream.WriteKey(71, 0);
			ProtocolParser.WriteUInt32(stream, instance.StorageFlags);
		}
		if (instance.RenderAlphaTest != 0)
		{
			stream.WriteKey(72, 0);
			ProtocolParser.WriteUInt32(stream, instance.RenderAlphaTest);
		}
		if (instance.HeldTpHitAnimation != null)
		{
			stream.WriteKey(73, 2);
			ProtocolParser.WriteString(stream, instance.HeldTpHitAnimation);
		}
		if (instance.HeldRightTpIdleAnimation != null)
		{
			stream.WriteKey(74, 2);
			ProtocolParser.WriteString(stream, instance.HeldRightTpIdleAnimation);
		}
		if (instance.HeldLeftTpIdleAnimation != null)
		{
			stream.WriteKey(80, 2);
			ProtocolParser.WriteString(stream, instance.HeldLeftTpIdleAnimation);
		}
		if (instance.HeldTpUseAnimation != null)
		{
			stream.WriteKey(75, 2);
			ProtocolParser.WriteString(stream, instance.HeldTpUseAnimation);
		}
		if (instance.RainPermeable != 0)
		{
			stream.WriteKey(78, 0);
			ProtocolParser.WriteUInt32(stream, instance.RainPermeable);
		}
		if (instance.LiquidCode != null)
		{
			stream.WriteKey(81, 2);
			ProtocolParser.WriteString(stream, instance.LiquidCode);
		}
		if (instance.Variant != null)
		{
			Packet_VariantPart[] elems20 = instance.Variant;
			int elemCount20 = instance.VariantCount;
			int k15 = 0;
			while (k15 < elems20.Length && k15 < elemCount20)
			{
				stream.WriteKey(82, 2);
				Packet_VariantPartSerializer.SerializeWithSize(stream, elems20[k15]);
				k15++;
			}
		}
		if (instance.Lod0shape != null)
		{
			stream.WriteKey(86, 2);
			Packet_CompositeShapeSerializer.SerializeWithSize(stream, instance.Lod0shape);
		}
		if (instance.Frostable != 0)
		{
			stream.WriteKey(89, 0);
			ProtocolParser.WriteUInt32(stream, instance.Frostable);
		}
		if (instance.CrushingProps != null)
		{
			stream.WriteKey(92, 2);
			Packet_CrushingPropertiesSerializer.SerializeWithSize(stream, instance.CrushingProps);
		}
		if (instance.RandomSizeAdjust != 0)
		{
			stream.WriteKey(93, 0);
			ProtocolParser.WriteUInt32(stream, instance.RandomSizeAdjust);
		}
		if (instance.Lod2shape != null)
		{
			stream.WriteKey(94, 2);
			Packet_CompositeShapeSerializer.SerializeWithSize(stream, instance.Lod2shape);
		}
		if (instance.DoNotRenderAtLod2 != 0)
		{
			stream.WriteKey(95, 0);
			ProtocolParser.WriteUInt32(stream, instance.DoNotRenderAtLod2);
		}
		if (instance.Width != 0)
		{
			stream.WriteKey(96, 0);
			ProtocolParser.WriteUInt32(stream, instance.Width);
		}
		if (instance.Height != 0)
		{
			stream.WriteKey(97, 0);
			ProtocolParser.WriteUInt32(stream, instance.Height);
		}
		if (instance.Length != 0)
		{
			stream.WriteKey(98, 0);
			ProtocolParser.WriteUInt32(stream, instance.Length);
		}
		if (instance.IsMissing != 0)
		{
			stream.WriteKey(100, 0);
			ProtocolParser.WriteUInt32(stream, instance.IsMissing);
		}
		if (instance.Durability != 0)
		{
			stream.WriteKey(101, 0);
			ProtocolParser.WriteUInt32(stream, instance.Durability);
		}
		if (instance.HeldLeftReadyAnimation != null)
		{
			stream.WriteKey(102, 2);
			ProtocolParser.WriteString(stream, instance.HeldLeftReadyAnimation);
		}
		if (instance.HeldRightReadyAnimation != null)
		{
			stream.WriteKey(103, 2);
			ProtocolParser.WriteString(stream, instance.HeldRightReadyAnimation);
		}
	}

	public static int GetSize(Packet_BlockType instance)
	{
		int size = 0;
		if (instance.TextureCodes != null)
		{
			for (int i = 0; i < instance.TextureCodesCount; i++)
			{
				string i2 = instance.TextureCodes[i];
				size += ProtocolParser.GetSize(i2) + 1;
			}
		}
		if (instance.CompositeTextures != null)
		{
			for (int j = 0; j < instance.CompositeTexturesCount; j++)
			{
				int packetlength = Packet_CompositeTextureSerializer.GetSize(instance.CompositeTextures[j]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
			}
		}
		if (instance.InventoryTextureCodes != null)
		{
			for (int k = 0; k < instance.InventoryTextureCodesCount; k++)
			{
				string i3 = instance.InventoryTextureCodes[k];
				size += ProtocolParser.GetSize(i3) + 1;
			}
		}
		if (instance.InventoryCompositeTextures != null)
		{
			for (int l = 0; l < instance.InventoryCompositeTexturesCount; l++)
			{
				int packetlength2 = Packet_CompositeTextureSerializer.GetSize(instance.InventoryCompositeTextures[l]);
				size += packetlength2 + ProtocolParser.GetSize(packetlength2) + 1;
			}
		}
		if (instance.BlockId != 0)
		{
			size += ProtocolParser.GetSize(instance.BlockId) + 1;
		}
		if (instance.Code != null)
		{
			size += ProtocolParser.GetSize(instance.Code) + 1;
		}
		if (instance.EntityClass != null)
		{
			size += ProtocolParser.GetSize(instance.EntityClass) + 2;
		}
		if (instance.Tags != null)
		{
			for (int m = 0; m < instance.TagsCount; m++)
			{
				int i4 = instance.Tags[m];
				size += ProtocolParser.GetSize(i4) + 2;
			}
		}
		if (instance.Behaviors != null)
		{
			for (int n = 0; n < instance.BehaviorsCount; n++)
			{
				int packetlength3 = Packet_BehaviorSerializer.GetSize(instance.Behaviors[n]);
				size += packetlength3 + ProtocolParser.GetSize(packetlength3) + 1;
			}
		}
		if (instance.EntityBehaviors != null)
		{
			size += ProtocolParser.GetSize(instance.EntityBehaviors) + 2;
		}
		if (instance.RenderPass != 0)
		{
			size += ProtocolParser.GetSize(instance.RenderPass) + 1;
		}
		if (instance.DrawType != 0)
		{
			size += ProtocolParser.GetSize(instance.DrawType) + 1;
		}
		if (instance.MatterState != 0)
		{
			size += ProtocolParser.GetSize(instance.MatterState) + 1;
		}
		if (instance.WalkSpeedFloat != 0)
		{
			size += ProtocolParser.GetSize(instance.WalkSpeedFloat) + 1;
		}
		if (instance.IsSlipperyWalk)
		{
			size += 2;
		}
		if (instance.Sounds != null)
		{
			int packetlength4 = Packet_BlockSoundSetSerializer.GetSize(instance.Sounds);
			size += packetlength4 + ProtocolParser.GetSize(packetlength4) + 1;
		}
		if (instance.HeldSounds != null)
		{
			int packetlength5 = Packet_HeldSoundSetSerializer.GetSize(instance.HeldSounds);
			size += packetlength5 + ProtocolParser.GetSize(packetlength5) + 2;
		}
		if (instance.LightHsv != null)
		{
			for (int k2 = 0; k2 < instance.LightHsvCount; k2++)
			{
				int i5 = instance.LightHsv[k2];
				size += ProtocolParser.GetSize(i5) + 1;
			}
		}
		if (instance.VertexFlags != 0)
		{
			size += ProtocolParser.GetSize(instance.VertexFlags) + 2;
		}
		if (instance.Climbable != 0)
		{
			size += ProtocolParser.GetSize(instance.Climbable) + 1;
		}
		if (instance.CreativeInventoryTabs != null)
		{
			for (int k3 = 0; k3 < instance.CreativeInventoryTabsCount; k3++)
			{
				string i6 = instance.CreativeInventoryTabs[k3];
				size += ProtocolParser.GetSize(i6) + 2;
			}
		}
		if (instance.CreativeInventoryStacks != null)
		{
			size += ProtocolParser.GetSize(instance.CreativeInventoryStacks) + 2;
		}
		if (instance.SideOpaqueFlags != null)
		{
			for (int k4 = 0; k4 < instance.SideOpaqueFlagsCount; k4++)
			{
				int i7 = instance.SideOpaqueFlags[k4];
				size += ProtocolParser.GetSize(i7) + 2;
			}
		}
		if (instance.FaceCullMode != 0)
		{
			size += ProtocolParser.GetSize(instance.FaceCullMode) + 2;
		}
		if (instance.SideSolidFlags != null)
		{
			for (int k5 = 0; k5 < instance.SideSolidFlagsCount; k5++)
			{
				int i8 = instance.SideSolidFlags[k5];
				size += ProtocolParser.GetSize(i8) + 2;
			}
		}
		if (instance.SeasonColorMap != null)
		{
			size += ProtocolParser.GetSize(instance.SeasonColorMap) + 2;
		}
		if (instance.ClimateColorMap != null)
		{
			size += ProtocolParser.GetSize(instance.ClimateColorMap) + 2;
		}
		if (instance.CullFaces != 0)
		{
			size += ProtocolParser.GetSize(instance.CullFaces) + 2;
		}
		if (instance.Replacable != 0)
		{
			size += ProtocolParser.GetSize(instance.Replacable) + 2;
		}
		if (instance.LightAbsorption != 0)
		{
			size += ProtocolParser.GetSize(instance.LightAbsorption) + 2;
		}
		if (instance.HardnessLevel != 0)
		{
			size += ProtocolParser.GetSize(instance.HardnessLevel) + 2;
		}
		if (instance.Resistance != 0)
		{
			size += ProtocolParser.GetSize(instance.Resistance) + 2;
		}
		if (instance.BlockMaterial != 0)
		{
			size += ProtocolParser.GetSize(instance.BlockMaterial) + 2;
		}
		if (instance.Moddata != null)
		{
			size += ProtocolParser.GetSize(instance.Moddata) + 2;
		}
		if (instance.Shape != null)
		{
			int packetlength6 = Packet_CompositeShapeSerializer.GetSize(instance.Shape);
			size += packetlength6 + ProtocolParser.GetSize(packetlength6) + 2;
		}
		if (instance.ShapeInventory != null)
		{
			int packetlength7 = Packet_CompositeShapeSerializer.GetSize(instance.ShapeInventory);
			size += packetlength7 + ProtocolParser.GetSize(packetlength7) + 2;
		}
		if (instance.Ambientocclusion != 0)
		{
			size += ProtocolParser.GetSize(instance.Ambientocclusion) + 2;
		}
		if (instance.CollisionBoxes != null)
		{
			for (int k6 = 0; k6 < instance.CollisionBoxesCount; k6++)
			{
				int packetlength8 = Packet_CubeSerializer.GetSize(instance.CollisionBoxes[k6]);
				size += packetlength8 + ProtocolParser.GetSize(packetlength8) + 2;
			}
		}
		if (instance.SelectionBoxes != null)
		{
			for (int k7 = 0; k7 < instance.SelectionBoxesCount; k7++)
			{
				int packetlength9 = Packet_CubeSerializer.GetSize(instance.SelectionBoxes[k7]);
				size += packetlength9 + ProtocolParser.GetSize(packetlength9) + 2;
			}
		}
		if (instance.ParticleCollisionBoxes != null)
		{
			for (int k8 = 0; k8 < instance.ParticleCollisionBoxesCount; k8++)
			{
				int packetlength10 = Packet_CubeSerializer.GetSize(instance.ParticleCollisionBoxes[k8]);
				size += packetlength10 + ProtocolParser.GetSize(packetlength10) + 2;
			}
		}
		if (instance.Blockclass != null)
		{
			size += ProtocolParser.GetSize(instance.Blockclass) + 2;
		}
		if (instance.GuiTransform != null)
		{
			int packetlength11 = Packet_ModelTransformSerializer.GetSize(instance.GuiTransform);
			size += packetlength11 + ProtocolParser.GetSize(packetlength11) + 2;
		}
		if (instance.FpHandTransform != null)
		{
			int packetlength12 = Packet_ModelTransformSerializer.GetSize(instance.FpHandTransform);
			size += packetlength12 + ProtocolParser.GetSize(packetlength12) + 2;
		}
		if (instance.TpHandTransform != null)
		{
			int packetlength13 = Packet_ModelTransformSerializer.GetSize(instance.TpHandTransform);
			size += packetlength13 + ProtocolParser.GetSize(packetlength13) + 2;
		}
		if (instance.TpOffHandTransform != null)
		{
			int packetlength14 = Packet_ModelTransformSerializer.GetSize(instance.TpOffHandTransform);
			size += packetlength14 + ProtocolParser.GetSize(packetlength14) + 2;
		}
		if (instance.GroundTransform != null)
		{
			int packetlength15 = Packet_ModelTransformSerializer.GetSize(instance.GroundTransform);
			size += packetlength15 + ProtocolParser.GetSize(packetlength15) + 2;
		}
		if (instance.Fertility != 0)
		{
			size += ProtocolParser.GetSize(instance.Fertility) + 2;
		}
		if (instance.ParticleProperties != null)
		{
			size += ProtocolParser.GetSize(instance.ParticleProperties) + 2;
		}
		if (instance.ParticlePropertiesQuantity != 0)
		{
			size += ProtocolParser.GetSize(instance.ParticlePropertiesQuantity) + 2;
		}
		if (instance.RandomDrawOffset != 0)
		{
			size += ProtocolParser.GetSize(instance.RandomDrawOffset) + 2;
		}
		if (instance.RandomizeAxes != 0)
		{
			size += ProtocolParser.GetSize(instance.RandomizeAxes) + 2;
		}
		if (instance.RandomizeRotations != 0)
		{
			size += ProtocolParser.GetSize(instance.RandomizeRotations) + 2;
		}
		if (instance.Drops != null)
		{
			for (int k9 = 0; k9 < instance.DropsCount; k9++)
			{
				int packetlength16 = Packet_BlockDropSerializer.GetSize(instance.Drops[k9]);
				size += packetlength16 + ProtocolParser.GetSize(packetlength16) + 2;
			}
		}
		if (instance.LiquidLevel != 0)
		{
			size += ProtocolParser.GetSize(instance.LiquidLevel) + 2;
		}
		if (instance.Attributes != null)
		{
			size += ProtocolParser.GetSize(instance.Attributes) + 2;
		}
		if (instance.CombustibleProps != null)
		{
			int packetlength17 = Packet_CombustiblePropertiesSerializer.GetSize(instance.CombustibleProps);
			size += packetlength17 + ProtocolParser.GetSize(packetlength17) + 2;
		}
		if (instance.SideAo != null)
		{
			for (int k10 = 0; k10 < instance.SideAoCount; k10++)
			{
				int i9 = instance.SideAo[k10];
				size += ProtocolParser.GetSize(i9) + 2;
			}
		}
		if (instance.NeighbourSideAo != 0)
		{
			size += ProtocolParser.GetSize(instance.NeighbourSideAo) + 2;
		}
		if (instance.GrindingProps != null)
		{
			int packetlength18 = Packet_GrindingPropertiesSerializer.GetSize(instance.GrindingProps);
			size += packetlength18 + ProtocolParser.GetSize(packetlength18) + 2;
		}
		if (instance.NutritionProps != null)
		{
			int packetlength19 = Packet_NutritionPropertiesSerializer.GetSize(instance.NutritionProps);
			size += packetlength19 + ProtocolParser.GetSize(packetlength19) + 2;
		}
		if (instance.TransitionableProps != null)
		{
			for (int k11 = 0; k11 < instance.TransitionablePropsCount; k11++)
			{
				int packetlength20 = Packet_TransitionablePropertiesSerializer.GetSize(instance.TransitionableProps[k11]);
				size += packetlength20 + ProtocolParser.GetSize(packetlength20) + 2;
			}
		}
		if (instance.MaxStackSize != 0)
		{
			size += ProtocolParser.GetSize(instance.MaxStackSize) + 2;
		}
		if (instance.CropProps != null)
		{
			size += ProtocolParser.GetSize(instance.CropProps) + 2;
		}
		if (instance.CropPropBehaviors != null)
		{
			for (int k12 = 0; k12 < instance.CropPropBehaviorsCount; k12++)
			{
				string i10 = instance.CropPropBehaviors[k12];
				size += ProtocolParser.GetSize(i10) + 2;
			}
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
		if (instance.RequiredMiningTier != 0)
		{
			size += ProtocolParser.GetSize(instance.RequiredMiningTier) + 2;
		}
		if (instance.Miningmaterial != null)
		{
			for (int k13 = 0; k13 < instance.MiningmaterialCount; k13++)
			{
				int i11 = instance.Miningmaterial[k13];
				size += ProtocolParser.GetSize(i11) + 2;
			}
		}
		if (instance.Miningmaterialspeed != null)
		{
			for (int k14 = 0; k14 < instance.MiningmaterialspeedCount; k14++)
			{
				int i12 = instance.Miningmaterialspeed[k14];
				size += ProtocolParser.GetSize(i12) + 2;
			}
		}
		if (instance.DragMultiplierFloat != 0)
		{
			size += ProtocolParser.GetSize(instance.DragMultiplierFloat) + 2;
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
		if (instance.RainPermeable != 0)
		{
			size += ProtocolParser.GetSize(instance.RainPermeable) + 2;
		}
		if (instance.LiquidCode != null)
		{
			size += ProtocolParser.GetSize(instance.LiquidCode) + 2;
		}
		if (instance.Variant != null)
		{
			for (int k15 = 0; k15 < instance.VariantCount; k15++)
			{
				int packetlength21 = Packet_VariantPartSerializer.GetSize(instance.Variant[k15]);
				size += packetlength21 + ProtocolParser.GetSize(packetlength21) + 2;
			}
		}
		if (instance.Lod0shape != null)
		{
			int packetlength22 = Packet_CompositeShapeSerializer.GetSize(instance.Lod0shape);
			size += packetlength22 + ProtocolParser.GetSize(packetlength22) + 2;
		}
		if (instance.Frostable != 0)
		{
			size += ProtocolParser.GetSize(instance.Frostable) + 2;
		}
		if (instance.CrushingProps != null)
		{
			int packetlength23 = Packet_CrushingPropertiesSerializer.GetSize(instance.CrushingProps);
			size += packetlength23 + ProtocolParser.GetSize(packetlength23) + 2;
		}
		if (instance.RandomSizeAdjust != 0)
		{
			size += ProtocolParser.GetSize(instance.RandomSizeAdjust) + 2;
		}
		if (instance.Lod2shape != null)
		{
			int packetlength24 = Packet_CompositeShapeSerializer.GetSize(instance.Lod2shape);
			size += packetlength24 + ProtocolParser.GetSize(packetlength24) + 2;
		}
		if (instance.DoNotRenderAtLod2 != 0)
		{
			size += ProtocolParser.GetSize(instance.DoNotRenderAtLod2) + 2;
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
		if (instance.IsMissing != 0)
		{
			size += ProtocolParser.GetSize(instance.IsMissing) + 2;
		}
		if (instance.Durability != 0)
		{
			size += ProtocolParser.GetSize(instance.Durability) + 2;
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

	public static void SerializeWithSize(CitoStream stream, Packet_BlockType instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_BlockTypeSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_BlockType instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_BlockTypeSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlockType instance)
	{
		byte[] data = Packet_BlockTypeSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
