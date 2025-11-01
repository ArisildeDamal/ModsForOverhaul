using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common
{
	public abstract class CollectibleNet
	{
		public static int SerializeFloat(float p)
		{
			return (int)(p * 64f);
		}

		public static float DeserializeFloat(int p)
		{
			return (float)p / 64f;
		}

		public static long SerializeDouble(double p)
		{
			return (long)(p * 1024.0);
		}

		public static double DeserializeDouble(long p)
		{
			return (double)p / 1024.0;
		}

		public static long SerializeDoublePrecise(double p)
		{
			return (long)(p * 16384.0);
		}

		public static double DeserializeDoublePrecise(long p)
		{
			return (double)p / 16384.0;
		}

		public static int SerializeFloatPrecise(float v)
		{
			return (int)(v * 1024f);
		}

		public static float DeserializeFloatPrecise(int v)
		{
			return (float)v / 1024f;
		}

		public static int SerializePlayerPos(double v)
		{
			return (int)(v * 10240.0 * 1000.0);
		}

		public static double DeserializePlayerPos(int v)
		{
			return (double)v / 10240000.0;
		}

		public static int SerializeFloatVeryPrecise(float v)
		{
			return (int)(v * 10000f);
		}

		public static float DeserializeFloatVeryPrecise(int v)
		{
			return (float)v / 10000f;
		}

		public static Packet_HeldSoundSet ToPacket(HeldSounds sounds)
		{
			Packet_HeldSoundSet packet_HeldSoundSet = new Packet_HeldSoundSet();
			AssetLocation idle = sounds.Idle;
			packet_HeldSoundSet.Idle = ((idle != null) ? idle.ToString() : null);
			AssetLocation attack = sounds.Attack;
			packet_HeldSoundSet.Attack = ((attack != null) ? attack.ToString() : null);
			AssetLocation equip = sounds.Equip;
			packet_HeldSoundSet.Equip = ((equip != null) ? equip.ToString() : null);
			AssetLocation unequip = sounds.Unequip;
			packet_HeldSoundSet.Unequip = ((unequip != null) ? unequip.ToString() : null);
			AssetLocation invPickup = sounds.InvPickup;
			packet_HeldSoundSet.InvPickup = ((invPickup != null) ? invPickup.ToString() : null);
			AssetLocation invPlace = sounds.InvPlace;
			packet_HeldSoundSet.InvPlace = ((invPlace != null) ? invPlace.ToString() : null);
			return packet_HeldSoundSet;
		}

		public static Packet_VariantPart[] ToPacket(OrderedDictionary<string, string> variant)
		{
			Packet_VariantPart[] p = new Packet_VariantPart[variant.Count];
			int i = 0;
			foreach (KeyValuePair<string, string> val in variant)
			{
				p[i++] = new Packet_VariantPart
				{
					Code = val.Key,
					Value = val.Value
				};
			}
			return p;
		}

		public static Packet_NutritionProperties ToPacket(FoodNutritionProperties props)
		{
			if (props.EatenStack == null)
			{
				return CollectibleNet.ToPacket(props, null);
			}
			Packet_NutritionProperties packet_NutritionProperties;
			using (FastMemoryStream ms = new FastMemoryStream())
			{
				packet_NutritionProperties = CollectibleNet.ToPacket(props, ms);
			}
			return packet_NutritionProperties;
		}

		public static Packet_NutritionProperties ToPacket(FoodNutritionProperties props, FastMemoryStream ms)
		{
			Packet_NutritionProperties p = new Packet_NutritionProperties
			{
				FoodCategory = (int)props.FoodCategory,
				Saturation = CollectibleNet.SerializeFloat(props.Satiety),
				Health = CollectibleNet.SerializeFloat(props.Health)
			};
			if (props.EatenStack != null)
			{
				ms.Reset();
				BinaryWriter writer = new BinaryWriter(ms);
				props.EatenStack.ToBytes(writer);
				p.SetEatenStack(ms.ToArray());
			}
			return p;
		}

		public static Packet_TransitionableProperties[] ToPacket(TransitionableProperties[] mprops)
		{
			Packet_TransitionableProperties[] array;
			using (FastMemoryStream ms = new FastMemoryStream())
			{
				array = CollectibleNet.ToPacket(mprops, ms);
			}
			return array;
		}

		public static Packet_TransitionableProperties[] ToPacket(TransitionableProperties[] mprops, FastMemoryStream ms)
		{
			Packet_TransitionableProperties[] packets = new Packet_TransitionableProperties[mprops.Length];
			for (int i = 0; i < mprops.Length; i++)
			{
				TransitionableProperties props = mprops[i];
				Packet_TransitionableProperties p = new Packet_TransitionableProperties
				{
					FreshHours = CollectibleNet.ToPacket(props.FreshHours),
					TransitionHours = CollectibleNet.ToPacket(props.TransitionHours),
					TransitionRatio = CollectibleNet.SerializeFloat(props.TransitionRatio),
					Type = (int)props.Type
				};
				if (props.TransitionedStack != null)
				{
					ms.Reset();
					BinaryWriter writer = new BinaryWriter(ms);
					props.TransitionedStack.ToBytes(writer);
					p.SetTransitionedStack(ms.ToArray());
				}
				packets[i] = p;
			}
			return packets;
		}

		public static Packet_NatFloat ToPacket(NatFloat val)
		{
			return new Packet_NatFloat
			{
				Avg = CollectibleNet.SerializeFloatPrecise(val.avg),
				Var = CollectibleNet.SerializeFloatPrecise(val.var),
				Dist = (int)val.dist
			};
		}

		public static NatFloat FromPacket(Packet_NatFloat val)
		{
			return new NatFloat(CollectibleNet.DeserializeFloatPrecise(val.Avg), CollectibleNet.DeserializeFloatPrecise(val.Var), (EnumDistribution)val.Dist);
		}

		public static Packet_GrindingProperties ToPacket(GrindingProperties props)
		{
			if (props.GroundStack == null)
			{
				return CollectibleNet.ToPacket(props, null);
			}
			Packet_GrindingProperties packet_GrindingProperties;
			using (FastMemoryStream ms = new FastMemoryStream())
			{
				packet_GrindingProperties = CollectibleNet.ToPacket(props, ms);
			}
			return packet_GrindingProperties;
		}

		public static Packet_GrindingProperties ToPacket(GrindingProperties props, FastMemoryStream ms)
		{
			Packet_GrindingProperties p = new Packet_GrindingProperties();
			if (props.GroundStack != null)
			{
				ms.Reset();
				BinaryWriter writer = new BinaryWriter(ms);
				props.GroundStack.ToBytes(writer);
				p.SetGroundStack(ms.ToArray());
			}
			return p;
		}

		public static Packet_CrushingProperties ToPacket(CrushingProperties props)
		{
			if (props.CrushedStack == null)
			{
				return CollectibleNet.ToPacket(props, null);
			}
			Packet_CrushingProperties packet_CrushingProperties;
			using (FastMemoryStream ms = new FastMemoryStream())
			{
				packet_CrushingProperties = CollectibleNet.ToPacket(props, ms);
			}
			return packet_CrushingProperties;
		}

		public static Packet_CrushingProperties ToPacket(CrushingProperties props, FastMemoryStream ms)
		{
			Packet_CrushingProperties p = new Packet_CrushingProperties();
			if (props.CrushedStack != null)
			{
				ms.Reset();
				BinaryWriter writer = new BinaryWriter(ms);
				props.CrushedStack.ToBytes(writer);
				p.SetCrushedStack(ms.ToArray());
				p.HardnessTier = props.HardnessTier;
				p.Quantity = CollectibleNet.ToPacket(props.Quantity);
			}
			return p;
		}

		public static HeldSounds FromPacket(Packet_HeldSoundSet p)
		{
			return new HeldSounds
			{
				Idle = ((p.Idle == null) ? null : new AssetLocation(p.Idle)),
				Equip = ((p.Equip == null) ? null : new AssetLocation(p.Equip)),
				Unequip = ((p.Unequip == null) ? null : new AssetLocation(p.Unequip)),
				Attack = ((p.Attack == null) ? null : new AssetLocation(p.Attack)),
				InvPickup = ((p.InvPickup == null) ? null : new AssetLocation(p.InvPickup)),
				InvPlace = ((p.InvPlace == null) ? null : new AssetLocation(p.InvPlace))
			};
		}

		public static GrindingProperties FromPacket(Packet_GrindingProperties pn, IWorldAccessor world)
		{
			GrindingProperties props = new GrindingProperties();
			if (pn.GroundStack != null)
			{
				using (MemoryStream ms = new MemoryStream(pn.GroundStack))
				{
					BinaryReader reader = new BinaryReader(ms);
					props.GroundStack = new JsonItemStack();
					props.GroundStack.FromBytes(reader, world.ClassRegistry);
				}
			}
			return props;
		}

		public static CrushingProperties FromPacket(Packet_CrushingProperties pn, IWorldAccessor world)
		{
			CrushingProperties props = new CrushingProperties();
			if (pn.CrushedStack != null)
			{
				using (MemoryStream ms = new MemoryStream(pn.CrushedStack))
				{
					BinaryReader reader = new BinaryReader(ms);
					props.CrushedStack = new JsonItemStack();
					props.CrushedStack.FromBytes(reader, world.ClassRegistry);
					props.HardnessTier = pn.HardnessTier;
					if (pn.Quantity != null)
					{
						props.Quantity = CollectibleNet.FromPacket(pn.Quantity);
					}
				}
			}
			return props;
		}

		public static FoodNutritionProperties FromPacket(Packet_NutritionProperties pn, IWorldAccessor world)
		{
			FoodNutritionProperties props = new FoodNutritionProperties
			{
				FoodCategory = (EnumFoodCategory)pn.FoodCategory,
				Satiety = CollectibleNet.DeserializeFloat(pn.Saturation),
				Health = CollectibleNet.DeserializeFloat(pn.Health)
			};
			if (pn.EatenStack != null)
			{
				using (MemoryStream ms = new MemoryStream(pn.EatenStack))
				{
					BinaryReader reader = new BinaryReader(ms);
					props.EatenStack = new JsonItemStack();
					props.EatenStack.FromBytes(reader, world.ClassRegistry);
				}
			}
			return props;
		}

		public static TransitionableProperties[] FromPacket(Packet_TransitionableProperties[] pns, IWorldAccessor world)
		{
			List<TransitionableProperties> mprops = new List<TransitionableProperties>();
			foreach (Packet_TransitionableProperties pn in pns)
			{
				if (pn != null)
				{
					TransitionableProperties props = new TransitionableProperties
					{
						FreshHours = CollectibleNet.FromPacket(pn.FreshHours),
						TransitionHours = CollectibleNet.FromPacket(pn.TransitionHours),
						TransitionRatio = CollectibleNet.DeserializeFloat(pn.TransitionRatio),
						Type = (EnumTransitionType)pn.Type
					};
					if (pn.TransitionedStack != null)
					{
						using (MemoryStream ms = new MemoryStream(pn.TransitionedStack))
						{
							BinaryReader reader = new BinaryReader(ms);
							props.TransitionedStack = new JsonItemStack();
							props.TransitionedStack.FromBytes(reader, world.ClassRegistry);
						}
					}
					mprops.Add(props);
				}
			}
			return mprops.ToArray();
		}

		public static Packet_CombustibleProperties ToPacket(CombustibleProperties props)
		{
			if (props.SmeltedStack == null)
			{
				return CollectibleNet.ToPacket(props, null);
			}
			Packet_CombustibleProperties packet_CombustibleProperties;
			using (FastMemoryStream ms = new FastMemoryStream())
			{
				packet_CombustibleProperties = CollectibleNet.ToPacket(props, ms);
			}
			return packet_CombustibleProperties;
		}

		public static Packet_CombustibleProperties ToPacket(CombustibleProperties props, FastMemoryStream ms)
		{
			Packet_CombustibleProperties p = new Packet_CombustibleProperties
			{
				BurnDuration = CollectibleNet.SerializeFloat(props.BurnDuration),
				BurnTemperature = props.BurnTemperature,
				HeatResistance = props.HeatResistance,
				MeltingDuration = CollectibleNet.SerializeFloat(props.MeltingDuration),
				MeltingPoint = props.MeltingPoint,
				SmeltedRatio = props.SmeltedRatio,
				RequiresContainer = ((props.RequiresContainer > false) ? 1 : 0),
				MeltingType = (int)props.SmeltingType,
				MaxTemperature = props.MaxTemperature
			};
			if (props.SmeltedStack != null)
			{
				ms.Reset();
				BinaryWriter writer = new BinaryWriter(ms);
				props.SmeltedStack.ToBytes(writer);
				p.SetSmeltedStack(ms.ToArray());
			}
			return p;
		}

		public static CombustibleProperties FromPacket(Packet_CombustibleProperties pc, IWorldAccessor world)
		{
			CombustibleProperties props = new CombustibleProperties
			{
				BurnDuration = CollectibleNet.DeserializeFloat(pc.BurnDuration),
				HeatResistance = pc.HeatResistance,
				BurnTemperature = pc.BurnTemperature,
				MeltingPoint = pc.MeltingPoint,
				MeltingDuration = CollectibleNet.DeserializeFloat(pc.MeltingDuration),
				SmeltedRatio = pc.SmeltedRatio,
				RequiresContainer = (pc.RequiresContainer > 0),
				SmeltingType = (EnumSmeltType)pc.MeltingType,
				MaxTemperature = pc.MaxTemperature
			};
			if (pc.SmeltedStack != null)
			{
				using (MemoryStream ms = new MemoryStream(pc.SmeltedStack))
				{
					BinaryReader reader = new BinaryReader(ms);
					props.SmeltedStack = new JsonItemStack();
					props.SmeltedStack.FromBytes(reader, world.ClassRegistry);
				}
			}
			return props;
		}

		public static Packet_ModelTransform ToTransformPacket(ModelTransform transform, ModelTransform defaultTf)
		{
			FastVec3f rotation = (transform.Rotation.Equals(ModelTransformNoDefaults.defaultTf) ? defaultTf.Rotation : transform.Rotation);
			FastVec3f origin = transform.Origin;
			FastVec3f translation = (transform.Translation.Equals(ModelTransformNoDefaults.defaultTf) ? defaultTf.Translation : transform.Translation);
			FastVec3f scaleXYZ = transform.ScaleXYZ;
			return new Packet_ModelTransform
			{
				RotateX = CollectibleNet.SerializeFloatVeryPrecise(rotation.X),
				RotateY = CollectibleNet.SerializeFloatVeryPrecise(rotation.Y),
				RotateZ = CollectibleNet.SerializeFloatVeryPrecise(rotation.Z),
				OriginX = CollectibleNet.SerializeFloatVeryPrecise(origin.X),
				OriginY = CollectibleNet.SerializeFloatVeryPrecise(origin.Y),
				OriginZ = CollectibleNet.SerializeFloatVeryPrecise(origin.Z),
				TranslateX = CollectibleNet.SerializeFloatVeryPrecise(translation.X),
				TranslateY = CollectibleNet.SerializeFloatVeryPrecise(translation.Y),
				TranslateZ = CollectibleNet.SerializeFloatVeryPrecise(translation.Z),
				ScaleX = CollectibleNet.SerializeFloatVeryPrecise(scaleXYZ.X),
				ScaleY = CollectibleNet.SerializeFloatVeryPrecise(scaleXYZ.Y),
				ScaleZ = CollectibleNet.SerializeFloatVeryPrecise(scaleXYZ.Z),
				Rotate = ((transform.Rotate > false) ? 1 : 0)
			};
		}

		public static ModelTransform FromTransformPacket(Packet_ModelTransform p)
		{
			return new ModelTransform
			{
				Rotation = new FastVec3f(CollectibleNet.DeserializeFloatVeryPrecise(p.RotateX), CollectibleNet.DeserializeFloatVeryPrecise(p.RotateY), CollectibleNet.DeserializeFloatVeryPrecise(p.RotateZ)),
				Origin = new FastVec3f(CollectibleNet.DeserializeFloatVeryPrecise(p.OriginX), CollectibleNet.DeserializeFloatVeryPrecise(p.OriginY), CollectibleNet.DeserializeFloatVeryPrecise(p.OriginZ)),
				Translation = new FastVec3f(CollectibleNet.DeserializeFloatVeryPrecise(p.TranslateX), CollectibleNet.DeserializeFloatVeryPrecise(p.TranslateY), CollectibleNet.DeserializeFloatVeryPrecise(p.TranslateZ)),
				ScaleXYZ = new FastVec3f(CollectibleNet.DeserializeFloatVeryPrecise(p.ScaleX), CollectibleNet.DeserializeFloatVeryPrecise(p.ScaleY), CollectibleNet.DeserializeFloatVeryPrecise(p.ScaleZ)),
				Rotate = (p.Rotate > 0)
			};
		}

		public static CompositeShape FromPacket(Packet_CompositeShape packet)
		{
			CompositeShape shape = new CompositeShape
			{
				Base = ((packet.Base != null) ? new AssetLocation(packet.Base) : null),
				InsertBakedTextures = packet.InsertBakedTextures,
				rotateX = CollectibleNet.DeserializeFloat(packet.Rotatex),
				rotateY = CollectibleNet.DeserializeFloat(packet.Rotatey),
				rotateZ = CollectibleNet.DeserializeFloat(packet.Rotatez),
				offsetX = CollectibleNet.DeserializeFloatVeryPrecise(packet.Offsetx),
				offsetY = CollectibleNet.DeserializeFloatVeryPrecise(packet.Offsety),
				offsetZ = CollectibleNet.DeserializeFloatVeryPrecise(packet.Offsetz),
				Scale = CollectibleNet.DeserializeFloat(packet.ScaleAdjust) + 1f,
				Format = (EnumShapeFormat)packet.Format,
				VoxelizeTexture = (packet.VoxelizeShape > 0),
				QuantityElements = new int?(packet.QuantityElements)
			};
			if (packet.QuantityElementsSet == 0)
			{
				shape.QuantityElements = null;
			}
			if (packet.AlternatesCount > 0)
			{
				shape.Alternates = new CompositeShape[packet.AlternatesCount];
				for (int i = 0; i < packet.AlternatesCount; i++)
				{
					shape.Alternates[i] = CollectibleNet.FromPacket(packet.Alternates[i]);
				}
			}
			if (packet.OverlaysCount > 0)
			{
				shape.Overlays = new CompositeShape[packet.OverlaysCount];
				for (int j = 0; j < packet.OverlaysCount; j++)
				{
					shape.Overlays[j] = CollectibleNet.FromPacket(packet.Overlays[j]);
				}
			}
			if (packet.SelectiveElementsCount > 0)
			{
				shape.SelectiveElements = new string[packet.SelectiveElementsCount];
				for (int k = 0; k < packet.SelectiveElementsCount; k++)
				{
					shape.SelectiveElements[k] = packet.SelectiveElements[k];
				}
			}
			if (packet.IgnoreElementsCount > 0)
			{
				shape.IgnoreElements = new string[packet.IgnoreElementsCount];
				for (int l = 0; l < packet.IgnoreElementsCount; l++)
				{
					shape.IgnoreElements[l] = packet.IgnoreElements[l];
				}
			}
			return shape;
		}

		public static Packet_CompositeShape ToPacket(CompositeShape shape)
		{
			Packet_CompositeShape packet_CompositeShape = new Packet_CompositeShape();
			packet_CompositeShape.InsertBakedTextures = shape.InsertBakedTextures;
			packet_CompositeShape.Rotatex = CollectibleNet.SerializeFloat(shape.rotateX);
			packet_CompositeShape.Rotatey = CollectibleNet.SerializeFloat(shape.rotateY);
			packet_CompositeShape.Rotatez = CollectibleNet.SerializeFloat(shape.rotateZ);
			packet_CompositeShape.Offsetx = CollectibleNet.SerializeFloatVeryPrecise(shape.offsetX);
			packet_CompositeShape.Offsety = CollectibleNet.SerializeFloatVeryPrecise(shape.offsetY);
			packet_CompositeShape.Offsetz = CollectibleNet.SerializeFloatVeryPrecise(shape.offsetZ);
			packet_CompositeShape.ScaleAdjust = CollectibleNet.SerializeFloat(shape.Scale - 1f);
			packet_CompositeShape.Format = (int)shape.Format;
			AssetLocation @base = shape.Base;
			packet_CompositeShape.Base = ((@base != null) ? @base.ToShortString() : null);
			packet_CompositeShape.VoxelizeShape = ((shape.VoxelizeTexture > false) ? 1 : 0);
			packet_CompositeShape.QuantityElements = ((shape.QuantityElements == null) ? 0 : shape.QuantityElements.Value);
			packet_CompositeShape.QuantityElementsSet = (((shape.QuantityElements != null) > false) ? 1 : 0);
			Packet_CompositeShape packet = packet_CompositeShape;
			if (shape.SelectiveElements != null)
			{
				packet.SetSelectiveElements(shape.SelectiveElements);
			}
			if (shape.IgnoreElements != null)
			{
				packet.SetIgnoreElements(shape.IgnoreElements);
			}
			if (shape.Alternates != null)
			{
				Packet_CompositeShape[] packets = new Packet_CompositeShape[shape.Alternates.Length];
				for (int i = 0; i < shape.Alternates.Length; i++)
				{
					packets[i] = CollectibleNet.ToPacket(shape.Alternates[i]);
				}
				packet.SetAlternates(packets);
			}
			if (shape.Alternates != null)
			{
				Packet_CompositeShape[] packets2 = new Packet_CompositeShape[shape.Alternates.Length];
				for (int j = 0; j < shape.Alternates.Length; j++)
				{
					packets2[j] = CollectibleNet.ToPacket(shape.Alternates[j]);
				}
				packet.SetAlternates(packets2);
			}
			if (shape.Overlays != null)
			{
				Packet_CompositeShape[] packets3 = new Packet_CompositeShape[shape.Overlays.Length];
				for (int k = 0; k < shape.Overlays.Length; k++)
				{
					packets3[k] = CollectibleNet.ToPacket(shape.Overlays[k]);
				}
				packet.SetOverlays(packets3);
			}
			return packet;
		}

		public static CompositeTexture FromPacket(Packet_CompositeTexture packet)
		{
			CompositeTexture ct = new CompositeTexture
			{
				Base = new AssetLocation(packet.Base),
				Rotation = packet.Rotation,
				Alpha = packet.Alpha
			};
			if (packet.OverlaysCount > 0)
			{
				ct.Overlays = new AssetLocation[packet.OverlaysCount];
				for (int i = 0; i < packet.OverlaysCount; i++)
				{
					ct.BlendedOverlays[i] = new BlendedOverlayTexture
					{
						Base = new AssetLocation(packet.Overlays[i].Base),
						BlendMode = (EnumColorBlendMode)packet.Overlays[i].Mode
					};
				}
			}
			if (packet.AlternatesCount > 0)
			{
				ct.Alternates = new CompositeTexture[packet.AlternatesCount];
				for (int j = 0; j < packet.AlternatesCount; j++)
				{
					ct.Alternates[j] = CollectibleNet.FromPacket(packet.Alternates[j]);
				}
			}
			if (packet.TilesCount > 0)
			{
				ct.Tiles = new CompositeTexture[packet.TilesCount];
				for (int k = 0; k < packet.TilesCount; k++)
				{
					ct.Tiles[k] = CollectibleNet.FromPacket(packet.Tiles[k]);
				}
				ct.TilesWidth = packet.TilesWidth;
			}
			return ct;
		}

		public static OrderedDictionary<string, string> FromPacket(Packet_VariantPart[] variant, int count)
		{
			OrderedDictionary<string, string> variantdict = new OrderedDictionary<string, string>();
			for (int i = 0; i < count; i++)
			{
				variantdict[variant[i].Code] = variant[i].Value;
			}
			return variantdict;
		}

		public static Packet_CompositeTexture ToPacket(CompositeTexture ct)
		{
			Packet_CompositeTexture packet = new Packet_CompositeTexture();
			packet.Rotation = ct.Rotation;
			packet.Alpha = ct.Alpha;
			if (ct.Base == null)
			{
				throw new Exception("Cannot encode entity texture, Base property is null!");
			}
			packet.Base = ct.Base.ToShortString();
			if (ct.BlendedOverlays != null)
			{
				Packet_BlendedOverlayTexture[] overlay = new Packet_BlendedOverlayTexture[ct.BlendedOverlays.Length];
				for (int i = 0; i < overlay.Length; i++)
				{
					overlay[i] = new Packet_BlendedOverlayTexture
					{
						Base = ct.BlendedOverlays[i].Base.ToString(),
						Mode = (int)ct.BlendedOverlays[i].BlendMode
					};
				}
				packet.SetOverlays(overlay);
			}
			if (ct.Alternates != null)
			{
				packet.SetAlternates(CollectibleNet.ToPackets(ct.Alternates));
			}
			if (ct.Tiles != null)
			{
				packet.SetTiles(CollectibleNet.ToPackets(ct.Tiles));
			}
			packet.TilesWidth = ct.TilesWidth;
			return packet;
		}

		public static Packet_CompositeTexture[] ToPackets(CompositeTexture[] textures)
		{
			Packet_CompositeTexture[] packets = new Packet_CompositeTexture[textures.Length];
			for (int i = 0; i < textures.Length; i++)
			{
				packets[i] = CollectibleNet.ToPacket(textures[i]);
			}
			return packets;
		}

		public static ModelTransform DefGuiTransform = ModelTransform.BlockDefaultGui();

		public static ModelTransform DefFpHandTransform = ModelTransform.BlockDefaultFp();

		public static ModelTransform DefTpHandTransform = ModelTransform.BlockDefaultTp();

		public static ModelTransform DefTpOffHandTransform = ModelTransform.BlockDefaultTp();

		public static ModelTransform DefGroundTransform = ModelTransform.BlockDefaultGround();
	}
}
