using System.IO;
using System.Net;
using System.Diagnostics;

using static Nake.FS;
using static Nake.Log;
using static Nake.Env;
using static Nake.Run;

var MSBuildExe    = @"%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe";
var NugetExe      = @".nuget/nuget.exe";

var AppVeyor      = Var["APPVEYOR"] == "True";
var Configuration = Var["Configuration"] = "Debug";

[Task] void Default() { Restore(); Clean(); Build(); }

[Task] void Restore()
{
	Cmd($@"{NugetExe} restore packages.config");
	Cmd($@"{NugetExe} restore Nake.sln");
}

[Task] void Clean() { Delete(@"**\bin\*|**\obj\*|-:*.vshost.exe"); }

[Step] void Build() { Exec(MSBuildExe, $"Nake.sln /p:Configuration={Configuration} /m"); }

[Step] void Test()
{
	Build();

	var tests = new FileSet { @"**\bin\*.Tests.dll" }.ToString(" ");
	var results = "nunit-test-results.xml";

	Cmd(@"Packages\NUnit.Runners.2.6.2\tools\nunit-console.exe " +
		$@"/xml:{results} /framework:net-4.0 /noshadow /nologo {tests}");

	if (AppVeyor)
		new WebClient().UploadFile("https://ci.appveyor.com/api/testresults/nunit/%APPVEYOR_JOB_ID%", results);
}

[Step] void Package()
{
	Clean();
	Test();
	Build();

	var version = Version();

	File.WriteAllText(
		"Nake.bat",
		"@ECHO OFF \r\n" +
		@"packages\Nake.{version}\tools\net45\Nake.exe %*");

	string readme = File.ReadAllText(@"README.md");
	File.WriteAllText($@"bin/{Configuration}/README.md", $"### Nake ({version})\r\n" + readme);

	Cmd($@"{NugetExe} pack Nake.nuspec -Version {version} -NoPackageAnalysis");
}

[Step] void Publish() { Cmd($@"{NugetExe} push Nake.{Version()}.nupkg %NuGetApiKey% -Source https://nuget.org/"); }

string Version()
{
	return FileVersionInfo
		.GetVersionInfo($@"Nake/bin/{Configuration}/Nake.exe")
		.ProductVersion;
}
