namespace NettspendToSautiSol
{
    public abstract class Network<TNode, TConnection>
        where TNode : Node
        where TConnection : Edge<TNode>
    {
        public Dictionary<TNode, List<TConnection>> AdjacencyMatrix { get; set; }

        protected Network()
        {
            AdjacencyMatrix = new Dictionary<TNode, List<TConnection>>();
        }

        protected virtual void AddNode(TNode node)
        {
            if (!AdjacencyMatrix.ContainsKey(node))
            {
                AdjacencyMatrix[node] = new List<TConnection>();
            }
        }

        protected virtual void AddConnection(TNode node1, TNode node2, double weight)
        {
            // Base implementation can remain empty for override by subclasses
        }

        

        public virtual void PrintAdjacencyMatrix()
        {
            // Base method to be overridden
        }
    }
}