namespace NettspendToSautiSol
{
    public class ArtistNetwork
    {
        private readonly DatabaseManager _databaseManager;
        public Dictionary<ArtistNode, List<ArtistConnection>> AdjacencyMatrix;
        public ArtistNetwork(DatabaseManager databaseManager)
        {
            AdjacencyMatrix = new Dictionary<ArtistNode, List<ArtistConnection>>();
            _databaseManager = databaseManager;
        }
        public void LoadNetwork()
        {
            Console.WriteLine("Loading the artist network...");
            LoadNetworkFromDatabase();
            Console.WriteLine("Finished loading the artist network.");
        }
        private void LoadNetworkFromDatabase()
        {
            Console.WriteLine("I am loading the artists from the database");
            List<ArtistNode> allArtists = _databaseManager.GetAllArtistNodesInDb();
            foreach (ArtistNode artist in allArtists)
            {
                Console.WriteLine(artist.Name);
                AddNode(artist);
            }
            List<ArtistConnection> edges = _databaseManager.GetAllConnectionsInDb();
            foreach (ArtistConnection edge in edges)
            {
                Console.WriteLine($"{edge.Node1.Name} -> {edge.Node2.Name} {edge.Weight}");
                AddConnection(edge.Node1, edge.Node2, (1-edge.Weight));
            }
        }

        private void AddNode(ArtistNode node)
        {
            if (!AdjacencyMatrix.ContainsKey(node))
            {
                AdjacencyMatrix[node] = new List<ArtistConnection>();
            }
        }
        
        private void AddConnection(ArtistNode node1, ArtistNode node2, double weight)
        {
            if (!AdjacencyMatrix.ContainsKey(node1))
                AddNode(node1);

            if (!AdjacencyMatrix.ContainsKey(node2))
                AddNode(node2);

            ArtistConnection edge1 = new ArtistConnection(node1, node2, weight);
            AdjacencyMatrix[node1].Add(edge1);
            ArtistConnection edge2 = new ArtistConnection(node2, node1, weight);
            AdjacencyMatrix[node2].Add(edge2);
        }
        public List<ArtistConnection> GetListOfConnections(ArtistNode node)
        {
            if (!AdjacencyMatrix.ContainsKey(node))
                return new List<ArtistConnection>();

            return AdjacencyMatrix[node].ToList();
        }
        public void DisplayAllConnections()
        {
            Console.WriteLine("All Artist Connections in the Network:");
            foreach (var kvp in AdjacencyMatrix)
            {
                ArtistNode artist = kvp.Key;
                foreach (ArtistConnection connection in kvp.Value)
                {
                    Console.WriteLine($"{artist.Name} -> {connection.Node2.Name} (Weight: {connection.Weight})");
                }
            }
        }

        
    }
}
