#r "System.Net.WebClient"

#r "nuget: Nake.Meta, 3.0.0-beta-01"
#r "nuget: Nake.Utility, 3.0.0-beta-01"

using System.IO;
using System.Text;
using System.Net;
using System.Linq;
using System.Diagnostics;

using Nake;
using static Nake.FS;
using static Nake.Log;
using static Nake.Env;

var RootPath = "%NakeScriptDirectory%";
var ArtifactsPath = $"{RootPath}/Artifacts";
var ReleasePackagesPath = $"{ArtifactsPath}/Release";

var AppVeyorJobId = Var["APPVEYOR_JOB_ID"];
var Version = "3.0.0-dev";

// global init
MakeDir(ArtifactsPath);

/// Restores packages and builds sources in Debug mode
[Nake] async Task Default() => await Build();

/// Builds sources using specified configuration
[Nake] async Task Build(string config = "Debug", bool verbose = false) => await 
    $@"dotnet build {RootPath}/Nake.sln \
    /p:Configuration={config} {(verbose ? "/v:d" : "")}";

/// Runs unit tests 
[Nake] async Task Test(bool slow = false)
{
    await Build("Debug");

    var tests = new FileSet{$"{RootPath}/**/bin/Debug/**/*.Tests.dll"}.ToString(" ");
    var results = $@"{ArtifactsPath}/nunit-test-results.xml";    

    try
    {
        await $@"dotnet vstest {tests} --logger:trx;LogFileName={results} \
              {(AppVeyorJobId != null||slow ? "" : "--TestCaseFilter:TestCategory!=Slow")}";
    }
    finally
    {    	
        if (AppVeyorJobId != null)
        {
            var workerApi = $"https://ci.appveyor.com/api/testresults/mstest/{AppVeyorJobId}";
            Info($"Uploading {results} to {workerApi} using job id {AppVeyorJobId} ...");
            
            var response = new WebClient().UploadFile(workerApi, results);
            var result = Encoding.UTF8.GetString(response);
                      
            Info($"Appveyor response is: {result}");
        }
    }
}

/// Builds official NuGet packages 
[Nake] async Task Pack(bool skipFullCheck = false)
{
    await Test(!skipFullCheck);
    
    var versionFile = $"{RootPath}/Source/AssemblyVersion.cs";
    await File.WriteAllTextAsync(versionFile,
        $@"[assembly:System.Reflection.AssemblyInformationalVersion(""{Version}"")]");

    await $"dotnet pack -c Release -p:PackageVersion={Version} Nake.sln";
    await $"git checkout {versionFile}";
}

/// Publishes package to NuGet gallery
[Nake] async Task Publish() => await 
    $@"dotnet nuget push {ReleasePackagesPath}/**/*.{Version}.nupkg \
    -k %NuGetApiKey% -s https://nuget.org/ -ss https://nuget.smbsrc.net --skip-duplicate";

/// Unlists nake packages from Nuget.org
[Nake] async Task Unlist() 
{
    await Delete("Nake");
    await Delete("Nake.Utility");
    await Delete("Nake.Meta");
        
    async Task Delete(string package) => await 
        $@"dotnet nuget delete {package} {Version} \
        -k %NuGetApiKey% -s https://nuget.org/ --non-interactive";
}