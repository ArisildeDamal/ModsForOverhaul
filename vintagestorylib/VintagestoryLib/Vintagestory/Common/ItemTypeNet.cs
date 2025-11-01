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
	public class ItemTypeNet : CollectibleNet
	{
		public static Item ReadItemTypePacket(Packet_ItemType packet, IWorldAccessor world, ClassRegistry registry)
		{
			Item item = registry.CreateItem(packet.ItemClass);
			item.Code = new AssetLocation(packet.Code);
			item.IsMissing = packet.IsMissing > 0;
			item.ItemId = packet.ItemId;
			Item item2 = item;
			ItemTagArray itemTagArray;
			if (packet.Tags == null)
			{
				itemTagArray = new ItemTagArray();
			}
			else
			{
				itemTagArray = new ItemTagArray(packet.Tags.Select((int tag) => (ushort)tag));
			}
			item2.Tags = itemTagArray;
			item.MaxStackSize = packet.MaxStackSize;
			item.VariantStrict = CollectibleNet.FromPacket(packet.Variant, packet.VariantCount);
			item.Variant = new RelaxedReadOnlyDictionary<string, string>(item.VariantStrict);
			item.Dimensions = ((packet.Width + packet.Height + packet.Length == 0) ? CollectibleObject.DefaultSize : new Size3f(CollectibleNet.DeserializeFloatVeryPrecise(packet.Width), CollectibleNet.DeserializeFloatVeryPrecise(packet.Height), CollectibleNet.DeserializeFloatVeryPrecise(packet.Length)));
			if (packet.LightHsv != null && packet.LightHsvCount > 2)
			{
				item.LightHsv = new byte[]
				{
					(byte)packet.LightHsv[0],
					(byte)packet.LightHsv[1],
					(byte)packet.LightHsv[2]
				};
			}
			else
			{
				item.LightHsv = new byte[3];
			}
			if (packet.BehaviorsCount > 0)
			{
				List<CollectibleBehavior> colbehaviors = new List<CollectibleBehavior>();
				for (int i = 0; i < packet.BehaviorsCount; i++)
				{
					Packet_Behavior bhpkt = packet.Behaviors[i];
					bool hasColBehavior = registry.collectibleBehaviorToTypeMapping.ContainsKey(bhpkt.Code);
					if (bhpkt.ClientSideOptional <= 0 || hasColBehavior)
					{
						CollectibleBehavior bh = registry.CreateCollectibleBehavior(item, bhpkt.Code);
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
					}
				}
				item.CollectibleBehaviors = colbehaviors.ToArray();
			}
			item.Textures = new Dictionary<string, CompositeTexture>(packet.TextureCodesCount);
			for (int j = 0; j < packet.TextureCodesCount; j++)
			{
				item.Textures[packet.TextureCodes[j]] = CollectibleNet.FromPacket(packet.CompositeTextures[j]);
			}
			item.CreativeInventoryTabs = new string[packet.CreativeInventoryTabsCount];
			for (int k = 0; k < item.CreativeInventoryTabs.Length; k++)
			{
				item.CreativeInventoryTabs[k] = packet.CreativeInventoryTabs[k];
			}
			if (item.IsMissing)
			{
				item.GuiTransform = CollectibleNet.DefGuiTransform;
				item.FpHandTransform = CollectibleNet.DefFpHandTransform;
				item.TpHandTransform = CollectibleNet.DefTpHandTransform;
				item.TpOffHandTransform = CollectibleNet.DefTpOffHandTransform;
				item.GroundTransform = CollectibleNet.DefGroundTransform;
			}
			else
			{
				item.GuiTransform = ((packet.GuiTransform == null) ? ModelTransform.ItemDefaultGui() : CollectibleNet.FromTransformPacket(packet.GuiTransform));
				item.FpHandTransform = ((packet.FpHandTransform == null) ? ModelTransform.ItemDefaultFp() : CollectibleNet.FromTransformPacket(packet.FpHandTransform));
				item.TpHandTransform = ((packet.TpHandTransform == null) ? ModelTransform.ItemDefaultTp() : CollectibleNet.FromTransformPacket(packet.TpHandTransform));
				item.TpOffHandTransform = ((packet.TpOffHandTransform == null) ? item.TpHandTransform.Clone() : CollectibleNet.FromTransformPacket(packet.TpOffHandTransform));
				item.GroundTransform = ((packet.GroundTransform == null) ? ModelTransform.ItemDefaultGround() : CollectibleNet.FromTransformPacket(packet.GroundTransform));
			}
			item.MatterState = (EnumMatterState)packet.MatterState;
			if (packet.HeldSounds != null)
			{
				item.HeldSounds = CollectibleNet.FromPacket(packet.HeldSounds);
			}
			if (packet.Miningmaterial != null)
			{
				item.MiningSpeed = new Dictionary<EnumBlockMaterial, float>();
				for (int l = 0; l < packet.MiningmaterialCount; l++)
				{
					int m = packet.Miningmaterial[l];
					float speed = CollectibleNet.DeserializeFloat(packet.Miningmaterialspeed[l]);
					item.MiningSpeed[(EnumBlockMaterial)m] = speed;
				}
			}
			item.Durability = packet.Durability;
			item.DamagedBy = new EnumItemDamageSource[packet.DamagedbyCount];
			for (int n = 0; n < packet.DamagedbyCount; n++)
			{
				item.DamagedBy[n] = (EnumItemDamageSource)packet.Damagedby[n];
			}
			if (packet.Attributes != null && packet.Attributes.Length > 0)
			{
				item.Attributes = new JsonObject(JToken.Parse(packet.Attributes));
			}
			if (packet.CombustibleProps != null)
			{
				item.CombustibleProps = CollectibleNet.FromPacket(packet.CombustibleProps, world);
			}
			if (packet.NutritionProps != null)
			{
				item.NutritionProps = CollectibleNet.FromPacket(packet.NutritionProps, world);
			}
			if (packet.TransitionableProps != null)
			{
				item.TransitionableProps = CollectibleNet.FromPacket(packet.TransitionableProps, world);
			}
			if (packet.GrindingProps != null)
			{
				item.GrindingProps = CollectibleNet.FromPacket(packet.GrindingProps, world);
			}
			if (packet.CrushingProps != null)
			{
				item.CrushingProps = CollectibleNet.FromPacket(packet.CrushingProps, world);
			}
			if (packet.CreativeInventoryStacks != null)
			{
				using (MemoryStream ms = new MemoryStream(packet.CreativeInventoryStacks))
				{
					BinaryReader reader = new BinaryReader(ms);
					int count = reader.ReadInt32();
					item.CreativeInventoryStacks = new CreativeTabAndStackList[count];
					for (int i2 = 0; i2 < count; i2++)
					{
						item.CreativeInventoryStacks[i2] = new CreativeTabAndStackList();
						item.CreativeInventoryStacks[i2].FromBytes(reader, world.ClassRegistry);
					}
				}
			}
			if (packet.Shape != null)
			{
				item.Shape = CollectibleNet.FromPacket(packet.Shape);
			}
			if (packet.Tool >= 0)
			{
				item.Tool = new EnumTool?((EnumTool)packet.Tool);
			}
			item.MaterialDensity = packet.MaterialDensity;
			item.AttackPower = CollectibleNet.DeserializeFloatPrecise(packet.AttackPower);
			item.AttackRange = CollectibleNet.DeserializeFloatPrecise(packet.AttackRange);
			item.LiquidSelectable = packet.LiquidSelectable > 0;
			item.ToolTier = packet.MiningTier;
			item.StorageFlags = (EnumItemStorageFlags)packet.StorageFlags;
			item.RenderAlphaTest = CollectibleNet.DeserializeFloatVeryPrecise(packet.RenderAlphaTest);
			item.HeldTpHitAnimation = packet.HeldTpHitAnimation;
			item.HeldRightTpIdleAnimation = packet.HeldRightTpIdleAnimation;
			item.HeldLeftTpIdleAnimation = packet.HeldLeftTpIdleAnimation;
			item.HeldLeftReadyAnimation = packet.HeldLeftReadyAnimation;
			item.HeldRightReadyAnimation = packet.HeldRightReadyAnimation;
			item.HeldTpUseAnimation = packet.HeldTpUseAnimation;
			return item;
		}

		public static Packet_ItemType GetItemTypePacket(Item item, IClassRegistryAPI registry)
		{
			Packet_ItemType itemTypePacket;
			using (FastMemoryStream ms = new FastMemoryStream())
			{
				itemTypePacket = ItemTypeNet.GetItemTypePacket(item, registry, ms);
			}
			return itemTypePacket;
		}

		public static Packet_ItemType GetItemTypePacket(Item item, IClassRegistryAPI registry, FastMemoryStream ms)
		{
			Packet_ItemType packet = new Packet_ItemType();
			if (item == null)
			{
				return packet;
			}
			packet.Code = item.Code.ToShortString();
			packet.SetVariant(CollectibleNet.ToPacket(item.VariantStrict));
			packet.ItemId = item.ItemId;
			packet.Tags = (from tag in item.Tags.ToArray()
				select (int)tag).ToArray<int>();
			packet.MaxStackSize = item.MaxStackSize;
			packet.IsMissing = ((item.IsMissing > false) ? 1 : 0);
			packet.ItemClass = registry.ItemClassToTypeMapping.FirstOrDefault((KeyValuePair<string, Type> x) => x.Value == item.GetType()).Key;
			if (item.Dimensions != null)
			{
				packet.Width = CollectibleNet.SerializeFloatVeryPrecise(item.Dimensions.Width);
				packet.Height = CollectibleNet.SerializeFloatVeryPrecise(item.Dimensions.Height);
				packet.Length = CollectibleNet.SerializeFloatVeryPrecise(item.Dimensions.Length);
			}
			packet.SetLightHsv(new int[]
			{
				(int)item.LightHsv[0],
				(int)item.LightHsv[1],
				(int)item.LightHsv[2]
			});
			if (item.Textures != null)
			{
				packet.SetTextureCodes(item.Textures.Keys.ToArray<string>());
				packet.SetCompositeTextures(CollectibleNet.ToPackets(item.Textures.Values.ToArray<CompositeTexture>()));
			}
			if (item.CreativeInventoryTabs != null)
			{
				packet.SetCreativeInventoryTabs(item.CreativeInventoryTabs);
			}
			if (item.CollectibleBehaviors != null)
			{
				Packet_Behavior[] blockBehaviorCodes = new Packet_Behavior[item.CollectibleBehaviors.Length];
				int i = 0;
				foreach (CollectibleBehavior behavior in item.CollectibleBehaviors)
				{
					blockBehaviorCodes[i++] = new Packet_Behavior
					{
						Code = registry.GetCollectibleBehaviorClassName(behavior.GetType()),
						Attributes = (behavior.propertiesAtString ?? ""),
						ClientSideOptional = ((behavior.ClientSideOptional > false) ? 1 : 0)
					};
				}
				packet.SetBehaviors(blockBehaviorCodes);
			}
			if (!item.IsMissing)
			{
				if (item.GuiTransform != null)
				{
					packet.GuiTransform = CollectibleNet.ToTransformPacket(item.GuiTransform, ItemTypeNet.tfDefaultGui);
				}
				if (item.FpHandTransform != null)
				{
					packet.FpHandTransform = CollectibleNet.ToTransformPacket(item.FpHandTransform, ItemTypeNet.tfDefaultFp);
				}
				if (item.TpHandTransform != null)
				{
					packet.TpHandTransform = CollectibleNet.ToTransformPacket(item.TpHandTransform, ItemTypeNet.tfDefaultTp);
				}
				if (item.TpOffHandTransform != null)
				{
					packet.TpOffHandTransform = CollectibleNet.ToTransformPacket(item.TpOffHandTransform, ItemTypeNet.tfDefaultTp);
				}
				if (item.GroundTransform != null)
				{
					packet.GroundTransform = CollectibleNet.ToTransformPacket(item.GroundTransform, ItemTypeNet.tfDefaultGround);
				}
			}
			if (item.MiningSpeed != null)
			{
				Enum.GetValues(typeof(EnumBlockMaterial));
				List<int> miningSpeeds = new List<int>();
				List<int> miningMats = new List<int>();
				foreach (KeyValuePair<EnumBlockMaterial, float> val in item.MiningSpeed)
				{
					miningSpeeds.Add(CollectibleNet.SerializeFloat(val.Value));
					miningMats.Add((int)val.Key);
				}
				packet.SetMiningmaterial(miningMats.ToArray());
				packet.SetMiningmaterialspeed(miningSpeeds.ToArray());
			}
			if (item.HeldSounds != null)
			{
				packet.HeldSounds = CollectibleNet.ToPacket(item.HeldSounds);
			}
			packet.Durability = item.Durability;
			if (item.DamagedBy != null)
			{
				int[] damagedby = new int[item.DamagedBy.Length];
				for (int j = 0; j < damagedby.Length; j++)
				{
					damagedby[j] = (int)item.DamagedBy[j];
				}
				packet.SetDamagedby(damagedby);
			}
			if (item.Attributes != null)
			{
				packet.Attributes = item.Attributes.ToString();
			}
			if (item.CombustibleProps != null)
			{
				packet.CombustibleProps = CollectibleNet.ToPacket(item.CombustibleProps, ms);
			}
			if (item.NutritionProps != null)
			{
				packet.NutritionProps = CollectibleNet.ToPacket(item.NutritionProps, ms);
			}
			if (item.TransitionableProps != null)
			{
				packet.SetTransitionableProps(CollectibleNet.ToPacket(item.TransitionableProps, ms));
			}
			if (item.GrindingProps != null)
			{
				packet.GrindingProps = CollectibleNet.ToPacket(item.GrindingProps, ms);
			}
			if (item.CrushingProps != null)
			{
				packet.CrushingProps = CollectibleNet.ToPacket(item.CrushingProps, ms);
			}
			if (item.CreativeInventoryStacks != null)
			{
				ms.Reset();
				BinaryWriter writer = new BinaryWriter(ms);
				writer.Write(item.CreativeInventoryStacks.Length);
				for (int k = 0; k < item.CreativeInventoryStacks.Length; k++)
				{
					item.CreativeInventoryStacks[k].ToBytes(writer, registry);
				}
				packet.SetCreativeInventoryStacks(ms.ToArray());
			}
			if (item.Shape != null)
			{
				packet.Shape = CollectibleNet.ToPacket(item.Shape);
			}
			if (item.Tool == null)
			{
				packet.Tool = -1;
			}
			else
			{
				packet.Tool = (int)item.Tool.Value;
			}
			packet.MaterialDensity = item.MaterialDensity;
			packet.AttackPower = CollectibleNet.SerializeFloatPrecise(item.AttackPower);
			packet.AttackRange = CollectibleNet.SerializeFloatPrecise(item.AttackRange);
			packet.LiquidSelectable = ((item.LiquidSelectable > false) ? 1 : 0);
			packet.MiningTier = item.ToolTier;
			packet.StorageFlags = (int)item.StorageFlags;
			packet.RenderAlphaTest = CollectibleNet.SerializeFloatVeryPrecise(item.RenderAlphaTest);
			packet.HeldTpHitAnimation = item.HeldTpHitAnimation;
			packet.HeldRightTpIdleAnimation = item.HeldRightTpIdleAnimation;
			packet.HeldLeftTpIdleAnimation = item.HeldLeftTpIdleAnimation;
			packet.HeldLeftReadyAnimation = item.HeldLeftReadyAnimation;
			packet.HeldRightReadyAnimation = item.HeldRightReadyAnimation;
			packet.HeldTpUseAnimation = item.HeldTpUseAnimation;
			packet.MatterState = (int)item.MatterState;
			return packet;
		}

		private static ModelTransform tfDefaultGui = ModelTransform.ItemDefaultGui();

		private static ModelTransform tfDefaultFp = ModelTransform.ItemDefaultFp();

		private static ModelTransform tfDefaultTp = ModelTransform.ItemDefaultTp();

		private static ModelTransform tfDefaultGround = ModelTransform.ItemDefaultGround();
	}
}
