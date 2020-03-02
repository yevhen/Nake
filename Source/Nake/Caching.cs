using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using static System.Environment;

using Nake.Scripting;

namespace Nake
{
    class CachingEngine
    {
        readonly Engine engine;
        readonly FileInfo script;
        readonly Task[] tasks;
        readonly bool reset;

        public CachingEngine(Engine engine, FileInfo script, Task[] tasks, bool reset)
        {
            this.engine = engine;
            this.script = script;
            this.tasks = tasks;
            this.reset = reset;
        }

        public BuildResult Build(string code, IDictionary<string, string> substitutions, bool debug)
        {
            var key = new CacheKey(script, code, substitutions, debug);

            var cached = key.Find(tasks);
            if (cached != null && !reset)
                return cached;

            if (reset)
                key.Reset();

            var output = engine.Build(code, substitutions, debug);
            key.Store(output);

            return output;
        }
    }

    class CacheKey
    {
        static readonly string rootCacheFolder;

        static CacheKey()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            rootCacheFolder = Path.Combine(Path.GetTempPath(), "Nake." + version);
        }

        readonly SHA1 sha1 = SHA1.Create();
        readonly string code;
        readonly bool debug;
        readonly FileInfo script;
        readonly IEnumerable<KeyValuePair<string, string>> substitutions;
        readonly string cacheFolder;

        public CacheKey(FileInfo script, string code, IEnumerable<KeyValuePair<string, string>> substitutions, bool debug)
        {
            this.script = script;
            this.code = code;
            this.debug = debug;
            this.substitutions = substitutions;

            var scriptBaseFolder = Path.Combine(rootCacheFolder, StringHash(script.FullName));
            cacheFolder = Path.Combine(scriptBaseFolder, ComputeScriptHash());
        }

        string CacheFolder
        {
            get { return cacheFolder; }
        }

        string AssemblyFile
        {
            get { return Path.Combine(cacheFolder, script.Name + ".dll"); }
        }

        string PdbFile
        {
            get { return Path.Combine(cacheFolder, script.Name + ".pdb"); }
        }

        string ReferencesFile
        {
            get { return Path.Combine(cacheFolder, "references"); }
        }

        string CapturedVariablesFile
        {
            get { return Path.Combine(cacheFolder, "variables"); }
        }

        string ComputeScriptHash()
        {
            return StringHash(code + ToDeterministicString(substitutions) + debug);
        }

        static string ToDeterministicString(IEnumerable<KeyValuePair<string, string>> substitutions)
        {
            return string.Join("", substitutions
                    .OrderBy(x => x.Key.ToLower())
                    .Select(x => x.Key.ToLower() + x.Value.ToLower()));
        }

        string StringHash(string s)
        {
            var hash = Convert.ToBase64String(sha1.ComputeHash(Encoding.UTF8.GetBytes(s)));
            return EnsureSafePath(hash);
        }

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
            return s.Replace(@"/", "_");
        }

        public BuildResult Find(Task[] tasks)
        {
            if (!CachedAssemblyExists())
                return null;

            if (CapturedVariablesMismatch())
                return null;

            var references = ReadReferences();
            var assembly = ReadAssembly();
            var symbols = ReadSymbols();

            return new BuildResult(tasks, references, null, assembly, debug ? symbols : null);
        }

        bool CachedAssemblyExists()
        {
            return File.Exists(AssemblyFile);
        }

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

        AssemblyReference[] ReadReferences()
        {
            return File.ReadAllLines(ReferencesFile)
                       .Select(line => new AssemblyReference(line))
                       .ToArray();
        }

        byte[] ReadAssembly()
        {
            return File.ReadAllBytes(AssemblyFile);
        }

        byte[] ReadSymbols()
        {
            return debug && File.Exists(PdbFile) ? File.ReadAllBytes(PdbFile) : null;
        }

        public void Store(BuildResult result)
        {
            CreateCacheFolder();

            WriteReferences(result);
            WriteVariables(result);
            WriteAssembly(result);
            WriteSymbols(result);
        }

        void CreateCacheFolder()
        {
            Directory.CreateDirectory(CacheFolder);
        }

        void WriteReferences(BuildResult result)
        {
            File.WriteAllLines(ReferencesFile, result.References.Select(x => x.FullPath));
        }

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
            Directory.Delete(CacheFolder, recursive: true);
        }
    }
}