using System;
using System.Collections;

using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;

namespace Nake
{
	class MSBuildEngineStub : IBuildEngine
	{
		public static readonly ConsoleLogger Logger = new ConsoleLogger(LoggerVerbosity.Normal);

		public void LogErrorEvent(BuildErrorEventArgs e)
		{
			Logger.ErrorHandler(this, e);
		}

		public void LogWarningEvent(BuildWarningEventArgs e)
		{
			Logger.WarningHandler(this, e);
		}

		public void LogMessageEvent(BuildMessageEventArgs e)
		{
			Logger.MessageHandler(this, e);
		}

		public void LogCustomEvent(CustomBuildEventArgs e)
		{
			Logger.CustomEventHandler(this, e);
		}

		public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
		{
			throw new NotImplementedException();
		}

		public bool ContinueOnError
		{
			get; set;
		}

		public int LineNumberOfTaskNode
		{
			get; set;
		}

		public int ColumnNumberOfTaskNode
		{
			get; set;
		}

		public string ProjectFileOfTaskNode
		{
			get; set;
		}
	}
}
