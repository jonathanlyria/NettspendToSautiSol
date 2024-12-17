namespace NettspendSautiPhase1
{
    public class ArtistNetwork : Network<ArtistNode, ArtistEdge>
    {
        public override void AddConnection(ArtistNode artist1, ArtistNode artist2, double weight)
        {
            if (!AdjacencyMatrix.ContainsKey(artist1) || !AdjacencyMatrix.ContainsKey(artist2))
                return;

            var reverseConnection = AdjacencyMatrix[artist2]
                .FirstOrDefault(c => c.Node2 == artist1);
            var forwardConnection = AdjacencyMatrix[artist1]
                .FirstOrDefault(c => c.Node1 == artist2);

            // Scenario 1: Both are empty
            if (forwardConnection == null && reverseConnection == null)
            {
                var forward = new ArtistEdge(artist1, artist2, weight, false);
                var reverse = new ArtistEdge(artist2, artist1, weight, true);

                AdjacencyMatrix[artist1].Add(forward);
                artist1.Connections.Add(forward);

                AdjacencyMatrix[artist2].Add(reverse);
                artist2.Connections.Add(reverse);
            }
            // Scenario 2: Forward is empty, reverse has connection
            else if (forwardConnection == null && reverseConnection != null)
            {
                var connection = new ArtistEdge(artist1, artist2, weight, false);
                AdjacencyMatrix[artist1].Add(connection);
                artist1.Connections.Add(connection);
            }
            // Scenario 3: Forward has connection, reverse is empty
            else if (forwardConnection != null && reverseConnection == null)
            {
                var connection = new ArtistEdge(artist2, artist1, weight, true);
                AdjacencyMatrix[artist2].Add(connection);
                artist2.Connections.Add(connection);
            }
            // Scenario 4: Forward is placeholder
            else if (forwardConnection != null && reverseConnection != null && forwardConnection.IsPlaceholder)
            {
                AdjacencyMatrix[artist1].Remove(forwardConnection);
                var connection = new ArtistEdge(artist1, artist2, weight, false);
                AdjacencyMatrix[artist1].Add(connection);
            }
            // Scenario 5: Reverse is placeholder
            else if (forwardConnection != null && reverseConnection != null && reverseConnection.IsPlaceholder)
            {
                AdjacencyMatrix[artist2].Remove(reverseConnection);
                var connection = new ArtistEdge(artist2, artist1, weight, false);
                AdjacencyMatrix[artist2].Add(connection);
            }
        }

        /* Uncomment and adjust if needed
        public override void PrintAdjacencyMatrix()
        {
            foreach (var artist in AdjacencyMatrix.Keys)
            {
                Console.WriteLine($": {artist.Name}");
                foreach (var connection in AdjacencyMatrix[artist])
                {
                    var connectedArtist = connection.Node1 == artist ? connection.Node2 : connection.Node1;
                    Console.WriteLine($"- {connectedArtist.Name} (Match: {connection.Weight})");
                }
                Console.WriteLine();
            }
        }
        */

        public List<ArtistEdge> GetConnections(ArtistNode artistNode)
        {
            if (!AdjacencyMatrix.ContainsKey(artistNode))
                return new List<ArtistEdge>();

            return AdjacencyMatrix[artistNode].Cast<ArtistEdge>().ToList();
        }
    }
}
