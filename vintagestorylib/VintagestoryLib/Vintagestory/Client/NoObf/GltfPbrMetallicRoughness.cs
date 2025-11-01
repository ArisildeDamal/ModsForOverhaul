using System;
using Newtonsoft.Json;

namespace Vintagestory.Client.NoObf
{
	[JsonObject(MemberSerialization.OptIn)]
	public class GltfPbrMetallicRoughness
	{
		[JsonProperty("baseColorTexture")]
		public GltfMatTexture BaseColorTexture { get; set; }

		[JsonProperty("baseColorFactor")]
		public float[] BaseColorFactor { get; set; }

		[JsonProperty("metallicFactor")]
		public float? MetallicFactor { get; set; }

		[JsonProperty("roughnessFactor")]
		public float? RoughnessFactor { get; set; }

		public float[] PbrFactor
		{
			get
			{
				return new float[]
				{
					this.MetallicFactor.GetValueOrDefault(),
					this.RoughnessFactor.GetValueOrDefault(1f)
				};
			}
		}

		[JsonProperty("metallicRoughnessTexture")]
		public GltfMatTexture MetallicRoughnessTexture { get; set; }
	}
}
