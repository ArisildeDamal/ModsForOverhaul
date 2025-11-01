using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Vintagestory.API.Config;

namespace Vintagestory.Common.Convert
{
	public static class ZstdNative
	{
		static ZstdNative()
		{
			NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), new DllImportResolver(ZstdNative.DllImportResolver));
		}

		private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
		{
			string text;
			switch (RuntimeEnv.OS)
			{
			case OS.Windows:
				text = ".dll";
				break;
			case OS.Mac:
				text = ".dylib";
				break;
			case OS.Linux:
				text = ".so";
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
			string suffix = text;
			IntPtr handle;
			if (RuntimeEnv.OS == OS.Linux)
			{
				if (NativeLibrary.TryLoad(libraryName + suffix + ".1", assembly, searchPath, out handle))
				{
					return handle;
				}
				if (NativeLibrary.TryLoad(libraryName, assembly, searchPath, out handle))
				{
					return handle;
				}
			}
			if (!NativeLibrary.TryLoad(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib", libraryName + suffix), assembly, searchPath, out handle))
			{
				return IntPtr.Zero;
			}
			return handle;
		}

		[DllImport("libzstd", CallingConvention = 2, ExactSpelling = true)]
		public unsafe static extern ZstdNative.ZstdCCtx* ZSTD_createCCtx();

		[DllImport("libzstd", CallingConvention = 2, ExactSpelling = true)]
		public unsafe static extern UIntPtr ZSTD_CCtx_setParameter(ZstdNative.ZstdCCtx* cctx, ZstdNative.ZstdCParameter param, int value);

		[DllImport("libzstd", CallingConvention = 2, ExactSpelling = true)]
		[return: ZstdNative.NativeTypeNameAttribute("size_t")]
		public static extern UIntPtr ZSTD_compressBound([ZstdNative.NativeTypeNameAttribute("size_t")] UIntPtr srcSize);

		[DllImport("libzstd", CallingConvention = 2, ExactSpelling = true)]
		[return: ZstdNative.NativeTypeNameAttribute("unsigned long long")]
		public unsafe static extern ulong ZSTD_getFrameContentSize([ZstdNative.NativeTypeNameAttribute("const void *")] void* src, [ZstdNative.NativeTypeNameAttribute("size_t")] UIntPtr srcSize);

		[DllImport("libzstd", CallingConvention = 2, ExactSpelling = true)]
		[return: ZstdNative.NativeTypeNameAttribute("size_t")]
		public unsafe static extern UIntPtr ZSTD_compressCCtx(ZstdNative.ZstdCCtx* cctx, void* dst, [ZstdNative.NativeTypeNameAttribute("size_t")] UIntPtr dstCapacity, [ZstdNative.NativeTypeNameAttribute("const void *")] void* src, [ZstdNative.NativeTypeNameAttribute("size_t")] UIntPtr srcSize, int compressionLevel);

		[DllImport("libzstd", CallingConvention = 2, ExactSpelling = true)]
		public unsafe static extern ZstdNative.ZstdDCtx* ZSTD_createDCtx();

		[DllImport("libzstd", CallingConvention = 2, ExactSpelling = true)]
		[return: ZstdNative.NativeTypeNameAttribute("size_t")]
		public unsafe static extern UIntPtr ZSTD_decompressDCtx(ZstdNative.ZstdDCtx* dctx, void* dst, [ZstdNative.NativeTypeNameAttribute("size_t")] UIntPtr dstCapacity, [ZstdNative.NativeTypeNameAttribute("const void *")] void* src, [ZstdNative.NativeTypeNameAttribute("size_t")] UIntPtr srcSize);

		[DllImport("libzstd", CallingConvention = 2, ExactSpelling = true)]
		public static extern uint ZSTD_versionNumber();

		public static Version Version
		{
			get
			{
				int version = (int)ZstdNative.ZSTD_versionNumber();
				return new Version(version / 10000 % 100, version / 100 % 100, version % 100);
			}
		}

		private const string DllName = "libzstd";

		internal sealed class NativeTypeNameAttribute : Attribute
		{
			public NativeTypeNameAttribute(string name)
			{
				this.Name = name;
			}

			public string Name { get; }
		}

		public struct ZstdCCtx
		{
		}

		public struct ZstdDCtx
		{
		}

		public enum ZstdCParameter
		{
			ZSTD_c_compressionLevel = 100,
			ZSTD_c_windowLog,
			ZSTD_c_hashLog,
			ZSTD_c_chainLog,
			ZSTD_c_searchLog,
			ZSTD_c_minMatch,
			ZSTD_c_targetLength,
			ZSTD_c_strategy,
			ZSTD_c_enableLongDistanceMatching = 160,
			ZSTD_c_ldmHashLog,
			ZSTD_c_ldmMinMatch,
			ZSTD_c_ldmBucketSizeLog,
			ZSTD_c_ldmHashRateLog,
			ZSTD_c_contentSizeFlag = 200,
			ZSTD_c_checksumFlag,
			ZSTD_c_dictIDFlag,
			ZSTD_c_nbWorkers = 400,
			ZSTD_c_jobSize,
			ZSTD_c_overlapLog
		}
	}
}
