using System.Text.Json;



namespace NettspendSautiPhase1
{
    public class Expander
    {
        private static readonly string ApiKey = "00751a650c0182344603b9252c66d416"; 
        public Queue<Artist> ArtistQueue { get; set; }
        private NetworkOfArtists Network { get; set; }

        public Expander(NetworkOfArtists network)
        {
            ArtistQueue = new Queue<Artist>();
            Network = network;
        }

        public void Expand(Artist startingArtist, int numIterations)
        {
            HashSet<Artist> visited = new HashSet<Artist>();

            ArtistQueue.Enqueue(startingArtist);
            visited.Add(startingArtist);

            int iterationCount = 0;
            while (iterationCount < numIterations && ArtistQueue.Count > 0)
            {
                int queueLength = ArtistQueue.Count;
                iterationCount++;
                for (int i = 0; i < queueLength; i++)
                {
                    Artist currentArtist = ArtistQueue.Dequeue();
                    var connections = GetSimilarArtists(currentArtist);

                    foreach (var connection in connections)
                    {
                        if (!visited.Contains(connection))
                        {
                            ArtistQueue.Enqueue(connection);
                            visited.Add(connection);
                        }
                    }
                }
            }
        }
                
        public List<Artist> GetSimilarArtists(Artist artist)
        {
            List<Artist> similarArtistsList = new List<Artist>();
            using (HttpClient client = new HttpClient())
            {
                string url = "http://ws.audioscrobbler.com/2.0/";
                var parameters = new Dictionary<string, string>
                {
                    { "method", "artist.getSimilar" },
                    { "artist", artist.Name },
                    { "api_key", ApiKey },
                    { "format", "json" },
                    { "limit", "5" }
                };

                var response = client.GetAsync(BuildUrlWithParams(url, parameters)).Result;
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = response.Content.ReadAsStringAsync().Result;
                    JsonDocument document = JsonDocument.Parse(jsonResponse);
                    JsonElement.ArrayEnumerator similarArtists = document.RootElement.GetProperty("similarartists")
                        .GetProperty("artist").EnumerateArray();

                    foreach (var artistData in similarArtists)
                    {
                        string name = artistData.GetProperty("name").GetString();
                        

                        // Check if the artist name contains the starting artist's name (case-insensitive)
                        
                        bool found = false;
                        foreach (var adjArtist in Network.AdjacencyMatrix.Keys)
                        {
                            if (name.IndexOf(adjArtist.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                Console.WriteLine($"Skipping similar artist: {name}, because it contains {adjArtist.Name}.");
                                found = true;
                                break;
                            }
                        }

                        if (found) continue;
                        
                        Console.WriteLine($"Checking similar artist: {name}, against starting artist: {artist.Name}");
                        if (name.IndexOf(artist.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Console.WriteLine($"Skipping similar artist: {name}, because it contains the starting artist's name.");
                            continue; // Skip if it contains the starting artist's name
                        }

                        Console.WriteLine($"Adding similar artist: {name}");

                        double match = Convert.ToDouble(artistData.GetProperty("match").GetString());
                        Artist similarArtist = new Artist(name);

                        if (!Network.AdjacencyMatrix.ContainsKey(artist))
                        {
                            Network.AddArtist(artist);
                        }
                        if (!Network.AdjacencyMatrix.ContainsKey(similarArtist))
                        {
                            Network.AddArtist(similarArtist);
                        }

                        Network.AddConnection(artist, similarArtist, match);

                        if (!similarArtistsList.Contains(similarArtist))
                        {
                            similarArtistsList.Add(similarArtist);
                        }

                    }
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode}");
                }
            }

            return similarArtistsList;
        }
        

        private static string BuildUrlWithParams(string url, Dictionary<string, string> parameters)
        {
            var paramList = new List<string>();
            foreach (var param in parameters)
            {
                paramList.Add($"{param.Key}={param.Value}");
            }

            return $"{url}?{string.Join("&", paramList)}";
        }
    }
}