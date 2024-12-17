namespace NettspendSautiPhase1
{
    public abstract class Network<TNode, TConnection>
        where TNode : Node
        where TConnection : Edge
    {
        public Dictionary<TNode, List<TConnection>> AdjacencyMatrix { get; set; }

        public Network()
        {
            AdjacencyMatrix = new Dictionary<TNode, List<TConnection>>();
        }

        public void AddNode(TNode node)
        {
            if (!AdjacencyMatrix.ContainsKey(node))
            {
                AdjacencyMatrix[node] = new List<TConnection>();
            }
        }

        public virtual void AddConnection(TNode node1, TNode node2, double weight)
        {
            // Base implementation can remain empty for override by subclasses
        }

        public List<TConnection> GetConnections(TNode node)
        {
            if (!AdjacencyMatrix.ContainsKey(node))
                return new List<TConnection>();

            return AdjacencyMatrix[node].Where(c => c.Node1 == node).ToList();
        }

        public virtual void PrintAdjacencyMatrix()
        {
            // Base method to be overridden
        }
    }
}