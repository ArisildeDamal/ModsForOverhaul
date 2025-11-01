using System;
using ProtoBuf;

namespace Vintagestory.Common.Network.Packets
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
	public class MountAnimationPacket
	{
		public string gaitCode;

		public AnimationPacket animPacket;
	}
}
