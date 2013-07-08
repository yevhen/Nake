using Nake;

using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;

public string OutputPath = Path.Combine(NakeProjectDirectory, "Output");
public string Configuration = "Debug";
public string Platform = "Any CPU";

desc("Creates build output directory if needed");
directory(OutputPath);

desc("Wipeout all build output if present");
task("clean", pre(OutputPath), () => 
{
	RemoveDirContents(OutputPath);
	RemoveDir("**/bin;**/obj");
});

desc("Builds Nake sources");
task("build", pre("clean"), ()=> 
{
	Projects("Nake.sln")
			
		.Property("Configuration", Configuration) 
		.Property("Platform", Platform)
		.Property("OutputPath", OutputPath)

	.BuildInParallel();
});

desc("Builds official NuGet package for Nake");
task("package", ()=> 
{	
	Configuration = "Release";
	Tasks[":build"].Invoke();

	string version = FileVersionInfo.GetVersionInfo("Output\\Nake.exe").FileVersion;

	var packageDir = "Output\\Package";
	MakeDir(packageDir);
	
	File.WriteAllText(
		Path.Combine(OutputPath, "Nake.bat"), 
		string.Format(@"@ECHO OFF{0}{0}Packages\Nake.{1}\tools\net45\Nake.exe %*", Environment.NewLine, version)
	);
	
	Exec(string.Format(@"Tools\Nuget.exe pack {0} -Version {1} -OutputDirectory {2} -BasePath {3} -NoPackageAnalysis", 
		 @"Build\NuGet\Nake.nuspec", version, packageDir, NakeProjectDirectory));
});

@default("build");