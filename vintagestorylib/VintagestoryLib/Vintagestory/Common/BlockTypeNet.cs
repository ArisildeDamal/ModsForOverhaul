using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Common
{
	public class BlockTypeNet : CollectibleNet
	{
		public static Block ReadBlockTypePacket(Packet_BlockType packet, IWorldAccessor world, ClassRegistry registry)
		{
			Block block = registry.CreateBlock(packet.Blockclass);
			block.IsMissing = packet.IsMissing > 0;
			block.Code = new AssetLocation(packet.Code);
			Block block2 = block;
			BlockTagArray blockTagArray;
			if (packet.Tags == null)
			{
				blockTagArray = new BlockTagArray();
			}
			else
			{
				blockTagArray = new BlockTagArray(packet.Tags.Select((int tag) => (ushort)tag));
			}
			block2.Tags = blockTagArray;
			block.Class = packet.Blockclass;
			block.VariantStrict = CollectibleNet.FromPacket(packet.Variant, packet.VariantCount);
			block.Variant = new RelaxedReadOnlyDictionary<string, string>(block.VariantStrict);
			block.EntityClass = packet.EntityClass;
			block.MaxStackSize = packet.MaxStackSize;
			block.StorageFlags = (EnumItemStorageFlags)packet.StorageFlags;
			block.RainPermeable = packet.RainPermeable > 0;
			block.Dimensions = ((packet.Width + packet.Height + packet.Length == 0) ? CollectibleObject.DefaultSize : new Size3f(CollectibleNet.DeserializeFloatVeryPrecise(packet.Width), CollectibleNet.DeserializeFloatVeryPrecise(packet.Height), CollectibleNet.DeserializeFloatVeryPrecise(packet.Length)));
			block.Durability = packet.Durability;
			block.BlockEntityBehaviors = JsonUtil.FromString<BlockEntityBehaviorType[]>(packet.EntityBehaviors);
			block.BlockId = packet.BlockId;
			block.DrawType = (EnumDrawType)packet.DrawType;
			block.RenderPass = (EnumChunkRenderPass)packet.RenderPass;
			block.VertexFlags = new VertexFlags(packet.VertexFlags);
			block.Frostable = packet.Frostable > 0;
			if (packet.LightHsv != null && packet.LightHsvCount > 2)
			{
				block.LightHsv = new byte[]
				{
					(byte)packet.LightHsv[0],
					(byte)packet.LightHsv[1],
					(byte)packet.LightHsv[2]
				};
			}
			else
			{
				block.LightHsv = new byte[3];
			}
			if (packet.Sounds != null)
			{
				Packet_BlockSoundSet packetSounds = packet.Sounds;
				block.Sounds = new BlockSounds
				{
					Break = AssetLocation.CreateOrNull(packetSounds.Break),
					Hit = AssetLocation.CreateOrNull(packetSounds.Hit),
					Place = AssetLocation.CreateOrNull(packetSounds.Place),
					Walk = AssetLocation.CreateOrNull(packetSounds.Walk),
					Inside = AssetLocation.CreateOrNull(packetSounds.Inside),
					Ambient = AssetLocation.CreateOrNull(packetSounds.Ambient),
					AmbientBlockCount = CollectibleNet.DeserializeFloat(packetSounds.AmbientBlockCount),
					AmbientSoundType = (EnumSoundType)packetSounds.AmbientSoundType,
					AmbientMaxDistanceMerge = (float)packetSounds.AmbientMaxDistanceMerge / 100f
				};
				for (int i = 0; i < packetSounds.ByToolSoundCount; i++)
				{
					if (i == 0)
					{
						BlockSounds sounds = block.Sounds;
						if (sounds.ByTool == null)
						{
							sounds.ByTool = new Dictionary<EnumTool, BlockSounds>();
						}
					}
					Packet_BlockSoundSet ByToolSound = packetSounds.ByToolSound[i];
					block.Sounds.ByTool[(EnumTool)packetSounds.ByToolTool[i]] = new BlockSounds
					{
						Break = AssetLocation.CreateOrNull(ByToolSound.Break),
						Hit = AssetLocation.CreateOrNull(ByToolSound.Hit)
					};
				}
			}
			int texturesCount = packet.TextureCodesCount;
			block.Textures = new TextureDictionary(texturesCount);
			if (texturesCount > 0)
			{
				string[] TextureCodes = packet.TextureCodes;
				int j = 0;
				while (j < TextureCodes.Length && j < texturesCount)
				{
					block.Textures.Add(TextureCodes[j], CollectibleNet.FromPacket(packet.CompositeTextures[j]));
					j++;
				}
			}
			texturesCount = packet.InventoryTextureCodesCount;
			block.TexturesInventory = new TextureDictionary(texturesCount);
			if (texturesCount > 0)
			{
				string[] TextureCodes2 = packet.InventoryTextureCodes;
				int k = 0;
				while (k < TextureCodes2.Length && k < texturesCount)
				{
					block.TexturesInventory.Add(TextureCodes2[k], CollectibleNet.FromPacket(packet.InventoryCompositeTextures[k]));
					k++;
				}
			}
			if (packet.Attributes != null && packet.Attributes.Length > 0)
			{
				block.Attributes = new JsonObject(JToken.Parse(packet.Attributes));
			}
			block.MatterState = (EnumMatterState)packet.MatterState;
			block.WalkSpeedMultiplier = CollectibleNet.DeserializeFloatVeryPrecise(packet.WalkSpeedFloat);
			block.DragMultiplier = CollectibleNet.DeserializeFloatVeryPrecise(packet.DragMultiplierFloat);
			block.Climbable = packet.Climbable > 0;
			block.SideOpaque = ((packet.SideOpaqueFlags == null) ? new SmallBoolArray(63) : new SmallBoolArray(packet.SideOpaqueFlags));
			block.SideAo = ((packet.SideAo == null) ? new SmallBoolArray(63) : new SmallBoolArray(packet.SideAo));
			block.EmitSideAo = (byte)packet.NeighbourSideAo;
			if (packet.SideSolidFlags != null)
			{
				block.SideSolid = new SmallBoolArray(packet.SideSolidFlags);
			}
			block.SeasonColorMap = packet.SeasonColorMap;
			block.ClimateColorMap = packet.ClimateColorMap;
			block.Fertility = packet.Fertility;
			block.Replaceable = packet.Replacable;
			block.LightAbsorption = (int)((ushort)packet.LightAbsorption);
			block.Resistance = CollectibleNet.DeserializeFloat(packet.Resistance);
			block.BlockMaterial = (EnumBlockMaterial)packet.BlockMaterial;
			if (packet.Shape != null)
			{
				block.Shape = CollectibleNet.FromPacket(packet.Shape);
			}
			if (packet.ShapeInventory != null)
			{
				block.ShapeInventory = CollectibleNet.FromPacket(packet.ShapeInventory);
			}
			if (packet.Lod0shape != null)
			{
				block.Lod0Shape = CollectibleNet.FromPacket(packet.Lod0shape);
			}
			if (packet.Lod2shape != null)
			{
				block.Lod2Shape = CollectibleNet.FromPacket(packet.Lod2shape);
			}
			block.DoNotRenderAtLod2 = packet.DoNotRenderAtLod2 > 0;
			block.Ambientocclusion = packet.Ambientocclusion > 0;
			if (packet.SelectionBoxes != null)
			{
				Cuboidf[] SelectionBoxes = (block.SelectionBoxes = new Cuboidf[packet.SelectionBoxesCount]);
				for (int l = 0; l < SelectionBoxes.Length; l++)
				{
					SelectionBoxes[l] = BlockTypeNet.DeserializeCuboid(packet.SelectionBoxes[l]);
				}
			}
			else
			{
				block.SelectionBoxes = null;
			}
			if (packet.CollisionBoxes != null)
			{
				Cuboidf[] CollisionBoxes = (block.CollisionBoxes = new Cuboidf[packet.CollisionBoxesCount]);
				for (int m = 0; m < CollisionBoxes.Length; m++)
				{
					CollisionBoxes[m] = BlockTypeNet.DeserializeCuboid(packet.CollisionBoxes[m]);
				}
			}
			else
			{
				block.CollisionBoxes = null;
			}
			if (packet.ParticleCollisionBoxes != null)
			{
				Cuboidf[] ParticleCollisionBoxes = (block.ParticleCollisionBoxes = new Cuboidf[packet.ParticleCollisionBoxesCount]);
				for (int n = 0; n < ParticleCollisionBoxes.Length; n++)
				{
					ParticleCollisionBoxes[n] = BlockTypeNet.DeserializeCuboid(packet.ParticleCollisionBoxes[n]);
				}
			}
			else
			{
				block.ParticleCollisionBoxes = null;
			}
			block.CreativeInventoryTabs = new string[packet.CreativeInventoryTabsCount];
			if (packet.CreativeInventoryTabs != null)
			{
				for (int i2 = 0; i2 < block.CreativeInventoryTabs.Length; i2++)
				{
					block.CreativeInventoryTabs[i2] = packet.CreativeInventoryTabs[i2];
				}
			}
			if (block.IsMissing)
			{
				block.GuiTransform = CollectibleNet.DefGuiTransform;
				block.FpHandTransform = CollectibleNet.DefFpHandTransform;
				block.TpHandTransform = CollectibleNet.DefTpHandTransform;
				block.TpOffHandTransform = CollectibleNet.DefTpOffHandTransform;
				block.GroundTransform = CollectibleNet.DefGroundTransform;
			}
			else
			{
				block.GuiTransform = ((packet.GuiTransform == null) ? ModelTransform.BlockDefaultGui() : CollectibleNet.FromTransformPacket(packet.GuiTransform).EnsureDefaultValues());
				block.FpHandTransform = ((packet.FpHandTransform == null) ? ModelTransform.BlockDefaultFp() : CollectibleNet.FromTransformPacket(packet.FpHandTransform).EnsureDefaultValues());
				block.TpHandTransform = ((packet.TpHandTransform == null) ? ModelTransform.BlockDefaultTp() : CollectibleNet.FromTransformPacket(packet.TpHandTransform).EnsureDefaultValues());
				block.TpOffHandTransform = ((packet.TpOffHandTransform == null) ? block.TpHandTransform.Clone() : CollectibleNet.FromTransformPacket(packet.TpOffHandTransform).EnsureDefaultValues());
				block.GroundTransform = ((packet.GroundTransform == null) ? ModelTransform.BlockDefaultGround() : CollectibleNet.FromTransformPacket(packet.GroundTransform).EnsureDefaultValues());
			}
			if (packet.ParticleProperties != null && packet.ParticleProperties.Length != 0)
			{
				block.ParticleProperties = new AdvancedParticleProperties[packet.ParticlePropertiesQuantity];
				using (MemoryStream ms = new MemoryStream(packet.ParticleProperties))
				{
					BinaryReader reader = new BinaryReader(ms);
					for (int i3 = 0; i3 < packet.ParticlePropertiesQuantity; i3++)
					{
						block.ParticleProperties[i3] = new AdvancedParticleProperties();
						block.ParticleProperties[i3].FromBytes(reader, world);
						if (block.ParticleProperties[i3].ColorByBlock)
						{
							block.ParticleProperties[i3].block = block;
						}
					}
				}
			}
			block.RandomDrawOffset = packet.RandomDrawOffset;
			block.RandomizeAxes = (EnumRandomizeAxes)packet.RandomizeAxes;
			block.RandomizeRotations = packet.RandomizeRotations > 0;
			block.RandomSizeAdjust = CollectibleNet.DeserializeFloatVeryPrecise(packet.RandomSizeAdjust);
			block.LiquidLevel = packet.LiquidLevel;
			block.LiquidCode = packet.LiquidCode;
			block.FaceCullMode = (EnumFaceCullMode)packet.FaceCullMode;
			if (packet.CombustibleProps != null)
			{
				block.CombustibleProps = CollectibleNet.FromPacket(packet.CombustibleProps, world);
			}
			if (packet.NutritionProps != null)
			{
				block.NutritionProps = CollectibleNet.FromPacket(packet.NutritionProps, world);
			}
			if (packet.TransitionableProps != null)
			{
				block.TransitionableProps = CollectibleNet.FromPacket(packet.TransitionableProps, world);
			}
			if (packet.GrindingProps != null)
			{
				block.GrindingProps = CollectibleNet.FromPacket(packet.GrindingProps, world);
			}
			if (packet.CrushingProps != null)
			{
				block.CrushingProps = CollectibleNet.FromPacket(packet.CrushingProps, world);
			}
			if (packet.CreativeInventoryStacks != null)
			{
				using (MemoryStream ms2 = new MemoryStream(packet.CreativeInventoryStacks))
				{
					BinaryReader reader2 = new BinaryReader(ms2);
					int count = reader2.ReadInt32();
					block.CreativeInventoryStacks = new CreativeTabAndStackList[count];
					for (int i4 = 0; i4 < count; i4++)
					{
						block.CreativeInventoryStacks[i4] = new CreativeTabAndStackList();
						block.CreativeInventoryStacks[i4].FromBytes(reader2, world.ClassRegistry);
					}
				}
			}
			if (packet.Drops != null)
			{
				block.Drops = new BlockDropItemStack[packet.DropsCount];
				for (int i5 = 0; i5 < block.Drops.Length; i5++)
				{
					block.Drops[i5] = BlockTypeNet.FromPacket(packet.Drops[i5], world);
				}
			}
			if (packet.CropProps != null)
			{
				block.CropProps = SerializerUtil.Deserialize<BlockCropProperties>(packet.CropProps);
				int cropBehaviorCount = packet.CropPropBehaviorsCount;
				if (cropBehaviorCount > 0)
				{
					block.CropProps.Behaviors = new CropBehavior[cropBehaviorCount];
					for (int i6 = 0; i6 < cropBehaviorCount; i6++)
					{
						block.CropProps.Behaviors[i6] = registry.createCropBehavior(block, packet.CropPropBehaviors[i6]);
					}
				}
			}
			block.MaterialDensity = packet.MaterialDensity;
			block.AttackPower = CollectibleNet.DeserializeFloatPrecise(packet.AttackPower);
			block.AttackRange = CollectibleNet.DeserializeFloatPrecise(packet.AttackRange);
			block.LiquidSelectable = packet.LiquidSelectable > 0;
			if (packet.HeldSounds != null)
			{
				block.HeldSounds = CollectibleNet.FromPacket(packet.HeldSounds);
			}
			if (packet.Miningmaterial != null)
			{
				block.MiningSpeed = new Dictionary<EnumBlockMaterial, float>();
				for (int i7 = 0; i7 < packet.MiningmaterialCount; i7++)
				{
					int m2 = packet.Miningmaterial[i7];
					float speed = CollectibleNet.DeserializeFloat(packet.Miningmaterialspeed[i7]);
					block.MiningSpeed[(EnumBlockMaterial)m2] = speed;
				}
			}
			block.ToolTier = packet.MiningTier;
			block.RequiredMiningTier = packet.RequiredMiningTier;
			block.RenderAlphaTest = CollectibleNet.DeserializeFloatVeryPrecise(packet.RenderAlphaTest);
			block.HeldTpHitAnimation = packet.HeldTpHitAnimation;
			block.HeldRightTpIdleAnimation = packet.HeldRightTpIdleAnimation;
			block.HeldLeftTpIdleAnimation = packet.HeldLeftTpIdleAnimation;
			block.HeldLeftReadyAnimation = packet.HeldLeftReadyAnimation;
			block.HeldRightReadyAnimation = packet.HeldRightReadyAnimation;
			block.HeldTpUseAnimation = packet.HeldTpUseAnimation;
			if (packet.BehaviorsCount > 0)
			{
				List<BlockBehavior> blbehaviors = new List<BlockBehavior>();
				List<CollectibleBehavior> colbehaviors = new List<CollectibleBehavior>();
				for (int i8 = 0; i8 < packet.BehaviorsCount; i8++)
				{
					Packet_Behavior bhpkt = packet.Behaviors[i8];
					bool hasBlBehavior = registry.blockbehaviorToTypeMapping.ContainsKey(bhpkt.Code);
					bool hasColBehavior = registry.collectibleBehaviorToTypeMapping.ContainsKey(bhpkt.Code);
					if (bhpkt.ClientSideOptional <= 0 || hasBlBehavior || hasColBehavior)
					{
						CollectibleBehavior bh = (hasBlBehavior ? registry.CreateBlockBehavior(block, bhpkt.Code) : registry.CreateCollectibleBehavior(block, bhpkt.Code));
						JsonObject properties;
						if (bhpkt.Attributes != "")
						{
							properties = new JsonObject(JToken.Parse(bhpkt.Attributes));
						}
						else
						{
							properties = new JsonObject(JToken.Parse("{}"));
						}
						bh.Initialize(properties);
						colbehaviors.Add(bh);
						BlockBehavior bbh = bh as BlockBehavior;
						if (bbh != null)
						{
							blbehaviors.Add(bbh);
						}
					}
				}
				block.BlockBehaviors = blbehaviors.ToArray();
				block.CollectibleBehaviors = colbehaviors.ToArray();
			}
			return block;
		}

		public static Packet_BlockType GetBlockTypePacket(Block block, IClassRegistryAPI registry)
		{
			Packet_BlockType blockTypePacket;
			using (FastMemoryStream ms = new FastMemoryStream())
			{
				blockTypePacket = BlockTypeNet.GetBlockTypePacket(block, registry, ms);
			}
			return blockTypePacket;
		}

		public static Packet_BlockType GetBlockTypePacket(Block block, IClassRegistryAPI registry, FastMemoryStream ms)
		{
			Packet_BlockType p = new Packet_BlockType();
			if (block == null)
			{
				return p;
			}
			p.Blockclass = registry.BlockClassToTypeMapping.FirstOrDefault((KeyValuePair<string, Type> x) => x.Value == block.GetType()).Key;
			p.Code = block.Code.ToShortString();
			p.Tags = (from tag in block.Tags.ToArray()
				select (int)tag).ToArray<int>();
			p.IsMissing = ((block.IsMissing > false) ? 1 : 0);
			p.EntityClass = block.EntityClass;
			p.MaxStackSize = block.MaxStackSize;
			p.RainPermeable = ((block.RainPermeable > false) ? 1 : 0);
			p.SetVariant(CollectibleNet.ToPacket(block.VariantStrict));
			if (block.Dimensions != null)
			{
				p.Width = CollectibleNet.SerializeFloatVeryPrecise(block.Dimensions.Width);
				p.Height = CollectibleNet.SerializeFloatVeryPrecise(block.Dimensions.Height);
				p.Length = CollectibleNet.SerializeFloatVeryPrecise(block.Dimensions.Length);
			}
			if (block.BlockBehaviors != null)
			{
				Packet_Behavior[] behaviorCodes = new Packet_Behavior[block.CollectibleBehaviors.Length];
				int i = 0;
				foreach (CollectibleBehavior behavior in block.CollectibleBehaviors)
				{
					behaviorCodes[i++] = new Packet_Behavior
					{
						Code = ((behavior is BlockBehavior) ? registry.GetBlockBehaviorClassName(behavior.GetType()) : registry.GetCollectibleBehaviorClassName(behavior.GetType())),
						Attributes = (behavior.propertiesAtString ?? ""),
						ClientSideOptional = ((behavior.ClientSideOptional > false) ? 1 : 0)
					};
				}
				p.SetBehaviors(behaviorCodes);
			}
			p.EntityBehaviors = JsonUtil.ToString<BlockEntityBehaviorType[]>(block.BlockEntityBehaviors);
			p.BlockId = block.BlockId;
			p.DrawType = (int)block.DrawType;
			p.RenderPass = (int)block.RenderPass;
			if (block.CreativeInventoryTabs != null)
			{
				p.SetCreativeInventoryTabs(block.CreativeInventoryTabs);
			}
			p.VertexFlags = ((block.VertexFlags == null) ? 0 : block.VertexFlags.All);
			p.Frostable = ((block.Frostable > false) ? 1 : 0);
			p.SetLightHsv(new int[]
			{
				(int)block.LightHsv[0],
				(int)block.LightHsv[1],
				(int)block.LightHsv[2]
			});
			if (block.Sounds != null)
			{
				BlockSounds blockSounds = block.Sounds;
				p.Sounds = new Packet_BlockSoundSet
				{
					Break = blockSounds.Break.ToNonNullString(),
					Hit = blockSounds.Hit.ToNonNullString(),
					Walk = blockSounds.Walk.ToNonNullString(),
					Place = blockSounds.Place.ToNonNullString(),
					Inside = blockSounds.Inside.ToNonNullString(),
					Ambient = blockSounds.Ambient.ToNonNullString(),
					AmbientBlockCount = CollectibleNet.SerializeFloat(blockSounds.AmbientBlockCount),
					AmbientSoundType = (int)blockSounds.AmbientSoundType,
					AmbientMaxDistanceMerge = (int)(blockSounds.AmbientMaxDistanceMerge * 100f)
				};
				if (blockSounds.ByTool != null)
				{
					int[] byToolTool = new int[blockSounds.ByTool.Count];
					Packet_BlockSoundSet[] byToolSound = new Packet_BlockSoundSet[blockSounds.ByTool.Count];
					int j = 0;
					foreach (KeyValuePair<EnumTool, BlockSounds> val in blockSounds.ByTool)
					{
						byToolTool[j] = (int)val.Key;
						byToolSound[j] = new Packet_BlockSoundSet
						{
							Break = val.Value.Break.ToNonNullString(),
							Hit = val.Value.Hit.ToNonNullString()
						};
						j++;
					}
					p.Sounds.SetByToolTool(byToolTool);
					p.Sounds.SetByToolSound(byToolSound);
				}
				else
				{
					p.Sounds.SetByToolTool(Array.Empty<int>());
					p.Sounds.SetByToolSound(Array.Empty<Packet_BlockSoundSet>());
				}
			}
			if (block.Textures != null)
			{
				p.SetTextureCodes(block.Textures.Keys.ToArray<string>());
				p.SetCompositeTextures(CollectibleNet.ToPackets(block.Textures.Values.ToArray<CompositeTexture>()));
			}
			if (block.TexturesInventory != null)
			{
				p.SetInventoryTextureCodes(block.TexturesInventory.Keys.ToArray<string>());
				p.SetInventoryCompositeTextures(CollectibleNet.ToPackets(block.TexturesInventory.Values.ToArray<CompositeTexture>()));
			}
			p.MatterState = (int)block.MatterState;
			p.WalkSpeedFloat = CollectibleNet.SerializeFloatVeryPrecise(block.WalkSpeedMultiplier);
			p.DragMultiplierFloat = CollectibleNet.SerializeFloatVeryPrecise(block.DragMultiplier);
			SmallBoolArray blockSideAo = new SmallBoolArray(block.SideAo);
			if (!blockSideAo.All)
			{
				p.SetSideAo(blockSideAo.ToIntArray(6));
			}
			p.SetNeighbourSideAo((int)block.EmitSideAo);
			SmallBoolArray blockSideOpaque = new SmallBoolArray(block.SideOpaque);
			if (!blockSideOpaque.All)
			{
				p.SetSideOpaqueFlags(blockSideOpaque.ToIntArray(6));
			}
			p.SetSideSolidFlags(block.SideSolid.ToIntArray(6));
			p.SeasonColorMap = block.SeasonColorMap;
			p.ClimateColorMap = block.ClimateColorMap;
			p.Fertility = block.Fertility;
			p.Replacable = block.Replaceable;
			p.LightAbsorption = block.LightAbsorption;
			p.Resistance = CollectibleNet.SerializeFloat(block.Resistance);
			p.BlockMaterial = (int)block.BlockMaterial;
			if (block.Shape != null)
			{
				p.Shape = CollectibleNet.ToPacket(block.Shape);
			}
			if (block.ShapeInventory != null)
			{
				p.ShapeInventory = CollectibleNet.ToPacket(block.ShapeInventory);
			}
			if (block.Lod0Shape != null)
			{
				p.Lod0shape = CollectibleNet.ToPacket(block.Lod0Shape);
			}
			if (block.Lod2Shape != null)
			{
				p.Lod2shape = CollectibleNet.ToPacket(block.Lod2Shape);
			}
			p.DoNotRenderAtLod2 = ((block.DoNotRenderAtLod2 > false) ? 1 : 0);
			p.Ambientocclusion = ((block.Ambientocclusion > false) ? 1 : 0);
			if (block.SelectionBoxes != null)
			{
				Packet_Cube[] selectionBoxes = new Packet_Cube[block.SelectionBoxes.Length];
				for (int k = 0; k < selectionBoxes.Length; k++)
				{
					selectionBoxes[k] = BlockTypeNet.SerializeCuboid(block.SelectionBoxes[k]);
				}
				p.SetSelectionBoxes(selectionBoxes);
			}
			if (block.CollisionBoxes != null)
			{
				Packet_Cube[] collisionBoxes = new Packet_Cube[block.CollisionBoxes.Length];
				for (int l = 0; l < collisionBoxes.Length; l++)
				{
					collisionBoxes[l] = BlockTypeNet.SerializeCuboid(block.CollisionBoxes[l]);
				}
				p.SetCollisionBoxes(collisionBoxes);
			}
			if (block.ParticleCollisionBoxes != null)
			{
				Packet_Cube[] ParticleCollisionBoxes = new Packet_Cube[block.ParticleCollisionBoxes.Length];
				for (int m = 0; m < ParticleCollisionBoxes.Length; m++)
				{
					ParticleCollisionBoxes[m] = BlockTypeNet.SerializeCuboid(block.ParticleCollisionBoxes[m]);
				}
				p.SetParticleCollisionBoxes(ParticleCollisionBoxes);
			}
			if (!block.IsMissing)
			{
				if (block.GuiTransform != null)
				{
					p.GuiTransform = CollectibleNet.ToTransformPacket(block.GuiTransform, BlockList.guitf);
				}
				if (block.FpHandTransform != null)
				{
					p.FpHandTransform = CollectibleNet.ToTransformPacket(block.FpHandTransform, BlockList.fptf);
				}
				if (block.TpHandTransform != null)
				{
					p.TpHandTransform = CollectibleNet.ToTransformPacket(block.TpHandTransform, BlockList.tptf);
				}
				if (block.TpOffHandTransform != null)
				{
					p.TpOffHandTransform = CollectibleNet.ToTransformPacket(block.TpOffHandTransform, BlockList.tptf);
				}
				if (block.GroundTransform != null)
				{
					p.GroundTransform = CollectibleNet.ToTransformPacket(block.GroundTransform, BlockList.gndtf);
				}
			}
			if (block.ParticleProperties != null && block.ParticleProperties.Length != 0)
			{
				ms.Reset();
				BinaryWriter writer = new BinaryWriter(ms);
				for (int n = 0; n < block.ParticleProperties.Length; n++)
				{
					block.ParticleProperties[n].ToBytes(writer);
				}
				p.SetParticleProperties(ms.ToArray());
				p.ParticlePropertiesQuantity = block.ParticleProperties.Length;
			}
			p.RandomDrawOffset = block.RandomDrawOffset;
			p.RandomizeAxes = (int)block.RandomizeAxes;
			p.RandomizeRotations = ((block.RandomizeRotations > false) ? 1 : 0);
			p.RandomSizeAdjust = CollectibleNet.SerializeFloatVeryPrecise(block.RandomSizeAdjust);
			p.Climbable = ((block.Climbable > false) ? 1 : 0);
			p.LiquidLevel = block.LiquidLevel;
			p.LiquidCode = block.LiquidCode;
			p.FaceCullMode = (int)block.FaceCullMode;
			if (block.CombustibleProps != null)
			{
				p.CombustibleProps = CollectibleNet.ToPacket(block.CombustibleProps, ms);
			}
			if (block.NutritionProps != null)
			{
				p.NutritionProps = CollectibleNet.ToPacket(block.NutritionProps, ms);
			}
			if (block.TransitionableProps != null)
			{
				p.SetTransitionableProps(CollectibleNet.ToPacket(block.TransitionableProps, ms));
			}
			if (block.GrindingProps != null)
			{
				p.GrindingProps = CollectibleNet.ToPacket(block.GrindingProps, ms);
			}
			if (block.CrushingProps != null)
			{
				p.CrushingProps = CollectibleNet.ToPacket(block.CrushingProps, ms);
			}
			if (block.CreativeInventoryStacks != null)
			{
				ms.Reset();
				BinaryWriter writer2 = new BinaryWriter(ms);
				writer2.Write(block.CreativeInventoryStacks.Length);
				for (int i2 = 0; i2 < block.CreativeInventoryStacks.Length; i2++)
				{
					block.CreativeInventoryStacks[i2].ToBytes(writer2, registry);
				}
				p.SetCreativeInventoryStacks(ms.ToArray());
			}
			if (block.Drops != null)
			{
				List<Packet_BlockDrop> drops = new List<Packet_BlockDrop>();
				for (int i3 = 0; i3 < block.Drops.Length; i3++)
				{
					if (block.Drops[i3].ResolvedItemstack != null)
					{
						drops.Add(BlockTypeNet.ToPacket(block.Drops[i3], ms));
					}
				}
				p.SetDrops(drops.ToArray());
			}
			if (block.CropProps != null)
			{
				p.CropProps = SerializerUtil.Serialize<BlockCropProperties>(block.CropProps);
			}
			p.MaterialDensity = block.MaterialDensity;
			p.AttackPower = CollectibleNet.SerializeFloatPrecise(block.AttackPower);
			p.AttackRange = CollectibleNet.SerializeFloatPrecise(block.AttackRange);
			p.Durability = block.Durability;
			if (block.Attributes != null)
			{
				p.Attributes = block.Attributes.ToString();
			}
			p.LiquidSelectable = ((block.LiquidSelectable > false) ? 1 : 0);
			p.RequiredMiningTier = block.RequiredMiningTier;
			p.MiningTier = block.ToolTier;
			if (block.HeldSounds != null)
			{
				p.HeldSounds = CollectibleNet.ToPacket(block.HeldSounds);
			}
			if (block.MiningSpeed != null)
			{
				Enum.GetValues(typeof(EnumBlockMaterial));
				List<int> miningSpeeds = new List<int>();
				List<int> miningMats = new List<int>();
				foreach (KeyValuePair<EnumBlockMaterial, float> val2 in block.MiningSpeed)
				{
					miningSpeeds.Add(CollectibleNet.SerializeFloat(val2.Value));
					miningMats.Add((int)val2.Key);
				}
				p.SetMiningmaterial(miningMats.ToArray());
				p.SetMiningmaterialspeed(miningSpeeds.ToArray());
			}
			p.StorageFlags = (int)block.StorageFlags;
			p.RenderAlphaTest = CollectibleNet.SerializeFloatVeryPrecise(block.RenderAlphaTest);
			p.HeldTpHitAnimation = block.HeldTpHitAnimation;
			p.HeldRightTpIdleAnimation = block.HeldRightTpIdleAnimation;
			p.HeldLeftTpIdleAnimation = block.HeldLeftTpIdleAnimation;
			p.HeldTpUseAnimation = block.HeldTpUseAnimation;
			p.HeldLeftReadyAnimation = block.HeldLeftReadyAnimation;
			p.HeldRightReadyAnimation = block.HeldRightReadyAnimation;
			return p;
		}

		public static byte[] PackSetBlocksList(List<BlockPos> positions, IBlockAccessor blockAccessor)
		{
			byte[] array;
			using (MemoryStream ms = new MemoryStream())
			{
				BinaryWriter writer = new BinaryWriter(ms);
				writer.Write(positions.Count);
				int[] liquids = new int[positions.Count];
				for (int i = 0; i < positions.Count; i++)
				{
					writer.Write(positions[i].X);
				}
				for (int j = 0; j < positions.Count; j++)
				{
					writer.Write(positions[j].InternalY);
				}
				for (int k = 0; k < positions.Count; k++)
				{
					writer.Write(positions[k].Z);
				}
				for (int l = 0; l < positions.Count; l++)
				{
					int solidBlockId = blockAccessor.GetBlockId(positions[l]);
					int liquidBlockId = blockAccessor.GetBlock(positions[l], 2).BlockId;
					writer.Write((solidBlockId == liquidBlockId) ? 0 : solidBlockId);
					liquids[l] = liquidBlockId;
				}
				for (int m = 0; m < liquids.Length; m++)
				{
					writer.Write(liquids[m]);
				}
				array = Compression.Compress(ms.ToArray());
			}
			return array;
		}

		public static byte[] PackSetDecorsList(WorldChunk chunk, long chunkIndex, IBlockAccessor blockAccessor)
		{
			byte[] array;
			using (MemoryStream ms = new MemoryStream())
			{
				BinaryWriter writer = new BinaryWriter(ms);
				writer.Write(chunkIndex);
				Dictionary<int, Block> decors = chunk.Decors;
				lock (decors)
				{
					writer.Write(chunk.Decors.Count);
					foreach (KeyValuePair<int, Block> val in chunk.Decors)
					{
						Block block = val.Value;
						writer.Write(val.Key);
						writer.Write((block == null) ? 0 : block.Id);
					}
				}
				array = Compression.Compress(ms.ToArray());
			}
			return array;
		}

		public static Dictionary<int, Block> UnpackSetDecors(byte[] data, IWorldAccessor worldAccessor, out long chunkIndex)
		{
			Dictionary<int, Block> dictionary;
			using (MemoryStream ms = new MemoryStream(Compression.Decompress(data)))
			{
				BinaryReader reader = new BinaryReader(ms);
				chunkIndex = reader.ReadInt64();
				int count = reader.ReadInt32();
				Dictionary<int, Block> decors = new Dictionary<int, Block>(count);
				for (int i = 0; i < count; i++)
				{
					int subPosition = reader.ReadInt32();
					int blockID = reader.ReadInt32();
					if (blockID != 0)
					{
						decors.Add(subPosition, worldAccessor.GetBlock(blockID));
					}
				}
				dictionary = decors;
			}
			return dictionary;
		}

		public static KeyValuePair<BlockPos[], int[]> UnpackSetBlocks(byte[] setBlocks, out int[] liquidsLayer)
		{
			KeyValuePair<BlockPos[], int[]> keyValuePair;
			using (MemoryStream ms = new MemoryStream(Compression.Decompress(setBlocks)))
			{
				BinaryReader reader = new BinaryReader(ms);
				int count = reader.ReadInt32();
				BlockPos[] positions = new BlockPos[count];
				int[] blockIds = new int[count];
				for (int i = 0; i < count; i++)
				{
					positions[i] = new BlockPos(reader.ReadInt32(), 0, 0);
				}
				for (int j = 0; j < count; j++)
				{
					int y = reader.ReadInt32();
					positions[j].Y = y % 32768;
					positions[j].dimension = y / 32768;
				}
				for (int k = 0; k < count; k++)
				{
					positions[k].Z = reader.ReadInt32();
				}
				for (int l = 0; l < count; l++)
				{
					blockIds[l] = reader.ReadInt32();
				}
				if (reader.BaseStream.Length > reader.BaseStream.Position)
				{
					liquidsLayer = new int[count];
					for (int m = 0; m < count; m++)
					{
						liquidsLayer[m] = reader.ReadInt32();
					}
				}
				else
				{
					liquidsLayer = null;
				}
				keyValuePair = new KeyValuePair<BlockPos[], int[]>(positions, blockIds);
			}
			return keyValuePair;
		}

		public static byte[] PackBlocksPositions(List<BlockPos> positions)
		{
			byte[] array;
			using (MemoryStream ms = new MemoryStream())
			{
				BinaryWriter writer = new BinaryWriter(ms);
				writer.Write(positions.Count);
				for (int i = 0; i < positions.Count; i++)
				{
					writer.Write(positions[i].X);
				}
				for (int j = 0; j < positions.Count; j++)
				{
					writer.Write(positions[j].InternalY);
				}
				for (int k = 0; k < positions.Count; k++)
				{
					writer.Write(positions[k].Z);
				}
				array = Compression.Compress(ms.ToArray());
			}
			return array;
		}

		public static BlockPos[] UnpackBlockPositions(byte[] setBlocks)
		{
			BlockPos[] array;
			using (MemoryStream ms = new MemoryStream(Compression.Decompress(setBlocks)))
			{
				BinaryReader reader = new BinaryReader(ms);
				int count = reader.ReadInt32();
				BlockPos[] positions = new BlockPos[count];
				for (int i = 0; i < count; i++)
				{
					positions[i] = new BlockPos(reader.ReadInt32(), 0, 0);
				}
				for (int j = 0; j < count; j++)
				{
					int y = reader.ReadInt32();
					positions[j].Y = y % 32768;
					positions[j].dimension = y / 32768;
				}
				for (int k = 0; k < count; k++)
				{
					positions[k].Z = reader.ReadInt32();
				}
				array = positions;
			}
			return array;
		}

		private static BlockDropItemStack FromPacket(Packet_BlockDrop packet, IWorldAccessor world)
		{
			BlockDropItemStack drop = new BlockDropItemStack();
			drop.Quantity = new NatFloat(CollectibleNet.DeserializeFloat(packet.QuantityAvg), CollectibleNet.DeserializeFloat(packet.QuantityVar), (EnumDistribution)packet.QuantityDist);
			if (packet.Tool < 99 && packet.Tool >= 0)
			{
				drop.Tool = new EnumTool?((EnumTool)packet.Tool);
			}
			using (MemoryStream ms = new MemoryStream(packet.DroppedStack))
			{
				BinaryReader reader = new BinaryReader(ms);
				drop.ResolvedItemstack = new ItemStack(reader);
			}
			return drop;
		}

		private static Packet_BlockDrop ToPacket(BlockDropItemStack drop, FastMemoryStream ms)
		{
			Packet_BlockDrop packet = new Packet_BlockDrop
			{
				QuantityAvg = CollectibleNet.SerializeFloat(drop.Quantity.avg),
				QuantityDist = (int)drop.Quantity.dist,
				QuantityVar = CollectibleNet.SerializeFloat(drop.Quantity.var)
			};
			if (drop.Tool != null)
			{
				packet.Tool = (int)drop.Tool.Value;
			}
			else
			{
				packet.Tool = 99;
			}
			ms.Reset();
			BinaryWriter writer = new BinaryWriter(ms);
			drop.ResolvedItemstack.ToBytes(writer);
			packet.SetDroppedStack(ms.ToArray());
			return packet;
		}

		private static Cuboidf DeserializeCuboid(Packet_Cube packet)
		{
			return new Cuboidf
			{
				X1 = CollectibleNet.DeserializeFloatVeryPrecise(packet.Minx),
				Y1 = CollectibleNet.DeserializeFloatVeryPrecise(packet.Miny),
				Z1 = CollectibleNet.DeserializeFloatVeryPrecise(packet.Minz),
				X2 = CollectibleNet.DeserializeFloatVeryPrecise(packet.Maxx),
				Y2 = CollectibleNet.DeserializeFloatVeryPrecise(packet.Maxy),
				Z2 = CollectibleNet.DeserializeFloatVeryPrecise(packet.Maxz)
			};
		}

		private static Packet_Cube SerializeCuboid(Cuboidf cube)
		{
			return new Packet_Cube
			{
				Minx = CollectibleNet.SerializeFloatVeryPrecise(cube.X1),
				Miny = CollectibleNet.SerializeFloatVeryPrecise(cube.Y1),
				Minz = CollectibleNet.SerializeFloatVeryPrecise(cube.Z1),
				Maxx = CollectibleNet.SerializeFloatVeryPrecise(cube.X2),
				Maxy = CollectibleNet.SerializeFloatVeryPrecise(cube.Y2),
				Maxz = CollectibleNet.SerializeFloatVeryPrecise(cube.Z2)
			};
		}
	}
}
