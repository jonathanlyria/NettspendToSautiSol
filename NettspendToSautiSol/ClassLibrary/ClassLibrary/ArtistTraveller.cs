namespace NettspendToSautiSol;

public class ArtistTraveller
{
    public ArtistNode StartArtistNode { get; set; }
    public ArtistNode EndArtistNode { get; set; }
    public List<ArtistNode> Path { get; set; }
    public double Cost { get; set; }
    private ArtistNetwork ArtistNetwork { get; set; }

    public ArtistTraveller(ArtistNode startArtistNode, ArtistNode endArtistNode, ArtistNetwork artistNetwork)
    {
        StartArtistNode = startArtistNode;
        EndArtistNode = endArtistNode;
        ArtistNetwork = artistNetwork;
        Path = new List<ArtistNode>();
        Cost = double.MaxValue;
        Traverse();
    }

    public void Traverse()
    {
        PriorityQueue<ArtistNode, double> priorityQueue = new PriorityQueue<ArtistNode, double>();
        Dictionary<ArtistNode, double> distances = new Dictionary<ArtistNode, double>();
        Dictionary<ArtistNode, ArtistNode> previous = new Dictionary<ArtistNode, ArtistNode>();

        foreach (ArtistNode artist in ArtistNetwork.AdjacencyMatrix.Keys)
        {
            distances[artist] = double.MaxValue;
            previous[artist] = null;
        }
        distances[StartArtistNode] = 0;

        priorityQueue.Enqueue(StartArtistNode, 0);

        while (priorityQueue.Count > 0)
        {
            ArtistNode currentArtist = priorityQueue.Dequeue();
            double currentDistance = distances[currentArtist];

            if (currentArtist.Equals(EndArtistNode))
                break;

            foreach (ArtistEdge connection in ArtistNetwork.GetListOfConnections(currentArtist))
            {
                ArtistNode neighbor = connection.Node2;
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