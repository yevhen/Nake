using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

using Nake.Magic;
using Nake.Scripting;
using System;
using System.Runtime.Loader;

namespace Nake
{
	class Engine
	{
		readonly IEnumerable<AssemblyNameReference> assemblyNameReferences;
		readonly IEnumerable<AssemblyAbsoluteReference> assemblyAbsoluteReferences;
		readonly IEnumerable<string> namespaces;

		public Engine(
			IEnumerable<AssemblyNameReference> assemblyNameReferences = null,
			IEnumerable<AssemblyAbsoluteReference> assemblyAbsoluteReferences = null,
			IEnumerable<string> namespaces = null)
		{
			this.assemblyNameReferences = assemblyNameReferences ?? Enumerable.Empty<AssemblyNameReference>();
			this.assemblyAbsoluteReferences = assemblyAbsoluteReferences ?? Enumerable.Empty<AssemblyAbsoluteReference>();
			this.namespaces = namespaces ?? Enumerable.Empty<string>();
		}

		public BuildResult Build(string code, IDictionary<string, string> substitutions, bool debug)
		{
			var magic = new PixieDust(Compile(code));
			return magic.Apply(substitutions, debug);
		}

		CompiledScript Compile(string code)
		{
			var script = new Script();

			foreach (var reference in assemblyNameReferences)
				script.AddReference(reference);

			foreach (var reference in assemblyAbsoluteReferences)
				script.AddReference(reference);

			foreach (var @namespace in namespaces)
				script.ImportNamespace(@namespace);

			return script.Compile(code);
		}
	}

	class PixieDust
	{
		readonly CompiledScript script;

		public PixieDust(CompiledScript script)
		{
			this.script = script;
		}

		public BuildResult Apply(IDictionary<string, string> substitutions, bool debug)
		{
			var analyzer = new Analyzer(script.Compilation, substitutions);
			var analyzed = analyzer.Analyze();

			var rewriter = new Rewriter(script.Compilation, analyzed);
			var rewritten = rewriter.Rewrite();

			ArraySegment<byte> assembly;
			ArraySegment<byte> symbols;

			if (debug)
				EmitDebug(rewritten, out assembly, out symbols);
			else
				Emit(rewritten, out assembly);

			return new BuildResult(
				analyzed.Tasks.ToArray(),
				script.References.ToArray(),
				rewriter.Captured.ToArray(),
				assembly, symbols
			);
		}

		static void Emit(Compilation compilation, out ArraySegment<byte> assembly)
		{
			using (var assemblyStream = new MemoryStream())
			{
				Check(compilation.Emit(assemblyStream));
				assemblyStream.TryGetBuffer(out assembly);
			}
		}

		static void EmitDebug(Compilation compilation, out ArraySegment<byte> assembly, out ArraySegment<byte> symbols)
		{
			using (var assemblyStream = new MemoryStream())
			using (var symbolStream = new MemoryStream())
			{
				Check(compilation.Emit(assemblyStream, pdbStream: symbolStream));

				assemblyStream.TryGetBuffer(out assembly);
				symbolStream.TryGetBuffer(out symbols);
			}
		}

		static void Check(EmitResult result)
		{
			if (result.Success)
				return;

			var errors = result.Diagnostics
				.Where(x => x.Severity == DiagnosticSeverity.Error)
				.ToArray();

			if (errors.Any())
				throw new NakeException("Compilation failed!\r\n\r\n" +
					string.Join("\r\n", errors.Select(x => x.ToString())));
		}
	}

	class BuildResult
	{
		public readonly Task[] Tasks;
		public readonly AssemblyReference[] References;
		public readonly EnvironmentVariable[] Variables;
		public readonly Assembly Assembly;
		public readonly ArraySegment<byte> AssemblyBytes;
		public readonly ArraySegment<byte> SymbolBytes;

		public BuildResult(
			Task[] tasks,
			AssemblyReference[] references,
			EnvironmentVariable[] variables,
			ArraySegment<byte> assembly,
			ArraySegment<byte> symbols)
		{
			Tasks = tasks;
			References = references;
			AssemblyBytes = assembly;
			SymbolBytes = symbols;
			Variables = variables;
			Assembly = Load();
			Reflect();
		}

		Assembly Load()
		{
			AssemblyResolver.Register();

			foreach (var reference in References)
				AssemblyResolver.Add(reference);

			return SymbolBytes != null
					   ? AssemblyLoadContext.Default.LoadFromStream(
						   new MemoryStream(AssemblyBytes.ToArray(), false),
						   new MemoryStream(SymbolBytes.ToArray(), false))
					   : AssemblyLoadContext.Default.LoadFromStream(
						   new MemoryStream(AssemblyBytes.ToArray(), false));
		}

		void Reflect()
		{
			foreach (var task in Tasks)
				task.Reflect(Assembly);
		}
	}
}