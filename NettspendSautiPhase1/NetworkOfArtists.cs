namespace NettspendSautiPhase1
{
    public class NetworkOfArtists
    {
        public Dictionary<Artist, List<Connection>> AdjacencyMatrix { get; set; }

        public NetworkOfArtists()
        {
            AdjacencyMatrix = new Dictionary<Artist, List<Connection>>();
        }

        public void AddArtist(Artist artist)
        {
            if (!AdjacencyMatrix.ContainsKey(artist))
            {
                AdjacencyMatrix[artist] = new List<Connection>();
            }
        }
        
        public void AddConnection(Artist artist1, Artist artist2, double weight)
        {
            if (!AdjacencyMatrix.ContainsKey(artist1) || !AdjacencyMatrix.ContainsKey(artist2))
                return;


            var reverseConnection = AdjacencyMatrix[artist2]
                .FirstOrDefault(c => c.Artist2 == artist1);
            Console.WriteLine(reverseConnection);

            var forwardConnection = AdjacencyMatrix[artist1]
                .FirstOrDefault(c => c.Artist2 == artist2);
            Console.WriteLine(forwardConnection);

            // Scenario 1: both are emptyu
            if (forwardConnection == null && reverseConnection == null)
            {
                var forward = new Connection(artist1, artist2, weight, false);
                var reverse = new Connection(artist2, artist1, weight, true);

                AdjacencyMatrix[artist1].Add(forward);
                artist1.Connections.Add(forward);

                AdjacencyMatrix[artist2].Add(reverse);
                artist2.Connections.Add(reverse);
                
            }

            // Scenario 2: artist a to artist b is empty, artist b to artist a has connection
            else if (forwardConnection == null && reverseConnection != null)
            {
                var connection = new Connection(artist1, artist2, weight, false);
                AdjacencyMatrix[artist1].Add(connection);
                artist1.Connections.Add(connection);
                
            }

            // Scenario 3: artist a to artist b is full, artist b to artist a is empty
            else if (forwardConnection != null && reverseConnection == null)
            {
                var connection = new Connection(artist2, artist1, weight, true);
                AdjacencyMatrix[artist2].Add(connection);
                artist2.Connections.Add(connection);
            
            }
            
            // Scenario 4: both ways are full but forward is placeholder 
            else if (forwardConnection != null && reverseConnection != null && forwardConnection.IsPlaceholder)
            {
                AdjacencyMatrix[artist1].Remove(forwardConnection);
                var connection = new Connection(artist1, artist2, weight, false);
                AdjacencyMatrix[artist1].Add(connection);
            }
            // Scenario 5: both ways are full but reverse is placeholder
            else if (forwardConnection != null && reverseConnection != null && reverseConnection.IsPlaceholder)
            {
                AdjacencyMatrix[artist2].Remove(reverseConnection);
                var connection = new Connection(artist2, artist1, weight, false);
                AdjacencyMatrix[artist2].Add(connection);
            }
        }

        public List<Connection> GetConnections(Artist artist)
        {
            if (!AdjacencyMatrix.ContainsKey(artist))
                return new List<Connection>();

            return AdjacencyMatrix[artist].Where(c => c.Artist1 == artist).ToList();
        }


        public void PrintAdjacencyMatrix()
        {
            foreach (var artist in AdjacencyMatrix.Keys)
            {
                Console.WriteLine($"Artist: {artist.Name}");
                foreach (var connection in AdjacencyMatrix[artist])
                {
                    var connectedArtist = connection.Artist1 == artist ? connection.Artist2 : connection.Artist1;
                    Console.WriteLine($"- {connectedArtist.Name} (Match: {connection.Weight})");
                }
                Console.WriteLine();
            }
        }
    }
}