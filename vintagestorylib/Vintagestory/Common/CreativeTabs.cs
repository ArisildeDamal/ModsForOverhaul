using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Common
{
	public class CreativeTabs
	{
		public void Add(CreativeTab tab)
		{
			if (tab == null)
			{
				return;
			}
			this.tabsByCode.Add(tab.Code, tab);
			tab.Index = this.index;
			this.index++;
		}

		public OrderedDictionary<string, CreativeTab> TabsByCode
		{
			get
			{
				return this.tabsByCode;
			}
		}

		public CreativeTab GetTabByCode(string code)
		{
			return this.tabsByCode[code];
		}

		public IEnumerable<CreativeTab> Tabs
		{
			get
			{
				return this.tabsByCode.ValuesOrdered;
			}
		}

		internal void CreateSearchCache(IWorldAccessor world)
		{
			foreach (KeyValuePair<string, CreativeTab> val in this.tabsByCode)
			{
				val.Value.CreateSearchCache(world);
			}
		}

		private int index;

		private OrderedDictionary<string, CreativeTab> tabsByCode = new OrderedDictionary<string, CreativeTab>();
	}
}
