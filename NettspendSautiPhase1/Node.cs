namespace NettspendSautiPhase1
{
    public abstract class Node
    {
        public List<Edge> Connections { get; set; }
        public string Identifier { get; set; }

        public Node(string identifier)
        {
            Identifier = identifier;
            Connections = new List<Edge>();
        }

        public override bool Equals(object obj)
        {
            if (obj is Node other)
            {
                return string.Equals(Identifier, other.Identifier, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Identifier.ToLowerInvariant().GetHashCode();
        }
    }
}