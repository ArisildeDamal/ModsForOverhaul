using System;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf
{
	public class RenderOrderReservation
	{
		public bool Conflicts(IRenderer handler, double newRenderOrder)
		{
			if (newRenderOrder < this.rangeStart)
			{
				return false;
			}
			if (newRenderOrder > this.rangeEnd)
			{
				return false;
			}
			Type testedType = handler.GetType();
			return !this.allowedType1.IsAssignableFrom(testedType) && !this.allowedType2.IsAssignableFrom(testedType);
		}

		public double rangeStart;

		public double rangeEnd;

		public Type allowedType1;

		public Type allowedType2;
	}
}
