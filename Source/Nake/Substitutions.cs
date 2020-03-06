using System;

namespace Nake
{
    public static class Substitutions
    {
        public static string EnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name) ?? "";
    }
}