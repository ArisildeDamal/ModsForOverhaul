using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ShadowBox
	{
		public ShadowBox(double[] lightViewMatrix, ClientMain game)
		{
			this.lightViewMatrix = lightViewMatrix;
			this.camera = game.MainCamera;
			this.game = game;
			this.calculateWidthsAndHeights();
		}

		public void update()
		{
			double[] rotationMat = this.getCameraRotationMatrix();
			Mat4d.MulWithVec4(rotationMat, ShadowBox.FORWARD, this.forwardVector);
			Vec3d vec3d = new Vec3d(this.forwardVector);
			vec3d.Mul(ShadowBox.SHADOW_DISTANCE);
			Vec3d vec3d2 = new Vec3d(this.forwardVector);
			vec3d2.Mul((double)this.camera.ZNear);
			Vec3d centerNear = vec3d2 + this.camera.OriginPosition;
			Vec3d centerFar = vec3d + this.camera.OriginPosition;
			Vec4d[] array = this.calculateFrustumVertices(rotationMat, this.forwardVector, centerNear, centerFar);
			bool first = true;
			foreach (Vec4d point in array)
			{
				if (first)
				{
					this.minX = point.X;
					this.maxX = point.X;
					this.minY = point.Y;
					this.maxY = point.Y;
					this.minZ = point.Z;
					this.maxZ = point.Z;
					first = false;
				}
				else
				{
					if (point.X > this.maxX)
					{
						this.maxX = point.X;
					}
					else if (point.X < this.minX)
					{
						this.minX = point.X;
					}
					if (point.Y > this.maxY)
					{
						this.maxY = point.Y;
					}
					else if (point.Y < this.minY)
					{
						this.minY = point.Y;
					}
					if (point.Z > this.maxZ)
					{
						this.maxZ = point.Z;
					}
					else if (point.Z < this.minZ)
					{
						this.minZ = point.Z;
					}
				}
			}
			this.minZ += 0.0;
			this.maxZ += ShadowBox.ShadowBoxZExtend;
		}

		public double Width
		{
			get
			{
				return this.maxX - this.minX;
			}
		}

		public double Height
		{
			get
			{
				return this.maxY - this.minY;
			}
		}

		public double Length
		{
			get
			{
				return this.maxZ - this.minZ;
			}
		}

		public Vec4d[] calculateFrustumVertices(double[] rotation, Vec4d forwardVector, Vec3d centerNear, Vec3d centerFar)
		{
			Mat4d.MulWithVec4(rotation, ShadowBox.UP, this.upVector);
			this.rightVector.Cross(forwardVector, this.upVector);
			this.downVector.Set(-this.upVector.X, -this.upVector.Y, -this.upVector.Z);
			this.leftVector.Set(-this.rightVector.X, -this.rightVector.Y, -this.rightVector.Z);
			this.farTop.Set(centerFar.X + this.upVector.X * this.farHeight, centerFar.Y + this.upVector.Y * this.farHeight, centerFar.Z + this.upVector.Z * this.farHeight);
			this.farBottom.Set(centerFar.X + this.downVector.X * this.farHeight, centerFar.Y + this.downVector.Y * this.farHeight, centerFar.Z + this.downVector.Z * this.farHeight);
			this.nearTop.Set(centerNear.X + this.upVector.X * this.nearHeight, centerNear.Y + this.upVector.Y * this.nearHeight, centerNear.Z + this.upVector.Z * this.nearHeight);
			this.nearBottom.Set(centerNear.X + this.downVector.X * this.nearHeight, centerNear.Y + this.downVector.Y * this.nearHeight, centerNear.Z + this.downVector.Z * this.nearHeight);
			this.calculateLightSpaceFrustumCorner(this.farTop, this.rightVector, this.farWidth, this.points[0]);
			this.calculateLightSpaceFrustumCorner(this.farTop, this.leftVector, this.farWidth, this.points[1]);
			this.calculateLightSpaceFrustumCorner(this.farBottom, this.rightVector, this.farWidth, this.points[2]);
			this.calculateLightSpaceFrustumCorner(this.farBottom, this.leftVector, this.farWidth, this.points[3]);
			this.calculateLightSpaceFrustumCorner(this.nearTop, this.rightVector, this.nearWidth, this.points[4]);
			this.calculateLightSpaceFrustumCorner(this.nearTop, this.leftVector, this.nearWidth, this.points[5]);
			this.calculateLightSpaceFrustumCorner(this.nearBottom, this.rightVector, this.nearWidth, this.points[6]);
			this.calculateLightSpaceFrustumCorner(this.nearBottom, this.leftVector, this.nearWidth, this.points[7]);
			return this.points;
		}

		public void calculateLightSpaceFrustumCorner(Vec3d startPoint, Vec3d direction, double width, Vec4d target)
		{
			this.vec4f[0] = startPoint.X + direction.X * width;
			this.vec4f[1] = startPoint.Y + direction.Y * width;
			this.vec4f[2] = startPoint.Z + direction.Z * width;
			Mat4d.MulWithVec4(this.lightViewMatrix, this.vec4f, target);
		}

		public double[] getCameraRotationMatrix()
		{
			Mat4d.Identity(this.rotation);
			return this.rotation;
		}

		public void calculateWidthsAndHeights()
		{
			float fowMul = Math.Min(1f, (float)ClientSettings.FieldOfView / 90f);
			this.farWidth = (double)((float)(ShadowBox.SHADOW_DISTANCE * (double)fowMul));
			this.nearWidth = (double)(this.camera.ZNear * fowMul);
			this.farHeight = this.farWidth / (double)this.getAspectRatio();
			this.nearHeight = this.nearWidth / (double)this.getAspectRatio();
		}

		private float getAspectRatio()
		{
			return (float)this.game.Width / (float)this.game.Height;
		}

		public static double ShadowBoxZExtend = 100.0;

		public static double ShadowBoxYExtend = 0.0;

		public static Vec4d UP = new Vec4d(0.0, 1.0, 0.0, 0.0);

		public static Vec4d FORWARD = new Vec4d(0.0, 0.0, -1.0, 0.0);

		public static double SHADOW_DISTANCE = 100.0;

		public double minX;

		public double maxX;

		public double minY;

		public double maxY;

		public double minZ;

		public double maxZ;

		public double[] lightViewMatrix;

		private Camera camera;

		private ClientMain game;

		public double farHeight;

		public double farWidth;

		public double nearHeight;

		public double nearWidth;

		private Vec4d forwardVector = new Vec4d();

		private Vec4d[] points = new Vec4d[]
		{
			new Vec4d(),
			new Vec4d(),
			new Vec4d(),
			new Vec4d(),
			new Vec4d(),
			new Vec4d(),
			new Vec4d(),
			new Vec4d()
		};

		private Vec4d upVector = new Vec4d();

		private Vec3d rightVector = new Vec3d();

		private Vec3d downVector = new Vec3d();

		private Vec3d leftVector = new Vec3d();

		private Vec3d farTop = new Vec3d();

		private Vec3d farBottom = new Vec3d();

		private Vec3d nearTop = new Vec3d();

		private Vec3d nearBottom = new Vec3d();

		private double[] vec4f = new double[] { 0.0, 0.0, 0.0, 1.0 };

		private double[] rotation = Mat4d.Create();
	}
}
