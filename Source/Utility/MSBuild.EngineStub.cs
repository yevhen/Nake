using System;
using System.Collections;
using System.Linq;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;

namespace Nake
{
    class MSBuildEngineStub : IBuildEngine
    {
        readonly NakeLogger logger;

        public MSBuildEngineStub(bool quiet = false)
        {
            logger = new NakeLogger(quiet ? LoggerVerbosity.Quiet : LoggerVerbosity.Normal);
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            logger.ErrorHandler(this, e);
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            logger.WarningHandler(this, e);
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            logger.MessageHandler(this, e);
        }

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            logger.CustomEventHandler(this, e);
        }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            throw new NotImplementedException("Use MSBuild.Projects instead");
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

        private class NakeLogger : ConsoleLogger
        {
            public NakeLogger(LoggerVerbosity verbosity)
                : base(verbosity, new WriteHandler(Log.Out), SetColor, Console.ResetColor)
            {

            }

            private static void SetColor(ConsoleColor color)
            {
                var background = Console.BackgroundColor;
                var alternativeColor = new[] { ConsoleColor.Gray, ConsoleColor.Black }.First(c => background != c);

                Console.ForegroundColor = color != background
                    ? color
                    : alternativeColor;
            }
        }
    }
}
