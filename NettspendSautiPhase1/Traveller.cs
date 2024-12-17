namespace NettspendSautiPhase1;

public class Traveller
{
    public ArtistNode StartArtistNode { get; set; }
    public ArtistNode EndArtistNode { get; set; }
    public List<ArtistNode> Path { get; set; }
    public double Cost { get; set; }
    private ArtistNetwork ArtistNetwork { get; set; }

    public Traveller(ArtistNode startArtistNode, ArtistNode endArtistNode, ArtistNetwork artistNetwork)
    {
        StartArtistNode = startArtistNode;
        EndArtistNode = endArtistNode;
        ArtistNetwork = artistNetwork;
        Path = new List<ArtistNode>();
        Cost = double.MaxValue;
    }

    public void Traverse()
    {
        var priorityQueue = new PriorityQueue<ArtistNode, double>();
        var distances = new Dictionary<ArtistNode, double>();
        var previous = new Dictionary<ArtistNode, ArtistNode>();

        foreach (var artist in ArtistNetwork.AdjacencyMatrix.Keys)
        {
            distances[artist] = double.MaxValue;
            previous[artist] = null;
        }
        distances[StartArtistNode] = 0;

        priorityQueue.Enqueue(StartArtistNode, 0);

        while (priorityQueue.Count > 0)
        {
            var currentArtist = priorityQueue.Dequeue();
            var currentDistance = distances[currentArtist];

            if (currentArtist.Equals(EndArtistNode))
                break;

            foreach (var connection in ArtistNetwork.GetConnections(currentArtist))
            {
                var neighbor = connection.Node2 as ArtistNode;
                double newDist = currentDistance + connection.Weight;

                if (newDist < distances[neighbor])
                {
                    distances[neighbor] = newDist;
                    previous[neighbor] = currentArtist;
                    priorityQueue.Enqueue(neighbor, newDist);
                }
            }
        }

        ArtistNode pathArtistNode = EndArtistNode;
        while (pathArtistNode != null)
        {
            Path.Insert(0, pathArtistNode);
            pathArtistNode = previous[pathArtistNode];
        }

        Cost = distances[EndArtistNode];
    }
}