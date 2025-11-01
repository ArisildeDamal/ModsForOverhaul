using System;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class SystemRenderInsideBlock : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "rib";
			}
		}

		public SystemRenderInsideBlock(ClientMain game)
			: base(game)
		{
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrame3D), EnumRenderStage.Opaque, this.Name, 0.45);
			this.testPositions = new Vec3d[]
			{
				new Vec3d(0.0, 0.0, 0.0),
				new Vec3d(0.0, 1.0, 0.0),
				new Vec3d(0.0, 0.0, -1.0),
				new Vec3d(1.0, 0.0, -1.0),
				new Vec3d(1.0, 0.0, 0.0),
				new Vec3d(1.0, 0.0, 1.0),
				new Vec3d(0.0, 0.0, 1.0),
				new Vec3d(-1.0, 0.0, 1.0),
				new Vec3d(-1.0, 0.0, 0.0),
				new Vec3d(-1.0, 0.0, -1.0),
				new Vec3d(0.0, -1.0, -1.0),
				new Vec3d(1.0, -1.0, -1.0),
				new Vec3d(1.0, -1.0, 0.0),
				new Vec3d(1.0, -1.0, 1.0),
				new Vec3d(0.0, -1.0, 1.0),
				new Vec3d(-1.0, -1.0, 1.0),
				new Vec3d(-1.0, -1.0, 0.0),
				new Vec3d(-1.0, -1.0, -1.0)
			};
			this.insideBlocks = new Block[this.testPositions.Length];
			this.meshRefs = new MeshRef[this.testPositions.Length];
		}

		internal override void OnLevelFinalize()
		{
			base.OnLevelFinalize();
			this.extChunkSize = 34;
			this.lightExt = new int[this.extChunkSize * this.extChunkSize * this.extChunkSize];
			this.blockExt = new Block[this.extChunkSize * this.extChunkSize * this.extChunkSize];
		}

		private void OnRenderFrame3D(float dt)
		{
			EntityPlayer entity = this.game.EntityPlayer;
			if (entity == null)
			{
				return;
			}
			if (this.game.player.worlddata.CurrentGameMode == EnumGameMode.Creative || this.game.player.worlddata.CurrentGameMode == EnumGameMode.Spectator)
			{
				return;
			}
			Vec3d camPos = this.game.api.World.Player.Entity.CameraPos.Clone().Add(this.game.api.World.Player.Entity.LocalEyePos);
			this.game.MainCamera.ZNear = GameMath.Clamp(0.1f - (float)ClientSettings.FieldOfView / 90f / 25f, 0.025f, 0.1f);
			for (int i = 0; i < this.testPositions.Length; i++)
			{
				Vec3d vec3d = this.testPositions[i];
				double offx = vec3d.X * (double)this.game.MainCamera.ZNear * 1.5;
				double offy = vec3d.Y * (double)this.game.MainCamera.ZNear * 1.5;
				double offz = vec3d.Z * (double)this.game.MainCamera.ZNear * 1.5;
				this.tmpPos.Set((int)(camPos.X + offx), (int)(camPos.Y + offy), (int)(camPos.Z + offz));
				Block block = this.game.BlockAccessor.GetBlock(this.tmpPos);
				if (block != null && (block.SideOpaque[0] || block.SideOpaque[1] || block.SideOpaque[2] || block.SideOpaque[3] || block.SideOpaque[4] || block.SideOpaque[5]))
				{
					if (block != this.insideBlocks[i])
					{
						MeshRef meshRef = this.meshRefs[i];
						if (meshRef != null)
						{
							meshRef.Dispose();
						}
						MeshData mesh = this.game.api.TesselatorManager.GetDefaultBlockMesh(block);
						int lx = this.tmpPos.X % 32;
						int num = this.tmpPos.X % 32;
						int lz = this.tmpPos.X % 32;
						int extIndex3d = ((num + 1) * this.extChunkSize + (lz + 1)) * this.extChunkSize + (lx + 1);
						this.blockExt[extIndex3d] = block;
						this.blockExt[extIndex3d + TileSideEnum.MoveIndex[5]] = this.game.BlockAccessor.GetBlock(this.tmpPos.X, this.tmpPos.Y - 1, this.tmpPos.Z);
						this.blockExt[extIndex3d + TileSideEnum.MoveIndex[4]] = this.game.BlockAccessor.GetBlock(this.tmpPos.X, this.tmpPos.Y + 1, this.tmpPos.Z);
						this.blockExt[extIndex3d + TileSideEnum.MoveIndex[0]] = this.game.BlockAccessor.GetBlock(this.tmpPos.X, this.tmpPos.Y, this.tmpPos.Z - 1);
						this.blockExt[extIndex3d + TileSideEnum.MoveIndex[1]] = this.game.BlockAccessor.GetBlock(this.tmpPos.X + 1, this.tmpPos.Y, this.tmpPos.Z);
						this.blockExt[extIndex3d + TileSideEnum.MoveIndex[2]] = this.game.BlockAccessor.GetBlock(this.tmpPos.X, this.tmpPos.Y, this.tmpPos.Z + 1);
						this.blockExt[extIndex3d + TileSideEnum.MoveIndex[3]] = this.game.BlockAccessor.GetBlock(this.tmpPos.X - 1, this.tmpPos.Y - 1, this.tmpPos.Z);
						block.OnJsonTesselation(ref mesh, ref this.lightExt, this.tmpPos, this.blockExt, extIndex3d);
						this.meshRefs[i] = this.game.api.Render.UploadMesh(mesh);
						this.insideBlocks[i] = block;
						int textureSubId = block.FirstTextureInventory.Baked.TextureSubId;
						this.atlasTextureId = this.game.api.BlockTextureAtlas.Positions[textureSubId].atlasTextureId;
					}
					IRenderAPI rapi = this.game.api.Render;
					rapi.GlDisableCullFace();
					rapi.GlToggleBlend(true, EnumBlendMode.Standard);
					IStandardShaderProgram standardShaderProgram = rapi.PreparedStandardShader((int)entity.Pos.X, (int)entity.Pos.Y, (int)entity.Pos.Z, null);
					standardShaderProgram.Tex2D = this.atlasTextureId;
					standardShaderProgram.SsaoAttn = 1f;
					standardShaderProgram.ModelMatrix = this.ModelMat.Identity().Translate((double)((int)(camPos.X + offx)) - camPos.X + entity.LocalEyePos.X, (double)((int)(camPos.Y + offy)) - camPos.Y + entity.LocalEyePos.Y, (double)((int)(camPos.Z + offz)) - camPos.Z + entity.LocalEyePos.Z).Scale(0.999f, 0.999f, 0.999f)
						.Values;
					standardShaderProgram.ExtraZOffset = -0.0001f;
					standardShaderProgram.ViewMatrix = rapi.CameraMatrixOriginf;
					standardShaderProgram.ProjectionMatrix = rapi.CurrentProjectionMatrix;
					rapi.RenderMesh(this.meshRefs[i]);
					standardShaderProgram.SsaoAttn = 0f;
					standardShaderProgram.Stop();
				}
			}
		}

		public override void Dispose(ClientMain game)
		{
			for (int i = 0; i < this.meshRefs.Length; i++)
			{
				MeshRef meshRef = this.meshRefs[i];
				if (meshRef != null)
				{
					meshRef.Dispose();
				}
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Render;
		}

		protected Block[] insideBlocks;

		protected MeshRef[] meshRefs;

		protected int atlasTextureId;

		protected Matrixf ModelMat = new Matrixf();

		protected Vec3d[] testPositions;

		public int[] lightExt;

		public Block[] blockExt;

		private int extChunkSize;

		private BlockPos tmpPos = new BlockPos();
	}
}
