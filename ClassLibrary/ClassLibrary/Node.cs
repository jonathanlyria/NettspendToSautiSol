namespace NettspendToSautiSol
{
    public abstract class Node
    {
        public List<Edge<Node>> Connections { get; set; } // Connections for the node
        public string Identifier { get; set; } // Unique identifier (string type for flexibility)

        protected Node(string identifier)
        {
            Identifier = identifier;
            Connections = new List<Edge<Node>>();
        }

        public override bool Equals(object obj)
        {
            if (obj is Node other)
            {
                return string.Equals(Identifier, other.Identifier, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
}