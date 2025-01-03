namespace NettspendSautiPhase1;
public abstract class Edge<TNode> where TNode : Node
{
    public TNode Node1 { get; set; }
    public TNode Node2 { get; set; }
    public double Weight { get; set; }

    public Edge(TNode node1, TNode node2, double weight)
    {
        Node1 = node1;
        Node2 = node2;
        Weight = weight;

    }
}