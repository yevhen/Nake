#load Secondary.csx

public string Property = "Set In Script";
public bool Switch = false;
public int Number = 1;

desc("This task will trigger other tasks as its prerequisite");
task("Target", pre("Build","DB:Setup", "DeepExternal"), ()=> 
{
	Log("Executing Target task");
	Log(Property);
	Log(Switch.ToString());
	Log(Number.ToString());

	Log(NakeProjectDirectory);
	Log(NakeStartupDirectory);
});

@default("Target");