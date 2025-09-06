using System;
using System.Collections.Generic;

namespace Nake;

class CaseInsensitiveEqualityComparer : IEqualityComparer<string>
{
    public bool Equals(string? x, string? y)
    {
        return string.Equals(x, y, StringComparison.CurrentCultureIgnoreCase);
    }

    public int GetHashCode(string? obj)
    {
        return obj?.ToLower().GetHashCode() ?? 0;
    }
}

static class Runner
{
    public static string Label(string? runner = null) => runner ?? "nake";
}