#r "System.Xml"
#r "System.Xml.Linq"

using Nake;
using Nake.FS;
using Nake.Cmd;
using Nake.Log;

using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Diagnostics;

const string RootPath = "$NakeScriptDirectory$";
const string OutputPath = RootPath + @"\Output";

/// Builds sources in debug mode 
[Task] void Default()
{
    Build();
}

/// Wipeout all build output and temporary build files 
[Task] void Clean(string path = OutputPath)
{
    Delete(@"{path}\*.*|-:*.vshost.exe");
    RemoveDir(@"**\bin|**\obj|{path}\*|-:*.vshost.exe");    
}

/// Builds sources using specified configuration and output path
[Task] void Build(string configuration = "Debug", string outputPath = OutputPath)
{
    Clean(outputPath);

    MSBuild
        .Projects("Nake.sln")
            .Property("Platform", "Any CPU")
            .Property("Configuration", configuration)
            .Property("OutDir", outputPath)
            .Property("ReferencePath", outputPath)
    .Build();
}

/// Runs unit tests 
[Task] void Test(string outputPath = OutputPath)
{
    Build("Debug", outputPath);

    string tests = new FileSet(@"{outputPath}\*.Tests.dll");
    Exec(@"Packages\NUnit.Runners.2.6.2\tools\nunit-console.exe /framework:net-4.0 /noshadow /nologo {tests}");
}

/// Builds official NuGet package 
[Task] void Package()
{
    var packagePath = OutputPath + @"\Package";
    var releasePath = packagePath + @"\Release";

    Test(packagePath + @"\Debug");
    Build("Release", releasePath);

    var version = FileVersionInfo
        .GetVersionInfo(@"{releasePath}\Nake.exe")
        .ProductVersion;

    File.WriteAllText(
        @"{releasePath}\Nake.bat",
        "@ECHO OFF \r\n" +
        @"Packages\Nake.{version}\tools\net45\Nake.exe %*"
    );

    Exec(@"Tools\Nuget.exe pack Build\NuGet\Nake.nuspec -Version {version} " +
          "-OutputDirectory {packagePath} -BasePath {RootPath} -NoPackageAnalysis");
}

/// Installs dependencies (packages) from NuGet 
[Task] void Install()
{
    var packagesDir = @"{RootPath}\Packages";

    var configs = XElement
        .Load(packagesDir + @"\repositories.config")
        .Descendants("repository")
        .Select(x => x.Attribute("path").Value.Replace("..", RootPath)); 

    foreach (var config in configs)
        Exec(@"Tools\NuGet.exe install {config} -o {packagesDir}");
}