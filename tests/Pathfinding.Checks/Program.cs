using DatabaseServices;
using GlobalTypes;
using Microsoft.Data.Sqlite;
using webserver;

var checks = new List<(string Name, Action Run)>
{
    ("Chooses the cheaper multi-hop route", () => Route(Graph(("a", "c", 0.8), ("a", "b", 0.1), ("b", "c", 0.1)), "a", "c", "a,b,c")),
    ("Chooses a cheaper direct route", () => Route(Graph(("a", "c", 0.1), ("a", "b", 0.3), ("b", "c", 0.3)), "a", "c", "a,c")),
    ("Disconnected artists return no route", () => Route(Graph(("a", "b", 0.2)), "a", "c", "")),
    ("An isolated artist can reach itself", () => Route(Graph(), "d", "d", "d")),
    ("Missing endpoints return no route", () => Route(Graph(), "missing", "a", "")),
    ("Artist identity is based on Spotify ID", () => {
        var network = new ArtistNetwork(new StubGraph(Graph(("a", "b", 0.2))));
        Equal("a,b", string.Join(",", network.FindPathWithDijkstras(new ArtistNode("Alias", "a"), Node("b")).Select(n => n.SpotifyId)));
    }),
    ("Zero-cost cycles terminate", () => {
        var path = new ArtistNetwork(new StubGraph(Graph(("a", "b", 0), ("b", "c", 0), ("a", "c", 0), ("c", "d", 0.2)))).FindPathWithDijkstras(Node("a"), Node("d"));
        Equal(Node("a"), path.First()); Equal(Node("d"), path.Last()); Equal(path.Count, path.Distinct().Count());
    }),
    ("Negative graph costs are rejected", () => Throws<ArgumentOutOfRangeException>(() => new ArtistNetwork(new StubGraph(Graph(("a", "b", -1)))))),
    ("NaN graph costs are rejected", () => Throws<ArgumentOutOfRangeException>(() => new ArtistNetwork(new StubGraph(Graph(("a", "b", double.NaN)))))),
    ("Infinite graph costs are rejected", () => Throws<ArgumentOutOfRangeException>(() => new ArtistNetwork(new StubGraph(Graph(("a", "b", double.PositiveInfinity)))))),
    ("Similarity endpoints map to dissimilarity", () => { Near(0, ArtistSimilarity.ToCost(1)); Near(1, ArtistSimilarity.ToCost(0)); Near(0.1, ArtistSimilarity.ToCost(0.9)); }),
    ("Invalid similarities are rejected", () => {
        foreach (double score in new[] { -0.1, 1.1, double.NaN, double.PositiveInfinity })
            Throws<ArgumentOutOfRangeException>(() => ArtistSimilarity.ToCost(score));
    }),
    ("SQLite loading converts scores and retains isolated artists", CheckDatabase),
    ("Generated graphs agree with Floyd-Warshall", CheckGeneratedGraphs)
};

int failures = 0;
foreach (var (name, run) in checks)
{
    try { run(); Console.WriteLine($"PASS {name}"); }
    catch (Exception error) { failures++; Console.Error.WriteLine($"FAIL {name}: {error.Message}"); }
}
Console.WriteLine($"{checks.Count - failures}/{checks.Count} checks passed.");
return failures == 0 ? 0 : 1;

static ArtistNode Node(string id) => new(id.ToUpperInvariant(), id);
static Dictionary<ArtistNode, Dictionary<ArtistNode, double>> Graph(params (string From, string To, double Cost)[] edges)
{
    var graph = new[] { "a", "b", "c", "d" }.ToDictionary(Node, _ => new Dictionary<ArtistNode, double>());
    foreach (var (from, to, cost) in edges) { graph[Node(from)][Node(to)] = cost; graph[Node(to)][Node(from)] = cost; }
    return graph;
}
static void Route(Dictionary<ArtistNode, Dictionary<ArtistNode, double>> graph, string from, string to, string expected)
{
    var path = new ArtistNetwork(new StubGraph(graph)).FindPathWithDijkstras(Node(from), Node(to));
    Equal(expected, string.Join(",", path.Select(n => n.SpotifyId)));
}
static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, received {actual}.");
}
static void Near(double expected, double actual)
{
    if (!double.IsFinite(actual) || Math.Abs(expected - actual) > 1e-10) throw new Exception($"Expected {expected}, received {actual}.");
}
static void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}.");
}
static void CheckDatabase()
{
    string path = Path.Combine(Path.GetTempPath(), $"artist-network-check-{Guid.NewGuid():N}.db");
    try
    {
        var repository = new DatabaseRepository(path);
        repository.InitialiseDatabase();
        using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO Artist VALUES ('a','A',0), ('b','B',0), ('c','C',0), ('d','D',0);
                INSERT INTO Connections VALUES ('a','c',0.2), ('a','b',0.9), ('b','c',0.9);
                """;
            command.ExecuteNonQuery();
        }
        var graph = repository.GetNetwork();
        Near(0.1, graph[Node("a")][Node("b")]);
        Route(graph, "a", "c", "a,b,c");
        Route(graph, "d", "d", "d");
        Route(graph, "a", "d", "");
        using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Weight FROM Connections WHERE SpotifyId1='a' AND SpotifyId2='b'";
            Near(0.9, Convert.ToDouble(command.ExecuteScalar()));
            command.CommandText = "UPDATE Connections SET Weight=1.2 WHERE SpotifyId1='a' AND SpotifyId2='b'";
            command.ExecuteNonQuery();
        }
        Throws<ArgumentOutOfRangeException>(() => repository.GetNetwork());
    }
    finally { SqliteConnection.ClearAllPools(); File.Delete(path); }
}
static void CheckGeneratedGraphs()
{
    var random = new Random(20260915);
    const int size = 8;
    for (int sample = 0; sample < 20; sample++)
    {
        var nodes = Enumerable.Range(0, size).Select(i => Node(i.ToString())).ToArray();
        var graph = nodes.ToDictionary(n => n, _ => new Dictionary<ArtistNode, double>());
        var distance = new double[size, size];
        for (int i = 0; i < size; i++)
        for (int j = 0; j < size; j++) distance[i,j] = i == j ? 0 : double.PositiveInfinity;
        for (int i = 0; i < size; i++)
        for (int j = i + 1; j < size; j++)
        {
            if (random.NextDouble() > 0.3) continue;
            double cost = random.Next(0, 11) / 10.0;
            graph[nodes[i]][nodes[j]] = graph[nodes[j]][nodes[i]] = cost;
            distance[i,j] = distance[j,i] = cost;
        }
        for (int k = 0; k < size; k++)
        for (int i = 0; i < size; i++)
        for (int j = 0; j < size; j++) distance[i,j] = Math.Min(distance[i,j], distance[i,k] + distance[k,j]);
        var network = new ArtistNetwork(new StubGraph(graph));
        for (int i = 0; i < size; i++)
        for (int j = 0; j < size; j++)
        {
            var route = network.FindPathWithDijkstras(nodes[i], nodes[j]);
            if (double.IsPositiveInfinity(distance[i,j])) { Equal(0, route.Count); continue; }
            Equal(nodes[i], route.First()); Equal(nodes[j], route.Last());
            double cost = 0;
            for (int step = 1; step < route.Count; step++) cost += graph[route[step - 1]][route[step]];
            Near(distance[i,j], cost);
        }
    }
}
sealed class StubGraph(Dictionary<ArtistNode, Dictionary<ArtistNode, double>> graph) : IArtistNetworkDatabaseService
{
    public Dictionary<ArtistNode, Dictionary<ArtistNode, double>> GetNetwork() => graph;
}
