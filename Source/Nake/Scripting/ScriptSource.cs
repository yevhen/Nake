using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dotnet.Script.DependencyModel.Compilation;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.ProjectSystem;

namespace Nake.Scripting;

class ScriptSource
{
    const string DefaultTargetFramework = "net8.0";
        
    public readonly Logger Log;
    public readonly string Code;
    public readonly FileInfo? File;
    public readonly ScriptSource[] Imports = [];
    readonly string framework;

    public ScriptSource(string code, FileInfo? file = null, Logger? log = null, string? framework = null)
    {
        Log = log ?? DotnetScript.Logger();
        this.framework = framework ?? DefaultTargetFramework;
            
        Code = code;
        File = file;
            
        if (file == null)
            return;

        var imports = new ScriptFilesResolver().GetScriptFiles(file.FullName);
        imports.RemoveWhere(x => x.ToLowerInvariant().Equals(File?.FullName.ToLowerInvariant()));

        Imports = imports
            .Select(x => new ScriptSource(System.IO.File.ReadAllText(x), new FileInfo(x), Log, this.framework))
            .ToArray();
    }

    public bool IsFile => File != null;

    public IEnumerable<ScriptSource> AllFiles()
    {
        yield return this;
        foreach (var each in Imports)
            yield return each;
    }

    public AssemblyReference[] ComputeDependencies(AssemblyReference[]? cached = null)
    {
        if (cached != null)
        {
            Log.Debug($"Reusing compilation dependencies from previous build for {File?.FullName}");
            return cached;
        }

        if (!IsFile)
            return [];

        Log.Debug($"Computing compilation dependencies for {File?.FullName}");

        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Select(a => a.Location)
            .Where(location => !string.IsNullOrEmpty(location))
            .Distinct()
            .ToDictionary(location => Path.GetFileName(location) ?? "", location => location);

        var dependencyResolver = new CompilationDependencyResolver(t => Log);
        var dependencies = dependencyResolver.GetDependencies(
            File?.DirectoryName ?? Directory.GetCurrentDirectory(), 
            AllFiles().Select(x => x.File?.ToString() ?? ""), 
            true, framework);

        return dependencies
            .SelectMany(d => d.AssemblyPaths)
            .Select(l => new AssemblyReference(loaded.TryGetValue(Path.GetFileName(l), out var e) ? e : l))
            .ToArray();
    }

    public string ProjectFileContents()
    {
        var provider = new ScriptProjectProvider(_ => Log);
        var scriptFiles = AllFiles().Select(x => x.File?.ToString() ?? "");
        var targetDirectory = File?.DirectoryName ?? Directory.GetCurrentDirectory();
        var project = provider.CreateProject(targetDirectory, scriptFiles, framework, true);
        return System.IO.File.ReadAllText(project.Path);
    }
}