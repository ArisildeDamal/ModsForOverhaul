using System;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf
{
	public class VAO : MeshRef
	{
		public override bool Initialized
		{
			get
			{
				return this.VaoId != 0;
			}
		}

		public VAO()
		{
			if (RuntimeEnv.DebugVAODispose)
			{
				this.trace = Environment.StackTrace;
			}
		}

		public override void Dispose()
		{
			if (base.Disposed)
			{
				return;
			}
			if (this.xyzVboId != 0)
			{
				GL.DeleteBuffer(this.xyzVboId);
			}
			if (this.normalsVboId != 0)
			{
				GL.DeleteBuffer(this.normalsVboId);
			}
			if (this.uvVboId != 0)
			{
				GL.DeleteBuffer(this.uvVboId);
			}
			if (this.rgbaVboId != 0)
			{
				GL.DeleteBuffer(this.rgbaVboId);
			}
			if (this.customDataFloatVboId != 0)
			{
				GL.DeleteBuffer(this.customDataFloatVboId);
			}
			if (this.customDataShortVboId != 0)
			{
				GL.DeleteBuffer(this.customDataShortVboId);
			}
			if (this.customDataIntVboId != 0)
			{
				GL.DeleteBuffer(this.customDataIntVboId);
			}
			if (this.customDataByteVboId != 0)
			{
				GL.DeleteBuffer(this.customDataByteVboId);
			}
			if (this.vboIdIndex != 0 && this.vboIdIndex != ClientPlatformAbstract.singleIndexBufferId)
			{
				GL.DeleteBuffer(this.vboIdIndex);
			}
			if (this.flagsVboId != 0)
			{
				GL.DeleteBuffer(this.flagsVboId);
			}
			GL.DeleteVertexArray(this.VaoId);
			base.Dispose();
		}

		~VAO()
		{
			if (!base.Disposed && !ScreenManager.Platform.IsShuttingDown)
			{
				if (!RuntimeEnv.DebugVAODispose)
				{
					ScreenManager.Platform.Logger.Debug("MeshRef with vao id {0} with {1} indices is leaking memory, missing call to Dispose. Set env var VAO_DEBUG_DISPOSE to get allocation trace.", new object[] { this.VaoId, this.IndicesCount });
				}
				else
				{
					ScreenManager.Platform.Logger.Debug("MeshRef with vao id {0} with {1} indices is leaking memory, missing call to Dispose. Allocated at {2}.", new object[] { this.VaoId, this.IndicesCount, this.trace });
				}
			}
		}

		public int VaoId;

		public int IndicesCount;

		public PrimitiveType drawMode = PrimitiveType.Triangles;

		public int vaoSlotNumber;

		public int vboIdIndex;

		public int xyzVboId;

		public int normalsVboId;

		public int uvVboId;

		public int rgbaVboId;

		public int flagsVboId;

		public int customDataFloatVboId;

		public int customDataIntVboId;

		public int customDataShortVboId;

		public int customDataByteVboId;

		public bool Persistent;

		public IntPtr xyzPtr;

		public IntPtr normalsPtr;

		public IntPtr uvPtr;

		public IntPtr rgbaPtr;

		public IntPtr flagsPtr;

		public IntPtr customDataFloatPtr;

		public IntPtr customDataIntPtr;

		public IntPtr customDataShortPtr;

		public IntPtr customDataBytePtr;

		public IntPtr indicesPtr;

		private string trace;
	}
}
