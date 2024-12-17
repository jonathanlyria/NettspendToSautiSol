using System.Text.Json;

namespace NettspendSautiPhase1
{
    public class ArtistExpander : Expander<ArtistNetwork, ArtistNode, ArtistEdge>
    {
        public ArtistExpander(ArtistNetwork artistNetwork, string apikey) : base(artistNetwork)
        {
            ApiKey = apikey;
        }
        
        protected override List<ArtistNode> GetConnections(ArtistNode artistNode)
        {
            List<ArtistNode> similarArtistsList = new List<ArtistNode>();

            using (HttpClient client = new HttpClient())
            {
                string url = "http://ws.audioscrobbler.com/2.0/";
                var parameters = new Dictionary<string, string>
                {
                    { "method", "artist.getSimilar" },
                    { "artist", artistNode.Name },
                    { "api_key", ApiKey },
                    { "format", "json" },
                    { "limit", "5" }
                };

                var response = client.GetAsync(BuildUrlWithParams(url, parameters)).Result;

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = response.Content.ReadAsStringAsync().Result;
                    JsonDocument document = JsonDocument.Parse(jsonResponse);

                    var similarArtists = document.RootElement.GetProperty("similarartists")
                        .GetProperty("artist").EnumerateArray();

                    foreach (var artistData in similarArtists)
                    {
                        string name = artistData.GetProperty("name").GetString();

                        // Skip duplicates and self-references
                        if (ShouldSkipArtist(name, artistNode)) continue;
                        Console.WriteLine($"Adding similar artist: {name}");

                        double match = Convert.ToDouble(artistData.GetProperty("match").GetString());
                        ArtistNode similarArtistNode = new ArtistNode(name);

                        if (!Network.AdjacencyMatrix.ContainsKey(artistNode))
                        {
                            Network.AddNode(artistNode);
                        }
                        if (!Network.AdjacencyMatrix.ContainsKey(similarArtistNode))
                        {
                            Network.AddNode(similarArtistNode);
                        }

                        Network.AddConnection(artistNode, similarArtistNode, match);

                        if (!similarArtistsList.Contains(similarArtistNode))
                        {
                            similarArtistsList.Add(similarArtistNode);
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
         
        private bool ShouldSkipArtist(string name, ArtistNode startingArtistNode) //edit this logic get rid of second selection
        {
            foreach (var adjArtist in Network.AdjacencyMatrix.Keys)
            {
                if (name.IndexOf(adjArtist.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Console.WriteLine($"Skipping similar artist: {name}, because it contains {adjArtist.Name}.");
                    return true;
                }
            }

            if (name.IndexOf(startingArtistNode.Name, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Console.WriteLine($"Skipping similar artist: {name}, because it contains the starting artist's name.");
                return true;
            }

            return false;
        }
    }
}
