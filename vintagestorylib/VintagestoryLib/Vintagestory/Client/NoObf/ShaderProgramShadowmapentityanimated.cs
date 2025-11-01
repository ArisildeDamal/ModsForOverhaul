using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramShadowmapentityanimated : ShaderProgram
	{
		public int EntityTex2D
		{
			set
			{
				base.BindTexture2D("entityTex", value, 0);
			}
		}

		public float[] ProjectionMatrix
		{
			set
			{
				base.UniformMatrix("projectionMatrix", value);
			}
		}

		public float[] ModelViewMatrix
		{
			set
			{
				base.UniformMatrix("modelViewMatrix", value);
			}
		}

		public int AddRenderFlags
		{
			set
			{
				base.Uniform("addRenderFlags", value);
			}
		}

		public override bool Compile()
		{
			bool flag = base.Compile();
			if (flag)
			{
				this.initUbos();
			}
			return flag;
		}

		public void initUbos()
		{
			foreach (UBORef uboref in this.ubos.Values)
			{
				uboref.Dispose();
			}
			this.ubos.Clear();
			this.ubos["Animation"] = ScreenManager.Platform.CreateUBO(this.ProgramId, 0, "Animation", GlobalConstants.MaxAnimatedElements * 16 * 4);
		}
	}
}
