using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Scripting;
using Roslyn.Scripting.CSharp;

namespace Nake.Magic
{
    class Script
    {
        public readonly IEnumerable<Task> Tasks;

        Script(IEnumerable<Task> tasks)
        {
            Tasks = tasks;
        }

        public static Script Build(string code, IDictionary<string, string> substitutions, bool debug)
        {
            return Build(Precompile(code), substitutions, debug);
        }

        public static Script Build(FileInfo file, IDictionary<string, string> substitutions, bool debug)
        {
            return Build(Precompile(file), substitutions, debug);
        }

        static Compilation Precompile(string code)
        {
            return new ScriptSession().Compile(code);
        }

        static Compilation Precompile(FileInfo file)
        {
            return new ScriptSession().Compile(file);
        }

        static Script Build(Compilation compilation, IDictionary<string, string> substitutions, bool debug)
        {
            var magic = new FairyDust(compilation, substitutions, debug);

            return new Script(magic.Apply());
        }

        class ScriptSession
        {
            static readonly string[] DefaultReferences = {"System", "System.Core", "System.Data", "System.Data.DataSetExtensions", "System.Xml", "System.Xml.Linq"};
            static readonly string[] DefaultUsings = {"Nake", "System", "System.Collections.Generic", "System.Linq", "System.Text", "System.IO"};

            static readonly List<string> NakeReferences = new List<string>
            {
                Assembly.GetExecutingAssembly().Location,
                typeof(TaskAttribute).Assembly.Location,
                typeof(Env).Assembly.Location
            };

            readonly Session session;

            public ScriptSession()
            {
                var engine = new ScriptEngine();

                foreach (var reference in NakeReferences)
                    engine.AddReference(reference);

                foreach (var reference in DefaultReferences)
                    engine.AddReference(reference);

                foreach (var @namespace in DefaultUsings)
                    engine.ImportNamespace(@namespace);

                session = engine.CreateSession();
            }

            public Compilation Compile(FileInfo file)
            {
                return Compile(Preprocess(file));
            }

            public Compilation Compile(string code)
            {
                var submission = session.CompileSubmission<object>(code);
                return (Compilation)submission.Compilation;
            }

            string Preprocess(FileInfo file)
            {
                var result = new Preprocessor()
                    .Process(file);

                var references = result.References.Except(DefaultReferences);
                var namespaces = result.Usings.Except(DefaultUsings);

                foreach (var reference in references)
                    session.AddReference(reference);

                foreach (var @namespace in namespaces)
                    session.ImportNamespace(@namespace);

                foreach (var absoluteReference in result.AbsoluteReferences)
                {
                    if (!File.Exists(absoluteReference))
                        throw new NakeException("Reference {0} defined in script {1} cannot be found", 
                            absoluteReference.AssemblyPath, absoluteReference.ScriptFile);

                    if (NakeReferences.Contains(absoluteReference))
                        continue;

                    session.AddReference(new MetadataFileReference(absoluteReference));
                }

                return result.Code();
            }
        }
    }
}
