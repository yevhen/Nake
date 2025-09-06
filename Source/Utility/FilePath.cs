using System.IO;

namespace Nake;

readonly struct FilePath
{
    public static FilePath From(string s) => new(s
        .Replace('/', Path.DirectorySeparatorChar)
        .Replace('\\', Path.DirectorySeparatorChar));

    readonly string value;

    FilePath(string value) => 
        this.value = value;

    public int Length => value.Length;

    public string CaseInsensitive() => value.ToLowerInvariant();

    public FilePath Combine(FilePath p) => new(Path.Combine(this, p));

    public static implicit operator string(FilePath p) => p.value;
}