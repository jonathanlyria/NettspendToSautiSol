using System.Text.Json;

namespace NettspendToSautiSol
{
    public abstract class Expander<TNode, TEdge>
        where TNode : Node
        where TEdge : Edge<TNode>
    {
        protected string ApiKey; // API key to be set or injected
        protected Queue<TNode> Queue { get; set; }

        protected Expander()
        {
            Queue = new Queue<TNode>();
        }
        
        public void Expand(int numIterations)
        { 
            HashSet<TNode> visited = new HashSet<TNode>();
            
            foreach (var item in Queue)
            {
                visited.Add(item);
            }

            int callCount = 0;
            
            Random random = new Random();

            while (Queue.Count > 0)
            {
                int queueLength = Queue.Count;
           

                for (int i = 0; i < queueLength; i++)
                {
                    int randomTime = random.Next(5000, 60000);
                    if (callCount != 0 && callCount % 1000 == 0)
                    {
                        Console.WriteLine($"Sleeping for {randomTime}ms");
                       // Thread.Sleep(randomTime);
                    }
                    
                    TNode currentNode = Queue.Dequeue();
                    var connections = GetConnections(currentNode);

                    callCount++;
                    Console.WriteLine($"{callCount} API calls made");

                    foreach (var connection in connections)
                    {
                        if (connection.Identifier == "Nettspend")
                        {
                            return;
                        }
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
        
        protected virtual bool RateLimitExceeded()
        {
            return false;
        }   
    }
}
