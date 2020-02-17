using System;
using System.Diagnostics;
using System.Threading.Tasks;

using AsyncTask = System.Threading.Tasks.Task;

namespace Nake
{
    public class Program
    {
        public static async AsyncTask Main(string[] args)
        {
            BreakInDebuggerIfRequested();
            ObserveUnhandledExceptions();
            await StartApplication(args);
        }

        static void BreakInDebuggerIfRequested()
        {
            if (Env.Var["NakeDebugger"] == "1")
                Debugger.Break();
        }

        static void ObserveUnhandledExceptions()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var e = (Exception) args.ExceptionObject;

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
    
        static async AsyncTask StartApplication(string[] args)
        {
            try
            {
                var options = Options.Parse(args);
                await new Application(options).Start();
            }
            catch (OptionParseException e)
            {
                Log.Error(e.Message);
                Options.PrintUsage();
                App.Fail(e);
            }
            catch (TaskInvocationException e)
            {
                Log.Error(e.GetBaseException());
                App.Fail(e);
            }
        }
    }
}
