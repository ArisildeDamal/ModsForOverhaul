using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common
{
	public static class EntityTypeNet
	{
		public static Packet_EntityType EntityPropertiesToPacket(EntityProperties properties)
		{
			Packet_EntityType packet_EntityType;
			using (FastMemoryStream ms = new FastMemoryStream())
			{
				packet_EntityType = EntityTypeNet.EntityPropertiesToPacket(properties, ms);
			}
			return packet_EntityType;
		}

		public static Packet_EntityType EntityPropertiesToPacket(EntityProperties properties, FastMemoryStream ms)
		{
			Packet_EntityType packet_EntityType = new Packet_EntityType();
			packet_EntityType.Class = properties.Class;
			packet_EntityType.Habitat = (int)properties.Habitat;
			packet_EntityType.Code = properties.Code.ToShortString();
			packet_EntityType.Tags = (from tag in properties.Tags.ToArray()
				select (int)tag).ToArray<int>();
			packet_EntityType.Drops = EntityTypeNet.getDropsPacket(properties.Drops, ms);
			packet_EntityType.Color = properties.Color;
			EntityClientProperties client = properties.Client;
			packet_EntityType.Shape = ((((client != null) ? client.Shape : null) != null) ? CollectibleNet.ToPacket(properties.Client.Shape) : null);
			EntityClientProperties client2 = properties.Client;
			packet_EntityType.Renderer = ((client2 != null) ? client2.RendererName : null);
			packet_EntityType.GlowLevel = ((properties.Client == null) ? 0 : properties.Client.GlowLevel);
			packet_EntityType.PitchStep = ((properties.Client.PitchStep > false) ? 1 : 0);
			JsonObject attributes = properties.Attributes;
			packet_EntityType.Attributes = ((attributes != null) ? attributes.ToString() : null);
			packet_EntityType.CollisionBoxLength = CollectibleNet.SerializePlayerPos((double)properties.CollisionBoxSize.X);
			packet_EntityType.CollisionBoxHeight = CollectibleNet.SerializePlayerPos((double)properties.CollisionBoxSize.Y);
			packet_EntityType.DeadCollisionBoxLength = CollectibleNet.SerializePlayerPos((double)properties.DeadCollisionBoxSize.X);
			packet_EntityType.DeadCollisionBoxHeight = CollectibleNet.SerializePlayerPos((double)properties.DeadCollisionBoxSize.Y);
			packet_EntityType.SelectionBoxLength = ((properties.SelectionBoxSize == null) ? (-1) : CollectibleNet.SerializeFloatPrecise(properties.SelectionBoxSize.X));
			packet_EntityType.SelectionBoxHeight = ((properties.SelectionBoxSize == null) ? (-1) : CollectibleNet.SerializeFloatPrecise(properties.SelectionBoxSize.Y));
			packet_EntityType.DeadSelectionBoxLength = ((properties.DeadSelectionBoxSize == null) ? (-1) : CollectibleNet.SerializeFloatPrecise(properties.DeadSelectionBoxSize.X));
			packet_EntityType.DeadSelectionBoxHeight = ((properties.DeadSelectionBoxSize == null) ? (-1) : CollectibleNet.SerializeFloatPrecise(properties.DeadSelectionBoxSize.Y));
			packet_EntityType.IdleSoundChance = CollectibleNet.SerializeFloatPrecise(100f * properties.IdleSoundChance);
			packet_EntityType.IdleSoundRange = CollectibleNet.SerializeFloatPrecise(properties.IdleSoundRange);
			packet_EntityType.Size = CollectibleNet.SerializeFloatPrecise((properties.Client == null) ? 1f : properties.Client.Size);
			packet_EntityType.SizeGrowthFactor = CollectibleNet.SerializeFloatPrecise((properties.Client == null) ? 0f : properties.Client.SizeGrowthFactor);
			packet_EntityType.EyeHeight = CollectibleNet.SerializeFloatPrecise((float)properties.EyeHeight);
			packet_EntityType.SwimmingEyeHeight = CollectibleNet.SerializeFloatPrecise((float)properties.SwimmingEyeHeight);
			packet_EntityType.Weight = CollectibleNet.SerializeFloatPrecise(properties.Weight);
			packet_EntityType.CanClimb = ((properties.CanClimb > false) ? 1 : 0);
			packet_EntityType.CanClimbAnywhere = ((properties.CanClimbAnywhere > false) ? 1 : 0);
			packet_EntityType.FallDamage = ((properties.FallDamage > false) ? 1 : 0);
			packet_EntityType.FallDamageMultiplier = CollectibleNet.SerializeFloatPrecise(properties.FallDamageMultiplier);
			packet_EntityType.RotateModelOnClimb = ((properties.RotateModelOnClimb > false) ? 1 : 0);
			packet_EntityType.ClimbTouchDistance = CollectibleNet.SerializeFloatVeryPrecise(properties.ClimbTouchDistance);
			packet_EntityType.KnockbackResistance = CollectibleNet.SerializeFloatPrecise(properties.KnockbackResistance);
			Packet_EntityType packet = packet_EntityType;
			packet.SetVariant(CollectibleNet.ToPacket(properties.Variant));
			EntityClientProperties client3 = properties.Client;
			if (((client3 != null) ? client3.Textures : null) != null)
			{
				packet.SetTextureCodes(properties.Client.Textures.Keys.ToArray<string>());
				packet.SetCompositeTextures(CollectibleNet.ToPackets(properties.Client.Textures.Values.ToArray<CompositeTexture>()));
			}
			EntityClientProperties client4 = properties.Client;
			if (((client4 != null) ? client4.Animations : null) != null)
			{
				ms.Reset();
				BinaryWriter writer = new BinaryWriter(ms);
				AnimationMetaData[] Animations = properties.Client.Animations;
				writer.Write(Animations.Length);
				for (int i = 0; i < Animations.Length; i++)
				{
					Animations[i].ToBytes(writer);
				}
				packet.SetAnimationMetaData(ms.ToArray());
			}
			EntityClientProperties client5 = properties.Client;
			if (((client5 != null) ? client5.BehaviorsAsJsonObj : null) != null)
			{
				JsonObject[] BehaviorsAsJsonObj = properties.Client.BehaviorsAsJsonObj;
				Packet_Behavior[] behaviors = new Packet_Behavior[BehaviorsAsJsonObj.Length];
				for (int j = 0; j < behaviors.Length; j++)
				{
					behaviors[j] = new Packet_Behavior
					{
						Attributes = BehaviorsAsJsonObj[j].ToString()
					};
				}
				packet.SetBehaviors(behaviors);
			}
			if (properties.Sounds != null)
			{
				packet.SetSoundKeys(properties.Sounds.Keys.ToArray<string>());
				AssetLocation[] locations = properties.Sounds.Values.ToArray<AssetLocation>();
				string[] names = new string[properties.Sounds.Count];
				for (int k = 0; k < names.Length; k++)
				{
					names[k] = locations[k].ToString();
				}
				packet.SetSoundNames(names);
			}
			return packet;
		}

		public static EntityProperties FromPacket(Packet_EntityType packet, IWorldAccessor worldForResolve)
		{
			JsonObject[] behaviors = new JsonObject[packet.BehaviorsCount];
			for (int i = 0; i < behaviors.Length; i++)
			{
				behaviors[i] = new JsonObject(JToken.Parse(packet.Behaviors[i].Attributes));
			}
			Dictionary<string, AssetLocation> sounds = new Dictionary<string, AssetLocation>();
			if (packet.SoundKeys != null)
			{
				for (int j = 0; j < packet.SoundKeysCount; j++)
				{
					sounds[packet.SoundKeys[j]] = new AssetLocation(packet.SoundNames[j]);
				}
			}
			AssetLocation code = new AssetLocation(packet.Code);
			EntityProperties entityProperties = new EntityProperties();
			entityProperties.Class = packet.Class;
			entityProperties.Variant = CollectibleNet.FromPacket(packet.Variant, packet.VariantCount);
			entityProperties.Code = code;
			EntityTagArray entityTagArray;
			if (packet.Tags == null)
			{
				entityTagArray = new EntityTagArray();
			}
			else
			{
				entityTagArray = new EntityTagArray(packet.Tags.Select((int tag) => (ushort)tag));
			}
			entityProperties.Tags = entityTagArray;
			entityProperties.Color = packet.Color;
			entityProperties.Habitat = (EnumHabitat)packet.Habitat;
			entityProperties.DropsPacket = packet.Drops;
			entityProperties.Client = new EntityClientProperties(behaviors, null)
			{
				GlowLevel = packet.GlowLevel,
				PitchStep = (packet.PitchStep > 0),
				RendererName = packet.Renderer,
				Shape = ((packet.Shape != null) ? CollectibleNet.FromPacket(packet.Shape) : null),
				Size = CollectibleNet.DeserializeFloatPrecise(packet.Size),
				SizeGrowthFactor = CollectibleNet.DeserializeFloatPrecise(packet.SizeGrowthFactor)
			};
			entityProperties.CollisionBoxSize = new Vec2f((float)CollectibleNet.DeserializePlayerPos(packet.CollisionBoxLength), (float)CollectibleNet.DeserializePlayerPos(packet.CollisionBoxHeight));
			entityProperties.DeadCollisionBoxSize = new Vec2f((float)CollectibleNet.DeserializePlayerPos(packet.DeadCollisionBoxLength), (float)CollectibleNet.DeserializePlayerPos(packet.DeadCollisionBoxHeight));
			entityProperties.SelectionBoxSize = new Vec2f(CollectibleNet.DeserializeFloatPrecise(packet.SelectionBoxLength), CollectibleNet.DeserializeFloatPrecise(packet.SelectionBoxHeight));
			entityProperties.DeadSelectionBoxSize = new Vec2f(CollectibleNet.DeserializeFloatPrecise(packet.DeadSelectionBoxLength), CollectibleNet.DeserializeFloatPrecise(packet.DeadSelectionBoxHeight));
			entityProperties.Attributes = ((packet.Attributes == null) ? null : new JsonObject(JToken.Parse(packet.Attributes)));
			entityProperties.Sounds = sounds;
			entityProperties.IdleSoundChance = CollectibleNet.DeserializeFloatPrecise(packet.IdleSoundChance) / 100f;
			entityProperties.IdleSoundRange = CollectibleNet.DeserializeFloatPrecise(packet.IdleSoundRange);
			entityProperties.EyeHeight = (double)CollectibleNet.DeserializeFloatPrecise(packet.EyeHeight);
			entityProperties.SwimmingEyeHeight = (double)CollectibleNet.DeserializeFloatPrecise(packet.SwimmingEyeHeight);
			entityProperties.Weight = CollectibleNet.DeserializeFloatPrecise(packet.Weight);
			entityProperties.CanClimb = packet.CanClimb > 0;
			entityProperties.CanClimbAnywhere = packet.CanClimbAnywhere > 0;
			entityProperties.FallDamage = packet.FallDamage > 0;
			entityProperties.FallDamageMultiplier = CollectibleNet.DeserializeFloatPrecise(packet.FallDamageMultiplier);
			entityProperties.RotateModelOnClimb = packet.RotateModelOnClimb > 0;
			entityProperties.ClimbTouchDistance = CollectibleNet.DeserializeFloatVeryPrecise(packet.ClimbTouchDistance);
			entityProperties.KnockbackResistance = CollectibleNet.DeserializeFloatPrecise(packet.KnockbackResistance);
			EntityProperties et = entityProperties;
			if (et.SelectionBoxSize.X < 0f)
			{
				et.SelectionBoxSize = null;
			}
			if (et.DeadSelectionBoxSize.X < 0f)
			{
				et.DeadSelectionBoxSize = null;
			}
			if (packet.AnimationMetaData != null)
			{
				using (MemoryStream ms = new MemoryStream(packet.AnimationMetaData))
				{
					BinaryReader reader = new BinaryReader(ms);
					int animationsCount = reader.ReadInt32();
					et.Client.Animations = new AnimationMetaData[animationsCount];
					for (int k = 0; k < animationsCount; k++)
					{
						et.Client.Animations[k] = AnimationMetaData.FromBytes(reader, "1.21.5");
					}
				}
			}
			et.Client.Init(et.Code, worldForResolve);
			et.Client.Textures = new FastSmallDictionary<string, CompositeTexture>(packet.TextureCodesCount);
			for (int l = 0; l < packet.TextureCodesCount; l++)
			{
				et.Client.Textures.Add(packet.TextureCodes[l], CollectibleNet.FromPacket(packet.CompositeTextures[l]));
			}
			CompositeTexture firstTexture = et.Client.FirstTexture;
			CompositeTexture[] alternates = ((firstTexture != null) ? firstTexture.Alternates : null);
			et.Client.TexturesAlternatesCount = ((alternates == null) ? 0 : alternates.Length);
			return et;
		}

		private static byte[] getDropsPacket(BlockDropItemStack[] drops, FastMemoryStream ms)
		{
			ms.Reset();
			BinaryWriter writer = new BinaryWriter(ms);
			if (drops == null)
			{
				writer.Write(0);
			}
			else
			{
				writer.Write(drops.Length);
				for (int i = 0; i < drops.Length; i++)
				{
					drops[i].ToBytes(writer);
				}
			}
			return ms.ToArray();
		}
	}
}
