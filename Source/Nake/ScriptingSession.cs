using System;
using System.IO;
using System.Linq;

using Roslyn.Compilers;
using Roslyn.Scripting;
using Roslyn.Scripting.CSharp;

namespace Nake
{
	class ScriptingSession
	{
		static readonly string[] DefaultReferences = new[] { "System", "System.Core", "System.Xml", "System.Xml.Linq" };
		static readonly string[] DefaultNamespaces = new[] { "Nake", "System", "System.Collections.Generic", "System.Linq", "System.Text", "System.IO" };

		readonly Session session;

		public ScriptingSession(Project host)
		{
			var engine = new ScriptEngine();
			engine.AddReference(typeof(Program).Assembly);

			foreach (var reference in DefaultReferences)
				engine.AddReference(reference);

			foreach (var @namespace in DefaultNamespaces)
				engine.ImportNamespace(@namespace);

			session = engine.CreateSession(host);
		}

		public void Load(string file)
		{
			var preprocessed = new ScriptPreProcessor()
					.ProcessFile(file);

			var references = preprocessed.References.Except(DefaultReferences);
			var namespaces = preprocessed.Usings.Except(DefaultNamespaces);

			foreach (var reference in references)
				session.AddReference(reference);

			foreach (var @namespace in namespaces)
				session.ImportNamespace(@namespace);

			foreach (var absoluteReference in preprocessed.AbsoluteReferences)
			{
				if (!File.Exists(absoluteReference))
					throw new NakeException("Reference {0} defined in script {1} cannot be found");

				session.AddReference(new MetadataFileReference(absoluteReference));
			}

			Execute(preprocessed.Code());
		}

		public void Execute(string code)
		{
			session.Execute(code);
		}
	}
}
