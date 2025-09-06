using System;
using Dotnet.Script.DependencyModel.Logging;
using Nake.Utility;

namespace Nake.Scripting;

public class DotnetScript
{
    public static Logger Logger()
    {
        return (level, message, exception) =>
        {
            switch (level)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    Log.Trace(message);
                    break;
                case LogLevel.Info:
                case LogLevel.Warning:
                    Log.Info(message);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Log.Error(exception);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        };
    }
}