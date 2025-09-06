using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace Nake;

class MSBuildEngineStub : IBuildEngine
{
    readonly ConsoleLogger logger;
    readonly ConcurrentBag<string> stdError = new();

    public MSBuildEngineStub(bool quiet = false)
    {
        logger = new ConsoleLogger(quiet ? LoggerVerbosity.Quiet : LoggerVerbosity.Normal);
    }

    public List<string> StdError => stdError.ToList();

    public void LogErrorEvent(BuildErrorEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Code))
            stdError.Add(e.Message);

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
}