#r "System.Net.WebClient"

#r "./Tools/Nake/Nake.Meta.dll"
#r "./Tools/Nake/Nake.Utility.dll"

using System.IO;
using System.Net;
using System.Linq;
using System.Diagnostics;

using Nake;
using static Nake.FS;
using static Nake.Log;
using static Nake.Env;
using static Nake.Run;

var RootPath = "%NakeScriptDirectory%";
var ArtifactsPath = $"{RootPath}/Artifacts";
var ReleasePackagesPath = $"{ArtifactsPath}/Release";

string AppVeyorJobId = null;
var Version = "3.0.0-dev";

// global init
MakeDir(ArtifactsPath);

/// Installs dependencies and builds sources in Debug mode
[Task] void Default() => Build();

/// Builds sources using specified configuration
[Step] void Build(string config = "Debug", bool verbose = false) => 
    $"dotnet build {RootPath}/Nake.sln /p:Configuration={config} {(verbose ? "/v:d" : "")}"._();

/// Runs unit tests 
[Step] void Test(bool slow = false)
{
    Build("Debug");

    var tests = new FileSet{$"{RootPath}/**/bin/Debug/**/*.Tests.dll"}.ToString(" ");
    var results = $@"{ArtifactsPath}/nunit-test-results.xml";

    try
    {
        $@"dotnet vstest {tests} --logger:trx;LogFileName={results} 
          {(AppVeyorJobId != null||slow ? "" : "--TestCaseFilter:TestCategory!=Slow")}"._();
    }
    finally
    {    	
	if (AppVeyorJobId != null)
        {
            var workerApi = $"https://ci.appveyor.com/api/testresults/mstest/{AppVeyorJobId}";
            Info($"Uploading {results} to {workerApi} using job id {AppVeyorJobId} ...");
            
            var response = new WebClient().UploadFile(workerApi, results);
            var result = System.Text.Encoding.UTF8.GetString(response);
                      
            Info($"Appveyor response is: {result}");
        }
    }
}

/// Builds official NuGet packages 
[Step] void Pack(bool skipFullCheck = false)
{
    Test(!skipFullCheck);
    $"dotnet pack -c Release -p:PackageVersion={Version} Nake.sln"._();
}

/// Publishes package to NuGet gallery
[Step] void Publish() => Push("Nake"); 

void Push(string package) => 
    $@"dotnet nuget push {ReleasePackagesPath}/{package}.{Version}.nupkg 
    -k %NuGetApiKey% -s https://nuget.org/ -ss https://nuget.smbsrc.net"._();
