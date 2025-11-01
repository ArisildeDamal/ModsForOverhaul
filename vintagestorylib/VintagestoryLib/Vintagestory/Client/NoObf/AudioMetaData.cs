using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf
{
	public class AudioMetaData : AudioData
	{
		public AudioMetaData(IAsset asset)
		{
			if (asset == null)
			{
				throw new ArgumentNullException("Asset cannot be null");
			}
			this.Asset = asset;
		}

		private void DoLoad()
		{
			if (!this.Asset.IsLoaded())
			{
				this.Asset.Origin.LoadAsset(this.Asset);
			}
			AudioMetaData meta = (AudioMetaData)ScreenManager.Platform.CreateAudioData(this.Asset);
			this.Pcm = meta.Pcm;
			this.Channels = meta.Channels;
			this.Rate = meta.Rate;
			this.BitsPerSample = meta.BitsPerSample;
			this.Asset.Data = null;
			this.Loaded = 2;
			lock (this)
			{
				if (this.waitingToPlay != null)
				{
					foreach (MainThreadAction mainThreadAction in this.waitingToPlay)
					{
						mainThreadAction.Enqueue();
					}
					this.waitingToPlay = null;
				}
			}
		}

		public override bool Load()
		{
			if (AsyncHelper.CanProceedOnThisThread(ref this.Loaded))
			{
				this.DoLoad();
				return true;
			}
			return this.Loaded >= 2;
		}

		public override int Load_Async(MainThreadAction onCompleted)
		{
			if (AsyncHelper.CanProceedOnThisThread(ref this.Loaded))
			{
				TyronThreadPool.QueueTask(delegate
				{
					this.DoLoad();
					onCompleted.Enqueue();
				});
				return -1;
			}
			if (this.Loaded == 1)
			{
				this.AddOnLoaded(onCompleted);
				return -1;
			}
			return onCompleted.Invoke();
		}

		public void AddOnLoaded(MainThreadAction onCompleted)
		{
			lock (this)
			{
				if (this.Loaded == 3)
				{
					onCompleted.Enqueue();
				}
				else
				{
					if (this.waitingToPlay == null)
					{
						this.waitingToPlay = new List<MainThreadAction>();
					}
					this.waitingToPlay.Add(onCompleted);
				}
			}
		}

		public void Unload()
		{
			this.Pcm = null;
			this.Channels = 0;
			this.Rate = 0;
			this.BitsPerSample = 0;
			this.Asset.Data = null;
			this.Loaded = 0;
		}

		public byte[] Pcm;

		public int Channels;

		public int Rate;

		public int BitsPerSample = 16;

		public IAsset Asset;

		public bool AutoUnload;

		private List<MainThreadAction> waitingToPlay;
	}
}
