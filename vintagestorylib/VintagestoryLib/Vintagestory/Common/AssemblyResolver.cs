using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Vintagestory.API.Config;

namespace Vintagestory.Common
{
	public static class AssemblyResolver
	{
		public static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
		{
			string dllName = new AssemblyName(args.Name).Name + ".dll";
			string assemblyPath = null;
			Assembly assembly;
			try
			{
				string[] assemblySearchPaths = AssemblyResolver.AssemblySearchPaths;
				for (int i = 0; i < assemblySearchPaths.Length; i++)
				{
					assemblyPath = Path.Combine(assemblySearchPaths[i], dllName);
					if (File.Exists(assemblyPath))
					{
						return Assembly.LoadFrom(assemblyPath);
					}
				}
				assembly = null;
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Failed to load assembly '");
				defaultInterpolatedStringHandler.AppendFormatted(args.Name);
				defaultInterpolatedStringHandler.AppendLiteral("' from '");
				defaultInterpolatedStringHandler.AppendFormatted(assemblyPath);
				defaultInterpolatedStringHandler.AppendLiteral("'");
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear(), ex);
			}
			return assembly;
		}

		private static readonly string[] AssemblySearchPaths = new string[]
		{
			GamePaths.Binaries,
			Path.Combine(GamePaths.Binaries, "Lib"),
			GamePaths.BinariesMods,
			GamePaths.DataPathMods
		};
	}
}
