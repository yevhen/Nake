using System;
using System.IO;
using System.Net.Http;

using Nake.Utility;
using static Nake.Utility.FS;
using static Nake.Utility.Log;
using static Nake.Utility.Env;
using static Nake.Utility.Run;

var AppVeyor      = Var["APPVEYOR"] == "True";
var Configuration = Var["Configuration"];

if (string.IsNullOrEmpty(Configuration)) { Configuration = "Debug"; }

[Step] public void Build()
{
	Exec($"dotnet build --configuration {Configuration}");
	Exec(@"cd ""Source/Nake"" && dotnet publish --runtime win10-x64");
}

[Task] public void Default() { Restore(); Clean(); Build(); }

[Task] void Restore() { Exec("dotnet restore"); }

[Task] void Clean() { Exec("dotnet clean"); }


[Step] void Test()
{
	Build();

	var tests = new FileSet { @"**\bin\*.Tests.dll" }.ToString(" ");
	var results = "nunit-test-results.xml";

	Exec(@"Packages\NUnit.Runners.2.6.2\tools\nunit-console.exe " + $@"/xml:{results} /framework:net-4.0 /noshadow /nologo {tests}");

	if (AppVeyor)
		new HttpClient().PostAsync("https://ci.appveyor.com/api/testresults/nunit/%APPVEYOR_JOB_ID%", new StreamContent(File.OpenRead(results)));
}

[Step] void Package()
{
	Clean();
	Test();
	Build();

	var version = Var["GitVersion_MajorMinorPatch"];

	File.WriteAllText("Nake.bat", "@ECHO OFF \r\n" +
		$@"packages\Nake.{version}\tools\net45\Nake.exe %*");
	File.WriteAllText($@"bin/{Configuration}/README.md", $"### Nake ({version})\r\n" + File.ReadAllText(@"README.md"));

	Exec($@"gitversion /l console /output buildserver /updateAssemblyInfo");
	Exec($@"nuget pack Nake.nuspec -Version %GitVersion_MajorMinorPatch% -NoPackageAnalysis");
}

[Step] void Publish() { Exec($@"nuget push Nake.%GitVersion_MajorMinorPatch%.nupkg %NuGetApiKey% -Source https://nuget.org/"); }

