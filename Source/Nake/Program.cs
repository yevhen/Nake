using System;

namespace Nake
{
	class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			try
			{
				var application = new Application(Options.Parse(args));
				application.Start();
			}
			catch (OptionParseException e)
			{
				Out.Fail(e.Message);
				Options.PrintUsage();
				Exit.Fail(e);
			}
			catch (Exception e)
			{
				Out.Fail(e);	
				Exit.Fail(e);
			}
		}
	}
}
