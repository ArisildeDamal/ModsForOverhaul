using System;
using ProtoBuf;

namespace Vintagestory.Common.Network.Packets
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
	public class EntityTagPacket
	{
		public long EntityId = 1L;

		public long TagsBitmask1 = 2L;

		public long TagsBitmask2 = 3L;
	}
}
