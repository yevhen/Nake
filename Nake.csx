using Nake;

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

@default("build");