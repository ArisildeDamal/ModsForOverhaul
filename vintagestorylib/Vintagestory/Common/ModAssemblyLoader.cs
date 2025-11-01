using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace Vintagestory.Common
{
	public class ModAssemblyLoader : IDisposable
	{
		public ModAssemblyLoader(IReadOnlyCollection<string> modSearchPaths, IReadOnlyCollection<ModContainer> mods)
		{
			this.modSearchPaths = modSearchPaths;
			this.mods = mods;
			AppDomain.CurrentDomain.AssemblyResolve += this.AssemblyResolveHandler;
		}

		public void Dispose()
		{
			AppDomain.CurrentDomain.AssemblyResolve -= this.AssemblyResolveHandler;
		}

		public Assembly LoadFrom(string path)
		{
			return Assembly.UnsafeLoadFrom(path);
		}

		public AssemblyDefinition LoadAssemblyDefinition(string path)
		{
			return AssemblyDefinition.ReadAssembly(path);
		}

		private IEnumerable<string> GetAssemblySearchPaths()
		{
			ModAssemblyLoader.<GetAssemblySearchPaths>d__6 <GetAssemblySearchPaths>d__ = new ModAssemblyLoader.<GetAssemblySearchPaths>d__6(-2);
			<GetAssemblySearchPaths>d__.<>4__this = this;
			return <GetAssemblySearchPaths>d__;
		}

		private Assembly AssemblyResolveHandler(object sender, ResolveEventArgs args)
		{
			return (from searchPath in this.GetAssemblySearchPaths()
				select Path.Combine(searchPath, args.Name + ".dll") into assemblyPath
				where File.Exists(assemblyPath)
				select this.LoadFrom(assemblyPath)).FirstOrDefault<Assembly>();
		}

		private readonly IReadOnlyCollection<string> modSearchPaths;

		private readonly IReadOnlyCollection<ModContainer> mods;
	}
}
