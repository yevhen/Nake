using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Dotnet.Script.DependencyModel.ProjectSystem;

namespace Nake.Scripting
{
    class ScriptSource
    {
        public readonly string Code;
        public readonly FileInfo File;
        public readonly ScriptSource[] Imports = Array.Empty<ScriptSource>();

        public ScriptSource(string code, FileInfo file = null)
        {
            Code = code;
            File = file;

            if (file == null)
                return;

            var imports = new ScriptFilesResolver().GetScriptFiles(file.FullName);
            imports.RemoveWhere(x => x.ToLowerInvariant().Equals(File.FullName.ToLowerInvariant()));

            Imports = imports
                .Select(x => new ScriptSource(System.IO.File.ReadAllText(x), new FileInfo(x)))
                .ToArray();
        }

        public bool IsFile => File != null;

        public IEnumerable<ScriptSource> AllFiles()
        {
            yield return this;
            foreach (var each in Imports)
                yield return each;
        }
    }
}