using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class SystemRenderPlayerEffects : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "reple";
			}
		}

		public SystemRenderPlayerEffects(ClientMain game)
			: base(game)
		{
			game.eventManager.RegisterRenderer(new Action<float>(this.onBeforeRender), EnumRenderStage.Before, this.Name, 0.1);
			this.maxDynLights = ClientSettings.MaxDynamicLights;
			ClientSettings.Inst.AddWatcher<int>("maxDynamicLights", delegate(int value)
			{
				this.maxDynLights = value;
			});
		}

		public override void OnOwnPlayerDataReceived()
		{
			this.game.api.Render.PerceptionEffects.OnOwnPlayerDataReceived(this.game.player.Entity);
		}

		private void onBeforeRender(float dt)
		{
			this.game.shUniforms.PointLightsCount = 0;
			Vec3d plrPos = this.game.EntityPlayer.Pos.XYZ;
			Entity[] ents = this.game.GetEntitiesAround(plrPos, 60f, 60f, (Entity e) => e.LightHsv != null && e.LightHsv[2] > 0);
			if (ents.Length > this.maxDynLights)
			{
				ents = ents.OrderBy((Entity e) => e.Pos.SquareDistanceTo(plrPos)).ToArray<Entity>();
			}
			foreach (Entity entity in ents)
			{
				byte[] lightHsv = entity.LightHsv;
				this.AddPointLight(lightHsv, entity.Pos);
			}
			for (int j = 0; j < this.game.pointlights.Count; j++)
			{
				IPointLight pl = this.game.pointlights[j];
				this.AddPointLight(pl.Color, pl.Pos);
			}
			this.game.api.Render.PerceptionEffects.OnBeforeGameRender(dt);
		}

		private void AddPointLight(byte[] lighthsv, EntityPos pos)
		{
			int cnt = this.game.shUniforms.PointLightsCount;
			if (cnt >= this.maxDynLights)
			{
				return;
			}
			this.inval.Set(pos.X, pos.InternalY, pos.Z, 1.0);
			Mat4d.MulWithVec4(this.game.CurrentModelViewMatrixd, this.inval, this.outval);
			this.outval.W = (double)lighthsv[2];
			this.game.shUniforms.PointLights3[3 * cnt] = (float)this.outval.X;
			this.game.shUniforms.PointLights3[3 * cnt + 1] = (float)this.outval.Y;
			this.game.shUniforms.PointLights3[3 * cnt + 2] = (float)this.outval.Z;
			int num = (int)this.game.WorldMap.hueLevels[(int)lighthsv[0]];
			int blocks = (int)this.game.WorldMap.satLevels[(int)lighthsv[1]];
			int blockv = (int)(this.game.WorldMap.BlockLightLevels[(int)lighthsv[2]] * 255f);
			ColorUtil.ToRGBVec3f(ColorUtil.HsvToRgba(num, blocks, blockv), ref this.outval3);
			this.game.shUniforms.PointLightColors3[3 * cnt] = this.outval3.Z;
			this.game.shUniforms.PointLightColors3[3 * cnt + 1] = this.outval3.Y;
			this.game.shUniforms.PointLightColors3[3 * cnt + 2] = this.outval3.X;
			this.game.shUniforms.PointLightsCount++;
		}

		private void AddPointLight(Vec3f color, Vec3d pos)
		{
			int cnt = this.game.shUniforms.PointLightsCount;
			if (cnt >= this.maxDynLights)
			{
				return;
			}
			this.inval.Set(pos.X, pos.Y, pos.Z, 1.0);
			Mat4d.MulWithVec4(this.game.CurrentModelViewMatrixd, this.inval, this.outval);
			this.game.shUniforms.PointLights3[3 * cnt] = (float)this.outval.X;
			this.game.shUniforms.PointLights3[3 * cnt + 1] = (float)this.outval.Y;
			this.game.shUniforms.PointLights3[3 * cnt + 2] = (float)this.outval.Z;
			this.game.shUniforms.PointLightColors3[3 * cnt] = color.Z;
			this.game.shUniforms.PointLightColors3[3 * cnt + 1] = color.Y;
			this.game.shUniforms.PointLightColors3[3 * cnt + 2] = color.X;
			this.game.shUniforms.PointLightsCount++;
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Render;
		}

		private Vec4d inval = new Vec4d();

		private Vec4d outval = new Vec4d();

		private Vec3f outval3 = new Vec3f();

		private int maxDynLights;
	}
}
