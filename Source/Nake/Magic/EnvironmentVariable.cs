namespace Nake.Magic;

struct EnvironmentVariable(string name, string value)
{
    public readonly string Name = name;
    public readonly string Value = value;

    public override bool Equals(object obj)
    {
        var other = (EnvironmentVariable) obj;
        return string.Equals(Name, other.Name);
    }

    public override int GetHashCode()
    {
        unchecked { return (Name.GetHashCode() * 397); }
    }
}