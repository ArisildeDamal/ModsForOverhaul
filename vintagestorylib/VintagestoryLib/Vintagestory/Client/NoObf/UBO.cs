using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf
{
	public class UBO : UBORef
	{
		public override void Bind()
		{
			GL.BindBuffer(BufferTarget.UniformBuffer, this.Handle);
			GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, this.Handle);
		}

		public override void Dispose()
		{
			base.Dispose();
			GL.DeleteBuffers(1, ref this.Handle);
		}

		public override void Unbind()
		{
			GL.BindBuffer(BufferTarget.UniformBuffer, 0);
		}

		public override void Update<T>(T data)
		{
			if (Unsafe.SizeOf<T>() != base.Size)
			{
				throw new ArgumentException("Supplied struct must be of byte size " + base.Size.ToString() + " but has size " + Unsafe.SizeOf<T>().ToString());
			}
			this.Bind();
			using (GCHandleProvider handleProvider = new GCHandleProvider(data))
			{
				GL.BufferData(BufferTarget.UniformBuffer, base.Size, handleProvider.Pointer, BufferUsageHint.DynamicDraw);
			}
			this.Unbind();
		}

		public override void Update<T>(T data, int offset, int size)
		{
			if (Unsafe.SizeOf<T>() != base.Size)
			{
				throw new ArgumentException("Supplied struct must be of byte size " + base.Size.ToString() + " but has size " + Unsafe.SizeOf<T>().ToString());
			}
			this.Bind();
			using (GCHandleProvider handleProvider = new GCHandleProvider(data))
			{
				GL.BufferSubData(BufferTarget.UniformBuffer, (IntPtr)offset, size, handleProvider.Pointer);
			}
			this.Unbind();
		}

		public override void Update(object data, int offset, int size)
		{
			this.Bind();
			GCHandle pinned = GCHandle.Alloc(data, GCHandleType.Pinned);
			IntPtr ptr = pinned.AddrOfPinnedObject();
			GL.BufferSubData(BufferTarget.UniformBuffer, (IntPtr)offset, size, ptr);
			pinned.Free();
			this.Unbind();
		}
	}
}
