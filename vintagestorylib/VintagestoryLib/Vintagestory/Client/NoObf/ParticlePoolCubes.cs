using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ParticlePoolCubes : ParticlePoolQuads
	{
		public ParticlePoolCubes(int capacity, ClientMain game, bool offthread)
			: base(capacity, game, offthread)
		{
			this.ModelType = EnumParticleModel.Cube;
		}

		internal override float ParticleHeight
		{
			get
			{
				return 0.0625f;
			}
		}

		public override MeshData LoadModel()
		{
			float num = 0.03125f;
			MeshData modeldata = CubeMeshUtil.GetCubeOnlyScaleXyz(num, num, new Vec3f());
			modeldata.WithNormals();
			modeldata.Rgba = null;
			for (int i = 0; i < 24; i++)
			{
				BlockFacing face = BlockFacing.ALLFACES[i / 4];
				modeldata.AddNormal(face);
			}
			return modeldata;
		}

		internal override void UpdateDebugScreen()
		{
			if (this.game.extendedDebugInfo)
			{
				this.game.DebugScreenInfo["cubeparticlepool"] = "Cube Particle pool: " + this.ParticlesPool.AliveCount.ToString() + " / " + ((int)((float)this.poolSize * (float)this.game.particleLevel / 100f)).ToString();
				return;
			}
			this.game.DebugScreenInfo["cubeparticlepool"] = "";
		}
	}
}
