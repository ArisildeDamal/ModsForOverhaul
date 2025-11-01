using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Common
{
	public class ClassRegistry
	{
		public ClassRegistry()
		{
			this.RegisterDefaultInventories();
			this.RegisterDefaultParticlePropertyProviders();
			this.RegisterItemClass("Item", typeof(Item));
			this.RegisterBlockClass("Block", typeof(Block));
			this.RegisterEntityType("EntityItem", typeof(EntityItem));
			this.RegisterEntityType("EntityChunky", typeof(EntityChunky));
			this.RegisterEntityType("EntityPlayer", typeof(EntityPlayer));
			this.RegisterEntityType("EntityHumanoid", typeof(EntityHumanoid));
			this.RegisterEntityType("EntityAgent", typeof(EntityAgent));
			this.RegisterentityBehavior("passivephysics", typeof(EntityBehaviorPassivePhysics));
		}

		public void RegisterMountable(string className, GetMountableDelegate mountableInstancer)
		{
			this.mountableEntries[className] = mountableInstancer;
		}

		public IMountableSeat GetMountable(IWorldAccessor world, TreeAttribute tree)
		{
			string className = tree.GetString("className", null);
			GetMountableDelegate dele;
			if (this.mountableEntries.TryGetValue(className, out dele))
			{
				return dele(world, tree);
			}
			return null;
		}

		public void RegisterInventoryClass(string inventoryClass, Type inventory)
		{
			this.inventoryClassToTypeMapping[inventoryClass] = inventory;
		}

		public InventoryBase CreateInventory(string inventoryClass, string inventoryId, ICoreAPI api)
		{
			Type inventoryType;
			if (!this.inventoryClassToTypeMapping.TryGetValue(inventoryClass, out inventoryType))
			{
				throw new Exception("Don't know how to instantiate inventory of class '" + inventoryClass + "' did you forget to register a mapping?");
			}
			InventoryBase inventoryBase;
			try
			{
				inventoryBase = (InventoryBase)Activator.CreateInstance(inventoryType, new object[] { inventoryId, api });
			}
			catch (Exception exception)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(60, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Error while instantiating inventory of class '");
				defaultInterpolatedStringHandler.AppendFormatted(inventoryClass);
				defaultInterpolatedStringHandler.AppendLiteral("' and id '");
				defaultInterpolatedStringHandler.AppendFormatted(inventoryId);
				defaultInterpolatedStringHandler.AppendLiteral("':\n ");
				defaultInterpolatedStringHandler.AppendFormatted<Exception>(exception);
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear(), exception);
			}
			return inventoryBase;
		}

		private void RegisterDefaultInventories()
		{
			this.RegisterInventoryClass("creative", typeof(InventoryPlayerCreative));
			this.RegisterInventoryClass("backpack", typeof(InventoryPlayerBackPacks));
			this.RegisterInventoryClass("ground", typeof(InventoryPlayerGround));
			this.RegisterInventoryClass("hotbar", typeof(InventoryPlayerHotbar));
			this.RegisterInventoryClass("mouse", typeof(InventoryPlayerMouseCursor));
			this.RegisterInventoryClass("craftinggrid", typeof(InventoryCraftingGrid));
			this.RegisterInventoryClass("character", typeof(InventoryCharacter));
		}

		public void RegisterRecipeRegistry(string recipeRegistryCode, Type recipeRegistry)
		{
			this.RecipeRegistryToTypeMapping[recipeRegistryCode] = recipeRegistry;
			this.TypeToRecipeRegistryMapping[recipeRegistry] = recipeRegistryCode;
		}

		public void RegisterRecipeRegistry<T>(string recipeRegistryCode)
		{
			this.RecipeRegistryToTypeMapping[recipeRegistryCode] = typeof(T);
		}

		public T CreateRecipeRegistry<T>(string recipeRegistryCode) where T : RecipeRegistryBase
		{
			Type recipeType;
			if (!this.RecipeRegistryToTypeMapping.TryGetValue(recipeRegistryCode, out recipeType))
			{
				throw new Exception("Don't know how to instantiate recipe registry of class '" + recipeRegistryCode + "' did you forget to register a mapping?");
			}
			T t;
			try
			{
				t = (T)((object)Activator.CreateInstance(recipeType));
			}
			catch (Exception exception)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(64, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Error on instantiating recipe registry of class '");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(T));
				defaultInterpolatedStringHandler.AppendLiteral("' and code '");
				defaultInterpolatedStringHandler.AppendFormatted(recipeRegistryCode);
				defaultInterpolatedStringHandler.AppendLiteral("':\n");
				defaultInterpolatedStringHandler.AppendFormatted<Exception>(exception);
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear(), exception);
			}
			return t;
		}

		public string GetRecipeRegistryCode<T>() where T : RecipeRegistryBase
		{
			return this.TypeToRecipeRegistryMapping[typeof(T)];
		}

		public void RegisterBlockClass(string blockClass, Type block)
		{
			this.BlockClassToTypeMapping[blockClass] = block;
		}

		public Block CreateBlock(string blockClass)
		{
			Type blockType;
			if (!this.BlockClassToTypeMapping.TryGetValue(blockClass, out blockType))
			{
				throw new Exception("Don't know how to instantiate block of class '" + blockClass + "' did you forget to register a mapping?");
			}
			Block block;
			try
			{
				block = (Block)Activator.CreateInstance(blockType);
			}
			catch (Exception exception)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(39, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Error on instantiating block class '");
				defaultInterpolatedStringHandler.AppendFormatted(blockClass);
				defaultInterpolatedStringHandler.AppendLiteral("':\n");
				defaultInterpolatedStringHandler.AppendFormatted<Exception>(exception);
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear(), exception);
			}
			return block;
		}

		public Type GetBlockClass(string blockClass)
		{
			Type val;
			this.BlockClassToTypeMapping.TryGetValue(blockClass, out val);
			return val;
		}

		public void RegisterBlockBehaviorClass(string code, Type block)
		{
			this.blockbehaviorToTypeMapping[code] = block;
		}

		public string GetBlockBehaviorClassName(Type blockBehaviorType)
		{
			return this.blockbehaviorToTypeMapping.FirstOrDefault((KeyValuePair<string, Type> x) => x.Value == blockBehaviorType).Key;
		}

		public BlockBehavior CreateBlockBehavior(Block block, string blockClass)
		{
			Type behaviorType;
			if (!this.blockbehaviorToTypeMapping.TryGetValue(blockClass, out behaviorType))
			{
				throw new Exception("Don't know how to instantiate block behavior of class '" + blockClass + "' did you forget to register a mapping?");
			}
			BlockBehavior blockBehavior;
			try
			{
				blockBehavior = (BlockBehavior)Activator.CreateInstance(behaviorType, new object[] { block });
			}
			catch (Exception exception)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(55, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Error on instantiating block behavior '");
				defaultInterpolatedStringHandler.AppendFormatted(blockClass);
				defaultInterpolatedStringHandler.AppendLiteral("' for block '");
				defaultInterpolatedStringHandler.AppendFormatted<AssetLocation>(block.Code);
				defaultInterpolatedStringHandler.AppendLiteral("':\n");
				defaultInterpolatedStringHandler.AppendFormatted<Exception>(exception);
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear(), exception);
			}
			return blockBehavior;
		}

		public void RegisterBlockEntityBehaviorClass(string blockClass, Type blockentity)
		{
			this.blockentitybehaviorToTypeMapping[blockClass] = blockentity;
		}

		public string GetBlockEntityBehaviorClassName(Type blockBehaviorType)
		{
			return this.blockentitybehaviorToTypeMapping.FirstOrDefault((KeyValuePair<string, Type> x) => x.Value == blockBehaviorType).Key;
		}

		public BlockEntityBehavior CreateBlockEntityBehavior(BlockEntity blockentity, string blockEntityClass)
		{
			Type beType;
			if (!this.blockentitybehaviorToTypeMapping.TryGetValue(blockEntityClass, out beType))
			{
				throw new Exception("Don't know how to instantiate block entity behavior of class '" + blockEntityClass + "' did you forget to register a mapping?");
			}
			BlockEntityBehavior blockEntityBehavior;
			try
			{
				blockEntityBehavior = (BlockEntityBehavior)Activator.CreateInstance(beType, new object[] { blockentity });
			}
			catch (Exception exception)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(62, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Error on instantiating block entity behavior '");
				defaultInterpolatedStringHandler.AppendFormatted(blockEntityClass);
				defaultInterpolatedStringHandler.AppendLiteral("' for block '");
				defaultInterpolatedStringHandler.AppendFormatted<AssetLocation>(blockentity.Block.Code);
				defaultInterpolatedStringHandler.AppendLiteral("':\n");
				defaultInterpolatedStringHandler.AppendFormatted<Exception>(exception);
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear(), exception);
			}
			return blockEntityBehavior;
		}

		public void RegisterCollectibleBehaviorClass(string code, Type block)
		{
			this.collectibleBehaviorToTypeMapping[code] = block;
		}

		public string GetCollectibleBehaviorClassName(Type blockBehaviorType)
		{
			return this.collectibleBehaviorToTypeMapping.FirstOrDefault((KeyValuePair<string, Type> x) => x.Value == blockBehaviorType).Key;
		}

		public CollectibleBehavior CreateCollectibleBehavior(CollectibleObject collectible, string code)
		{
			Type behaviorType;
			if (!this.collectibleBehaviorToTypeMapping.TryGetValue(code, out behaviorType))
			{
				throw new Exception("Don't know how to instantiate collectible behavior of class '" + code + "' did you forget to register a mapping?");
			}
			CollectibleBehavior collectibleBehavior;
			try
			{
				collectibleBehavior = (CollectibleBehavior)Activator.CreateInstance(behaviorType, new object[] { collectible });
			}
			catch (Exception exception)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(55, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Error on instantiating collectible behavior '");
				defaultInterpolatedStringHandler.AppendFormatted(code);
				defaultInterpolatedStringHandler.AppendLiteral("' for '");
				defaultInterpolatedStringHandler.AppendFormatted<AssetLocation>(collectible.Code);
				defaultInterpolatedStringHandler.AppendLiteral("':\n");
				defaultInterpolatedStringHandler.AppendFormatted<Exception>(exception);
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear(), exception);
			}
			return collectibleBehavior;
		}

		public Type GetCollectibleBehaviorClass(string code)
		{
			Type type;
			this.collectibleBehaviorToTypeMapping.TryGetValue(code, out type);
			return type;
		}

		public void RegisterCropBehavior(string cropBehaviorClass, Type block)
		{
			this.cropbehaviorToTypeMapping[cropBehaviorClass] = block;
		}

		public string GetCropBehaviorClassName(Type cropBehaviorType)
		{
			return this.cropbehaviorToTypeMapping.FirstOrDefault((KeyValuePair<string, Type> x) => x.Value == cropBehaviorType).Key;
		}

		public CropBehavior createCropBehavior(Block block, string cropBehaviorClass)
		{
			Type behaviorType;
			if (!this.cropbehaviorToTypeMapping.TryGetValue(cropBehaviorClass, out behaviorType))
			{
				throw new Exception("Don't know how to instantiate crop behavior of class '" + cropBehaviorClass + "' did you forget to register a mapping?");
			}
			CropBehavior cropBehavior;
			try
			{
				cropBehavior = (CropBehavior)Activator.CreateInstance(behaviorType, new object[] { block });
			}
			catch (Exception exception)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(48, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Error on instantiating crop behavior '");
				defaultInterpolatedStringHandler.AppendFormatted(cropBehaviorClass);
				defaultInterpolatedStringHandler.AppendLiteral("' for '");
				defaultInterpolatedStringHandler.AppendFormatted<AssetLocation>(block.Code);
				defaultInterpolatedStringHandler.AppendLiteral("':\n");
				defaultInterpolatedStringHandler.AppendFormatted<Exception>(exception);
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear(), exception);
			}
			return cropBehavior;
		}

		public void RegisterItemClass(string itemClass, Type item)
		{
			this.ItemClassToTypeMapping[itemClass] = item;
		}

		public Item CreateItem(string itemClass)
		{
			Type itemType;
			if (!this.ItemClassToTypeMapping.TryGetValue(itemClass, out itemType))
			{
				throw new Exception("Don't know how to instantiate item of class '" + itemClass + "' did you forget to register a mapping?");
			}
			Item item;
			try
			{
				item = (Item)Activator.CreateInstance(itemType);
			}
			catch (Exception exception)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(32, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Error on instantiating item '");
				defaultInterpolatedStringHandler.AppendFormatted(itemClass);
				defaultInterpolatedStringHandler.AppendLiteral("':\n");
				defaultInterpolatedStringHandler.AppendFormatted<Exception>(exception);
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear(), exception);
			}
			return item;
		}

		public Type GetItemClass(string itemClass)
		{
			Type val;
			this.ItemClassToTypeMapping.TryGetValue(itemClass, out val);
			return val;
		}

		public void RegisterEntityType(string className, Type entity)
		{
			this.entityClassNameToTypeMapping[className] = entity;
			this.entityTypeToClassNameMapping[entity] = className;
		}

		public string GetEntityClassName(Type entityType)
		{
			if (!this.entityTypeToClassNameMapping.ContainsKey(entityType))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(79, 1);
				defaultInterpolatedStringHandler.AppendLiteral("I don't have a mapping for entity type '");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(entityType);
				defaultInterpolatedStringHandler.AppendLiteral("' did you forget to register a mapping?");
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return this.entityTypeToClassNameMapping[entityType];
		}

		public Entity CreateEntity(Type entityType)
		{
			if (!this.entityClassNameToTypeMapping.ContainsValue(entityType))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(85, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Don't know how to instantiate entity of type '");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(entityType);
				defaultInterpolatedStringHandler.AppendLiteral("' did you forget to register a mapping?");
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			Entity entity;
			try
			{
				entity = (Entity)Activator.CreateInstance(entityType);
			}
			catch (Exception exception)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Error on instantiating entity '");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(entityType);
				defaultInterpolatedStringHandler.AppendLiteral("':\n");
				defaultInterpolatedStringHandler.AppendFormatted<Exception>(exception);
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear(), exception);
			}
			return entity;
		}

		public Entity CreateEntity(string className)
		{
			if (className == "player")
			{
				className = "EntityPlayer";
			}
			if (className == "item")
			{
				className = "EntityItem";
			}
			if (className == "playerbot")
			{
				className = "EntityNpc";
			}
			if (className == "humanoid")
			{
				className = "EntityHumanoid";
			}
			if (className == "living")
			{
				className = "EntityAgent";
			}
			if (className == "blockfalling")
			{
				className = "EntityBlockFalling";
			}
			if (className == "projectile")
			{
				className = "EntityProjectile";
			}
			Type entityType;
			if (!this.entityClassNameToTypeMapping.TryGetValue(className, out entityType))
			{
				throw new Exception("Don't know how to instantiate entity of type '" + className + "' did you forget to register a mapping?");
			}
			Entity entity;
			try
			{
				entity = (Entity)Activator.CreateInstance(entityType);
			}
			catch (Exception exception)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Error on instantiating entity '");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(entityType);
				defaultInterpolatedStringHandler.AppendLiteral("':\n");
				defaultInterpolatedStringHandler.AppendFormatted<Exception>(exception);
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear(), exception);
			}
			return entity;
		}

		public void RegisterEntityRendererType(string className, Type EntityRenderer)
		{
			Type oldType;
			if (this.EntityRendererClassNameToTypeMapping.TryGetValue(className, out oldType))
			{
				this.EntityRendererTypeToClassNameMapping.Remove(oldType);
			}
			this.EntityRendererClassNameToTypeMapping[className] = EntityRenderer;
			this.EntityRendererTypeToClassNameMapping[EntityRenderer] = className;
		}

		public string GetEntityRendererClassName(Type EntityRendererType)
		{
			if (!this.EntityRendererTypeToClassNameMapping.ContainsKey(EntityRendererType))
			{
				throw new Exception("I don't have a mapping for EntityRenderer type " + ((EntityRendererType != null) ? EntityRendererType.ToString() : null) + " did you forget to register a mapping?");
			}
			return this.EntityRendererTypeToClassNameMapping[EntityRendererType];
		}

		public EntityRenderer CreateEntityRenderer(Type EntityRendererType)
		{
			if (!this.EntityRendererClassNameToTypeMapping.ContainsValue(EntityRendererType))
			{
				throw new Exception("Don't know how to instantiate EntityRenderer of type " + ((EntityRendererType != null) ? EntityRendererType.ToString() : null) + " did you forget to register a mapping?");
			}
			return (EntityRenderer)Activator.CreateInstance(EntityRendererType);
		}

		public EntityRenderer CreateEntityRenderer(string className, params object[] args)
		{
			Type rendererType;
			if (!this.EntityRendererClassNameToTypeMapping.TryGetValue(className, out rendererType))
			{
				throw new Exception("Don't know how to instantiate EntityRenderer of type " + className + " did you forget to register a mapping?");
			}
			return (EntityRenderer)Activator.CreateInstance(rendererType, args);
		}

		public Type GetEntityBehaviorClass(string entityBehaviorName)
		{
			Type type;
			this.entityBehaviorClassNameToTypeMapping.TryGetValue(entityBehaviorName, out type);
			return type;
		}

		public void RegisterentityBehavior(string className, Type entityBehavior)
		{
			if (this.entityBehaviorTypeToClassNameMapping.ContainsKey(entityBehavior))
			{
				return;
			}
			this.entityBehaviorClassNameToTypeMapping.Add(className, entityBehavior);
			this.entityBehaviorTypeToClassNameMapping.Add(entityBehavior, className);
		}

		public string GetEntityBehaviorClassName(Type entityBehaviorType)
		{
			if (!this.entityBehaviorTypeToClassNameMapping.ContainsKey(entityBehaviorType))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(87, 1);
				defaultInterpolatedStringHandler.AppendLiteral("I don't have a mapping for EntityBehavior type '");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(entityBehaviorType);
				defaultInterpolatedStringHandler.AppendLiteral("' did you forget to register a mapping?");
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return this.entityBehaviorTypeToClassNameMapping[entityBehaviorType];
		}

		public EntityBehavior CreateEntityBehavior(Entity forEntity, Type entityBehaviorType)
		{
			if (!this.entityBehaviorClassNameToTypeMapping.ContainsValue(entityBehaviorType))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(93, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Don't know how to instantiate entityBehavior of type '");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(entityBehaviorType);
				defaultInterpolatedStringHandler.AppendLiteral("' did you forget to register a mapping?");
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			EntityBehavior entityBehavior;
			try
			{
				entityBehavior = (EntityBehavior)Activator.CreateInstance(entityBehaviorType, new object[] { forEntity });
			}
			catch (Exception exception)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(53, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Error while instantiating entity behavior '");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(entityBehaviorType);
				defaultInterpolatedStringHandler.AppendLiteral("' for '");
				defaultInterpolatedStringHandler.AppendFormatted<AssetLocation>(forEntity.Code);
				defaultInterpolatedStringHandler.AppendLiteral("':\n");
				defaultInterpolatedStringHandler.AppendFormatted<Exception>(exception);
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear(), exception);
			}
			return entityBehavior;
		}

		public EntityBehavior CreateEntityBehavior(Entity forEntity, string className)
		{
			Type behaviorType;
			if (!this.entityBehaviorClassNameToTypeMapping.TryGetValue(className, out behaviorType))
			{
				throw new Exception("Don't know how to instantiate entityBehavior of type '" + className + "' did you forget to register a mapping?");
			}
			EntityBehavior entityBehavior;
			try
			{
				entityBehavior = (EntityBehavior)Activator.CreateInstance(behaviorType, new object[] { forEntity });
			}
			catch (Exception exception)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(60, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Error while instantiating entity behavior '");
				defaultInterpolatedStringHandler.AppendFormatted(className);
				defaultInterpolatedStringHandler.AppendLiteral("' for entity '");
				defaultInterpolatedStringHandler.AppendFormatted<AssetLocation>(forEntity.Code);
				defaultInterpolatedStringHandler.AppendLiteral("':\n");
				defaultInterpolatedStringHandler.AppendFormatted<Exception>(exception);
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear(), exception);
			}
			return entityBehavior;
		}

		public void RegisterBlockEntityType(string className, Type blockentity)
		{
			if (ClassRegistry.legacyBlockEntityClassNames.ContainsKey(className))
			{
				throw new ArgumentException("Classname '" + className + "' is a reserved name for backwards compatibility reasons. Please use another term.");
			}
			this.blockEntityClassnameToTypeMapping[className] = blockentity;
			this.blockEntityTypeToClassnameMapping[blockentity] = className;
		}

		public BlockEntity CreateBlockEntity(Type entityType)
		{
			if (!this.blockEntityClassnameToTypeMapping.ContainsValue(entityType))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(85, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Don't know how to instantiate entity of type '");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(entityType);
				defaultInterpolatedStringHandler.AppendLiteral("' did you forget to register a mapping?");
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			BlockEntity blockEntity;
			try
			{
				blockEntity = (BlockEntity)Activator.CreateInstance(entityType);
			}
			catch (Exception exception)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(40, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Error on instantiating block entity '");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(entityType);
				defaultInterpolatedStringHandler.AppendLiteral("':\n");
				defaultInterpolatedStringHandler.AppendFormatted<Exception>(exception);
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear(), exception);
			}
			return blockEntity;
		}

		public BlockEntity CreateBlockEntity(string className)
		{
			if (ClassRegistry.legacyBlockEntityClassNames.ContainsKey(className))
			{
				className = ClassRegistry.legacyBlockEntityClassNames[className];
			}
			Type beType;
			if (!this.blockEntityClassnameToTypeMapping.TryGetValue(className, out beType))
			{
				throw new Exception("Don't know how to instantiate entity of type '" + className + "' did you forget to register a mapping?");
			}
			BlockEntity blockEntity;
			try
			{
				blockEntity = (BlockEntity)Activator.CreateInstance(beType);
			}
			catch (Exception exception)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(40, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Error on instantiating block entity '");
				defaultInterpolatedStringHandler.AppendFormatted(className);
				defaultInterpolatedStringHandler.AppendLiteral("':\n");
				defaultInterpolatedStringHandler.AppendFormatted<Exception>(exception);
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear(), exception);
			}
			return blockEntity;
		}

		public Type GetBlockEntityType(string className)
		{
			if (ClassRegistry.legacyBlockEntityClassNames.ContainsKey(className))
			{
				className = ClassRegistry.legacyBlockEntityClassNames[className];
			}
			Type beType;
			if (!this.blockEntityClassnameToTypeMapping.TryGetValue(className, out beType))
			{
				throw new Exception("Don't know how to instantiate entity of type '" + className + "' did you forget to register a mapping?");
			}
			return beType;
		}

		public void RegisterParticlePropertyProvider(string className, Type ParticleProvider)
		{
			if (this.ParticleProviderTypeToClassnameMapping.ContainsKey(ParticleProvider))
			{
				return;
			}
			this.ParticleProviderClassnameToTypeMapping.Add(className, ParticleProvider);
			this.ParticleProviderTypeToClassnameMapping.Add(ParticleProvider, className);
		}

		public IParticlePropertiesProvider CreateParticlePropertyProvider(Type entityType)
		{
			if (!this.ParticleProviderClassnameToTypeMapping.ContainsValue(entityType))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(85, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Don't know how to instantiate entity of type '");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(entityType);
				defaultInterpolatedStringHandler.AppendLiteral("' did you forget to register a mapping?");
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return (IParticlePropertiesProvider)Activator.CreateInstance(entityType);
		}

		public IParticlePropertiesProvider CreateParticlePropertyProvider(string className)
		{
			Type providerType;
			if (!this.ParticleProviderClassnameToTypeMapping.TryGetValue(className, out providerType))
			{
				throw new Exception("Don't know how to instantiate entity of type '" + className + "' did you forget to register a mapping?");
			}
			return (IParticlePropertiesProvider)Activator.CreateInstance(providerType);
		}

		private void RegisterDefaultParticlePropertyProviders()
		{
			this.RegisterParticlePropertyProvider("simple", typeof(SimpleParticleProperties));
			this.RegisterParticlePropertyProvider("advanced", typeof(AdvancedParticleProperties));
			this.RegisterParticlePropertyProvider("bubbles", typeof(AirBubbleParticles));
			this.RegisterParticlePropertyProvider("watersplash", typeof(WaterSplashParticles));
			this.RegisterParticlePropertyProvider("explosion", typeof(ExplosionSmokeParticles));
			this.RegisterParticlePropertyProvider("stackcubes", typeof(StackCubeParticles));
			this.RegisterParticlePropertyProvider("block", typeof(BlockCubeParticles));
			this.RegisterParticlePropertyProvider("entity", typeof(EntityCubeParticles));
			this.RegisterParticlePropertyProvider("blockbreaking", typeof(BlockBrokenParticleProps));
		}

		public Dictionary<string, GetMountableDelegate> mountableEntries = new Dictionary<string, GetMountableDelegate>();

		public Dictionary<string, Type> inventoryClassToTypeMapping = new Dictionary<string, Type>();

		public Dictionary<string, Type> RecipeRegistryToTypeMapping = new Dictionary<string, Type>();

		public Dictionary<Type, string> TypeToRecipeRegistryMapping = new Dictionary<Type, string>();

		public Dictionary<string, Type> BlockClassToTypeMapping = new Dictionary<string, Type>();

		public Dictionary<string, Type> blockbehaviorToTypeMapping = new Dictionary<string, Type>();

		public Dictionary<string, Type> blockentitybehaviorToTypeMapping = new Dictionary<string, Type>();

		public Dictionary<string, Type> collectibleBehaviorToTypeMapping = new Dictionary<string, Type>();

		public Dictionary<string, Type> cropbehaviorToTypeMapping = new Dictionary<string, Type>();

		public Dictionary<string, Type> ItemClassToTypeMapping = new Dictionary<string, Type>();

		public Dictionary<string, Type> entityClassNameToTypeMapping = new Dictionary<string, Type>();

		public Dictionary<Type, string> entityTypeToClassNameMapping = new Dictionary<Type, string>();

		public Dictionary<string, Type> EntityRendererClassNameToTypeMapping = new Dictionary<string, Type>();

		public Dictionary<Type, string> EntityRendererTypeToClassNameMapping = new Dictionary<Type, string>();

		public Dictionary<string, Type> entityBehaviorClassNameToTypeMapping = new Dictionary<string, Type>();

		public Dictionary<Type, string> entityBehaviorTypeToClassNameMapping = new Dictionary<Type, string>();

		public Dictionary<string, Type> blockEntityClassnameToTypeMapping = new Dictionary<string, Type>();

		public Dictionary<Type, string> blockEntityTypeToClassnameMapping = new Dictionary<Type, string>();

		public static Dictionary<string, string> legacyBlockEntityClassNames = new Dictionary<string, string>
		{
			{ "Chest", "GenericContainer" },
			{ "Basket", "GenericContainer" },
			{ "Axle", "Generic" },
			{ "AngledGears", "Generic" },
			{ "WindmillRotor", "Generic" },
			{ "ClutterBookshelf", "Generic" },
			{ "Clutter", "Generic" },
			{ "EchoChamber", "Resonator" },
			{ "BECommand", "GuiConfigurableCommands" },
			{ "BEConditional", "Conditional" },
			{ "BETicker", "Ticker" }
		};

		public Dictionary<string, Type> ParticleProviderClassnameToTypeMapping = new Dictionary<string, Type>();

		public Dictionary<Type, string> ParticleProviderTypeToClassnameMapping = new Dictionary<Type, string>();
	}
}
