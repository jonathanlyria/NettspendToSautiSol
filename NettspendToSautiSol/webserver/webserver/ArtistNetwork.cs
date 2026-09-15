using GlobalTypes;

namespace webserver;

/// <summary>A graph whose edge values are finite, non-negative traversal costs.</summary>
public class ArtistNetwork : IArtistNetwork
{
    private readonly Dictionary<ArtistNode, Dictionary<ArtistNode, double>> _adjacencyList;

    public ArtistNetwork(IArtistNetworkDatabaseService artistNetworkDatabaseService)
    {
        _adjacencyList = artistNetworkDatabaseService.GetNetwork();
        foreach (var neighbours in _adjacencyList.Values)
        foreach (double cost in neighbours.Values)
        {
            if (!double.IsFinite(cost) || cost < 0)
                throw new ArgumentOutOfRangeException(nameof(artistNetworkDatabaseService),
                    "Dijkstra's algorithm requires finite, non-negative edge costs.");
        }
    }

    public void DisplayAllConnections()
    {
        foreach (var (artist, neighbours) in _adjacencyList)
        foreach (var (neighbour, cost) in neighbours)
            Console.WriteLine($"{artist.Name} -> {neighbour.Name} (Cost: {cost})");
    }

    /// <summary>Returns an empty route when either endpoint is absent or no route exists.</summary>
    public List<ArtistNode> FindPathWithDijkstras(ArtistNode startArtistNode, ArtistNode endArtistNode)
    {
        if (!_adjacencyList.ContainsKey(startArtistNode) || !_adjacencyList.ContainsKey(endArtistNode))
            return [];

        var queue = new PriorityQueue<ArtistNode, double>();
        var distances = new Dictionary<ArtistNode, double> { [startArtistNode] = 0 };
        var previous = new Dictionary<ArtistNode, ArtistNode>();
        queue.Enqueue(startArtistNode, 0);

        while (queue.TryDequeue(out var current, out double queuedDistance))
        {
            // A better route may have superseded an earlier entry for this artist.
            if (queuedDistance > distances[current])
                continue;

            if (current.Equals(endArtistNode))
            {
                var path = new List<ArtistNode> { current };
                while (previous.TryGetValue(current, out var predecessor))
                {
                    path.Add(predecessor);
                    current = predecessor;
                }
                path.Reverse();
                return path;
            }

            if (!_adjacencyList.TryGetValue(current, out var neighbours))
                continue;

            foreach (var (neighbour, cost) in neighbours)
            {
                double candidate = queuedDistance + cost;
                if (!double.IsFinite(candidate))
                    throw new OverflowException("The route cost exceeds the supported numeric range.");

                if (!distances.TryGetValue(neighbour, out double known) || candidate < known)
                {
                    distances[neighbour] = candidate;
                    previous[neighbour] = current;
                    queue.Enqueue(neighbour, candidate);
                }
            }
        }

        return [];
    }
}
