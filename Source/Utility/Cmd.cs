using System;

using Microsoft.Build.Tasks;

namespace Nake
{
    public static class Cmd
    {
        public static int Exec(string command)
        {
            return MSBuild.Execute(new Exec
            {
                Command = command,
                EchoOff = true,
                WorkingDirectory = Location.CurrentDirectory(),
                LogStandardErrorAsError = true,
                EnvironmentVariables = Env.All(),
            })
            .ExitCode;
        }
    }
}
