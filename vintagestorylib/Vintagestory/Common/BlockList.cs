using System;
using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.Common
{
	public class BlockList : IList<Block>, ICollection<Block>, IEnumerable<Block>, IEnumerable
	{
		public Block[] BlocksFast
		{
			get
			{
				return this.blocks;
			}
		}

		public Block this[int index]
		{
			get
			{
				if (index >= this.count)
				{
					return this.getOrCreateNoBlock(index);
				}
				Block block = this.blocks[index];
				if (block == null || block.Id != index)
				{
					return this.blocks[index] = BlockList.getNoBlock(index, this.game.World.Api);
				}
				return block;
			}
			set
			{
				if (index != value.Id)
				{
					throw new InvalidOperationException("Trying to add a block at index != id");
				}
				while (index >= this.count)
				{
					this.Add(null);
				}
				this.blocks[index] = value;
			}
		}

		public int Count
		{
			get
			{
				return this.count;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		public BlockList(GameMain game, int initialSize = 10000)
		{
			this.game = game;
			this.blocks = new Block[initialSize];
		}

		public BlockList(GameMain game, Block[] fromBlocks)
		{
			this.game = game;
			this.blocks = fromBlocks;
			this.count = fromBlocks.Length;
			for (int index = 0; index < fromBlocks.Length; index++)
			{
				Block block = fromBlocks[index];
				if (block == null || block.Id != index)
				{
					this.blocks[index] = BlockList.getNoBlock(index, game.World.Api);
				}
			}
		}

		public void PreAlloc(int atLeastSize)
		{
			if (atLeastSize > this.blocks.Length)
			{
				Array.Resize<Block>(ref this.blocks, atLeastSize + 10);
			}
		}

		public void Add(Block block)
		{
			if (this.blocks.Length <= this.count)
			{
				Array.Resize<Block>(ref this.blocks, this.blocks.Length + 250);
			}
			Block[] array = this.blocks;
			int num = this.count;
			this.count = num + 1;
			array[num] = block;
		}

		public void Clear()
		{
			this.count = 0;
		}

		public bool Contains(Block item)
		{
			return this.blocks.Contains(item);
		}

		public Block[] Search(AssetLocation wildcard)
		{
			if (wildcard.Path.Length == 0)
			{
				return Array.Empty<Block>();
			}
			string wildcardPathAsRegex = WildcardUtil.Prepare(wildcard.Path);
			if (wildcardPathAsRegex == null)
			{
				for (int i = 0; i < this.blocks.Length; i++)
				{
					if (i > this.count)
					{
						return Array.Empty<Block>();
					}
					Block block = this.blocks[i];
					if (block != null && !block.IsMissing && wildcard.Equals(block.Code) && block.Id == i)
					{
						return new Block[] { block };
					}
				}
				return Array.Empty<Block>();
			}
			List<Block> foundBlocks = new List<Block>();
			for (int j = 0; j < this.blocks.Length; j++)
			{
				if (j > this.count)
				{
					return foundBlocks.ToArray();
				}
				Block block2 = this.blocks[j];
				if (((block2 != null) ? block2.Code : null) != null && !block2.IsMissing && wildcard.WildCardMatch(block2.Code, wildcardPathAsRegex) && block2.Id == j)
				{
					foundBlocks.Add(block2);
				}
			}
			return foundBlocks.ToArray();
		}

		public void CopyTo(Block[] array, int arrayIndex)
		{
			for (int i = arrayIndex; i < this.count; i++)
			{
				array[i] = this[i];
			}
		}

		public IEnumerator<Block> GetEnumerator()
		{
			BlockList.<GetEnumerator>d__21 <GetEnumerator>d__ = new BlockList.<GetEnumerator>d__21(0);
			<GetEnumerator>d__.<>4__this = this;
			return <GetEnumerator>d__;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			BlockList.<System-Collections-IEnumerable-GetEnumerator>d__22 <System-Collections-IEnumerable-GetEnumerator>d__ = new BlockList.<System-Collections-IEnumerable-GetEnumerator>d__22(0);
			<System-Collections-IEnumerable-GetEnumerator>d__.<>4__this = this;
			return <System-Collections-IEnumerable-GetEnumerator>d__;
		}

		public int IndexOf(Block item)
		{
			return this.blocks.IndexOf(item);
		}

		public void Insert(int index, Block item)
		{
			throw new NotImplementedException("This method should not be used on block lists, it changes block ids in unexpected ways");
		}

		public bool Remove(Block item)
		{
			throw new NotImplementedException("This method should not be used on block lists, it changes block ids in unexpected ways");
		}

		public void RemoveAt(int index)
		{
			throw new NotImplementedException("This method should not be used on block lists, it changes block ids in unexpected ways");
		}

		private Block getOrCreateNoBlock(int id)
		{
			Block block;
			if (!this.noBlocks.TryGetValue(id, out block))
			{
				block = (this.noBlocks[id] = BlockList.getNoBlock(id, this.game.World.Api));
			}
			return block;
		}

		public static Block getNoBlock(int id, ICoreAPI Api)
		{
			Block block = new Block();
			block.Code = null;
			block.BlockId = id;
			block.IsMissing = true;
			block.Textures = new FastSmallDictionary<string, CompositeTexture>(0);
			block.GuiTransform = BlockList.guitf;
			block.FpHandTransform = BlockList.fptf;
			block.GroundTransform = BlockList.gndtf;
			block.TpHandTransform = BlockList.tptf;
			block.DrawType = EnumDrawType.Empty;
			block.MatterState = EnumMatterState.Gas;
			block.Sounds = new BlockSounds();
			block.Replaceable = 999;
			block.CollisionBoxes = null;
			block.SelectionBoxes = null;
			block.RainPermeable = true;
			block.AllSidesOpaque = false;
			block.SideSolid = new SmallBoolArray(0);
			block.VertexFlags = new VertexFlags();
			block.OnLoadedNative(Api);
			return block;
		}

		private Block[] blocks;

		private int count;

		private Dictionary<int, Block> noBlocks = new Dictionary<int, Block>();

		private GameMain game;

		public static ModelTransform guitf = ModelTransform.BlockDefaultGui();

		public static ModelTransform fptf = ModelTransform.BlockDefaultFp();

		public static ModelTransform gndtf = ModelTransform.BlockDefaultGround();

		public static ModelTransform tptf = ModelTransform.BlockDefaultTp();
	}
}
