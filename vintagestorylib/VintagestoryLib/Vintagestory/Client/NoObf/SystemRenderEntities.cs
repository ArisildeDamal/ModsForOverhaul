using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class SystemRenderEntities : ClientSystem
	{
		public SystemRenderEntities(ClientMain game)
			: base(game)
		{
			game.eventManager.RegisterRenderer(new Action<float>(this.OnBeforeRender), EnumRenderStage.Before, this.Name, 0.4);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderOpaque3D), EnumRenderStage.Opaque, this.Name, 0.4);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderOIT), EnumRenderStage.OIT, this.Name, 0.4);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderAfterOIT), EnumRenderStage.AfterOIT, this.Name, 0.7);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrame2D), EnumRenderStage.Ortho, this.Name, 0.4);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrameShadows), EnumRenderStage.ShadowFar, this.Name, 0.4);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrameShadows), EnumRenderStage.ShadowNear, this.Name, 0.4);
		}

		private void OnBeforeRender(float dt)
		{
			int viewDistSq = ClientSettings.ViewDistance * ClientSettings.ViewDistance;
			Vec3d plrPos = this.game.EntityPlayer.Pos.XYZ;
			int plrDim = this.game.EntityPlayer.Pos.Dimension;
			foreach (KeyValuePair<long, EntityRenderer> val in this.game.EntityRenderers)
			{
				Entity entity = val.Value.entity;
				if (this.game.frustumCuller.SphereInFrustum((double)((float)entity.Pos.X), (double)((float)entity.Pos.InternalY), (double)((float)entity.Pos.Z), entity.FrustumSphereRadius) && entity.Pos.Dimension == plrDim && (entity.AllowOutsideLoadedRange || (plrPos.HorizontalSquareDistanceTo(entity.Pos.X, entity.Pos.Z) < (float)viewDistSq && (entity == this.game.EntityPlayer || this.game.WorldMap.IsChunkRendered((int)entity.Pos.X / 32, (int)entity.Pos.InternalY / 32, (int)entity.Pos.Z / 32)))))
				{
					entity.IsRendered = true;
					val.Value.BeforeRender(dt);
				}
				else
				{
					entity.IsRendered = false;
				}
				this.game.api.World.FrameProfiler.Mark("esr-beforeanim");
				try
				{
					IAnimationManager animManager = entity.AnimManager;
					if (animManager != null)
					{
						animManager.OnClientFrame(dt);
					}
				}
				catch (Exception)
				{
					ILogger logger = this.game.Logger;
					string text = "Animations error for entity ";
					string text2 = entity.Code.ToShortString();
					string text3 = " at ";
					BlockPos asBlockPos = entity.ServerPos.AsBlockPos;
					logger.Error(text + text2 + text3 + ((asBlockPos != null) ? asBlockPos.ToString() : null));
					throw;
				}
				this.game.api.World.FrameProfiler.Mark("esr-afteranim");
			}
		}

		public void OnRenderOpaque3D(float deltaTime)
		{
			RuntimeStats.renderedEntities = 0;
			this.game.GlMatrixModeModelView();
			this.game.Platform.GlDisableCullFace();
			this.game.Platform.GlToggleBlend(true, EnumBlendMode.Standard);
			this.game.Platform.GlEnableDepthTest();
			foreach (KeyValuePair<long, EntityRenderer> val in this.game.EntityRenderers)
			{
				if (val.Value.entity.IsRendered)
				{
					val.Value.DoRender3DOpaque(deltaTime, false);
					RuntimeStats.renderedEntities++;
				}
			}
			ScreenManager.FrameProfiler.Mark("ree-op");
			ShaderProgramEntityanimated prog = ShaderPrograms.Entityanimated;
			prog.Use();
			prog.RgbaAmbientIn = this.game.api.renderapi.AmbientColor;
			prog.RgbaFogIn = this.game.api.renderapi.FogColor;
			prog.FogMinIn = this.game.api.renderapi.FogMin;
			prog.FogDensityIn = this.game.api.renderapi.FogDensity;
			prog.ProjectionMatrix = this.game.CurrentProjectionMatrix;
			prog.EntityTex2D = this.game.EntityAtlasManager.AtlasTextures[0].TextureId;
			prog.AlphaTest = 0.05f;
			prog.LightPosition = this.game.shUniforms.LightPosition3D;
			this.game.Platform.GlDisableCullFace();
			this.game.GlMatrixModeModelView();
			this.game.GlPushMatrix();
			this.game.GlLoadMatrix(this.game.MainCamera.CameraMatrixOrigin);
			this.game.Platform.GlToggleBlend(true, EnumBlendMode.Standard);
			foreach (KeyValuePair<long, EntityRenderer> val2 in this.game.EntityRenderers)
			{
				if (val2.Value.entity.IsRendered)
				{
					val2.Value.DoRender3DOpaqueBatched(deltaTime, false);
				}
			}
			this.game.GlPopMatrix();
			prog.Stop();
			ScreenManager.FrameProfiler.Mark("ree-op-b");
			this.game.Platform.GlToggleBlend(false, EnumBlendMode.Standard);
		}

		private void OnRenderOIT(float dt)
		{
		}

		private void OnRenderAfterOIT(float dt)
		{
			this.game.GlMatrixModeModelView();
			this.game.Platform.GlDisableCullFace();
			this.game.Platform.GlToggleBlend(true, EnumBlendMode.Standard);
			this.game.Platform.GlEnableDepthTest();
			foreach (KeyValuePair<long, EntityRenderer> val in this.game.EntityRenderers)
			{
				if (val.Value.entity.IsRendered)
				{
					val.Value.DoRender3DAfterOIT(dt, false);
				}
			}
		}

		private void OnRenderFrameShadows(float dt)
		{
			int plrDim = this.game.EntityPlayer.Pos.Dimension;
			ShaderProgramShadowmapgeneric prog = (ShaderProgramShadowmapgeneric)ShaderProgramBase.CurrentShaderProgram;
			if (this.game.api.Render.UseSSBOs)
			{
				prog.Stop();
				prog = (ShaderProgramShadowmapgeneric)this.game.api.Shader.GetProgram(42);
				prog.Use();
			}
			foreach (KeyValuePair<long, EntityRenderer> val in this.game.EntityRenderers)
			{
				Entity entity = val.Value.entity;
				if (this.game.frustumCuller.SphereInFrustum((double)((float)entity.Pos.X), (double)((float)entity.Pos.InternalY), (double)((float)entity.Pos.Z), 3.0) && (entity == this.game.EntityPlayer || (this.game.WorldMap.IsValidPos((int)entity.Pos.X, (int)entity.Pos.InternalY, (int)entity.Pos.Z) && this.game.WorldMap.IsChunkRendered((int)entity.Pos.X / 32, (int)entity.Pos.InternalY / 32, (int)entity.Pos.Z / 32))) && entity.Pos.Dimension == plrDim)
				{
					entity.IsShadowRendered = true;
					val.Value.DoRender3DOpaque(dt, true);
				}
				else
				{
					entity.IsShadowRendered = false;
				}
			}
			prog.Stop();
			ShaderProgramShadowmapentityanimated proge = ShaderPrograms.Shadowmapentityanimated;
			proge.Use();
			proge.ProjectionMatrix = this.game.CurrentProjectionMatrix;
			proge.EntityTex2D = this.game.EntityAtlasManager.AtlasTextures[0].TextureId;
			IRenderAPI render = this.game.api.Render;
			EntityPlayer entity2 = this.game.api.World.Player.Entity;
			foreach (KeyValuePair<long, EntityRenderer> val2 in this.game.EntityRenderers)
			{
				if (val2.Value.entity.IsShadowRendered)
				{
					val2.Value.DoRender3DOpaqueBatched(dt, true);
				}
			}
			proge.Stop();
			prog.Use();
			prog.MvpMatrix = this.game.shadowMvpMatrix;
		}

		private void OnRenderFrame2D(float dt)
		{
			foreach (KeyValuePair<long, EntityRenderer> val in this.game.EntityRenderers)
			{
				Entity entity = val.Value.entity;
				EntityRenderer renderer = val.Value;
				if (entity.IsRendered)
				{
					renderer.DoRender2D(dt);
				}
			}
			ScreenManager.FrameProfiler.Mark("ree2d-d");
		}

		public override void Dispose(ClientMain game)
		{
			foreach (KeyValuePair<long, EntityRenderer> val in game.EntityRenderers)
			{
				val.Value.Dispose();
			}
		}

		public override string Name
		{
			get
			{
				return "ree";
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Render;
		}
	}
}
