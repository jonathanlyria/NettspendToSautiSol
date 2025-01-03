namespace NettspendSautiPhase1
{
    public class ArtistNode : Node
    {
        public string Name { get; } // Artist name

        public ArtistNode(string name) : base(name)
        {
            Name = name;
        }

        public override bool Equals(object obj)
        {
            return obj is ArtistNode other && Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
        }
        public override int GetHashCode()
        {
            return Name.ToLowerInvariant().GetHashCode();
        }
    }
}