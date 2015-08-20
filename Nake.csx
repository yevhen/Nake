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

var MSBuildExe = @"%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe";
var AppVeyor = Var["APPVEYOR"] == "True";

/// Builds sources in debug mode 
[Task] void Default()
{
    Clean();
    Build();
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
    Info(MSBuildExe);
    Exec(MSBuildExe, 
        "Nake.sln /p:Configuration={config};OutDir={outDir};ReferencePath={outDir} /m");
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
    var packagePath = OutputPath + @"\Package";
    var releasePath = packagePath + @"\Release";

    Clean();
    Test(@"{packagePath}\Debug");
    Build("Release", releasePath);

    var version = FileVersionInfo
        .GetVersionInfo(@"{releasePath}\Nake.exe")
        .ProductVersion;

    File.WriteAllText(
        @"{releasePath}\Nake.bat",
        "@ECHO OFF \r\n" +
        @"Packages\Nake.{version}\tools\net45\Nake.exe %*"
    );

    string readme = File.ReadAllText(@"{RootPath}\Build\Readme.txt");
    File.WriteAllText(@"{releasePath}\Readme.txt", readme.Replace("###", "Nake.{version}"));
    
    Cmd(@"Tools\Nuget.exe pack Build\Nake.nuspec -Version {version} " +
         "-OutputDirectory {packagePath} -BasePath {RootPath} -NoPackageAnalysis");
}

/// Restores dependencies (packages) from NuGet 
[Task] void Restore()
{
    Cmd(@"Tools\NuGet.exe restore {RootPath}\Tools\Packages.config -o {RootPath}\Packages");
    Cmd(@"Tools\NuGet.exe restore {RootPath}\Nake.sln -o {RootPath}\Packages");
}
