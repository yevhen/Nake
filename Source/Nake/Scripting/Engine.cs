using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CSharp.RuntimeBinder;

namespace Nake.Scripting
{
    class Engine
    {
        static readonly List<MetadataFileReference> NakeReferences = new List<MetadataFileReference>
        {
            Reference(typeof(Script)),
            Reference(typeof(TaskAttribute)),
            Reference(typeof(Env))
        };

        static readonly Dictionary<string, MetadataFileReference> DefaultReferences = new Dictionary<string, MetadataFileReference>
        {
            {"mscorlib",                        Reference(typeof(object))},
            {"System",                          Reference(typeof(Component))},
            {"System.Core",                     Reference(typeof(IQueryable))},
            {"System.Data",                     Reference(typeof(DataSet))},
            {"System.Data.DataSetExtensions",   Reference(typeof(DataTableExtensions))},
            {"System.Xml",                      Reference(typeof(XmlElement))},
            {"System.Xml.Linq",                 Reference(typeof(XElement))},
            {"Microsoft.CSharp",                Reference(typeof(RuntimeBinderException))},
        };

        static readonly string[] DefaultNamespaces =
        {
            "Nake", "System", "System.Collections.Generic", "System.Linq", "System.Text", "System.IO"
        };

        static MetadataFileReference Reference(Type type)
        {
            return new MetadataFileReference(type.Assembly.Location);
        }

        readonly Submission submission;

        public Engine()
        {
            submission = new Submission(DefaultNamespaces, NakeReferences.Concat(DefaultReferences.Select(x => x.Value)));
        }

        public SubmissionCompilation Compile(FileInfo file)
        {
            return Compile(Preprocess(file));
        }

        public SubmissionCompilation Compile(string code)
        {
            return submission.Compile(code);
        }

        string Preprocess(FileInfo file)
        {
            var result = new Preprocessor()
                .Process(file);

            var uniqueReferences = result.References.Where(
                x => !DefaultReferences.ContainsKey(x.AssemblyName));

            foreach (var reference in uniqueReferences)
                submission.AddScriptDefinedAssemblyNameReference(reference);

            foreach (var reference in result.AbsoluteReferences)
                submission.AddScriptDefinedAbsoluteReference(reference);

            foreach (var @namespace in result.Namespaces)
                submission.ImportScriptDefinedNamespace(@namespace);

            return result.Code();
        }
    }
}