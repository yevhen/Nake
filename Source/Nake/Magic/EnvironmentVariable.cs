namespace Nake.Magic
{
    struct EnvironmentVariable
    {
        public readonly string Name;
        public readonly string Value;

        public EnvironmentVariable(string name, string value)
        {
            Name = name;
            Value = value;
        }

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
}