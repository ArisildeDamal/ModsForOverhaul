using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Common
{
	[JsonObject(MemberSerialization.OptIn)]
	public class StartServerArgs
	{
		public StartServerArgs Clone()
		{
			return (StartServerArgs)base.MemberwiseClone();
		}

		[JsonProperty]
		public string Seed;

		[JsonProperty]
		public string SaveFileLocation;

		[JsonProperty]
		public string WorldName;

		[JsonProperty]
		public bool AllowCreativeMode;

		[JsonProperty]
		public string PlayStyle;

		[JsonProperty]
		public string PlayStyleLangCode;

		[JsonProperty]
		public string WorldType;

		[JsonProperty]
		[JsonConverter(typeof(JsonAttributesConverter))]
		public JsonObject WorldConfiguration;

		[JsonProperty]
		public int? MapSizeY;

		[JsonProperty]
		internal string CreatedByPlayerName;

		[JsonProperty]
		internal List<string> DisabledMods;

		[JsonProperty]
		internal bool RepairMode;

		public string Language;

		public bool IsNew;
	}
}
