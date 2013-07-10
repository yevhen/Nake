using System;

using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Nake
{
    public static class MSBuild
    {
        public static TTask Execute<TTask>(TTask task, bool ignoreLogErrors = true) where TTask : MSBuildTask
        {
            task.BuildEngine = new MSBuildEngineStub();

            if (!task.Execute() || (task.Log.HasLoggedErrors && !ignoreLogErrors))
                throw new ApplicationException(string.Format("{0} failed", task.GetType()));

            return task;
        }

        public static MSBuildProjects Projects(FileSet projects)
        {
            return new MSBuildProjects(projects);
        }
    }
}
