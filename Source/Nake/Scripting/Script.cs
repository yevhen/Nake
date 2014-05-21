using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;

using Nake.Magic;

namespace Nake.Scripting
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

        static CSharpCompilation Precompile(string code)
        {
            return new Engine().Compile(code);
        }

        static CSharpCompilation Precompile(FileInfo file)
        {
            return new Engine().Compile(file);
        }

        static Script Build(CSharpCompilation compilation, IDictionary<string, string> substitutions, bool debug)
        {
            var magic = new FairyDust(compilation, substitutions, debug);

            return new Script(magic.Apply());
        }
    }
}
