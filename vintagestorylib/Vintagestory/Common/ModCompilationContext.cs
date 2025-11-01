using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.Common
{
	public class ModCompilationContext
	{
		public ModCompilationContext()
		{
			this.references = new string[]
			{
				"System.dll",
				"System.Core.dll",
				"System.Data.dll",
				"System.Runtime.dll",
				"System.Private.CoreLib.dll",
				"SkiaSharp.dll",
				"System.Xml.dll",
				"System.Xml.Linq.dll",
				"System.Net.Http.dll",
				"VintagestoryAPI.dll",
				"Newtonsoft.Json.dll",
				"protobuf-net.dll",
				"Tavis.JsonPatch.dll",
				"cairo-sharp.dll",
				Path.Combine("Mods", "VSCreativeMod.dll"),
				Path.Combine("Mods", "VSEssentials.dll"),
				Path.Combine("Mods", "VSSurvivalMod.dll")
			};
			string assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
			if (assemblyPath == null)
			{
				throw new Exception("Could not find core/system assembly path for mod compilation.");
			}
			for (int i = 0; i < this.references.Length; i++)
			{
				if (File.Exists(Path.Combine(GamePaths.Binaries, this.references[i])))
				{
					this.references[i] = Path.Combine(GamePaths.Binaries, this.references[i]);
				}
				else if (File.Exists(Path.Combine(GamePaths.Binaries, "Lib", this.references[i])))
				{
					this.references[i] = Path.Combine(GamePaths.Binaries, "Lib", this.references[i]);
				}
				else
				{
					if (!File.Exists(Path.Combine(assemblyPath, this.references[i])))
					{
						throw new Exception("Referenced library not found: " + this.references[i]);
					}
					this.references[i] = Path.Combine(assemblyPath, this.references[i]);
				}
			}
		}

		public Assembly CompileFromFiles(ModContainer mod)
		{
			List<PortableExecutableReference> refsMetadata = (from sourceFile in mod.SourceFiles
				where sourceFile.EndsWithOrdinal(".dll")
				select MetadataReference.CreateFromFile(sourceFile, default(MetadataReferenceProperties), null)).ToList<PortableExecutableReference>();
			refsMetadata.AddRange(this.references.Select((string dlls) => MetadataReference.CreateFromFile(dlls, default(MetadataReferenceProperties), null)));
			IEnumerable<SyntaxTree> syntaxTrees = mod.SourceFiles.Select((string file) => CSharpSyntaxTree.ParseText(File.ReadAllText(file), null, "", null, default(CancellationToken)));
			CSharpCompilation compilation = CSharpCompilation.Create(mod.FileName + Guid.NewGuid().ToString(), syntaxTrees, refsMetadata, new CSharpCompilationOptions(2, false, null, null, null, null, 0, false, false, null, null, default(ImmutableArray<byte>), null, 0, 0, 4, null, true, false, null, null, null, null, null, false, 0, 0));
			Assembly assembly;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				EmitResult result = compilation.Emit(memoryStream, null, null, null, null, null, null, null, null, null, default(CancellationToken));
				if (!result.Success)
				{
					foreach (Diagnostic error in result.Diagnostics.Where((Diagnostic d) => d.IsWarningAsError || d.Severity == 3))
					{
						mod.Logger.Error("{0}: {1}", new object[]
						{
							error.Id,
							error.GetMessage(null)
						});
					}
					assembly = null;
				}
				else
				{
					memoryStream.Seek(0L, SeekOrigin.Begin);
					mod.Logger.Debug("Successfully compiled mod with Roslyn");
					assembly = Assembly.Load(memoryStream.ToArray());
				}
			}
			return assembly;
		}

		private readonly string[] references;
	}
}
