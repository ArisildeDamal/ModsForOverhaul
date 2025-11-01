using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Common
{
	public class ClassRegistryAPI : IClassRegistryAPI
	{
		public Dictionary<string, Type> BlockClassToTypeMapping
		{
			get
			{
				return this.registry.BlockClassToTypeMapping;
			}
		}

		public Dictionary<string, Type> ItemClassToTypeMapping
		{
			get
			{
				return this.registry.ItemClassToTypeMapping;
			}
		}

		public ClassRegistryAPI(IWorldAccessor world, ClassRegistry registry)
		{
			this.world = world;
			this.registry = registry;
		}

		public Block CreateBlock(string blockclass)
		{
			return this.registry.CreateBlock(blockclass);
		}

		public BlockBehavior CreateBlockBehavior(Block block, string blockclass)
		{
			return this.registry.CreateBlockBehavior(block, blockclass);
		}

		public Type GetBlockBehaviorClass(string blockclass)
		{
			Type type;
			this.registry.blockbehaviorToTypeMapping.TryGetValue(blockclass, out type);
			return type;
		}

		public Item CreateItem(string itemclass)
		{
			return this.registry.CreateItem(itemclass);
		}

		public IAttribute CreateItemstackAttribute(ItemStack itemstack = null)
		{
			return new ItemstackAttribute(itemstack);
		}

		public IAttribute CreateStringAttribute(string value = null)
		{
			return new StringAttribute(value);
		}

		public ITreeAttribute CreateTreeAttribute()
		{
			return new TreeAttribute();
		}

		public JsonTreeAttribute CreateJsonTreeAttributeFromDict(Dictionary<string, JsonTreeAttribute> attributes)
		{
			JsonTreeAttribute tree = new JsonTreeAttribute();
			if (attributes == null)
			{
				return tree;
			}
			tree.type = EnumAttributeType.Tree;
			foreach (KeyValuePair<string, JsonTreeAttribute> val in attributes)
			{
				tree.elems[val.Key] = val.Value.Clone();
			}
			return tree;
		}

		public Entity CreateEntity(string entityClass)
		{
			return this.registry.CreateEntity(entityClass);
		}

		public Entity CreateEntity(EntityProperties entityType)
		{
			Entity entity = this.registry.CreateEntity(entityType.Class);
			entity.Code = entityType.Code;
			return entity;
		}

		public BlockEntity CreateBlockEntity(string blockEntityClass)
		{
			return this.registry.CreateBlockEntity(blockEntityClass);
		}

		public Type GetBlockEntity(string blockEntityClass)
		{
			Type type;
			this.registry.blockEntityClassnameToTypeMapping.TryGetValue(blockEntityClass, out type);
			return type;
		}

		public EntityBehavior CreateEntityBehavior(Entity forEntity, string entityBehaviorName)
		{
			return this.registry.CreateEntityBehavior(forEntity, entityBehaviorName);
		}

		public string GetBlockEntityClass(Type type)
		{
			string classsName;
			this.registry.blockEntityTypeToClassnameMapping.TryGetValue(type, out classsName);
			return classsName;
		}

		public CropBehavior CreateCropBehavior(Block forBlock, string cropBehaviorName)
		{
			return this.registry.createCropBehavior(forBlock, cropBehaviorName);
		}

		public IInventoryNetworkUtil CreateInvNetworkUtil(InventoryBase inv, ICoreAPI api)
		{
			return new InventoryNetworkUtil(inv, api);
		}

		public IMountableSeat GetMountable(TreeAttribute tree)
		{
			return this.registry.GetMountable(this.world, tree);
		}

		public Type GetBlockClass(string blockclass)
		{
			return this.registry.GetBlockClass(blockclass);
		}

		public Type GetItemClass(string itemClass)
		{
			return this.registry.GetItemClass(itemClass);
		}

		public string GetBlockBehaviorClassName(Type blockBehaviorType)
		{
			return this.registry.GetBlockBehaviorClassName(blockBehaviorType);
		}

		public string GetEntityClassName(Type entityType)
		{
			return this.registry.GetEntityClassName(entityType);
		}

		public Type GetBlockEntityBehaviorClass(string name)
		{
			Type t;
			this.registry.blockentitybehaviorToTypeMapping.TryGetValue(name, out t);
			return t;
		}

		public BlockEntityBehavior CreateBlockEntityBehavior(BlockEntity blockEntity, string name)
		{
			return this.registry.CreateBlockEntityBehavior(blockEntity, name);
		}

		public Type GetEntityBehaviorClass(string entityBehaviorName)
		{
			return this.registry.GetEntityBehaviorClass(entityBehaviorName);
		}

		public CollectibleBehavior CreateCollectibleBehavior(CollectibleObject forCollectible, string code)
		{
			return this.registry.CreateCollectibleBehavior(forCollectible, code);
		}

		public Type GetCollectibleBehaviorClass(string code)
		{
			return this.registry.GetCollectibleBehaviorClass(code);
		}

		public string GetCollectibleBehaviorClassName(Type type)
		{
			return this.registry.GetCollectibleBehaviorClassName(type);
		}

		public void RegisterParticlePropertyProvider(string className, Type ParticleProvider)
		{
			this.registry.RegisterParticlePropertyProvider(className, ParticleProvider);
		}

		public IParticlePropertiesProvider CreateParticlePropertyProvider(Type entityType)
		{
			return this.registry.CreateParticlePropertyProvider(entityType);
		}

		public IParticlePropertiesProvider CreateParticlePropertyProvider(string className)
		{
			return this.registry.CreateParticlePropertyProvider(className);
		}

		private IWorldAccessor world;

		internal ClassRegistry registry;
	}
}
