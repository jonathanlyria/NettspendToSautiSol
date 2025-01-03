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
                AddConnection(edge.Node1, edge.Node2, edge.Weight);
            }
        }

        protected override void AddNode(ArtistNode node)
        {
            if (!AdjacencyMatrix.ContainsKey(node))
            {
                AdjacencyMatrix[node] = new List<ArtistEdge>();
            }
        }
        
        protected override void AddConnection(ArtistNode node1, ArtistNode node2, double weight)
        {
            var edge1 = new ArtistEdge(node1, node2, weight);
            AdjacencyMatrix[node1].Add(edge1);
            var edge2 = new ArtistEdge(node2, node1, weight);
            AdjacencyMatrix[node2].Add(edge2);
        }
       

        public virtual List<ArtistEdge> GetListOfConnections(ArtistNode node)
        {
            if (!AdjacencyMatrix.ContainsKey(node))
                return new List<ArtistEdge>();

            return AdjacencyMatrix[node].ToList();
        }

        public void PrintMatrix()
        {
            foreach (var node in AdjacencyMatrix.Keys)
            {
                // Print the artist name
                Console.WriteLine(node.Name);

                // Get all connections for the artist
                foreach (var connection in AdjacencyMatrix[node])
                {
                    Console.WriteLine($"  {connection.Node2.Name} ({connection.Weight:F2})");
                }
            }
        }


    }
}
