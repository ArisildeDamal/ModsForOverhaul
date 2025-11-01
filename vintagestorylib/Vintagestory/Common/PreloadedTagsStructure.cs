using System;
using System.Runtime.CompilerServices;

namespace Vintagestory.Common
{
	[NullableContext(1)]
	[Nullable(0)]
	public class PreloadedTagsStructure
	{
		public string[] EntityTags = Array.Empty<string>();

		public string[] ItemTags = Array.Empty<string>();

		public string[] BlockTags = Array.Empty<string>();
	}
}
