namespace NettspendSautiPhase1
{
    public abstract class Edge
    {
        public Node Node1 { get; set; }
        public Node Node2 { get; set; }
        public double Weight { get; set; }
        public bool IsPlaceholder { get; set; }

        public Edge(Node node1, Node node2, double weight, bool isPlaceholder = false)
        {
            Node1 = node1;
            Node2 = node2;
            Weight = weight;
            IsPlaceholder = isPlaceholder;
        }
    }
}