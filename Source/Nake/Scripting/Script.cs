using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Reflection;
using System.Net.Http;

namespace Nake.Scripting
{
	class Script
	{
		static readonly List<MetadataReference> NakeReferences = new List<MetadataReference>
		{
			Reference(typeof(Program))
		};

		static readonly Dictionary<string, MetadataReference> DefaultReferences = new Dictionary<string, MetadataReference>
		{
			{"mscorlib",                        Reference(typeof(object))},
			{"System",                          Reference(typeof(string))},
			{"System.Net.Http",                      Reference(typeof(HttpClient))},
			{"System.Core",                     Reference(typeof(IQueryable))},
			{"System.Xml",                      Reference(typeof(XmlElement))},
			{"System.Xml.Linq",                 Reference(typeof(XElement))},
			{"Microsoft.CSharp",                Reference(typeof(RuntimeBinderException))}
		};

		static readonly string[] DefaultNamespaces =
		{
			"Nake",
			"System",
			"System.Linq",
			"System.Text",
			"System.IO",
			"System.Collections.Generic",
			"Microsoft.Build.Framework",
			"Microsoft.Build.Utilities"
		};

		static MetadataReference Reference(Type type)
		{
			var location = type.GetTypeInfo().Assembly.Location;

			try { return MetadataReference.CreateFromFile(location); }
			catch (Exception exception) { throw; }
		}

		readonly HashSet<string> namespaces;
		readonly List<MetadataReference> references;

		public Script()
		{
			namespaces = new HashSet<string>(DefaultNamespaces);
			references = new List<MetadataReference>(
				NakeReferences.Concat(DefaultReferences.Select(x => x.Value)));
		}

		public CompiledScript Compile(string code)
		{
			var options = ScriptOptions.Default
				.AddImports(namespaces)
				.AddReferences(references);

			var script = CSharpScript.Create(code, options);
			var compilation = (CSharpCompilation)script.GetCompilation();

			var diagnostics = compilation.GetDiagnostics();
			if (diagnostics.Any(x => x.Severity == DiagnosticSeverity.Error))
				throw new NakeException("Script compilation failure! See diagnostics below." + Environment.NewLine +
										string.Join("\n", diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error)));

			return new CompiledScript(references.Select(x => new AssemblyReference(x)), compilation);
		}

		public void AddReference(AssemblyAbsoluteReference reference)
		{
			if (!File.Exists(reference))
				throw new NakeException(
					"Reference {0} defined in script {1} cannot be found",
					reference.AssemblyPath, reference.ScriptFile);

			AddReference(MetadataReference.CreateFromFile(reference));
		}

		void AddReference(MetadataReference reference)
		{
			if (references.Any(x => x == reference))
				return;

			references.Add(reference);
		}

		public void ImportNamespace(string ns)
		{
			namespaces.Add(ns);
		}
	}

	class CompiledScript
	{
		public readonly IEnumerable<AssemblyReference> References;
		public readonly CSharpCompilation Compilation;

		public CompiledScript(IEnumerable<AssemblyReference> references, CSharpCompilation compilation)
		{
			References = references;
			Compilation = compilation;
		}
	}
}