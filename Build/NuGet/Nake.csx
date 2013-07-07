using Nake;

public string OutDir = "Output";
public string Configuration = "Debug";
public string Platform = "Any CPU";

desc("This is a build task for your solution");
task("build", ()=> 
{
	var solution = new MSBuildProject("Your.sln")
	{
		{"Configuration", Configuration}, 
		{"Platform", Platform}
	};

	solution.Build();
});

desc("This is a greeting task");
task("greeting", () => Console.WriteLine("Hello from Nake!"));

@default("greeting");