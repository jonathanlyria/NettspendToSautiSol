namespace NettspendSautiPhase1
{
    public class ArtistNetwork : Network<ArtistNode, ArtistEdge>
    {
        private readonly DatabaseManager _databaseManager;

        public ArtistNetwork(DatabaseManager databaseManager)
        {
            _databaseManager = databaseManager;
            LoadNetworkFromDatabase();
        }
        private void LoadNetworkFromDatabase()
        {
            // Fetch all artists from the database
            var allArtists = _databaseManager.GetAllArtists();
            foreach (var artist in allArtists)
            {
                AddNode(artist);
            }

            // Fetch all connections from the database
            var edges = _databaseManager.GetAllConnections();
            foreach (var edge in edges)
            {
                // Add the connection to the adjacency matrix
                if (edge.Node1 != null && edge.Node2 != null)
                {
                    AddConnection(edge.Node1, edge.Node2, edge.Weight);
                }
            }
        }


        protected override void AddConnection(ArtistNode node1, ArtistNode node2, double weight)
        {
            if (node1 == null || node2 == null) return;

            // Check if the connection already exists
            var existingConnection = AdjacencyMatrix[node1]?.FirstOrDefault(edge => edge.Node2 == node2);
            if (existingConnection != null) return;

            // Create the new edge and update the adjacency matrix
            var edge = new ArtistEdge(node1, node2, weight, false);
            AdjacencyMatrix[node1].Add(edge);
            AdjacencyMatrix[node2].Add(edge);
        }

        public virtual List<ArtistEdge> GetListOfConnections(ArtistNode node)
        {
            if (!AdjacencyMatrix.ContainsKey(node))
                return new List<ArtistEdge>();

            return AdjacencyMatrix[node].Where(c => c.Node1 == node).ToList();
        }

        public void PrintMatrix()
        {
            // Get all artist nodes
            var nodes = AdjacencyMatrix.Keys.ToList();

            // Print header row (artist names)
            Console.Write("     "); // Padding for the top-left corner
            foreach (var node in nodes)
            {
                Console.Write($"{node.Name,-15}");
            }
            Console.WriteLine();

            // Print rows
            foreach (var rowNode in nodes)
            {
                // Print the row artist name
                Console.Write($"{rowNode.Name,-15}");

                // Print connection weights
                foreach (var colNode in nodes)
                {
                    var connection = AdjacencyMatrix[rowNode]
                        .FirstOrDefault(edge => edge.Node2 == colNode || edge.Node1 == colNode);

                    if (connection != null)
                    {
                        Console.Write($"{connection.Weight,-15:F2}"); // Print weight with 2 decimal places
                    }
                    else
                    {
                        Console.Write($"{"-", -15}"); // No connection
                    }
                }
                Console.WriteLine();
            }
        }

    }
}
