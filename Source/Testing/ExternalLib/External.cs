using System;

namespace ExternalLib
{
    public class External
    {
	    public static bool InvokedFromNakeTask;

	    public static void Invoke()
	    {
		    Console.WriteLine("External lib invoked!");
		    InvokedFromNakeTask = true;
	    }
    }
}
