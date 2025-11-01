using System;
using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.Common.Network.Packets
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
	public class AnimationPacket
	{
		public AnimationPacket()
		{
		}

		public AnimationPacket(Entity entity)
		{
			this.entityId = entity.EntityId;
			if (entity.AnimManager == null)
			{
				return;
			}
			Dictionary<string, AnimationMetaData> activeAnimationsByAnimCode = entity.AnimManager.ActiveAnimationsByAnimCode;
			if (activeAnimationsByAnimCode.Count <= 0)
			{
				return;
			}
			int[] activeAnimationsArr = new int[activeAnimationsByAnimCode.Count];
			int[] activeAnimationSpeedsArr = new int[activeAnimationsByAnimCode.Count];
			int index = 0;
			foreach (KeyValuePair<string, AnimationMetaData> anim in activeAnimationsByAnimCode)
			{
				AnimationTrigger triggeredBy = anim.Value.TriggeredBy;
				if (triggeredBy == null || !triggeredBy.DefaultAnim)
				{
					activeAnimationSpeedsArr[index] = CollectibleNet.SerializeFloatPrecise(anim.Value.AnimationSpeed);
					activeAnimationsArr[index++] = (int)anim.Value.CodeCrc32;
				}
			}
			this.activeAnimations = activeAnimationsArr;
			this.activeAnimationsCount = index;
			this.activeAnimationsLength = activeAnimationsArr.Length;
			this.activeAnimationSpeeds = activeAnimationSpeedsArr;
			this.activeAnimationSpeedsCount = index;
			this.activeAnimationSpeedsLength = activeAnimationSpeedsArr.Length;
		}

		public long entityId;

		public int[] activeAnimations;

		public int activeAnimationsCount;

		public int activeAnimationsLength;

		public int[] activeAnimationSpeeds;

		public int activeAnimationSpeedsCount;

		public int activeAnimationSpeedsLength;
	}
}
