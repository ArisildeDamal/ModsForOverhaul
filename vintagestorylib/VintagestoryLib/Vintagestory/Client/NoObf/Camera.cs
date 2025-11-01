using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class Camera
	{
		public Vec3d CamSourcePosition
		{
			get
			{
				return this.camEyePosIn;
			}
			set
			{
				this.camEyePosIn = value;
			}
		}

		public Vec3d OriginPosition
		{
			get
			{
				return this.originPos;
			}
			set
			{
				this.originPos = value;
			}
		}

		public double Yaw { get; set; }

		public double Pitch { get; set; }

		public double Roll { get; set; }

		public Camera()
		{
			this.CameraMode = EnumCameraMode.FirstPerson;
			this.CameraOffset.EnsureDefaultValues();
			this.Tppcameradistance = 3f;
			this.TppCameraDistanceMin = 1;
			this.TppCameraDistanceMax = 10;
			this.CameraMatrix = Mat4d.Create();
			this.upVec3 = Vec3Utilsd.FromValues(0.0, 1.0, 0.0);
		}

		internal virtual void SetMode(EnumCameraMode type)
		{
			this.CameraMode = type;
		}

		public void Update(float deltaTime, AABBIntersectionTest intersectionTester)
		{
			this.CameraMatrix = this.GetCameraMatrix(this.camEyePosIn, this.camEyePosIn, this.Yaw, this.Pitch, intersectionTester);
			this.CameraEyePos.Set(this.camEyePosOutTmp);
			this.CameraMatrixOrigin = this.GetCameraMatrix(this.originPos, this.camEyePosIn, this.Yaw, this.Pitch, intersectionTester);
			double[] cameraMatrixOrigin = this.CameraMatrixOrigin;
			double[] cameraMatrixOrigin2 = this.CameraMatrixOrigin;
			double roll = this.Roll;
			double[] array = new double[3];
			array[0] = 1.0;
			Mat4d.Rotate(cameraMatrixOrigin, cameraMatrixOrigin2, roll, array);
			for (int i = 0; i < 16; i++)
			{
				this.CameraMatrixOriginf[i] = (float)this.CameraMatrixOrigin[i];
			}
		}

		public double[] GetCameraMatrix(Vec3d camEyePosIn, Vec3d worldPos, double yaw, double pitch, AABBIntersectionTest intersectionTester)
		{
			VectorTool.ToVectorInFixedSystem((double)this.CameraOffset.Translation.X, (double)this.CameraOffset.Translation.Y, (double)(this.CameraOffset.Translation.Z + 1f), (double)this.CameraOffset.Rotation.X + pitch, (double)this.CameraOffset.Rotation.Y - yaw + 3.1415927410125732, this.forwardVec);
			IClientWorldAccessor cworld = intersectionTester.bsTester as IClientWorldAccessor;
			EntityPlayer plr = cworld.Player.Entity;
			(cworld.Player as ClientPlayer).OverrideCameraMode = null;
			EnumCameraMode cameraMode = this.CameraMode;
			if (cameraMode - EnumCameraMode.ThirdPerson <= 1)
			{
				float camDist = ((this.CameraMode == EnumCameraMode.FirstPerson) ? 0f : this.Tppcameradistance);
				this.camTargetTmp.X = worldPos.X + plr.LocalEyePos.X;
				this.camTargetTmp.Y = worldPos.Y + plr.LocalEyePos.Y;
				this.camTargetTmp.Z = worldPos.Z + plr.LocalEyePos.Z;
				this.camEyePosOutTmp.X = this.camTargetTmp.X + this.forwardVec.X * (double)(-(double)camDist);
				this.camEyePosOutTmp.Y = this.camTargetTmp.Y + this.forwardVec.Y * (double)(-(double)camDist);
				this.camEyePosOutTmp.Z = this.camTargetTmp.Z + this.forwardVec.Z * (double)(-(double)camDist);
				FloatRef currentCameradistance = FloatRef.Create(camDist);
				if (camDist > 0f && !this.LimitThirdPersonCameraToWalls(intersectionTester, yaw, this.camEyePosOutTmp, this.camTargetTmp, currentCameradistance))
				{
					(cworld.Player as ClientPlayer).OverrideCameraMode = new EnumCameraMode?(EnumCameraMode.FirstPerson);
					return this.lookatFp(plr, camEyePosIn);
				}
				if ((double)currentCameradistance.value > 0.5)
				{
					this.camTargetTmp.X = camEyePosIn.X + plr.LocalEyePos.X;
					this.camTargetTmp.Y = camEyePosIn.Y + plr.LocalEyePos.Y;
					this.camTargetTmp.Z = camEyePosIn.Z + plr.LocalEyePos.Z;
					this.camEyePosOutTmp.X = this.camTargetTmp.X + this.forwardVec.X * (double)(-(double)currentCameradistance.value);
					this.camEyePosOutTmp.Y = this.camTargetTmp.Y + this.forwardVec.Y * (double)(-(double)currentCameradistance.value);
					this.camEyePosOutTmp.Z = this.camTargetTmp.Z + this.forwardVec.Z * (double)(-(double)currentCameradistance.value);
					return this.lookAt(this.camEyePosOutTmp, this.camTargetTmp);
				}
				this.camEyePosOutTmp.X = camEyePosIn.X + plr.LocalEyePos.X + this.forwardVec.X * 0.2;
				this.camEyePosOutTmp.Y = camEyePosIn.Y + plr.LocalEyePos.Y + this.forwardVec.Y * 0.2;
				this.camEyePosOutTmp.Z = camEyePosIn.Z + plr.LocalEyePos.Z + this.forwardVec.Z * 0.2;
				this.camTargetTmp.X = this.camEyePosOutTmp.X + this.forwardVec.X;
				this.camTargetTmp.Y = this.camEyePosOutTmp.Y + this.forwardVec.Y;
				this.camTargetTmp.Z = this.camEyePosOutTmp.Z + this.forwardVec.Z;
				return this.lookAt(this.camEyePosOutTmp, this.camTargetTmp);
			}
			else
			{
				ICoreAPI api = cworld.Api;
				this.camTargetTmp.X = camEyePosIn.X + plr.LocalEyePos.X;
				this.camTargetTmp.Y = camEyePosIn.Y + plr.LocalEyePos.Y;
				this.camTargetTmp.Z = camEyePosIn.Z + plr.LocalEyePos.Z;
				if (camEyePosIn == this.OriginPosition || !cworld.Player.ImmersiveFpMode)
				{
					return this.lookatFp(plr, camEyePosIn);
				}
				float cameraSize = 0.5f;
				RenderAPIGame rpi = (cworld as ClientMain).api.renderapi;
				if (cworld.Player.WorldData.NoClip || this.cameraStuck)
				{
					this.eyePosAbs.Set(this.camTargetTmp);
					rpi.CameraStuck = cworld.CollisionTester.IsColliding(cworld.BlockAccessor, new Cuboidf(cameraSize), this.eyePosAbs, false);
					return this.lookatFp(plr, camEyePosIn);
				}
				if (this.camTargetTmp.DistanceTo(this.eyePosAbs) > 1f)
				{
					this.eyePosAbs.Set(this.camTargetTmp);
				}
				else
				{
					Vec3d cameraMotion = this.camTargetTmp - this.eyePosAbs;
					EnumCollideFlags flags = this.UpdateCameraMotion(cworld, this.eyePosAbs, cameraMotion.Mul(1.01), cameraSize);
					this.eyePosAbs.Add(cameraMotion.Mul(0.99));
					plr.LocalEyePos.Set(this.eyePosAbs.X - camEyePosIn.X, this.eyePosAbs.Y - camEyePosIn.Y, this.eyePosAbs.Z - camEyePosIn.Z);
					rpi.CameraStuck = flags > (EnumCollideFlags)0;
					if (flags != (EnumCollideFlags)0)
					{
						if ((double)cworld.Player.CameraPitch > 3.769911289215088)
						{
							plr.LocalEyePos.Y += ((double)cworld.Player.CameraPitch - 3.769911289215088) / 8.0;
						}
						this.cameraStuck = cworld.CollisionTester.IsColliding(cworld.BlockAccessor, new Cuboidf(cameraSize * 0.99f), this.eyePosAbs, false);
					}
				}
				this.camEyePosOutTmp.X = this.eyePosAbs.X;
				this.camEyePosOutTmp.Y = this.eyePosAbs.Y;
				this.camEyePosOutTmp.Z = this.eyePosAbs.Z;
				this.to.Set(this.camEyePosOutTmp.X + this.forwardVec.X, this.camEyePosOutTmp.Y + this.forwardVec.Y, this.camEyePosOutTmp.Z + this.forwardVec.Z);
				return this.lookAt(this.camTargetTmp, this.to);
			}
		}

		private double[] lookatFp(EntityPlayer plr, Vec3d camEyePosIn)
		{
			this.camEyePosOutTmp.X = camEyePosIn.X + plr.LocalEyePos.X;
			this.camEyePosOutTmp.Y = camEyePosIn.Y + plr.LocalEyePos.Y;
			this.camEyePosOutTmp.Z = camEyePosIn.Z + plr.LocalEyePos.Z;
			this.camTargetTmp.X = this.camEyePosOutTmp.X + this.forwardVec.X;
			this.camTargetTmp.Y = this.camEyePosOutTmp.Y + this.forwardVec.Y;
			this.camTargetTmp.Z = this.camEyePosOutTmp.Z + this.forwardVec.Z;
			return this.lookAt(this.camEyePosOutTmp, this.camTargetTmp);
		}

		private double[] lookAt(Vec3d from, Vec3d to)
		{
			double[] array = new double[16];
			Mat4d.LookAt(array, from.ToDoubleArray(), to.ToDoubleArray(), this.upVec3);
			return array;
		}

		public bool LimitThirdPersonCameraToWalls(AABBIntersectionTest intersectionTester, double yaw, Vec3d eye, Vec3d target, FloatRef curtppcameradistance)
		{
			float centerDistance = this.GetIntersectionDistance(intersectionTester, eye, target);
			float leftDistance = this.GetIntersectionDistance(intersectionTester, eye.AheadCopy(0.15000000596046448, 0.0, yaw), target.AheadCopy(0.15000000596046448, 0.0, yaw));
			float rightDistance = this.GetIntersectionDistance(intersectionTester, eye.AheadCopy(-0.15000000596046448, 0.0, yaw), target.AheadCopy(-0.15000000596046448, 0.0, yaw));
			float distance = GameMath.Min(new float[] { centerDistance, leftDistance, rightDistance });
			if ((double)distance < 0.35)
			{
				return false;
			}
			curtppcameradistance.value = Math.Min(curtppcameradistance.value, distance);
			double raydirX = eye.X - target.X;
			double raydirY = eye.Y - target.Y;
			double raydirZ = eye.Z - target.Z;
			float raydirLength = (float)Math.Sqrt(raydirX * raydirX + raydirY * raydirY + raydirZ * raydirZ);
			raydirX /= (double)raydirLength;
			raydirY /= (double)raydirLength;
			raydirZ /= (double)raydirLength;
			raydirX *= (double)(this.Tppcameradistance + 1f);
			raydirY *= (double)(this.Tppcameradistance + 1f);
			raydirZ *= (double)(this.Tppcameradistance + 1f);
			float raydirLength2 = (float)Math.Sqrt(raydirX * raydirX + raydirY * raydirY + raydirZ * raydirZ);
			raydirX /= (double)raydirLength2;
			raydirY /= (double)raydirLength2;
			raydirZ /= (double)raydirLength2;
			eye.X = target.X + raydirX * (double)curtppcameradistance.value;
			eye.Y = target.Y + raydirY * (double)curtppcameradistance.value;
			eye.Z = target.Z + raydirZ * (double)curtppcameradistance.value;
			return true;
		}

		private float GetIntersectionDistance(AABBIntersectionTest intersectionTester, Vec3d eye, Vec3d target)
		{
			Line3D pick = new Line3D();
			double raydirX = eye.X - target.X;
			double raydirY = eye.Y - target.Y;
			double raydirZ = eye.Z - target.Z;
			float raydirLength = (float)Math.Sqrt(raydirX * raydirX + raydirY * raydirY + raydirZ * raydirZ);
			raydirX /= (double)raydirLength;
			raydirY /= (double)raydirLength;
			raydirZ /= (double)raydirLength;
			raydirX *= (double)(this.Tppcameradistance + 1f);
			raydirY *= (double)(this.Tppcameradistance + 1f);
			raydirZ *= (double)(this.Tppcameradistance + 1f);
			pick.Start = target.ToDoubleArray();
			pick.End = new double[3];
			pick.End[0] = target.X + raydirX;
			pick.End[1] = target.Y + raydirY;
			pick.End[2] = target.Z + raydirZ;
			intersectionTester.LoadRayAndPos(pick);
			BlockSelection selection = intersectionTester.GetSelectedBlock((float)this.TppCameraDistanceMax, (BlockPos pos, Block block) => block.CollisionBoxes != null && block.CollisionBoxes.Length != 0 && block.RenderPass != EnumChunkRenderPass.Transparent && block.RenderPass != EnumChunkRenderPass.Meta, false);
			if (selection != null)
			{
				float pickX = (float)((double)selection.Position.X + selection.HitPosition.X - target.X);
				float pickY = (float)((double)selection.Position.InternalY + selection.HitPosition.Y - target.Y);
				float pickZ = (float)((double)selection.Position.Z + selection.HitPosition.Z - target.Z);
				float pickdistance = this.Length(pickX, pickY, pickZ);
				return GameMath.Max(0.3f, pickdistance - 1f);
			}
			return 999f;
		}

		public float Length(float x, float y, float z)
		{
			return GameMath.Sqrt(x * x + y * y + z * z);
		}

		public EnumCollideFlags UpdateCameraMotion(IWorldAccessor world, Vec3d pos, Vec3d motion, float size)
		{
			this.cameraCollBox.Set(pos.X - (double)(size / 2f), pos.Y - (double)(size / 2f), pos.Z - (double)(size / 2f), pos.X + (double)(size / 2f), pos.Y + (double)(size / 2f), pos.Z + (double)(size / 2f));
			motion.X = GameMath.Clamp(motion.X, (double)(-(double)this.MotionCap), (double)this.MotionCap);
			motion.Y = GameMath.Clamp(motion.Y, (double)(-(double)this.MotionCap), (double)this.MotionCap);
			motion.Z = GameMath.Clamp(motion.Z, (double)(-(double)this.MotionCap), (double)this.MotionCap);
			EnumCollideFlags flags = (EnumCollideFlags)0;
			this.minPos.SetAndCorrectDimension((int)(this.cameraCollBox.X1 + Math.Min(0.0, motion.X)), (int)(this.cameraCollBox.Y1 + Math.Min(0.0, motion.Y) - 1.0), (int)(this.cameraCollBox.Z1 + Math.Min(0.0, motion.Z)));
			this.maxPos.SetAndCorrectDimension((int)(this.cameraCollBox.X2 + Math.Max(0.0, motion.X)), (int)(this.cameraCollBox.Y2 + Math.Max(0.0, motion.Y)), (int)(this.cameraCollBox.Z2 + Math.Max(0.0, motion.Z)));
			this.tmpPos.dimension = this.minPos.dimension;
			this.cameraCollBox.Y1 %= 32768.0;
			this.cameraCollBox.Y2 %= 32768.0;
			this.CollisionBoxList.Clear();
			world.BlockAccessor.WalkBlocks(this.minPos, this.maxPos, delegate(Block cblock, int x, int y, int z)
			{
				Cuboidf[] collisionBoxes = cblock.GetCollisionBoxes(world.BlockAccessor, this.tmpPos.Set(x, y, z));
				if (collisionBoxes != null)
				{
					this.CollisionBoxList.Add(collisionBoxes, x, y, z, cblock);
				}
			}, false);
			EnumPushDirection pushDirection = EnumPushDirection.None;
			for (int i = 0; i < this.CollisionBoxList.Count; i++)
			{
				this.blockCollBox = this.CollisionBoxList.cuboids[i];
				motion.Y = (double)((float)this.blockCollBox.pushOutY(this.cameraCollBox, motion.Y, ref pushDirection));
				if (pushDirection != EnumPushDirection.None)
				{
					flags |= EnumCollideFlags.CollideY;
				}
			}
			this.cameraCollBox.Translate(0.0, motion.Y, 0.0);
			for (int j = 0; j < this.CollisionBoxList.Count; j++)
			{
				this.blockCollBox = this.CollisionBoxList.cuboids[j];
				motion.X = (double)((float)this.blockCollBox.pushOutX(this.cameraCollBox, motion.X, ref pushDirection));
				if (pushDirection != EnumPushDirection.None)
				{
					flags |= EnumCollideFlags.CollideX;
				}
			}
			this.cameraCollBox.Translate(motion.X, 0.0, 0.0);
			for (int k = 0; k < this.CollisionBoxList.Count; k++)
			{
				this.blockCollBox = this.CollisionBoxList.cuboids[k];
				motion.Z = (double)((float)this.blockCollBox.pushOutZ(this.cameraCollBox, motion.Z, ref pushDirection));
				if (pushDirection != EnumPushDirection.None)
				{
					flags |= EnumCollideFlags.CollideZ;
				}
			}
			return flags;
		}

		public float ZNear = 0.1f;

		public float ZFar = 3000f;

		public float Fov;

		public Vec3d CameraEyePos = new Vec3d();

		public double[] CameraMatrix;

		public double[] CameraMatrixOrigin;

		public float[] CameraMatrixOriginf = Mat4f.Create();

		public float Tppcameradistance;

		public int TppCameraDistanceMin;

		public int TppCameraDistanceMax;

		internal EnumCameraMode CameraMode;

		private double[] upVec3;

		private Vec3d camEyePosIn = new Vec3d();

		private Vec3d originPos = new Vec3d();

		public double PlayerHeight;

		public Vec3d forwardVec = new Vec3d();

		private Vec3d camTargetTmp = new Vec3d();

		private Vec3d camEyePosOutTmp = new Vec3d();

		public ModelTransform CameraOffset = new ModelTransform();

		private bool cameraStuck;

		private Vec3d to = new Vec3d();

		private Vec3d eyePosAbs = new Vec3d();

		public CachedCuboidList CollisionBoxList = new CachedCuboidList();

		public float MotionCap = 2f;

		private BlockPos minPos = new BlockPos();

		private BlockPos maxPos = new BlockPos();

		private Cuboidd cameraCollBox = new Cuboidd();

		private Cuboidd blockCollBox = new Cuboidd();

		private BlockPos tmpPos = new BlockPos();
	}
}
