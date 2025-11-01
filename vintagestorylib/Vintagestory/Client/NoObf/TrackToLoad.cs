using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.Client.NoObf
{
	public class TrackToLoad
	{
		public EnumSoundType SoundType { get; set; } = EnumSoundType.Music;

		public IMusicTrack ByTrack;

		public AssetLocation Location;

		public float volume = 1f;

		public float pitch = 1f;

		public Action<ILoadedSound> OnLoaded;
	}
}
