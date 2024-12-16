namespace NettspendSautiPhase1
{
    public class Artist
    {
        public string Name { get; set; }
        public List<Connection> Connections { get; set; }

        public Artist(string name)
        {
            Name = name;
            Connections = new List<Connection>();
        }

        public override bool Equals(object obj)
        {
            if (obj is Artist other)
            {
                return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Name.ToLowerInvariant().GetHashCode();
        }
    }
}