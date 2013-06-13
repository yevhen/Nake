using System;

namespace Nake
{
	public static class Exit
	{
		internal static Action<int, string, Exception> Terminator = (code, msg, ex) => Environment.Exit(code);

		public static void Ok()
		{
			Terminator(0, "", null);
		}

		public static void Fail()
		{
			Fail("");
		}

		public static void Fail(string message, params object[] args)
		{
			Terminator(-1, string.Format(message, args), null);
		}

		public static void Fail(Exception exception)
		{
			Terminator(-1, exception.Message, exception);
		}
	}
}
