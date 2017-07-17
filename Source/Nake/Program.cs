using Nake.Utility;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Nake
{
	public class Program
	{
		public static void Main(string[] args)
		{
			BreakInDebuggerIfRequested();
			ObserveUnhandledExceptions();
			StartApplication(args);
		}

		static void BreakInDebuggerIfRequested()
		{
			if (Env.Var["NakeDebugger"] == "1")
				Debugger.Break();
		}

		static void ObserveUnhandledExceptions()
		{
			// TODO: Should be removed when dotnet/corefx#6398 is resolved
			AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
			{
				var e = (Exception)args.ExceptionObject;

				Log.Error(e);
				App.Fail(e);
			};

			TaskScheduler.UnobservedTaskException += (sender, args) =>
			{
				var e = args.Exception;
				args.SetObserved();

				foreach (var inner in e.Flatten().InnerExceptions)
					Log.Error(inner);

				App.Fail(e);
			};
		}

		static void StartApplication(string[] args)
		{
			try
			{
				var options = Options.Parse(args);
				new Application(options).Start();
			}
			catch (OptionParseException e)
			{
				Log.Error(e);
				Options.PrintUsage();
				App.Fail(e);
			}
			catch (TaskInvocationException e)
			{
				var baseException = e.GetBaseException();
				Log.Error(baseException);
				App.Fail(e);
			}
		}
	}
}