using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.Common
{
	[NullableContext(1)]
	[Nullable(0)]
	public class TagRegistry : ITagRegistry
	{
		public void RegisterEntityTags(params string[] tags)
		{
			this.ProcessTags(tags, this.entityTags, this.entityTagsToTagIds, false, 128);
		}

		public void RegisterItemTags(params string[] tags)
		{
			this.ProcessTags(tags, this.itemTags, this.itemTagsToTagIds, false, 256);
		}

		public void RegisterBlockTags(params string[] tags)
		{
			this.ProcessTags(tags, this.blockTags, this.blockTagsToTagIds, false, 256);
		}

		public ushort[] EntityTagsToTagIds(string[] tags, bool removeUnknownTags = false)
		{
			return (from id in tags.Select(new Func<string, ushort>(this.EntityTagToTagId))
				where !removeUnknownTags || id > 0
				select id).Order<ushort>().ToArray<ushort>();
		}

		public ushort[] ItemTagsToTagIds(string[] tags, bool removeUnknownTags = false)
		{
			return (from id in tags.Select(new Func<string, ushort>(this.ItemTagToTagId))
				where !removeUnknownTags || id > 0
				select id).Order<ushort>().ToArray<ushort>();
		}

		public ushort[] BlockTagsToTagIds(string[] tags, bool removeUnknownTags = false)
		{
			return (from id in tags.Select(new Func<string, ushort>(this.BlockTagToTagId))
				where !removeUnknownTags || id > 0
				select id).Order<ushort>().ToArray<ushort>();
		}

		public EntityTagArray EntityTagsToTagArray(params string[] tags)
		{
			return new EntityTagArray(tags.Select(new Func<string, ushort>(this.EntityTagToTagId)));
		}

		public ItemTagArray ItemTagsToTagArray(params string[] tags)
		{
			return new ItemTagArray(tags.Select(new Func<string, ushort>(this.ItemTagToTagId)));
		}

		public BlockTagArray BlockTagsToTagArray(params string[] tags)
		{
			return new BlockTagArray(tags.Select(new Func<string, ushort>(this.BlockTagToTagId)));
		}

		public ushort EntityTagToTagId(string tag)
		{
			return TagRegistry.TryGetTagId(tag, this.entityTagsToTagIds);
		}

		public ushort ItemTagToTagId(string tag)
		{
			return TagRegistry.TryGetTagId(tag, this.itemTagsToTagIds);
		}

		public ushort BlockTagToTagId(string tag)
		{
			return TagRegistry.TryGetTagId(tag, this.blockTagsToTagIds);
		}

		public string EntityTagIdToTag(ushort id)
		{
			if (id != 0)
			{
				return this.entityTags[(int)(id - 1)];
			}
			return "";
		}

		public string ItemTagIdToTag(ushort id)
		{
			if (id != 0)
			{
				return this.itemTags[(int)(id - 1)];
			}
			return "";
		}

		public string BlockTagIdToTag(ushort id)
		{
			if (id != 0)
			{
				return this.blockTags[(int)(id - 1)];
			}
			return "";
		}

		public void LoadTagsFromAssets(ICoreServerAPI api)
		{
			foreach (KeyValuePair<AssetLocation, PreloadedTagsStructure> keyValuePair in api.Assets.GetMany<PreloadedTagsStructure>(api.Logger, "config/preloaded-tags", null))
			{
				AssetLocation assetLocation;
				PreloadedTagsStructure preloadedTagsStructure;
				keyValuePair.Deconstruct(out assetLocation, out preloadedTagsStructure);
				AssetLocation location = assetLocation;
				PreloadedTagsStructure tags = preloadedTagsStructure;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
				try
				{
					this.RegisterEntityTags(tags.EntityTags);
					this.RegisterItemTags(tags.ItemTags);
					this.RegisterBlockTags(tags.BlockTags);
				}
				catch (Exception exception)
				{
					ILogger logger = api.Logger;
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(42, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Error while loading tags from domain '");
					defaultInterpolatedStringHandler.AppendFormatted(location.Domain);
					defaultInterpolatedStringHandler.AppendLiteral("': \n");
					defaultInterpolatedStringHandler.AppendFormatted<Exception>(exception);
					logger.Error(defaultInterpolatedStringHandler.ToStringAndClear());
					continue;
				}
				ILogger logger2 = api.Logger;
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(62, 4);
				defaultInterpolatedStringHandler.AppendLiteral("Loaded ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(tags.EntityTags.Length);
				defaultInterpolatedStringHandler.AppendLiteral(" entity tags, ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(tags.ItemTags.Length);
				defaultInterpolatedStringHandler.AppendLiteral(" item tags and ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(tags.BlockTags.Length);
				defaultInterpolatedStringHandler.AppendLiteral(" block tags from '");
				defaultInterpolatedStringHandler.AppendFormatted(location.Domain);
				defaultInterpolatedStringHandler.AppendLiteral("' domain");
				logger2.Notification(defaultInterpolatedStringHandler.ToStringAndClear());
				if (tags.EntityTags.Length != 0)
				{
					string loadedEntityTags = tags.EntityTags.Aggregate((string first, string second) => first + ", " + second);
					ILogger logger3 = api.Logger;
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(36, 3);
					defaultInterpolatedStringHandler.AppendLiteral("Loaded ");
					defaultInterpolatedStringHandler.AppendFormatted<int>(tags.EntityTags.Length);
					defaultInterpolatedStringHandler.AppendLiteral(" entity tags from '");
					defaultInterpolatedStringHandler.AppendFormatted(location.Domain);
					defaultInterpolatedStringHandler.AppendLiteral("' domain: ");
					defaultInterpolatedStringHandler.AppendFormatted(loadedEntityTags);
					logger3.VerboseDebug(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				if (tags.ItemTags.Length != 0)
				{
					string loadedItemTags = tags.ItemTags.Aggregate((string first, string second) => first + ", " + second);
					ILogger logger4 = api.Logger;
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 3);
					defaultInterpolatedStringHandler.AppendLiteral("Loaded ");
					defaultInterpolatedStringHandler.AppendFormatted<int>(tags.ItemTags.Length);
					defaultInterpolatedStringHandler.AppendLiteral(" item tags from '");
					defaultInterpolatedStringHandler.AppendFormatted(location.Domain);
					defaultInterpolatedStringHandler.AppendLiteral("' domain: ");
					defaultInterpolatedStringHandler.AppendFormatted(loadedItemTags);
					logger4.VerboseDebug(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				if (tags.BlockTags.Length != 0)
				{
					string loadedBlockTags = tags.BlockTags.Aggregate((string first, string second) => first + ", " + second);
					ILogger logger5 = api.Logger;
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 3);
					defaultInterpolatedStringHandler.AppendLiteral("Loaded ");
					defaultInterpolatedStringHandler.AppendFormatted<int>(tags.BlockTags.Length);
					defaultInterpolatedStringHandler.AppendLiteral(" block tags from '");
					defaultInterpolatedStringHandler.AppendFormatted(location.Domain);
					defaultInterpolatedStringHandler.AppendLiteral("' domain: ");
					defaultInterpolatedStringHandler.AppendFormatted(loadedBlockTags);
					logger5.VerboseDebug(defaultInterpolatedStringHandler.ToStringAndClear());
				}
			}
		}

		internal bool restrictNewTags { get; set; }

		internal EnumAppSide Side { get; set; } = EnumAppSide.Server;

		internal void RegisterEntityTagsOnClient(IEnumerable<string> tags)
		{
			this.ProcessTags(tags, this.entityTags, this.entityTagsToTagIds, true, 0);
		}

		internal void RegisterItemTagsOnClient(IEnumerable<string> tags)
		{
			this.ProcessTags(tags, this.itemTags, this.itemTagsToTagIds, true, 0);
		}

		internal void RegisterBlockTagsOnClient(IEnumerable<string> tags)
		{
			this.ProcessTags(tags, this.blockTags, this.blockTagsToTagIds, true, 0);
		}

		private static ushort TryGetTagId(string tag, OrderedDictionary<string, ushort> mapping)
		{
			ushort id;
			if (mapping.TryGetValue(tag, out id))
			{
				return id;
			}
			return 0;
		}

		private void ProcessTags(IEnumerable<string> objectTags, List<string> idsToTags, OrderedDictionary<string, ushort> tagsToIds, bool ignoreClientSide = false, int maximumTags = 0)
		{
			if (!objectTags.Any<string>())
			{
				return;
			}
			objectTags = objectTags.Distinct<string>();
			if (!ignoreClientSide && this.Side == EnumAppSide.Client)
			{
				throw new InvalidOperationException("Error when registering tags: " + objectTags.Aggregate((string first, string second) => first + ", " + second) + ".\nCannot register new tags on client side.");
			}
			if (this.restrictNewTags)
			{
				throw new InvalidOperationException("Error when registering tags: " + objectTags.Aggregate((string first, string second) => first + ", " + second) + ".\nCannot add new tags. The registry is synchronized and locked.");
			}
			List<string> newTags = new List<string>();
			foreach (string tag in objectTags)
			{
				if (!idsToTags.Contains(tag))
				{
					newTags.Add(tag);
				}
			}
			foreach (string tag2 in newTags)
			{
				ushort id = (ushort)(idsToTags.Count + 1);
				if (maximumTags > 0 && (int)id > maximumTags)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(85, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Error when registering tags: ");
					defaultInterpolatedStringHandler.AppendFormatted(objectTags.Aggregate((string first, string second) => first + ", " + second));
					defaultInterpolatedStringHandler.AppendLiteral(".\nCannot register more than ");
					defaultInterpolatedStringHandler.AppendFormatted<int>(maximumTags);
					defaultInterpolatedStringHandler.AppendLiteral(" tags. The registry is full.");
					throw new InvalidOperationException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				idsToTags.Add(tag2);
				tagsToIds.Add(tag2, id);
			}
		}

		public const int MaxEntityTags = 128;

		public const int MaxItemTags = 256;

		public const int MaxBlockTags = 256;

		public const string TagsAssetPath = "config/preloaded-tags";

		internal List<string> entityTags = new List<string>();

		internal List<string> itemTags = new List<string>();

		internal List<string> blockTags = new List<string>();

		private readonly OrderedDictionary<string, ushort> entityTagsToTagIds = new OrderedDictionary<string, ushort>();

		private readonly OrderedDictionary<string, ushort> itemTagsToTagIds = new OrderedDictionary<string, ushort>();

		private readonly OrderedDictionary<string, ushort> blockTagsToTagIds = new OrderedDictionary<string, ushort>();
	}
}
