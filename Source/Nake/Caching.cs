using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using static System.Environment;

namespace Nake
{
    using Scripting;

    class CachingBuildEngine
    {
        readonly BuildEngine engine;
        readonly Task[] tasks;
        readonly bool reset;

        public CachingBuildEngine(BuildEngine engine, Task[] tasks, bool reset)
        {
            this.engine = engine;
            this.tasks = tasks;
            this.reset = reset;
        }

        public (BuildResult result, CacheKey cache) Build(BuildInput input)
        {
            if (!input.Source.IsFile)
                return (engine.Build(input), null);

            var cache = new CacheKey(input);
            if (reset)
                cache.Reset();

            var dependencies = cache.FindDependencies();
            if (dependencies != null)
            {
                input = input.WithCached(dependencies);

                var compilation = cache.FindCompilation(tasks, dependencies);
                if (compilation != null)
                    return (compilation, cache);
            }

            var output = engine.Build(input);
            cache.Store(output);

            return (output, cache);
        }
    }

    class CacheKey
    {
        public static readonly string RootCacheFolder;

        static CacheKey()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            RootCacheFolder = Path.Combine(Path.GetTempPath(), "Nake." + version);
        }

        public readonly string ScriptFolder;
        public readonly string ProjectFolder;
        public readonly string CompilationFolder;

        readonly SHA1 sha1 = SHA1.Create();
        readonly bool debug;
        readonly ScriptSource source;
        readonly IEnumerable<KeyValuePair<string, string>> substitutions;
        
        public CacheKey(BuildInput input)
        {
            Debug.Assert(input.Source.IsFile);

            source = input.Source;
            debug = input.Debug;
            substitutions = input.Substitutions;

            ScriptFolder = Path.Combine(RootCacheFolder, StringHash(source.File.FullName));
            ProjectFolder = Path.Combine(ScriptFolder, ComputeProjectHash());
            CompilationFolder = Path.Combine(ProjectFolder, ComputeCompilationHash());
        }

        string AssemblyFile => Path.Combine(CompilationFolder, source.File.Name + ".dll");
        string PdbFile => Path.Combine(CompilationFolder, source.File.Name + ".pdb");
        
        string ReferencesFile => Path.Combine(ProjectFolder, "references");
        string CapturedVariablesFile => Path.Combine(CompilationFolder, "variables");
        
        string ComputeCompilationHash() => StringHash(source.Code + ToDeterministicString(substitutions) + debug);
        string ComputeProjectHash() => StringHash(source.ProjectFileContents());

        static string ToDeterministicString(IEnumerable<KeyValuePair<string, string>> substitutions) =>
            string.Join("", substitutions
                .OrderBy(x => x.Key.ToLower())
                .Select(x => x.Key.ToLower() + x.Value.ToLower()));

        string StringHash(string s) => EnsureSafePath(Convert.ToBase64String(sha1.ComputeHash(Encoding.UTF8.GetBytes(s))));

        string EnsureSafePath(string s)
        {
            // Hashed value is used as a directory name.
            // According to http://en.wikipedia.org/wiki/Base64 Base64 could contain '/' symbol.
            // It breaks Path.Combine(temp, hash) sematics. 
            // E.g Path.Combine("c:\temp\nake", "/somebase64string") would produce '/somebase64string'
            // which is not quite expected.
            // Workaround it by replacing dangerous character with '_' symbol.
            // It's used in some alternative Base64 implementations:
            // http://en.wikipedia.org/wiki/Base64#Variants_summary_table
            return s.Replace("/", "_").Replace("\\", "_").Replace("+", "_").Replace("=", "_");
        }

        public AssemblyReference[] FindDependencies() =>
            ProjectFolderExists()
                ? ReadReferences()
                : null;

        public BuildResult FindCompilation(Task[] tasks, AssemblyReference[] references)
        {
            if (!CachedAssemblyExists())
                return null;

            if (CapturedVariablesMismatch())
                return null;

            var assembly = ReadAssembly();
            var symbols = ReadSymbols();

            return new BuildResult(tasks, references, null, assembly, debug ? symbols : null);
        }

        bool ProjectFolderExists() => Directory.Exists(ProjectFolder);
        bool CachedAssemblyExists() => File.Exists(AssemblyFile);

        bool CapturedVariablesMismatch()
        {
            var lines = File.ReadAllLines(CapturedVariablesFile);

            var names = lines[0].Split(new[]{"!#!"}, StringSplitOptions.RemoveEmptyEntries);
            var captured = lines[1];

            if (names.Length == 0)
                return false;

            var current = new StringBuilder();
            foreach (var name in names)
                current.Append(GetEnvironmentVariable(name));

            return StringHash(current.ToString()) != captured;
        }

        AssemblyReference[] ReadReferences() =>
            File.ReadAllLines(ReferencesFile)
                .Select(line => new AssemblyReference(line))
                .ToArray();

        byte[] ReadAssembly() => File.ReadAllBytes(AssemblyFile);
        byte[] ReadSymbols() => debug && File.Exists(PdbFile) ? File.ReadAllBytes(PdbFile) : null;

        public void Store(BuildResult result)
        {
            CreateCacheFolders();

            WriteReferences(result);
            WriteVariables(result);
            WriteAssembly(result);
            WriteSymbols(result);
        }

        void CreateCacheFolders() => Directory.CreateDirectory(CompilationFolder);
        void WriteReferences(BuildResult result) => File.WriteAllLines(ReferencesFile, result.References.Select(x => x.FullPath));

        void WriteVariables(BuildResult result)
        {
            var variables = result.Variables
                .OrderBy(x => x.Name)
                .ToArray();

            var names  = string.Join("!#!", variables.Select(x => x.Name));
            var values = StringHash(string.Join("", variables.Select(x => x.Value)));

            File.WriteAllLines(CapturedVariablesFile, new[]{names, values});
        }

        void WriteAssembly(BuildResult result)
        {
            File.WriteAllBytes(AssemblyFile, result.AssemblyBytes);
        }

        void WriteSymbols(BuildResult result)
        {
            if (result.SymbolBytes != null)
                File.WriteAllBytes(PdbFile, result.SymbolBytes);
        }

        public void Reset()
        {
            if (Directory.Exists(ScriptFolder))
                Directory.Delete(ScriptFolder, recursive: true);
        }
    }
}