namespace NettspendToSautiSol
{
    public class ArtistNetwork : IArtistNetwork
    {
        private readonly Dictionary<ArtistNode, Dictionary<ArtistNode, double>> _adjacencyMatrix;
        public ArtistNetwork(IArtistNetworkDatabaseService artistNetworkDatabaseService)
        {
            _adjacencyMatrix = new Dictionary<ArtistNode, Dictionary<ArtistNode, double>>();
            _adjacencyMatrix = artistNetworkDatabaseService.GetNetwork();
            Console.WriteLine("Finished loading the artist network.");        
        }
        
        public void DisplayAllConnections()
        {
            Console.WriteLine("All Artist Connections in the Network:");
            foreach (var kvp in _adjacencyMatrix)
            {
                ArtistNode artist = kvp.Key;
                foreach (var connection in kvp.Value)
                {
                    Console.WriteLine($"{artist.Name} -> {connection.Key.Name} (Weight: {connection.Value})");
                }
            }
        }
        public List<ArtistNode> FindPathWithDijkstras(ArtistNode startArtistNode, ArtistNode endArtistNode)
        {
            PriorityQueue<ArtistNode, double> priorityQueue = new PriorityQueue<ArtistNode, double>();
            Dictionary<ArtistNode, double> distances = new Dictionary<ArtistNode, double>();
            Dictionary<ArtistNode, ArtistNode> previous = new Dictionary<ArtistNode, ArtistNode>();
            List<ArtistNode> Path = new List<ArtistNode>();

        
            foreach (ArtistNode artist in _adjacencyMatrix.Keys)
            {
                distances[artist] = double.MaxValue;
                previous[artist] = null;
            }
            distances[startArtistNode] = 0;

            priorityQueue.Enqueue(startArtistNode, 0);

            while (priorityQueue.Count > 0)
            {
                ArtistNode currentArtist = priorityQueue.Dequeue();
                double currentDistance = distances[currentArtist];

                if (currentArtist.SpotifyId == endArtistNode.SpotifyId)
                    break;

                foreach (KeyValuePair<ArtistNode, double> connection in _adjacencyMatrix[currentArtist])
                {
                    ArtistNode neighbour = connection.Key;
                    double newDist = currentDistance + connection.Value;

                    if (newDist < distances[connection.Key])
                    {
                        distances[neighbour] = newDist;
                        previous[neighbour] = currentArtist;
                        priorityQueue.Enqueue(neighbour, newDist);
                    }
                }
            }

            ArtistNode pathArtistNode = endArtistNode;
            while (pathArtistNode != null)
            {
                Path.Insert(0, pathArtistNode);
                pathArtistNode = previous[pathArtistNode];
            }
            return Path;

        }

        
    }
}
