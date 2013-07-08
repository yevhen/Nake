using Nake;

public string OutDir = "Output";
public string Configuration = "Debug";
public string Platform = "Any CPU";

desc("This is a build task for your solution");
task("build", ()=> 
{
	Projects("Your.sln")
	
		.Property("Configuration", Configuration) 
		.Property("Platform", Platform)
		.Property("OutputPath", OutDir)
	
	.BuildInParallel();
});

desc("This is a greeting task");
task("greeting", () => Console.WriteLine("Hello from Nake!"));

@default("greeting");