using System;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.Client.NoObf
{
	public class CameraPoint
	{
		public static CameraPoint FromEntityPos(EntityPos pos)
		{
			return new CameraPoint
			{
				x = pos.X,
				y = pos.Y,
				z = pos.Z,
				pitch = pos.Pitch,
				yaw = pos.Yaw,
				roll = pos.Roll
			};
		}

		internal CameraPoint Clone()
		{
			return new CameraPoint
			{
				x = this.x,
				y = this.y,
				z = this.z,
				pitch = this.pitch,
				yaw = this.yaw,
				roll = this.roll
			};
		}

		internal CameraPoint ExtrapolateFrom(CameraPoint p, int direction)
		{
			double dx = p.x - this.x;
			double dy = p.y - this.y;
			double dz = p.z - this.z;
			float dpitch = p.pitch - this.pitch;
			float dyaw = p.yaw - this.yaw;
			float droll = p.roll - this.roll;
			return new CameraPoint
			{
				x = this.x - dx * (double)direction,
				y = this.y - dy * (double)direction,
				z = this.z - dz * (double)direction,
				pitch = this.pitch - dpitch * (float)direction,
				yaw = this.yaw - dyaw * (float)direction,
				roll = this.roll - droll * (float)direction
			};
		}

		internal bool PositionEquals(CameraPoint point)
		{
			return point.x == this.x && point.y == this.y && point.z == this.z;
		}

		internal double x;

		internal double y;

		internal double z;

		internal float pitch;

		internal float yaw;

		internal float roll;

		internal double distance;
	}
}
