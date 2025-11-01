using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Common
{
	public abstract class APIBase : ICoreAPICommon
	{
		public Dictionary<string, object> ObjectCache
		{
			get
			{
				return this.objectCache;
			}
		}

		public abstract ClassRegistry ClassRegistryNative { get; }

		public APIBase(GameMain gamemain)
		{
			this.gamemain = gamemain;
		}

		public T RegisterRecipeRegistry<T>(string recipeRegistryCode) where T : RecipeRegistryBase
		{
			return this.gamemain.RegisterRecipeRegistry<T>(recipeRegistryCode);
		}

		public void RegisterEntity(string classsName, Type entity)
		{
			this.ClassRegistryNative.RegisterEntityType(classsName, entity);
		}

		public void RegisterEntityBehaviorClass(string className, Type entityBehavior)
		{
			this.ClassRegistryNative.RegisterentityBehavior(className, entityBehavior);
		}

		public void RegisterBlockClass(string blockClassName, Type type)
		{
			this.ClassRegistryNative.RegisterBlockClass(blockClassName, type);
		}

		public void RegisterCropBehavior(string className, Type type)
		{
			this.ClassRegistryNative.RegisterCropBehavior(className, type);
		}

		public void RegisterBlockEntityClass(string className, Type blockentity)
		{
			this.ClassRegistryNative.RegisterBlockEntityType(className, blockentity);
		}

		public void RegisterItemClass(string className, Type itemType)
		{
			this.ClassRegistryNative.RegisterItemClass(className, itemType);
		}

		public void RegisterBlockBehaviorClass(string className, Type blockBehaviorType)
		{
			this.ClassRegistryNative.RegisterBlockBehaviorClass(className, blockBehaviorType);
		}

		public void RegisterCollectibleBehaviorClass(string className, Type blockBehaviorType)
		{
			this.ClassRegistryNative.RegisterCollectibleBehaviorClass(className, blockBehaviorType);
		}

		public void RegisterBlockEntityBehaviorClass(string className, Type blockEntityBehaviorType)
		{
			this.ClassRegistryNative.RegisterBlockEntityBehaviorClass(className, blockEntityBehaviorType);
		}

		public void RegisterMountable(string className, GetMountableDelegate mountableInstancer)
		{
			this.ClassRegistryNative.RegisterMountable(className, mountableInstancer);
		}

		public string DataBasePath
		{
			get
			{
				return GamePaths.DataPath;
			}
		}

		public string GetOrCreateDataPath(string foldername)
		{
			string path = Path.Combine(GamePaths.DataPath, foldername);
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			return path;
		}

		public T LoadModConfig<T>(string filename)
		{
			string path = Path.Combine(GamePaths.ModConfig, filename);
			if (File.Exists(path))
			{
				return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
			}
			return default(T);
		}

		public void StoreModConfig<T>(T jsonSerializeableData, string filename)
		{
			FileInfo fileInfo = new FileInfo(Path.Combine(GamePaths.ModConfig, filename));
			GamePaths.EnsurePathExists(fileInfo.Directory.FullName);
			string json = JsonConvert.SerializeObject(jsonSerializeableData, Formatting.Indented);
			File.WriteAllText(fileInfo.FullName, json);
		}

		public abstract void RegisterColorMap(ColorMap map);

		public void StoreModConfig(JsonObject jobj, string filename)
		{
			FileInfo fileInfo = new FileInfo(Path.Combine(GamePaths.ModConfig, filename));
			GamePaths.EnsurePathExists(fileInfo.Directory.FullName);
			File.WriteAllText(fileInfo.FullName, jobj.Token.ToString());
		}

		public JsonObject LoadModConfig(string filename)
		{
			string path = Path.Combine(GamePaths.ModConfig, filename);
			if (File.Exists(path))
			{
				return new JsonObject(JObject.Parse(File.ReadAllText(path)));
			}
			return null;
		}

		internal Dictionary<string, object> objectCache = new Dictionary<string, object>();

		private GameMain gamemain;
	}
}
