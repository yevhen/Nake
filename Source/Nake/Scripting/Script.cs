using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Nake.Magic;
using Microsoft.CodeAnalysis;

namespace Nake.Scripting
{
    class Script
    {
        public readonly IEnumerable<Task> Tasks;
        public readonly IEnumerable<MetadataFileReference> References;

        Script(IEnumerable<Task> tasks, IEnumerable<MetadataFileReference> references)
        {
            Tasks = tasks;
            References = references;
        }

        public static Script Build(string code, IDictionary<string, string> substitutions, bool debug)
        {
            return Build(Precompile(code), substitutions, debug);
        }

        public static Script Build(FileInfo file, IDictionary<string, string> substitutions, bool debug)
        {
            return Build(Precompile(file), substitutions, debug);
        }

        static SubmissionCompilation Precompile(string code)
        {
            return new Engine().Compile(code);
        }

        static SubmissionCompilation Precompile(FileInfo file)
        {
            return new Engine().Compile(file);
        }

        static Script Build(SubmissionCompilation submission, IDictionary<string, string> substitutions, bool debug)
        {
            var magic = new FairyDust(submission.Compilation, substitutions, debug);
            return new Script(magic.Apply(), submission.References);
        }
    }
}
