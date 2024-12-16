namespace NettspendSautiPhase1
{
    public class Traveller
    {
        public Artist StartArtist { get; set; }
        public Artist EndArtist { get; set; }
        public List<Artist> Path { get; set; }
        public double Cost { get; set; }

        private NetworkOfArtists Network { get; set; }

        public Traveller(Artist startArtist, Artist endArtist, NetworkOfArtists network)
        {
            StartArtist = startArtist;
            EndArtist = endArtist;
            Network = network;
            Path = new List<Artist>();
            Cost = double.MaxValue;
        }

        public void Traverse()
        {
            var priorityQueue = new PriorityQueue<Artist, double>();
            var distances = new Dictionary<Artist, double>();
            var previous = new Dictionary<Artist, Artist>();

            // Initialize distances to infinity and startArtist's distance to 0
            foreach (var artist in Network.AdjacencyMatrix.Keys)
            {
                distances[artist] = double.MaxValue;
                previous[artist] = null;
            }
            distances[StartArtist] = 0;

            priorityQueue.Enqueue(StartArtist, 0); // Start with the start artist at distance 0

            while (priorityQueue.Count > 0)
            {
                var currentArtist = priorityQueue.Dequeue();
                var currentDistance = distances[currentArtist];

                if (currentArtist == EndArtist)
                {
                    break;
                }

                // Explore neighbors
                foreach (var connection in Network.GetConnections(currentArtist))
                {
                    Artist neighbor = connection.Artist1 == currentArtist ? connection.Artist2 : connection.Artist1;
                    double newDist = currentDistance + connection.Weight;

                    if (newDist < distances[neighbor])
                    {
                        distances[neighbor] = newDist;
                        previous[neighbor] = currentArtist;
                        priorityQueue.Enqueue(neighbor, newDist);
                    }
                }
            }

            // Reconstruct the path
            Artist pathArtist = EndArtist;
            while (pathArtist != null)
            {
                Path.Insert(0, pathArtist);
                pathArtist = previous[pathArtist];
            }

            Cost = distances[EndArtist];
        }
    }
}