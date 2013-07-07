using Nake;

using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;

public string OutDir = "Output";
public string Configuration = "Debug";
public string Platform = "Any CPU";

desc("This task will wipeout all build output");
task("clean", () => RemoveDirContents(OutDir));

desc("This task will build Nake solution");
task("build", ()=> 
{
	var solution = new MSBuildProject("Nake.sln")
	{
		{"Configuration", Configuration}, 
		{"Platform", Platform}
	};

	solution.Build();
});

desc("This task will build official NuGet package for Nake");
task("package", ()=> 
{	
	Configuration = "Release";
	Invoke("build");

	string version = FileVersionInfo.GetVersionInfo("Output\\Nake.exe").FileVersion;

	var PackageDir = "Output\\Package";
	MakeDir(PackageDir);
	
	File.WriteAllText(
		Path.Combine(OutDir, "Nake.bat"), 
		string.Format(@"@ECHO OFF{0}{0}Packages\Nake.{1}\tools\net45\Nake.exe %*", Environment.NewLine, version)
	);

	Exec(string.Format(@"Tools\Nuget.exe pack {0} -Version {1} -OutputDirectory {2} -BasePath {3} -NoPackageAnalysis", "Nake.nuspec",
		 version, PackageDir, Path.Combine(NakeProjectDirectory, OutDir)));
});

@default("build");