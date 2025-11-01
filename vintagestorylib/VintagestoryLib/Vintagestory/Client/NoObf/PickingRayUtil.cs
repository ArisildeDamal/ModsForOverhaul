using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class PickingRayUtil
	{
		public PickingRayUtil()
		{
			this.unproject = new Unproject();
			this.tempViewport = new double[4];
			this.tempRay = new double[4];
			this.tempRayStartPoint = new double[4];
		}

		public Ray GetPickingRayByMouseCoordinates(ClientMain game)
		{
			int mouseX = game.MouseCurrentX;
			int mouseY = game.MouseCurrentY;
			this.tempViewport[0] = 0.0;
			this.tempViewport[1] = 0.0;
			this.tempViewport[2] = (double)game.Width;
			this.tempViewport[3] = (double)game.Height;
			this.unproject.UnProject(mouseX, game.Height - mouseY, 1, game.MvMatrix.Top, game.PMatrix.Top, this.tempViewport, this.tempRay);
			this.unproject.UnProject(mouseX, game.Height - mouseY, 0, game.MvMatrix.Top, game.PMatrix.Top, this.tempViewport, this.tempRayStartPoint);
			double raydirX = this.tempRay[0] - this.tempRayStartPoint[0];
			double raydirY = this.tempRay[1] - this.tempRayStartPoint[1];
			double raydirZ = this.tempRay[2] - this.tempRayStartPoint[2];
			float raydirLength = this.Length((float)raydirX, (float)raydirY, (float)raydirZ);
			raydirX /= (double)raydirLength;
			raydirY /= (double)raydirLength;
			raydirZ /= (double)raydirLength;
			float pickDistance = game.player.WorldData.PickingRange;
			bool doOffsetOrigin = game.MainCamera.CameraMode != EnumCameraMode.FirstPerson && (game.MouseGrabbed || game.mouseWorldInteractAnyway);
			Ray ray = new Ray(new Vec3d(this.tempRayStartPoint[0] + (doOffsetOrigin ? (raydirX * (double)game.MainCamera.Tppcameradistance) : 0.0), this.tempRayStartPoint[1] + (doOffsetOrigin ? (raydirY * (double)game.MainCamera.Tppcameradistance) : 0.0), this.tempRayStartPoint[2] + (doOffsetOrigin ? (raydirZ * (double)game.MainCamera.Tppcameradistance) : 0.0)), new Vec3d(raydirX * (double)pickDistance, raydirY * (double)pickDistance, raydirZ * (double)pickDistance));
			if (double.IsNaN(ray.origin.X))
			{
				return null;
			}
			return ray;
		}

		internal float Length(float x, float y, float z)
		{
			return (float)Math.Sqrt((double)(x * x + y * y + z * z));
		}

		private Unproject unproject;

		private double[] tempViewport;

		private double[] tempRay;

		private double[] tempRayStartPoint;
	}
}
