using System;
using System.Collections;
using System.Collections.Generic;

namespace Vintagestory.Common
{
	public class BlocksCompositeIdEnumerator : IEnumerator<int>, IEnumerator, IDisposable
	{
		public BlocksCompositeIdEnumerator(ChunkData inst)
		{
			this.inst = inst;
		}

		public int Current
		{
			get
			{
				return this.inst[this.index];
			}
		}

		object IEnumerator.Current
		{
			get
			{
				return this.inst[this.index];
			}
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			this.index++;
			return this.index < this.inst.Length;
		}

		public void Reset()
		{
			this.index = 0;
		}

		private int index;

		public ChunkData inst;
	}
}
