namespace NettspendSautiPhase1
{
    public class ArtistNode : Node
    {
        public string Name { get; set; }

        public ArtistNode(string name)
            : base(name) // Passes the name as the Identifier to the base Node
        {
            Name = name;
        }

        public override bool Equals(object obj)
        {
            if (obj is ArtistNode other)
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
