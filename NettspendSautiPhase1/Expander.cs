using System.Text.Json;

namespace NettspendSautiPhase1
{
    public abstract class Expander<TNetwork, TNode, TEdge>
        where TNetwork : Network<TNode, TEdge>
        where TNode : Node
        where TEdge : Edge
    {
        protected string ApiKey; // API key to be set or injected
        protected Queue<TNode> Queue { get; set; }
        protected TNetwork Network { get; set; }

        protected Expander(TNetwork network)
        {
            Queue = new Queue<TNode>();
            Network = network;
        }
        
        public void Expand(TNode startingNode, int numIterations)
        {
            HashSet<TNode> visited = new HashSet<TNode>();
            Queue.Enqueue(startingNode);
            visited.Add(startingNode);

            int callCount = 0, iterationCount = 0;

            while (iterationCount < numIterations && Queue.Count > 0)
            {
                int queueLength = Queue.Count;
                iterationCount++;

                for (int i = 0; i < queueLength; i++)
                {
                    TNode currentNode = Queue.Dequeue();
                    var connections = GetConnections(currentNode);

                    callCount++;
                    Console.WriteLine($"{callCount} API calls made");

                    foreach (var connection in connections)
                    {
                        if (!visited.Contains(connection))
                        {
                            Queue.Enqueue(connection);
                            visited.Add(connection);
                        }
                    }
                }
            }
        }
        
        protected abstract List<TNode> GetConnections(TNode node);

        protected static string BuildUrlWithParams(string url, Dictionary<string, string> parameters)
        {
            var paramList = new List<string>();
            foreach (var param in parameters)
            {
                paramList.Add($"{param.Key}={param.Value}");
            }
            return $"{url}?{string.Join("&", paramList)}";
        }
    }
}
