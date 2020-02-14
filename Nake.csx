#r "System.Xml"
#r "System.Xml.Linq"

using System.IO;
using System.Net;
using System.Linq;
using System.Xml.Linq;
using System.Diagnostics;

using static Nake.FS;
using static Nake.Log;
using static Nake.Env;
using static Nake.Run;

const string RootPath = "%NakeScriptDirectory%";
const string OutputPath = RootPath + @"\Output";

var PackagePath = @"{OutputPath}\Package";
var DebugOutputPath = @"{PackagePath}\Debug";
var ReleaseOutputPath = @"{PackagePath}\Release";

var MSBuildExe = @"%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe";
var AppVeyor = Var["APPVEYOR"] == "True";

/// Builds sources in debug mode 
[Task] void Default()
{
    Restore();
    Clean();
    Build();
}

/// Restores dependencies (packages) from NuGet 
[Task] void Restore()
{
    Cmd(@"dotnet restore {RootPath}\Nake.sln");
}

/// Wipeout all build output and temporary build files 
[Task] void Clean(string path = OutputPath)
{
    Delete(@"{path}\*.*|-:*.vshost.exe");
    RemoveDir(@"**\bin|**\obj|{path}\*");    
}

/// Builds sources using specified configuration and output path
[Step] void Build(string config = "Debug", string outDir = OutputPath)
{
    Exec(MSBuildExe, "Nake.sln /p:Configuration={config};OutDir={outDir};ReferencePath={outDir} /m");
}

/// Runs unit tests 
[Step] void Test(string outputPath = OutputPath)
{
    Build("Debug", outputPath);

    var tests = new FileSet{@"{outputPath}\*.Tests.dll"}.ToString(" ");
    var results = @"{outputPath}\nunit-test-results.xml";

    Cmd(@"Packages\NUnit.Runners.2.6.2\tools\nunit-console.exe " + 
    	@"/xml:{results} /framework:net-4.0 /noshadow /nologo {tests}");

    if (AppVeyor)
    	new WebClient().UploadFile("https://ci.appveyor.com/api/testresults/nunit/%APPVEYOR_JOB_ID%", results);
}

/// Builds official NuGet package 
[Step] void Package()
{
    Clean();

    Test(DebugOutputPath);
    Build("Release", ReleaseOutputPath);

    var version = FileVersionInfo
        .GetVersionInfo(@"{ReleaseOutputPath}\Nake.exe")
        .ProductVersion;

    File.WriteAllText(
        @"{ReleaseOutputPath}\Nake.bat",
        "@ECHO OFF \r\n" +
        @"Packages\Nake.{version}\tools\net45\Nake.exe %*"
    );

    string readme = File.ReadAllText(@"{RootPath}\Build\Readme.txt");
    File.WriteAllText(@"{ReleaseOutputPath}\Readme.txt", readme.Replace("###", "Nake.{version}"));
    
    Cmd(@"Tools\Nuget.exe pack Build\Nake.nuspec -Version {version} " +
         "-OutputDirectory {PackagePath} -BasePath {RootPath} -NoPackageAnalysis");
}

/// Publishes package to NuGet gallery
[Step] void Publish()
{
    var packageFile = @"{PackagePath}\Nake.{Version()}.nupkg";
    Cmd(@"Tools\Nuget.exe push {packageFile} %NuGetApiKey% -Source https://nuget.org/");
}

string Version()
{
    return FileVersionInfo
            .GetVersionInfo(@"{ReleaseOutputPath}\Nake.exe")
            .ProductVersion;
}
