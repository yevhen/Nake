#load "Below\Deep.csx"

desc("This task will compile all sources in DEBUG mode");
task("Build", () =>
{
	Log("Building sources ...");
	Log(Property);
	Log(Env["Path"]);	
});

@namespace("DB", ()=>
{
	desc("This task will setup db from scratch");
	task("Setup", () =>
	{
		Log("Setting up db ...");		
	});
});