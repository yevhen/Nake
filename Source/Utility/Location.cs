using System;
using System.IO;

namespace Nake
{
    public static class Location
    {
        public static Func<string> CurrentDirectory = () => NakeStartupDirectory;

        public static string NakeScriptDirectory
        {
            get { return Env.Var["NakeScriptDirectory"]; }
        }

        public static string NakeStartupDirectory
        {
            get { return Env.Var["NakeStartupDirectory"]; }
        }

        public static string GetFullPath(string path)
        {
            return GetFullPath(path, CurrentDirectory());
        }

        public static string GetFullPath(string path, string basePath)
        {
            return Path.IsPathRooted(path)
                    ? path
                    : Path.GetFullPath(Path.Combine(basePath, path));
        }

        public static string GetRootedPath(string path)
        {
            return Path.IsPathRooted(path)
                    ? path
                    : Path.Combine(CurrentDirectory(), path);
        }
    }
}
