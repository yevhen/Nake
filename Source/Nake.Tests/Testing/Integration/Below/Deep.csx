#r "System.Data.dll"
#r "..\ExternalLib.dll"

using ExternalLib;
using System.Data;

desc("This task will invoke code in external dll referenced by relative path");
task("DeepExternal", () =>
{
	var ds = new DataSet();
	Console.WriteLine(ds);

	Log("Deep down sources ...");
	External.Invoke();
});